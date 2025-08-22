using UnityEngine;

namespace ARKOM.Anomalies.Data
{
    [CreateAssetMenu(fileName = "New Anomaly", menuName = "ARKOM/Anomaly Data")]
    public class AnomalyData : ScriptableObject
    {
        [Header("ข้อมูลพื้นฐาน")]
        [Tooltip("รหัสไม่ซ้ำสำหรับใช้ตรวจสอบ/อ้างอิง (ต้องไม่ซ้ำ)")]
        public string anomalyId;
        [Tooltip("ชื่อที่จะแสดงบน UI หรือ Log")]
        public string displayName;

        [Header("ประเภทความผิดปกติ")]
        [Tooltip("รูปแบบการเปลี่ยนแปลงที่จะใช้")]
        public AnomalyType type;

        [Header("Transform Offsets (ใช้กับ Position / Rotation / Scale)")]
        [Tooltip("ระยะที่จะเลื่อน (ทบกับตำแหน่งเดิม)")]
        public Vector3 positionOffset;
        [Tooltip("องศาที่จะหมุน (ทบกับหมุนเดิม)")]
        public Vector3 rotationOffset;
        [Tooltip("ตัวคูณขนาด (1 = เท่าเดิม)")]
        public float scaleMultiplier = 1f;

        [Header("Color Change (ใช้กับ Color)")]
        [Tooltip("สีที่จะแทนที่วัสดุ (Material._Color)")]
        public Color colorChange = Color.red;

        [Header("Prefab / เอฟเฟกต์เสริม")]
        [Tooltip("Prefab ที่จะ Spawn เมื่อใช้ประเภท ShadowMovement หรือ SpawnPrefab")]
        public GameObject effectPrefab;
        [Tooltip("ติ๊กเพื่อให้ใช้ effectPrefab")]
        public bool useEffectPrefab;
        [Tooltip("เสียงที่จะเล่นตอน Anomaly ถูกเปิดใช้งาน")]
        public AudioClip soundEffect;

        public enum AnomalyType
        {
            Position,       // เลื่อนตำแหน่ง
            Rotation,       // หมุน
            Scale,          // เปลี่ยนขนาด
            Color,          // เปลี่ยนสี
            Disappear,      // ซ่อนวัตถุ (Renderer ปิด แต่ Collider ยังอยู่ให้กดได้)
            PictureFlip,    // พลิกรูป/กลับด้าน (หมุน 180 องศา)
            ShadowMovement, // สร้างเงาเคลื่อนไหว (Prefab)
            SpawnPrefab     // สร้าง Prefab ปกติ (สิ่งของเพิ่มขึ้น)
        }
    }
}