using System.Collections;
using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;
using ARKOM.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Sequence Controller")]
 public class SequenceController : MonoBehaviour
 {
 public static SequenceController Instance { get; private set; }

 [Header("References")]
 public PlayerController player;
 public SeatInteractable introSeat;
 public SeatInteractable reuseSeat;
 public FlashlightPickupInteractable flashlightPickup;
 public Interactable breakerInteractable;
 public TVController tv; // optional
 public PowerManager powerManager; // optional

 [Header("Config Timings")]
 public float introNewsDuration =4f;
 public float blackoutDelay =0.5f;
 public float timeSkipFadeOut =1.2f;
 public float timeSkipBlackHold =1.5f;
 public float timeSkipFadeIn =1.2f;

 [Header("IDs / Seat")]
 public string introSeatId = "IntroSeat";

 [Header("UI / Hints")] public HintPresenter hint;

 [Header("Post TimeSkip References")]
 public GhostSpawner ghostSpawner; // optional
 public PraySequenceController prayController; // optional
 public HouseSweepManager sweepManager; // optional
 public FinalBedTrigger finalBedTrigger; // optional

 [Header("Auto Anomaly After Sweep")] public bool autoAnomalyAfterSweep = true; public float anomalyAutoDelay =6f; public bool spawnGhostDirectOnAuto = true;

 [Header("Hint System")] public bool useProgressiveHints = true;
 [Header("Flow Options")] public bool autoTriggerKitchenEntered = true;

 [Header("Global Game Loop Audio")] public AudioClip globalLoopClip; [Range(0f,1f)] public float globalLoopVolume =1f; public bool startGlobalLoopOnAwake = true; public bool startGlobalLoopOnIntro = true; public bool stopAllAudioAtSleepEnd = true;
 private AudioSource globalLoopSource; private bool globalLoopStarted;

 // Internal flags
 private bool platesCleaned; private bool requireCleanPlatesBeforeFuse; private bool storageFuseTriggered; private bool started;

 [Header("TV Scare (Storage Fuse)")]
 [Tooltip("เวลาถือ Static บนทีวีก่อนปิด (วินาที)")] public float tvStaticHoldTime =4f;

 // Public accessors for other scripts
 public bool RequireCleanPlatesBeforeFuse => requireCleanPlatesBeforeFuse;
 public bool PlatesCleaned => platesCleaned;
 [Tooltip("ข้อความเตือนเมื่อยังต้องเก็บเศษจานก่อนใส่ฟิวส์")] public string needCleanPlatesHint = "เก็บเศษจานในครัวให้หมดก่อน";

 [Header("Find Ooy Flow")]
 public string findOoyHint = "ไปหาออย";
 public AudioClip ooyNotFoundVoice; [Range(0f,1f)] public float ooyNotFoundVoiceVolume =1f;
 public string ooyNotFoundText = "นี่มันเกิดอะไรขึ้นวะ ลองไปปลุกออยหน่อยดีกว่า";
 public float blackoutAgainDelay =0.25f;

 [Header("Upstairs Unlock Flow")]
 public string pliersToolId = "Pliers"; public string needPliersHint = "ตามหาคีมเพื่อปลดโซ่";
 public AudioClip upstairsFootstepVoice; [Range(0,1)] public float upstairsFootstepVoiceVolume =1f;
 public string objectiveFindNoiseSource = "ตามหาต้นตอของเสียง";
 public Transform upstairsDoor; public string needUpstairsKeyHint = "ตามหากุญแจเพื่อไขขึ้นไปข้างบน";
 public Transform prayerRoom; public string goTurnOffRadioHint = "เสียงดังมาจากวิทยุในห้องพระ"; public string readDiaryHint = "มีไดอารี่ของออย ลองอ่านดู";

 [Header("Upstairs Objects (Optional)")]
 [Tooltip("RadioInteractable ที่อยู่ในห้องพระ เพื่อสั่งเล่นอัตโนมัติหลังขึ้นชั้นสองได้")] public RadioInteractable upstairsRadio;
 [Tooltip("Collider ของไดอารี่ ถ้าต้องการบังคับให้อ่านก่อนอนุญาตให้ไปหาออย")] public Collider diaryInteractCollider;
 [Tooltip("ต้องอ่านไดอารี่ก่อนหรือไม่ (ถ้ามี collider)")] public bool requireDiaryBeforeOoy = false;
 private bool diaryRead;

 [Header("Sleep End Options")] public bool blackScreenOnSleepEnd = true; public float sleepEndFadeTime =1f; public string sleepEndHintText = "";
 [Header("Sleep End Display")] public string sleepEndDisplayText = "TO BE CONTINUED"; public float sleepEndTextDelay =0.6f; public bool useHintPresenterForSleepEndText = true; public float sleepEndTextDuration =9999f; public AudioClip sleepEndBackgroundClip; [Range(0f,1f)] public float sleepEndBackgroundVolume =1f; public bool sleepEndBackgroundLoop = true; private AudioSource sleepEndBgSource;

 [Header("Debug / Dev")] public bool debugSkipToSleepEndOnStart = false; public KeyCode debugSkipKey = KeyCode.F9;
#if ENABLE_INPUT_SYSTEM
 public Key debugSkipKeyInputSystem = Key.None;
#endif

 private AudioSource persistentLoopSource; private bool cleanPlatesLoopStarted;
 [Header("Audio Loops")] public AudioClip cleanPlatesLoopClip; [Range(0f,1f)] public float cleanPlatesLoopVolume =0.7f; public bool cleanPlatesLoopPlayOnce = true;

 public enum StoryState
 {
 IntroSeated, FindFlashlight, RestorePower, ReturnToSeat, TimeSkipCutscene, Finished,
 PlateCrashStart, InvestigateKitchen, CleanPlates, FridgeSequence, CheckOoy, HouseSweep, AnomalyFound, GhostSpawn, RunToBed, PraySequence, SleepEnd,
 InvestigateUpstairs, FindOoy
 }

 private StoryState state;
 public StoryState CurrentState => state;

 void Awake()
 {
 if (Instance && Instance != this) { Destroy(gameObject); return; }
 Instance = this;
 if (globalLoopClip)
 {
 globalLoopSource = gameObject.AddComponent<AudioSource>();
 globalLoopSource.clip = globalLoopClip; globalLoopSource.loop = true; globalLoopSource.playOnAwake = false; globalLoopSource.volume = globalLoopVolume;
 if (startGlobalLoopOnAwake) { globalLoopSource.Play(); globalLoopStarted = true; }
 }
 var loopGo = new GameObject("PersistentLoopAudio"); loopGo.transform.SetParent(transform);
 persistentLoopSource = loopGo.AddComponent<AudioSource>();
 persistentLoopSource.loop = true; persistentLoopSource.playOnAwake = false; persistentLoopSource.spatialBlend =0f; persistentLoopSource.volume = cleanPlatesLoopVolume;
 }

 void Start()
 {
 if (!player) player = FindObjectOfType<PlayerController>();
 if (!introSeat) introSeat = FindObjectOfType<SeatInteractable>();
 if (!reuseSeat) reuseSeat = introSeat;
 SetupInitial();
 }

 void OnEnable()
 {
 EventBus.Subscribe<FlashlightAcquiredEvent>(OnFlashlight);
 EventBus.Subscribe<PowerRestoredEvent>(OnPowerRestored);
 EventBus.Subscribe<PlayerSeatedEvent>(OnPlayerSeated);
 EventBus.Subscribe<TimeSkipFinishedEvent>(OnTimeSkipFinished);
 EventBus.Subscribe<KitchenEnteredEvent>(OnKitchenEntered);
 EventBus.Subscribe<PlatesCleanedEvent>(OnPlatesCleaned);
 EventBus.Subscribe<FuseFoundEvent>(OnFuseFound);
 EventBus.Subscribe<OoyCheckedEvent>(OnOoyChecked);
 EventBus.Subscribe<UpstairsDoorUnlockedEvent>(OnUpstairsDoorUnlocked);
 EventBus.Subscribe<KeyPickedEvent>(OnKeyPickedGeneric);
 EventBus.Subscribe<RadioToggledEvent>(OnRadioToggled);
 }
 void OnDisable()
 {
 EventBus.Unsubscribe<FlashlightAcquiredEvent>(OnFlashlight);
 EventBus.Unsubscribe<PowerRestoredEvent>(OnPowerRestored);
 EventBus.Unsubscribe<PlayerSeatedEvent>(OnPlayerSeated);
 EventBus.Unsubscribe<TimeSkipFinishedEvent>(OnTimeSkipFinished);
 EventBus.Unsubscribe<KitchenEnteredEvent>(OnKitchenEntered);
 EventBus.Unsubscribe<PlatesCleanedEvent>(OnPlatesCleaned);
 EventBus.Unsubscribe<FuseFoundEvent>(OnFuseFound);
 EventBus.Unsubscribe<OoyCheckedEvent>(OnOoyChecked);
 EventBus.Unsubscribe<UpstairsDoorUnlockedEvent>(OnUpstairsDoorUnlocked);
 EventBus.Unsubscribe<KeyPickedEvent>(OnKeyPickedGeneric);
 EventBus.Unsubscribe<RadioToggledEvent>(OnRadioToggled);
 }

 private void SetupInitial()
 {
 if (started) return; started = true;
 state = StoryState.IntroSeated;
 EventBus.Publish(new StoryStateChangedEvent(StoryState.IntroSeated, StoryState.IntroSeated));
 if (!globalLoopStarted && globalLoopSource && !startGlobalLoopOnAwake && startGlobalLoopOnIntro)
 { globalLoopSource.volume = globalLoopVolume; globalLoopSource.Play(); globalLoopStarted = true; }
 if (introSeat && player && !player.IsSeated) player.EnterSeat(introSeat.seatAnchor, introSeat.cameraPoint);
 if (flashlightPickup) flashlightPickup.gameObject.SetActive(false);
 if (breakerInteractable) breakerInteractable.gameObject.SetActive(false);
 if (tv) tv.PlayIntro();
 ShowHint("ชมข่าว...", introNewsDuration);
 StartCoroutine(IntroRoutine());
 }

 private IEnumerator IntroRoutine()
 {
 yield return new WaitForSeconds(introNewsDuration);
 yield return new WaitForSeconds(blackoutDelay);
 TriggerBlackout();
 }

 private bool debugLogVerbose = true;
 private void DLog(string msg)
 {
 if (debugLogVerbose) Debug.Log("[SequenceController] " + msg, this);
 }

 private void TriggerBlackout()
 {
 DLog("TriggerBlackout");
 if (tv) tv.PowerOff();
 if (powerManager) powerManager.SetPower(false);
 EventBus.Publish(new BlackoutStartedEvent());
 if (player && player.IsSeated) player.ExitSeat();
 if (flashlightPickup) flashlightPickup.gameObject.SetActive(true);
 SetState(StoryState.FindFlashlight);
 if (!useProgressiveHints) ShowHint("ไฟดับ... หาไฟฉายก่อน",4f);
 }

 private void OnFlashlight(FlashlightAcquiredEvent _)
 {
 if (state != StoryState.FindFlashlight) return;
 SetState(StoryState.RestorePower);
 if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
 if (!useProgressiveHints) ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์",4f);
 }

 private void OnPowerRestored(PowerRestoredEvent _)
 {
 DLog("OnPowerRestored received (state=" + state + ")");
 if (state != StoryState.RestorePower)
 {
 DLog("Ignored PowerRestoredEvent because state != RestorePower");
 return;
 }
 if (powerManager) powerManager.SetPower(true);
 if (tv) tv.PreparePostRestoreNews();
 if (upstairsFootstepVoice) AudioSource.PlayClipAtPoint(upstairsFootstepVoice, player ? player.transform.position : transform.position, upstairsFootstepVoiceVolume);
 if (!string.IsNullOrEmpty(objectiveFindNoiseSource)) ShowHint(objectiveFindNoiseSource,4f);
 SetState(StoryState.InvestigateUpstairs);
 }

 private void OnPlayerSeated(PlayerSeatedEvent e)
 {
 if (state != StoryState.ReturnToSeat) return;
 if (e.SeatId != introSeatId) return;
 StartCoroutine(TimeSkipRoutine());
 }

 private IEnumerator TimeSkipRoutine()
 {
 SetState(StoryState.TimeSkipCutscene);
 LockPlayer(true);
 var fader = FindObjectOfType<ScreenFader>();
 if (fader) yield return fader.FadeOut(timeSkipFadeOut);
 if (tv) tv.PlayTimeSkipNews();
 yield return new WaitForSeconds(timeSkipBlackHold);
 if (fader) yield return fader.FadeIn(timeSkipFadeIn);
 LockPlayer(false);
 SetState(StoryState.Finished);
 EventBus.Publish(new TimeSkipFinishedEvent());
 ShowHint("กด F เพื่อลุก",3f);
 }

 private void OnTimeSkipFinished(TimeSkipFinishedEvent _)
 {
 if (state != StoryState.Finished) return;
 StartCoroutine(PlateCrashSequence());
 }

 private IEnumerator PlateCrashSequence()
 {
 SetState(StoryState.PlateCrashStart);
 ShowHint("เกิดเสียงดังจากครัว...",3f);
 yield return new WaitForSeconds(2f);
 SetState(StoryState.InvestigateKitchen);
 ShowHint("ไปดูที่ครัว",4f);
 if (autoTriggerKitchenEntered) EventBus.Publish(new KitchenEnteredEvent());
 }

 private void OnKitchenEntered(KitchenEnteredEvent _)
 {
 DLog("OnKitchenEntered (state=" + state + ")" );
 if (state != StoryState.InvestigateKitchen) return;
 SetState(StoryState.CleanPlates);
 var shards = FindObjectsOfType<PlateShardPickup>();
 PlateShardPickup.ResetCounter(shards.Length);
 foreach (var s in shards) if (s) s.RevealForCleanPlates();
 if (cleanPlatesLoopClip && persistentLoopSource && (!cleanPlatesLoopPlayOnce || !cleanPlatesLoopStarted))
 { persistentLoopSource.clip = cleanPlatesLoopClip; persistentLoopSource.volume = cleanPlatesLoopVolume; persistentLoopSource.Play(); cleanPlatesLoopStarted = true; }
 if (!useProgressiveHints) ShowHint("เก็บเศษจานให้หมด",4f);
 }

 private void OnPlatesCleaned(PlatesCleanedEvent e)
 {
 if (state != StoryState.CleanPlates) return;
 platesCleaned = true;
 if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
 SetState(StoryState.RestorePower);
 ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์",4f);
 }

 private void OnFridgeScareDone(FridgeScareDoneEvent _)
 {
 if (state != StoryState.FridgeSequence) return;
 SetState(StoryState.CheckOoy);
 if (!useProgressiveHints) ShowHint("ไปดูออย",4f);
 }

 private void OnOoyChecked(OoyCheckedEvent _)
 {
 if (state != StoryState.CheckOoy && state != StoryState.FindOoy) return;
 if (ooyNotFoundVoice) AudioSource.PlayClipAtPoint(ooyNotFoundVoice, player ? player.transform.position : transform.position, ooyNotFoundVoiceVolume);
 else if (!string.IsNullOrEmpty(ooyNotFoundText)) ShowHint(ooyNotFoundText,3.5f);
 StartCoroutine(SecondBlackoutRoutine());
 }

 private IEnumerator SecondBlackoutRoutine()
 {
 float wait =0f; if (ooyNotFoundVoice) wait = Mathf.Max(wait, ooyNotFoundVoice.length); if (blackoutAgainDelay >0f) wait += blackoutAgainDelay; if (wait >0f) yield return new WaitForSeconds(wait);
 if (tv) tv.PowerOff(); if (powerManager) powerManager.SetPower(false);
 EventBus.Publish(new BlackoutStartedEvent());
 if (ghostSpawner) ghostSpawner.SpawnRandom(GhostSpawner.GhostKind.Chasing);
 }

 private void OnUpstairsDoorUnlocked(UpstairsDoorUnlockedEvent _)
 {
 DLog("OnUpstairsDoorUnlocked -> auto start radio if assigned");
 if (!string.IsNullOrEmpty(needUpstairsKeyHint)) ShowHint(needUpstairsKeyHint,3.5f);
 // เล่นวิทยุอัตโนมัติเมื่อขึ้นชั้นสอง (ถ้ามี)
 if (upstairsRadio) upstairsRadio.StartRadio();
 }

 private void OnKeyPickedGeneric(KeyPickedEvent e)
 {
 if (e.KeyId == "UpstairsDoorKey")
 {
 if (!string.IsNullOrEmpty(goTurnOffRadioHint)) ShowHint(goTurnOffRadioHint,4f);
 }
 }

 private void OnRadioToggled(RadioToggledEvent e)
 {
 if (!e.On)
 {
 if (!string.IsNullOrEmpty(readDiaryHint)) ShowHint(readDiaryHint,3.5f);
 // ถ้าไม่บังคับอ่านไดอารี่ หรือไม่มี collider ให้ไปหาออยต่อได้เลย
 if (!requireDiaryBeforeOoy || !diaryInteractCollider)
 {
 SetState(StoryState.FindOoy);
 if (!string.IsNullOrEmpty(findOoyHint)) ShowHint(findOoyHint,4f);
 }
 }
 }

 private void OnFuseFound(FuseFoundEvent e)
 {
 if (e.Location == FuseLocation.Upstairs)
 {
 // trigger plate crash flow
 StartCoroutine(PlateCrashSequence());
 return;
 }
 if (e.Location == FuseLocation.StorageRoom)
 {
 if (storageFuseTriggered) return; storageFuseTriggered = true;
 if (tv) { tv.PlayStatic(); }
 if (ghostSpawner) ghostSpawner.SpawnAtIndex(1, GhostSpawner.GhostKind.NonChasing);
 ShowHint("เสียงทีวีซ่า...",2.5f);
 return;
 }
 }

 private void EnterSleepEnd()
 {
 SetState(StoryState.SleepEnd);
 if (!string.IsNullOrEmpty(sleepEndHintText)) ShowHint(sleepEndHintText,1f);
 if (blackScreenOnSleepEnd)
 {
 var fader = FindObjectOfType<ScreenFader>();
 if (fader) StartCoroutine(fader.FadeOut(Mathf.Max(0.01f, sleepEndFadeTime)));
 LockPlayer(true);
 }
 if (stopAllAudioAtSleepEnd)
 {
 if (globalLoopSource) globalLoopSource.Stop(); if (persistentLoopSource) persistentLoopSource.Stop(); if (!sleepEndBackgroundClip) AudioListener.pause = true;
 }
 if (sleepEndBackgroundClip)
 {
 if (!sleepEndBgSource)
 { var bg = new GameObject("SleepEndBackgroundAudio"); bg.transform.SetParent(transform); sleepEndBgSource = bg.AddComponent<AudioSource>(); sleepEndBgSource.loop = sleepEndBackgroundLoop; sleepEndBgSource.playOnAwake = false; sleepEndBgSource.spatialBlend =0f; }
 sleepEndBgSource.clip = sleepEndBackgroundClip; sleepEndBgSource.volume = sleepEndBackgroundVolume; sleepEndBgSource.Play();
 }
 StartCoroutine(SleepEndTextRoutine());
 }

 private IEnumerator SleepEndTextRoutine()
 {
 if (!useHintPresenterForSleepEndText) yield break; if (string.IsNullOrEmpty(sleepEndDisplayText)) yield break; if (sleepEndTextDelay >0f) yield return new WaitForSeconds(sleepEndTextDelay); ShowHint(sleepEndDisplayText, sleepEndTextDuration);
 }

 private void LockPlayer(bool locked)
 {
 if (!player) return; player.enabled = !locked;
 }

 public void ShowTempHint(string text, float duration =2f) => ShowHint(text, duration);
 private void ShowHint(string text, float duration) { if (hint) hint.Show(text, duration); }

 private void SetState(StoryState newState)
 {
 if (state == newState) return; var prev = state; state = newState;
 DLog($"State -> {newState} (from {prev})");
 EventBus.Publish(new StoryStateChangedEvent(prev, newState));
 }

 void Update()
 {
#if ENABLE_LEGACY_INPUT_MANAGER
 if (debugSkipKey != KeyCode.None && UnityEngine.Input.GetKeyDown(debugSkipKey)) { if (state != StoryState.SleepEnd) { EnterSleepEnd(); } }
#endif
#if ENABLE_INPUT_SYSTEM
 if (debugSkipKeyInputSystem != Key.None)
 {
 var kb = Keyboard.current; if (kb != null) { var keyCtrl = kb[debugSkipKeyInputSystem]; if (keyCtrl != null && keyCtrl.wasPressedThisFrame) { if (state != StoryState.SleepEnd) { EnterSleepEnd(); } } }
 }
#endif
 }

 // Call this from Diary interact component when diary is read
 public void NotifyDiaryRead()
 {
 diaryRead = true;
 if (requireDiaryBeforeOoy)
 {
 SetState(StoryState.FindOoy);
 if (!string.IsNullOrEmpty(findOoyHint)) ShowHint(findOoyHint,4f);
 }
 }
 }
}
