using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

namespace ARKOM.Story
{
    [RequireComponent(typeof(Collider))]
    public class OoyBedTrigger : MonoBehaviour
    {
        [Header("Settings")] public bool fireOnce = true;
        [Tooltip("เปิดเพื่อพิมพ์ Log รายละเอียดการชนทุกครั้ง (แม้ไม่ผ่านเงื่อนไข)")] public bool verboseDebug = true;

        private bool fired;
        private Collider _col;

        void Awake()
        {
            _col = GetComponent<Collider>();
            if (_col && !_col.isTrigger)
            {
                if (verboseDebug) StoryDebug.Log("Collider ไม่ได้ตั้ง IsTrigger = true", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (verboseDebug) StoryDebug.Log("OnTriggerEnter: " + other.name, this);

            if (!other.CompareTag("Player"))
            {
                if (verboseDebug) StoryDebug.Log("ไม่ใช่ Player (Tag=" + other.tag + ")", this);
                return;
            }

            // ต้องถึงสถานะ CheckOoy เท่านั้น
            if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.CheckOoy)
            {
                if (verboseDebug) StoryDebug.Log("ยังไม่ถึงขั้น CheckOoy (state ปัจจุบัน = " + (SequenceController.Instance? SequenceController.Instance.CurrentState.ToString():"NULL") + ")", this);
                return; // ไม่ล๊อก fired เพื่อให้กลับมาซ้ำได้
            }

            if (fireOnce && fired)
            {
                if (verboseDebug) StoryDebug.Log("เคยยิงไปแล้ว (fireOnce)", this);
                return;
            }

            fired = true;
            StoryDebug.LogEvent("OoyCheckedEvent", this);
            EventBus.Publish(new OoyCheckedEvent());
        }

        // ช่วยให้เห็นขนาด Trigger ใน Scene / Game (Gizmos)
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
            var c = GetComponent<Collider>();
            if (!c) return;
            if (c is BoxCollider b)
            {
                Gizmos.matrix = b.transform.localToWorldMatrix;
                Gizmos.DrawCube(b.center, b.size);
            }
            else if (c is SphereCollider s)
            {
                Gizmos.matrix = s.transform.localToWorldMatrix;
                Gizmos.DrawSphere(s.center, s.radius);
            }
            else if (c is CapsuleCollider cc)
            {
                Gizmos.matrix = cc.transform.localToWorldMatrix;
                float r = cc.radius; float h = cc.height;
                // วาดทรงกระบอกง่าย ๆ (ประมาณ)
                Gizmos.DrawCube(cc.center, new Vector3(r * 2f, h, r * 2f));
            }
        }
    }
}
