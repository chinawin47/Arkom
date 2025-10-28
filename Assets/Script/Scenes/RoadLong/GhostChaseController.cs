using UnityEngine;
using System.Collections;

namespace ARKOM.Scenes.Road
{
 public class GhostChaseController : MonoBehaviour
 {
 [Header("Movement")] public float WalkSpeed =1.6f; public float RunSpeed =4.5f;
 [Tooltip("How close to the player the ghost tries to stay")]
 public float followDistance =3f;
 [Tooltip("Rotation lerp speed")] public float turnSpeed =5f;
 
 [Header("Animator Params (legacy)")]
 [Tooltip("Animator parameter name for speed (float)")] public string animSpeedParam = "Speed";
 [Tooltip("Animator parameter name for running (bool)")] public string animRunParam = "Run";
 
 [Header("Animator Params (2D Blend)")]
 [Tooltip("Animator float parameter for horizontal (strafe) movement")] public string animMoveXParam = "X";
 [Tooltip("Animator float parameter for forward movement")] public string animMoveYParam = "Y";
 
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
 target = t; following = true; running = run; UpdateAnim();
 }

 public void SwitchToRun()
 { running = true; UpdateAnim(); }

 public void StopFollowing(bool hideGhost)
 {
 following = false; target = null; UpdateAnim();
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
 // world velocity
 Vector3 v = delta / dt; // m/s
 // local velocity relative to ghost forward
 Vector3 lv = transform.InverseTransformDirection(v);

 // Set2D axes if exist
 if (hasMoveX)
 animator.SetFloat(hashMoveX, Mathf.Clamp(lv.x / Mathf.Max(0.01f, RunSpeed), -1f,1f));
 if (hasMoveY)
 animator.SetFloat(hashMoveY, Mathf.Clamp(lv.z / Mathf.Max(0.01f, RunSpeed), -1f,1f));

 // Also set Speed if exists
 if (hasSpeed)
 {
 float speedScalar = v.magnitude; // m/s
 animator.SetFloat(hashSpeed, speedScalar);
 }

 // Always maintain run bool if present
 if (hasRun) animator.SetBool(hashRun, running);
 }
 }
}
