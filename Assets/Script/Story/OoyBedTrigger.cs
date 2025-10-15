using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

namespace ARKOM.Story
{
    [RequireComponent(typeof(Collider))]
    public class OoyBedTrigger : MonoBehaviour
    {
        [Header("Settings")] public bool fireOnce = true;
        [Tooltip("แสดงข้อความ Log เพิ่มเติมช่วยดีบัก (ปล่อยปิดในงานจริง)")] public bool verboseDebug = true;

        private bool fired;
        private Collider _col;

        void Awake()
        {
            _col = GetComponent<Collider>();
            if (_col && !_col.isTrigger)
            {
                if (verboseDebug) StoryDebug.Log("Collider ควรตั้ง IsTrigger = true", this);
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

            var sc = SequenceController.Instance;
            if (!sc)
            {
                if (verboseDebug) StoryDebug.Log("SequenceController = NULL", this);
                return;
            }

            // อนุญาตทั้งสเตตัสเก่า (CheckOoy) และสเตตัสใหม่ (FindOoy)
            bool validState = sc.CurrentState == SequenceController.StoryState.CheckOoy
                              || sc.CurrentState == SequenceController.StoryState.FindOoy;
            if (!validState)
            {
                if (verboseDebug) StoryDebug.Log("[ลำดับเรื่อง] ยังไม่ถึงขั้น CheckOoy/FindOoy (state ปัจจุบัน = " + sc.CurrentState + ")", this);
                return;
            }

            if (fireOnce && fired)
            {
                if (verboseDebug) StoryDebug.Log("ทริกเกอร์แล้ว (fireOnce)", this);
                return;
            }

            fired = true;
            if (fireOnce && _col) _col.enabled = false;
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
