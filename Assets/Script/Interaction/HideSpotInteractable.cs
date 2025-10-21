using UnityEngine;
using ARKOM.Core;
using ARKOM.Player;

[AddComponentMenu("Interactable/Hide Spot (Closet)")]
public class HideSpotInteractable : Interactable
{
 [Header("Refs")] public DoorInteractable door;
 public Transform seatAnchor;
 public Transform cameraPoint;
 [Tooltip("จุดหน้าตู้ที่ให้ศัตรูมาค้นหา")] public Transform searchPoint;

 [Header("Rules")] public bool requireDoorOpenToEnter = true;
 public bool autoCloseOnEnter = true;
 public bool autoOpenOnExit = true;
 public bool denyExitWhileDanger = true;
 [Tooltip("ระยะขั้นต่ำจากศัตรูที่ยอมให้ออก")] public float minSafeExitDistance =3.0f;
 [Tooltip("เวลาที่ต้องไม่ถูกเห็นก่อนอนุญาตให้ออก")] public float requireNotSeenDuration =1.0f;
 [Tooltip("คูลดาวน์กันสแปม (วินาที)")] public float exitCooldown =0.5f;

 [Header("UI")]
 public string cannotEnterHint = "เปิดประตูก่อนถึงจะหลบได้";
 public string denyExitHint = "ออกไม่ได้ อันตรายเกินไป";

 private bool occupied;
 private float lastExitTime;
 private float lastSeenTimer;

 public override bool CanInteract(object interactor)
 {
 if (oneTime && occupied) return false;
 return base.CanInteract(interactor);
 }

 protected override void OnInteract(object interactor)
 {
 if (!occupied)
 {
 TryEnter(interactor);
 }
 else
 {
 TryExit(interactor);
 }
 }

 private void TryEnter(object interactor)
 {
 if (requireDoorOpenToEnter && door && !door.isOpen)
 {
 ARKOM.Story.SequenceController.Instance?.ShowTempHint(cannotEnterHint,2f);
 return;
 }
 var pc = interactor as ARKOM.Player.PlayerController;
 if (!pc) pc = GameObject.FindObjectOfType<ARKOM.Player.PlayerController>();
 if (!pc) return;

 occupied = true;
 PlayerStealth.SetHidden(transform);
 pc.EnterSeat(seatAnchor, cameraPoint);
 // ปิดประตูอัตโนมัติเมื่อเข้าตู้ (ต้องปิดจากสถานะเปิดอยู่)
 if (autoCloseOnEnter && door && door.isOpen) door.ToggleDoor();

 // แจ้งให้ AI มาค้นเฉพาะเมื่อกำลังไล่เท่านั้น
 if (searchPoint)
 {
 var chasers = GameObject.FindObjectsOfType<ARKOM.Enemy.ChasingGhost>();
 bool anyChasing = false;
 foreach (var c in chasers)
 {
 if (c && c.enabled && c.GetComponent<UnityEngine.AI.NavMeshAgent>())
 {
 var ag = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
 if (ag.velocity.magnitude >= Mathf.Max(0.1f, c.runSpeed *0.6f)) { anyChasing = true; break; }
 }
 }
 if (anyChasing)
 {
 EventBus.Publish(new ARKOM.Story.InvestigatePointEvent(searchPoint.position));
 }
 }
 }

 private void TryExit(object interactor)
 {
 if (Time.time - lastExitTime < exitCooldown) return;

 if (denyExitWhileDanger && !IsSafeToExit())
 {
 ARKOM.Story.SequenceController.Instance?.ShowTempHint(denyExitHint,2f);
 return;
 }
 var pc = interactor as ARKOM.Player.PlayerController;
 if (!pc) pc = GameObject.FindObjectOfType<ARKOM.Player.PlayerController>();
 if (!pc) return;

 occupied = false;
 lastExitTime = Time.time;
 PlayerStealth.Clear();
 pc.ExitSeat();
 if (autoOpenOnExit && door && !door.isOpen) door.ToggleDoor();
 }

 private bool IsSafeToExit()
 {
 // เงื่อนไขง่าย: ไม่มีศัตรู (ChasingGhost) อยู่ใกล้เกินกำหนด และไม่ได้ถูกเห็นล่าสุด
 var enemies = GameObject.FindObjectsOfType<ARKOM.Enemy.ChasingGhost>();
 float minDist = float.MaxValue;
 foreach (var e in enemies)
 {
 float d = Vector3.Distance(e.transform.position, transform.position);
 if (d < minDist) minDist = d;
 // ถ้าเห็นผู้เล่นอยู่ แปลว่ายังไม่ปลอดภัย
 if (e && e.enabled)
 {
 // ไม่มีเมธอด public ตรวจเห็น จึงใช้ระยะอย่างเดียวในดีไซน์แรก
 }
 }
 if (minDist < minSafeExitDistance) return false;
 return true;
 }

 private void OnDrawGizmosSelected()
 {
 Gizmos.color = Color.cyan;
 if (seatAnchor) Gizmos.DrawWireSphere(seatAnchor.position,0.1f);
 Gizmos.color = Color.yellow;
 if (searchPoint) Gizmos.DrawWireSphere(searchPoint.position,0.15f);
 }
}
