using UnityEngine;
using System.Collections;

namespace ARKOM.Scenes.Road
{
 public class GhostChaseController : MonoBehaviour
 {
 [Header("Movement")] public float WalkSpeed =1.6f; public float RunSpeed =4.5f;
 [Tooltip("How close to the player the ghost tries to stay")] public float followDistance =3f;
 [Tooltip("Rotation lerp speed")] public float turnSpeed =5f;
 [Tooltip("Distance threshold to treat as 'caught' and play catch SFX")] public float catchTriggerDistance =1.2f;
 
 [Header("Animator Params (legacy)")]
 [Tooltip("Animator parameter name for speed (float)")] public string animSpeedParam = "Speed";
 [Tooltip("Animator parameter name for running (bool)")] public string animRunParam = "Run";
 
 [Header("Animator Params (2D Blend)")]
 [Tooltip("Animator float parameter for horizontal (strafe) movement")] public string animMoveXParam = "X";
 [Tooltip("Animator float parameter for forward movement")] public string animMoveYParam = "Y";
 
 [Header("Audio")]
 [Tooltip("เล่นเสียงเมื่อผีจับผู้เล่น (เข้าใกล้ระยะ catchTriggerDistance)")] public AudioClip catchClip;
 [Range(0f,1f)] public float catchVolume =1f;
 [Tooltip("สุ่ม pitch (min,max)")] public Vector2 catchPitchRange = new Vector2(0.95f,1.05f);
 [Tooltip("ใช้ AudioManager ถ้ามี (3D)")] public bool useGlobalAudioManager = true;
 [Tooltip("ถ้าไม่ใช้ AudioManager จะสร้าง AudioSource โลคัล")] public bool createLocalSourceIfNeeded = true;
 private AudioSource localSource;
 private bool catchPlayed;
 
 public Animator animator;

 private Transform target;
 private bool following;
 private bool running;

 // cached animator params existence
 private bool hasMoveX, hasMoveY, hasSpeed, hasRun;
 private int hashMoveX, hashMoveY, hashSpeed, hashRun;

 private void Awake()
 {
 CacheAnimatorParams();
 if (createLocalSourceIfNeeded && !useGlobalAudioManager)
 {
 localSource = gameObject.AddComponent<AudioSource>();
 localSource.playOnAwake = false; localSource.loop = false; localSource.spatialBlend =1f; localSource.minDistance =2f; localSource.maxDistance =25f;
 }
 }

 private void CacheAnimatorParams()
 {
 if (!animator) return;
 var pars = animator.parameters;
 for (int i=0;i<pars.Length;i++)
 {
 var p = pars[i];
 if (!string.IsNullOrEmpty(animMoveXParam) && p.type == AnimatorControllerParameterType.Float && p.name == animMoveXParam)
 { hasMoveX = true; hashMoveX = Animator.StringToHash(animMoveXParam); }
 if (!string.IsNullOrEmpty(animMoveYParam) && p.type == AnimatorControllerParameterType.Float && p.name == animMoveYParam)
 { hasMoveY = true; hashMoveY = Animator.StringToHash(animMoveYParam); }
 if (!string.IsNullOrEmpty(animSpeedParam) && p.type == AnimatorControllerParameterType.Float && p.name == animSpeedParam)
 { hasSpeed = true; hashSpeed = Animator.StringToHash(animSpeedParam); }
 if (!string.IsNullOrEmpty(animRunParam) && p.type == AnimatorControllerParameterType.Bool && p.name == animRunParam)
 { hasRun = true; hashRun = Animator.StringToHash(animRunParam); }
 }
 }

 public void StartFollowing(Transform t, bool run)
 {
 target = t; following = true; running = run; catchPlayed = false; UpdateAnim();
 }

 public void SwitchToRun()
 { running = true; UpdateAnim(); }

 public void StopFollowing(bool hideGhost)
 {
 following = false; target = null; catchPlayed = false; UpdateAnim();
 if (hideGhost) gameObject.SetActive(false);
 }

 private void Update()
 {
 if (!following || !target) return;
 Vector3 to = (target.position - transform.position); to.y =0f;
 float dist = to.magnitude;
 if (dist <0.01f) { UpdateAnimMotion(Vector3.zero); return; }
 Vector3 dir = to.normalized;
 float spd = running ? RunSpeed : WalkSpeed;

 // keep distance
 float move = Mathf.Max(0f, dist - followDistance);
 Vector3 delta = dir * Mathf.Min(move, spd * Time.deltaTime);
 transform.position += delta;

 // face target
 if (dir.sqrMagnitude >0.0001f)
 {
 Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
 transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
 }

 // animator motion params
 UpdateAnimMotion(delta);

 // catch SFX trigger: trigger when within either catchTriggerDistance or the followDistance threshold
 float triggerDist = Mathf.Max(catchTriggerDistance, followDistance) + 0.05f; // small margin
 if (!catchPlayed && dist <= triggerDist)
 {
 PlayCatchSfx();
 catchPlayed = true;
 }
 }

 private void EnsureLocalSource()
 {
 if (localSource == null && createLocalSourceIfNeeded)
 {
 localSource = gameObject.AddComponent<AudioSource>();
 localSource.playOnAwake = false; localSource.loop = false; localSource.spatialBlend =1f; localSource.minDistance =2f; localSource.maxDistance =25f;
 }
 }

 private void PlayCatchSfx()
 {
 if (!catchClip) return;
 float pitch = Random.Range(catchPitchRange.x, catchPitchRange.y);
 if (useGlobalAudioManager && ARKOM.Audio.AudioManager.Instance)
 {
 ARKOM.Audio.AudioManager.Instance.Play3D(catchClip, transform.position, catchVolume, pitch, pitch,2f,20f);
 }
 else
 {
 EnsureLocalSource();
 if (localSource)
 {
 localSource.pitch = pitch;
 localSource.PlayOneShot(catchClip, catchVolume);
 }
 else
 {
 AudioSource.PlayClipAtPoint(catchClip, transform.position, catchVolume);
 }
 }
 }

 private void UpdateAnim()
 {
 if (!animator) return;
 if (!hasRun || hashRun ==0) CacheAnimatorParams();
 if (hasRun) animator.SetBool(hashRun, running);
 }

 private void UpdateAnimMotion(Vector3 delta)
 {
 if (!animator) return;
 if ((!hasMoveX && !hasMoveY && !hasSpeed) || (hashMoveX==0 && hashMoveY==0 && hashSpeed==0)) CacheAnimatorParams();
 float dt = Mathf.Max(Time.deltaTime,0.0001f);
 Vector3 v = delta / dt; // m/s
 Vector3 lv = transform.InverseTransformDirection(v);
 if (hasMoveX) animator.SetFloat(hashMoveX, Mathf.Clamp(lv.x / Mathf.Max(0.01f, RunSpeed), -1f,1f));
 if (hasMoveY) animator.SetFloat(hashMoveY, Mathf.Clamp(lv.z / Mathf.Max(0.01f, RunSpeed), -1f,1f));
 if (hasSpeed) { float speedScalar = v.magnitude; animator.SetFloat(hashSpeed, speedScalar); }
 if (hasRun) animator.SetBool(hashRun, running);
 }
 }
}
