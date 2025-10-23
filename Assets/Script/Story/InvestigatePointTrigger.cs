using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Investigate Point Trigger")] 
 [RequireComponent(typeof(Collider))]
 public class InvestigatePointTrigger : MonoBehaviour
 {
 [Header("Point")] public Transform targetPoint; // เว้นว่าง = ใช้ตำแหน่งตัวเอง
 [Tooltip("ยิงได้ครั้งเดียว")] public bool once = true;
 [Tooltip("ให้ผีตอบสนองเฉพาะตอนกำลังไล่ (AI จะกรองซ้ำอีกชั้น)")] public bool onlyWhenChasing = true;
 [Tooltip("Tag ของผู้เล่นที่ใช้ชนทริกเกอร์")] public string playerTag = "Player";

 private bool fired;

 void Reset()
 {
 var col = GetComponent<Collider>();
 col.isTrigger = true;
 }

 void OnTriggerEnter(Collider other)
 {
 if (fired && once) return;
 if (!other.CompareTag(playerTag)) return;

 // ปล่อยอีเวนต์ให้ AI (ChasingGhost) รับ
 Vector3 pos = targetPoint ? targetPoint.position : transform.position;
 EventBus.Publish(new InvestigatePointEvent(pos));

 fired = true;
 }

#if UNITY_EDITOR
 void OnDrawGizmos()
 {
 Gizmos.color = new Color(0.2f,0.8f,1f,0.8f);
 Vector3 p = targetPoint ? targetPoint.position : transform.position;
 Gizmos.DrawSphere(p,0.2f);
 Gizmos.DrawLine(transform.position, p);
 }
#endif
 }
}
