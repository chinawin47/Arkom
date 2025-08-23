using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Game;

namespace ARKOM.Anomalies.Runtime
{
    public class AnomalyManager : MonoBehaviour
    {
        [Tooltip("รายการ Anomaly (คอมโพเนนต์) ที่เตรียมไว้ในซีนทั้งหมด / All anomaly components placed in scene")]
        public List<Anomaly> anomalyPool = new();

        [Header("Difficulty / ความยาก")]
        public int baseAnomaliesPerNight = 3;
        public int dayIntervalIncrease = 2;

        [Header("Debug")]
        [Tooltip("พิมพ์รายละเอียดทุกครั้งที่แก้ไข / Verbose logs")]
        public bool debugVerbose = false;

        private readonly List<Anomaly> activeAnomalies = new();
        private readonly HashSet<int> resolvedInstanceIds = new();
        private int totalActive;

        public int ActiveAnomalyCount => totalActive;
        public int ResolvedCount => resolvedInstanceIds.Count;
        public int RemainingCount => ActiveAnomalyCount - ResolvedCount;

        private void OnEnable()  => EventBus.Subscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        private void OnDisable() => EventBus.Unsubscribe<AnomalyResolvedEvent>(OnAnomalyResolved);

        public void StartNight()
        {
            ResetAll();
            ActivateRandomSet();
            EventBus.Publish(new AnomalyProgressEvent(ResolvedCount, ActiveAnomalyCount));
            if (debugVerbose)
                Debug.Log($"[AnomalyManager] StartNight totalActive={ActiveAnomalyCount}");
        }

        private void ResetAll()
        {
            foreach (var a in activeAnomalies)
                if (a) a.Deactivate();

            activeAnomalies.Clear();
            resolvedInstanceIds.Clear();
            totalActive = 0;
        }

        private void ActivateRandomSet()
        {
            if (anomalyPool.Count == 0) return;

            int currentDay = FindObjectOfType<GameManager>()?.currentDay ?? 1;
            int bonus = Mathf.FloorToInt((currentDay - 1) / Mathf.Max(1, dayIntervalIncrease));
            int target = Mathf.Clamp(baseAnomaliesPerNight + bonus, 1, anomalyPool.Count);

            var shuffled = new List<Anomaly>(anomalyPool);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int j = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            totalActive = 0;
            for (int i = 0; i < target; i++)
            {
                var a = shuffled[i];
                if (!a || a.data == null) continue;
                a.Activate();
                activeAnomalies.Add(a);
                totalActive++;
            }
        }

        private void OnAnomalyResolved(AnomalyResolvedEvent evt)
        {
            if (evt.Source == null) return;
            int instId = evt.Source.GetInstanceID();

            if (!resolvedInstanceIds.Add(instId))
            {
                if (debugVerbose)
                    Debug.Log($"[AnomalyManager] DUPLICATE resolve ignored id={evt.Id} instId={instId}");
                return;
            }

            if (debugVerbose)
                Debug.Log($"[AnomalyManager] Resolve id={evt.Id} instId={instId} progress {ResolvedCount}/{ActiveAnomalyCount}");

            EventBus.Publish(new AnomalyProgressEvent(ResolvedCount, ActiveAnomalyCount));

            if (ResolvedCount >= ActiveAnomalyCount)
                EventBus.Publish(new NightCompletedEvent());
        }
    }
}