using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ARKOM.Player;

namespace ARKOM.Scenes.Road
{
 public class RoadSequenceController : MonoBehaviour
 {
 [Header("References")]
 [Tooltip("Player reference; if null will try to find in scene")] public PlayerController player;
 [Tooltip("Ghost root object (model 'ออย')")] public GameObject ghostRoot;
 [Tooltip("Ghost chase controller on ghost root")] public GhostChaseController ghostChase;
 [Tooltip("Street light flicker controller for Event1")] public StreetLightFlicker streetLightFlicker;

 [Header("SFX / Audio")]
 public AudioSource crySfx; // Event2/3 (loop)
 public AudioSource crashSfx; // Event2 (optional)
 [Tooltip("Footstep/Chase loop on ghost")] public AudioSource ghostFootstepsLoop;
 [Tooltip("SFX when player is caught by the ghost (Event2)")] public AudioSource catchSfx; // can be empty; use clip fallback below
 [Tooltip("If no AudioSource.clip or you prefer a one-shot clip, assign here")] public AudioClip catchSfxClip;
 [Range(0f,1f)] public float catchSfxVolume = 1f;
 [Tooltip("Use AudioManager for 3D one-shot if available when using clip fallback")] public bool catchUseGlobalAudioManager = true;
 [Tooltip("Car horn SFX to play near shrine at the end of Event3")] public AudioSource carHornSfx;

 [Header("VFX / Objects")]
 [Tooltip("Crash FX object or animator root; will SetActive(true) on Event2")] public GameObject carCrashFx;
 [Tooltip("Optional overlay (e.g., black screen) to flash when caught")] public GameObject catchOverlay;

 [Header("UI")] public GameObject sprintHintUI; // Enable on Event3

 [Header("Timings")] 
 [Tooltip("How long the street lights flicker on Event1 (used if not using loop)")] public float flickerDuration =2.5f;
 [Tooltip("Delay after passing Event3 trigger before ghost starts following")] public float ghostFollowDelay =1.5f;
 [Tooltip("Duration to ramp ghost footsteps volume during chase phase")] public float footstepsRampDuration =4f;
 [Tooltip("Auto hide sprint hint after this many seconds (0 to keep until Shift pressed)")] public float sprintHintAutoHide =5f;

 [Header("Ghost Speeds")] public float ghostWalkSpeed =1.6f; 
 public float ghostRunSpeed =4.5f;
 [Tooltip("Footstep pitch while walking -> running")] public float walkFootstepPitch =0.85f;
 public float runFootstepPitch =1.15f;
 public float footstepPitchLerp =0.35f;

 [Header("Loop End Mode")]
 [Tooltip("Place a trigger at the end of the path and warp back to start each lap, triggering events in order")] public bool useEndLoopMode = false;
 [Tooltip("Player will be warped here when reaching the end trigger (only for Event1->2)")] public Transform warpStartPoint;
 [Tooltip("Small delay before warping player after hitting end trigger")] public float warpDelay =0.15f;

 [Header("Auto Start")]
 [Tooltip("Start Event1 automatically on scene start")] public bool autoStartFirstEvent = true;

 [Header("Debug/Testing")]
 [Tooltip("Override the starting event for quick testing. If true, autoStartFirstEvent is ignored.")] public bool debugOverrideStartEvent = false;
 [Tooltip("Which event to start at when overriding (1..3)")][Range(1,3)] public int debugStartEvent =1;
 [Tooltip("Mark previous events as already done when starting at a later event")] public bool debugMarkPreviousAsDone = true;

 [Header("Event2 - Reveal Settings")]
 [Tooltip("Optional spawn point for the ghost when Event2 triggers")] public Transform ghostSpawnPoint;
 [Tooltip("Snap ghost to spawn point on Event2")] public bool snapGhostOnEvent2 = true;
 [Tooltip("Rotate ghost to face the player horizontally on reveal (if no spawn rotation)")] public bool facePlayerOnReveal = true;
 [Tooltip("Fade-in time for the crying SFX (seconds)")] public float cryFadeInSeconds =1.5f;
 [Tooltip("Also pulse street lights briefly on Event2")] public bool pulseLightsOnEvent2 = true;
 [Tooltip("Pulse duration for street lights on Event2")] public float event2LightPulseSeconds =2f;
 [Tooltip("Play crash FX/SFX on Event2")] public bool playCrashOnEvent2 = false;
 
