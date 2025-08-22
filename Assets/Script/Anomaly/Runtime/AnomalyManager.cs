using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Data;
using ARKOM.Game;

namespace ARKOM.Anomalies.Runtime
{
    // ผู้จัดการสุ่มและติดตาม Anomaly กลางคืน (Random + Track) 
    public class AnomalyManager : MonoBehaviour
    {
        [Tooltip("รายการ Anomaly (คอมโพเนนต์) ที่เตรียมไว้ในซีนทั้งหมด / All anomaly components placed in scene")]
        public List<Anomaly> anomalyPool = new();

        [Header("Difficulty / ความยาก")]
        [Tooltip("จำนวนพื้นฐานต่อคืน / Base anomalies per night")]
        public int baseAnomaliesPerNight = 3;
        [Tooltip("เพิ่ม +1 ทุก ๆ X วัน / Add +1 every X days")]
        public int dayIntervalIncrease = 2;

        private readonly HashSet<string> activeIds = new(); // id ที่กำลัง Active
        private int resolvedCount;                          // จำนวนที่ผู้เล่นตรวจพบแล้ว

        // Public read-only info / ข้อมูลอ่านอย่างเดียวให้ UI ใช้
        public int ActiveAnomalyCount => activeIds.Count;          // จำนวนที่สุ่มเปิดทั้งหมด
        public int ResolvedCount => resolvedCount;                 // ที่พบแล้ว
        public int RemainingCount => ActiveAnomalyCount - ResolvedCount; // ที่ยังเหลือ

        private void OnEnable()  => EventBus.Subscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        private void OnDisable() => EventBus.Unsubscribe<AnomalyResolvedEvent>(OnAnomalyResolved);

        // เริ่มคืนใหม่ / Start a night
        public void StartNight()
        {
            resolvedCount = 0;
            ResetAll();
            ActivateRandomSet();
            Debug.Log($"[AnomalyManager] Night started. Active = {ActiveAnomalyCount}"); // บอกจำนวนที่เปิด
        }

        // ปิดทั้งหมดก่อนสุ่ม / Deactivate all
        private void ResetAll()
        {
            activeIds.Clear();
            foreach (var a in anomalyPool)
                a.Deactivate();
        }

        // สุ่มเลือกกี่ตัวจะ Active / Random pick which anomalies to activate
        private void ActivateRandomSet()
        {
            if (anomalyPool.Count == 0) return;

            int currentDay = FindObjectOfType<GameManager>()?.currentDay ?? 1;
            int bonus = Mathf.FloorToInt((currentDay - 1) / Mathf.Max(1, dayIntervalIncrease));
            int target = Mathf.Clamp(baseAnomaliesPerNight + bonus, 1, anomalyPool.Count);

            // Fisher-Yates shuffle / สุ่มลิสต์
            List<Anomaly> shuffled = new(anomalyPool);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int j = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < target; i++)
            {
                var a = shuffled[i];
                a.Activate();
                if (a.data != null)
                    activeIds.Add(a.data.anomalyId);
            }
        }

        // เมื่อผู้เล่นตรวจพบ / Player confirmed anomaly
        private void OnAnomalyResolved(AnomalyResolvedEvent evt)
        {
            if (!activeIds.Contains(evt.Id)) return;
            resolvedCount++;
            Debug.Log($"[AnomalyManager] Resolved {evt.Id} ({resolvedCount}/{ActiveAnomalyCount})");
            if (resolvedCount >= activeIds.Count)
                EventBus.Publish(new NightCompletedEvent());
        }
    }
}