 [Header("Event2 - Catch/Fail")]
 [Tooltip("Enable ghost catch in Event2 if the player walks too close")]
 public bool enableGhostCatch = true;
 [Tooltip("Distance within which the ghost will catch the player (meters)")]
 public float catchRadius =2.25f;
 [Tooltip("Delay before resetting after being caught (for SFX/UI)")]
 public float catchResetDelay =0.8f;
 [Tooltip("Optional explicit camera point placed in front of the ghost for the catch view")] public Transform catchCameraPoint;
 [Tooltip("If no explicit camera point, spawn a temporary point this far in front of the ghost (meters)")] public float catchCamForward =0.9f;
 [Tooltip("If no explicit camera point, offset up by this height (meters)")] public float catchCamHeight =1.6f;
 [Tooltip("Animator to play catch animation on; if null, will try ghostChase.animator")] public Animator ghostAnimator;
 [Tooltip("Animator trigger to fire when caught (leave empty to use CrossFade)")] public string catchTriggerParam = "Catch";
 [Tooltip("Animator state name to crossfade to when caught (used if no trigger specified)")] public string catchStateName;
 [Tooltip("Crossfade duration when using state name")] public float catchCrossFadeDuration =0.1f;

 // New: Car animation during Event2 on long road
 [Header("Event2 - Car Animation")]
 [Tooltip("Trigger a car animation when Event2 starts")] public bool triggerCarOnEvent2 = true;
 [Tooltip("Animator on the car that will play its pass animation")] public Animator carAnimator;
 [Tooltip("Animator trigger parameter on the car")] public string carTriggerParam = "PlayAnim";
 [Tooltip("Delay before triggering the car animation (seconds)")] public float carStartDelay =0f;
 [Tooltip("Optional point used to trigger street light surge near the car position")] public Transform carSurgePoint;
 [Tooltip("Also trigger street lights surge around the car when it plays")] public bool surgeLightsOnCar = true;
 
 [Header("Event2 - Car Visibility")]
 [Tooltip("Root GameObject of the car to hide at start and show on Event2")] public GameObject carRoot;
 [Tooltip("Hide car at start then show when Event2 begins")] public bool hideCarUntilEvent2 = true;
 
 [Header("Event3 - Shrine/Exit")] 
 [Tooltip("Call OnShrineReached from a trigger near the shrine to finish scene")] public bool requiresShrineTrigger = true;
 [Tooltip("Scene name to load after horn plays (leave empty to skip)")] public string nextSceneName;
 [Tooltip("Delay after horn before scene change")] public float afterHornDelay =1.0f;

 private bool event1Done;
 private bool event2Done;
 private bool event3Done;
 private int nextEventIndex =1;
 private bool sequenceCompleted;
 private bool processingEnd;
 
 // runtime state for Event2 catch
 private bool inEvent2;
 private bool caughtPending;
 private Coroutine event2WatchRoutine;
 
 // runtime state for Event3
 private bool inEvent3;
 private bool awaitingShrine;
 private bool hasSwitchedToRun;

 // camera override state
 private Transform originalCamParent;
 private Vector3 originalCamLocalPos;
 private Quaternion originalCamLocalRot;
 private Transform tempCatchAnchor;

 private void Awake()
 {
 if (!player)
 player = FindObjectOfType<PlayerController>();
 if (ghostRoot)
 ghostRoot.SetActive(false); // hidden until Event2
 if (sprintHintUI)
 sprintHintUI.SetActive(false);
 if (catchOverlay) catchOverlay.SetActive(false);
 // Hide car at start if requested
 if (hideCarUntilEvent2 && carRoot)
 carRoot.SetActive(false);
 }

 private void Start()
 {
 if (debugOverrideStartEvent)
 {
 int startIdx = Mathf.Clamp(debugStartEvent,1,3);
 if (debugMarkPreviousAsDone)
 {
 event1Done = startIdx >=2;
 event2Done = startIdx >=3;
 }
 OnNodeTriggered(startIdx);
 nextEventIndex = Mathf.Clamp(startIdx +1,1,3);
 }
 else if (autoStartFirstEvent)
 {
 OnNodeTriggered(1);
 nextEventIndex =2; // so end trigger continues with Event2
 }
 }

 // Called by RoadEndTrigger when player reaches end of path
 public void OnEndOfPathReached(PlayerController who)
 {
 if (!useEndLoopMode || sequenceCompleted) return;
 if (processingEnd) return;
 StartCoroutine(HandleEndReached(who));
 }

 private IEnumerator HandleEndReached(PlayerController who)
 {
 processingEnd = true;
 int idx = nextEventIndex;
 // Fire next event in sequence
 OnNodeTriggered(idx);
 nextEventIndex = Mathf.Min(3, nextEventIndex +1);
 if (idx >=3) sequenceCompleted = true;

 // Only warp for Event1->2 transitions, not after Event3 starts
 if (idx <3)
 {
 if (warpDelay >0f)
 yield return new WaitForSeconds(warpDelay);
 if (warpStartPoint && who)
 TeleportPlayer(who, warpStartPoint);
 }
 processingEnd = false;
 }

 private static void TeleportPlayer(PlayerController who, Transform start)
 {
 if (!who || !start) return;
 var cc = who.GetComponent<CharacterController>();
 if (cc) cc.enabled = false;
 who.transform.SetPositionAndRotation(start.position, Quaternion.Euler(0f, start.eulerAngles.y,0f));
 if (cc) cc.enabled = true;
 }

 public void OnNodeTriggered(int index)
 {
 switch (index)
 {
 case 1:
 if (event1Done) return;
 event1Done = true;
 StartCoroutine(RunEvent1());
 break;
 case 2:
 if (event2Done) return;
 event2Done = true;
 StartCoroutine(RunEvent2());
 break;
 case 3:
 if (event3Done) return;
 event3Done = true;
 StartCoroutine(RunEvent3());
 break;
 }
 }

 private IEnumerator RunEvent1()
 {
 // เล่นกับไฟถนนแทนรถผ่าน: เริ่มกระพริบแบบวนรอบ (มีคูลดาวน์) เพื่อให้ถนนดูมีชีวิต
 if (streetLightFlicker)
 {
 streetLightFlicker.StartFlickerLoop();
 }
 else
 {
 // ถ้าไม่ได้ใส่ StreetLightFlicker ให้ทำ burst ธรรมดา (เผื่อไม่มีระบบใหม่)
 }
 yield break;
 }

 private IEnumerator RunEvent2()
 {
 // Show the car now if it was hidden until Event2
 if (hideCarUntilEvent2 && carRoot && !carRoot.activeSelf)
 carRoot.SetActive(true);

 // Trigger car animation/effects for long road setup
 if (triggerCarOnEvent2)
 StartCoroutine(PlayCarAnimationAndEffects());

 // วาง/หมุนผีตามจุดกำหนด (ถ้ามี)
 if (ghostRoot)
 {
 if (snapGhostOnEvent2 && ghostSpawnPoint)
 {
 ghostRoot.transform.SetPositionAndRotation(ghostSpawnPoint.position, ghostSpawnPoint.rotation);
 }
 else if (facePlayerOnReveal && player)
 {
 Vector3 to = player.transform.position - ghostRoot.transform.position; to.y =0f;
 if (to.sqrMagnitude >0.0001f)
 ghostRoot.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
 }
 }

 // แสดงผี
 if (ghostRoot)
 ghostRoot.SetActive(true);

 // เปิดเสียงร้องไห้แบบเฟดอิน
 if (crySfx)
 {
 float target = Mathf.Clamp01(crySfx.volume);
 crySfx.volume =0f;
 if (!crySfx.isPlaying) crySfx.Play();
 yield return StartCoroutine(RampAudio(crySfx, target, Mathf.Max(0.05f, cryFadeInSeconds)));
 }

 // เน้นบรรยากาศด้วยการ pulse ไฟถนนสั้นๆ
 if (pulseLightsOnEvent2 && streetLightFlicker)
 {
 streetLightFlicker.StartFlicker(event2LightPulseSeconds);
 }

 // เปิดโหมดจับผู้เล่นเมื่อเข้าใกล้ในช่วง Event2 เท่านั้น
 inEvent2 = true;
 StartEvent2CatchWatch();

 // เอฟเฟกต์รถชน (ถ้าต้องการ)
 if (playCrashOnEvent2)
 {
 if (carCrashFx) carCrashFx.SetActive(true);
 if (crashSfx) crashSfx.Play();
 }

 yield break;
 }

 private IEnumerator PlayCarAnimationAndEffects()
 {
 if (carStartDelay >0f)
 yield return new WaitForSeconds(carStartDelay);

 if (carAnimator)
 {
 if (!string.IsNullOrEmpty(carTriggerParam))
 carAnimator.SetTrigger(carTriggerParam);
 }

 if (surgeLightsOnCar)
 {
 var mgr = StreetLightingManager.Instance;
 if (mgr == null) mgr = FindObjectOfType<StreetLightingManager>();
 if (mgr)
 {
 Vector3 pos = carSurgePoint ? carSurgePoint.position : (ghostRoot ? ghostRoot.transform.position : (player ? player.transform.position : transform.position));
 mgr.TriggerCarPass(pos);
 }
 }
 }

 private void StartEvent2CatchWatch()
 {
 if (!enableGhostCatch) return;
 if (!player || !ghostRoot) return;
 StopEvent2CatchWatch();
 event2WatchRoutine = StartCoroutine(Event2CatchWatchLoop());
 }

 private void StopEvent2CatchWatch()
 {
 if (event2WatchRoutine != null)
 {
 StopCoroutine(event2WatchRoutine);
 event2WatchRoutine = null;
 }
 }

 private IEnumerator Event2CatchWatchLoop()
 {
 while (inEvent2 && !caughtPending && player && ghostRoot)
 {
 Vector3 a = player.transform.position;
 Vector3 b = ghostRoot.transform.position;
 a.y = b.y =0f;
 if (Vector3.SqrMagnitude(a - b) <= catchRadius * catchRadius)
 {
 caughtPending = true;
 // เล่นอนิเมชันจับของผี (Trigger หรือ CrossFade)
 PlayCatchAnimation();
 // ย้ายกล้องผู้เล่นไปจุดหน้าผี
 AttachCatchCamera();
 // ปิดการควบคุมผู้เล่นชั่วคราว
 if (player) player.enabled = false;
 // play catch SFX robustly
 PlayCatchSfx();
 if (catchOverlay) catchOverlay.SetActive(true);
 yield return new WaitForSeconds(catchResetDelay);
 if (catchOverlay) catchOverlay.SetActive(false);
 yield return StartCoroutine(ResetSequenceToStart());
 yield break;
 }
 yield return null;
 }
 }

 private void PlayCatchSfx()
 {
 // Prefer assigned AudioSource if it has a clip
 if (catchSfx && catchSfx.clip)
 {
 // position 3D source near catch camera or ghost
 Vector3 pos = catchCameraPoint ? catchCameraPoint.position : (ghostRoot ? ghostRoot.transform.position : transform.position);
 catchSfx.transform.position = pos;
 catchSfx.PlayOneShot(catchSfx.clip, 1f);
 return;
 }
 // Fallback to clip one-shot
 if (catchSfxClip)
 {
 Vector3 pos = catchCameraPoint ? catchCameraPoint.position : (ghostRoot ? ghostRoot.transform.position : transform.position);
 if (catchUseGlobalAudioManager && ARKOM.Audio.AudioManager.Instance)
 {
 ARKOM.Audio.AudioManager.Instance.Play3D(catchSfxClip, pos, catchSfxVolume, 1f,1f, 2f, 18f);
 }
 else
 {
 AudioSource.PlayClipAtPoint(catchSfxClip, pos, catchSfxVolume);
 }
 }
 }

 private void PlayCatchAnimation()
 {
 var anim = ghostAnimator ? ghostAnimator : (ghostChase ? ghostChase.animator : null);
 if (!anim) return;
 if (!string.IsNullOrEmpty(catchStateName))
 anim.CrossFade(catchStateName, catchCrossFadeDuration,0,0f);
 else if (!string.IsNullOrEmpty(catchTriggerParam))
 anim.SetTrigger(catchTriggerParam);
 }

 private void AttachCatchCamera()
 {
 if (!player || !player.cameraRoot) return;
 // บันทึกพาเรนต์/ทรานส์ฟอร์มเดิม
 if (!originalCamParent)
 {
 originalCamParent = player.cameraRoot.parent;
 originalCamLocalPos = player.cameraRoot.localPosition;
 originalCamLocalRot = player.cameraRoot.localRotation;
 }
 // คำนวณ/เตรียม anchor
 Transform anchor = catchCameraPoint;
 if (!anchor)
 {
 if (!tempCatchAnchor)
 {
 GameObject go = new GameObject("CatchCamAnchor");
 tempCatchAnchor = go.transform;
 }
 // วางหน้า ghost โดยหันเข้าหา ghost
 Vector3 pos = ghostRoot ? ghostRoot.transform.position : player.transform.position;
 Vector3 fwd = ghostRoot ? ghostRoot.transform.forward : player.transform.forward;
 pos += fwd * catchCamForward;
 pos.y += catchCamHeight;
 tempCatchAnchor.position = pos;
 tempCatchAnchor.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
 anchor = tempCatchAnchor;
 }
 // ย้ายพาเรนต์กล้องไปใต้ anchor
 player.cameraRoot.SetParent(anchor, worldPositionStays:false);
 player.cameraRoot.localPosition = Vector3.zero;
 player.cameraRoot.localRotation = Quaternion.identity;
 }

 private void RestorePlayerCamera()
 {
 if (!player || !player.cameraRoot || !originalCamParent) return;
 player.cameraRoot.SetParent(originalCamParent, worldPositionStays:false);
 player.cameraRoot.localPosition = originalCamLocalPos;
 player.cameraRoot.localRotation = originalCamLocalRot;
 if (tempCatchAnchor)
 {
 Destroy(tempCatchAnchor.gameObject);
 tempCatchAnchor = null;
 }
 originalCamParent = null;
 }

 public IEnumerator ResetSequenceToStart()
 {
 // หยุด watch และโหมด Event2
 inEvent2 = false;
 StopEvent2CatchWatch();

 // หยุดเสียง/แอนิเมชันต่างๆ
 if (ghostFootstepsLoop && ghostFootstepsLoop.isPlaying) ghostFootstepsLoop.Stop();
 if (crySfx && crySfx.isPlaying) crySfx.Stop();
 if (ghostChase) ghostChase.StopFollowing(hideGhost: true);
 else if (ghostRoot) ghostRoot.SetActive(false);
 if (streetLightFlicker) streetLightFlicker.StopFlickerLoop();

 // Hide car again when resetting sequence
 if (hideCarUntilEvent2 && carRoot)
 carRoot.SetActive(false);

 // คืนกล้องและควบคุมผู้เล่น
 RestorePlayerCamera();
 if (player) player.enabled = true;

 // รีเซ็ตสถานะเหตุการณ์และกลับไปเริ่มใหม่
 event1Done = event2Done = event3Done = false;
 nextEventIndex =1;
 sequenceCompleted = false;
 caughtPending = false;

 // วาปผู้เล่นกลับจุดเริ่ม
 if (warpStartPoint && player)
 TeleportPlayer(player, warpStartPoint);

 // เริ่ม Event1 ใหม่อัตโนมัติ (ให้บรรยากาศกลับมา)
 yield return null; // รอหนึ่งเฟรมให้ transform/cc เสถียร
 OnNodeTriggered(1);
 nextEventIndex =2;
 }

 private IEnumerator RunEvent3()
 {
 inEvent2 = false;
 StopEvent2CatchWatch();
 inEvent3 = true; awaitingShrine = true; hasSwitchedToRun = false;

 // Make sure ghost is visible even when starting directly at Event3 (debug)
 if (ghostRoot && !ghostRoot.activeSelf)
 ghostRoot.SetActive(true);
 // Ensure crying SFX keeps playing if skipped Event2
 if (crySfx && !crySfx.isPlaying)
 {
 float keep = Mathf.Clamp01(crySfx.volume);
 crySfx.volume = keep; // keep current inspector volume
 crySfx.Play();
 }

 // Ghost waits then starts following (walk)
 yield return new WaitForSeconds(ghostFollowDelay);
 if (ghostChase && player)
 {
 ghostChase.WalkSpeed = ghostWalkSpeed;
 ghostChase.RunSpeed = ghostRunSpeed;
 ghostChase.StartFollowing(player.transform, run: false);
 }

 // footsteps loop start quietly and ramp + set walk pitch
 if (ghostFootstepsLoop)
 {
 ghostFootstepsLoop.pitch = walkFootstepPitch;
 if (!ghostFootstepsLoop.isPlaying) ghostFootstepsLoop.Play();
 StartCoroutine(RampAudio(ghostFootstepsLoop, target:1f, duration: footstepsRampDuration));
 }

 // UI hint
 if (sprintHintUI)
 {
 sprintHintUI.SetActive(true);
 if (sprintHintAutoHide >0f)
 StartCoroutine(HideHintAfterSeconds(sprintHintAutoHide));
 }

 // Wait for Shift press before switching to run (or small delay fallback)
 float waited =0f;
 bool shifted = false;
 while (waited <8f && !shifted)
 {
 var kb = Keyboard.current;
 if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
 shifted = true;
 waited += Time.deltaTime;
 yield return null;
 }
 if (sprintHintUI) sprintHintUI.SetActive(false);

 if (ghostChase && !hasSwitchedToRun)
 {
 ghostChase.SwitchToRun();
 hasSwitchedToRun = true;
 if (ghostFootstepsLoop)
 StartCoroutine(LerpAudioPitch(ghostFootstepsLoop, runFootstepPitch, footstepPitchLerp));
 }
 }

 public void OnShrineReached()
 {
 if (!inEvent3 || !awaitingShrine) return;
 awaitingShrine = false;
 StartCoroutine(HandleShrineReached());
 }

 private IEnumerator HandleShrineReached()
 {
 // fade footsteps and stop chase
 if (ghostFootstepsLoop)
 yield return StartCoroutine(RampAudio(ghostFootstepsLoop,0f,0.6f, stopWhenDone: true));
 if (ghostChase)
 ghostChase.StopFollowing(hideGhost: true);
 else if (ghostRoot)
 ghostRoot.SetActive(false);

 // stop crying and play horn
 if (crySfx && crySfx.isPlaying) crySfx.Stop();
 if (carHornSfx) carHornSfx.Play();

 // wait and change scene
 if (!string.IsNullOrEmpty(nextSceneName))
 {
 if (afterHornDelay >0f) yield return new WaitForSeconds(afterHornDelay);
 SceneManager.LoadScene(nextSceneName);
 }
 }

 private IEnumerator HideHintAfterSeconds(float t)
 {
 yield return new WaitForSeconds(t);
 if (sprintHintUI) sprintHintUI.SetActive(false);
 }

 private IEnumerator RampAudio(AudioSource src, float target, float duration, bool stopWhenDone = false)
 {
 if (!src) yield break;
 float start = src.volume;
 float time =0f;
 while (time < duration)
 {
 time += Time.deltaTime;
 float k = duration <=0f ?1f : Mathf.Clamp01(time / duration);
 src.volume = Mathf.Lerp(start, target, k);
 yield return null;
 }
 src.volume = target;
 if (stopWhenDone && target <=0.001f)
 src.Stop();
 }

 private IEnumerator LerpAudioPitch(AudioSource src, float targetPitch, float duration)
 {
 if (!src) yield break;
 float start = src.pitch; float time =0f;
 while (time < duration)
 {
 time += Time.deltaTime;
 float k = duration <=0f ?1f : Mathf.Clamp01(time / duration);
 src.pitch = Mathf.Lerp(start, targetPitch, k);
 yield return null;
 }
 src.pitch = targetPitch;
 }
 }
}
