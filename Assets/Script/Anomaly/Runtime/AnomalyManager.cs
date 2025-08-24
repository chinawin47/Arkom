using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Data;
using ARKOM.Game;

namespace ARKOM.Anomalies.Runtime
{
    // ผู้จัดการสุ่มและติดตาม Anomaly (Progressive + Metrics)
    public class AnomalyManager : MonoBehaviour
    {
        [Tooltip("รายการ Anomaly (คอมโพเนนต์) ที่เตรียมไว้ในซีนทั้งหมด / All anomaly components placed in scene")]
        public List<Anomaly> anomalyPool = new();

        [Header("Difficulty / ความยาก")]
        public int baseAnomaliesPerNight = 3;
        public int dayIntervalIncrease = 2;

        [Header("Progressive Spawning / ทยอยเกิด")]
        [Range(0f,1f)] public float initialSpawnFraction = 0.5f;
        public float spawnInterval = 25f;
        public int spawnBatchSize = 1;

        [Header("Spawn Feedback")]
        public AudioClip spawnSfx;
        public Transform audioPoint;

        [Header("Metrics")]
        [Tooltip("พิมพ์ Log เวลา resolve แต่ละ anomaly")]
        public bool logResolveTimes = true;
        [Tooltip("สรุปค่าเฉลี่ยเมื่อจบคืน")]
        public bool logSummaryAtNightEnd = true;

        private readonly HashSet<string> activeIds = new();
        private int resolvedCount;

        // Progressive runtime
        private bool nightRunning;
        private int targetForNight;
        private float spawnTimer;

        // Metrics
        private readonly Dictionary<string, float> spawnTimes = new(); // anomalyId -> Time.time ตอน spawn
        private readonly List<float> resolveDurations = new();        // เวลาที่ใช้หาแต่ละตัว
        public float AverageResolveTime => resolveDurations.Count == 0 ? 0f : TotalResolveTime / resolveDurations.Count;
        public float LastResolveTime { get; private set; }
        public float TotalResolveTime { get; private set; }

        // Public read-only info
        public int ActiveAnomalyCount => activeIds.Count;
        public int ResolvedCount => resolvedCount;
        public int RemainingCount => targetForNight - resolvedCount;
        public int RemainingToActivate => Mathf.Max(0, targetForNight - ActiveAnomalyCount);

        private void OnEnable()  => EventBus.Subscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        private void OnDisable() => EventBus.Unsubscribe<AnomalyResolvedEvent>(OnAnomalyResolved);

        private void Update()
        {
            if (!nightRunning) return;
            if (ActiveAnomalyCount >= targetForNight) return;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnBatch();
                spawnTimer = spawnInterval;
            }
        }

        public void StartNight()
        {
            resolvedCount = 0;
            ResetAll();
            nightRunning = true;

            resolveDurations.Clear();
            spawnTimes.Clear();
            TotalResolveTime = 0f;
            LastResolveTime = 0f;

            int currentDay = FindObjectOfType<GameManager>()?.currentDay ?? 1;
            int bonus = Mathf.FloorToInt((currentDay - 1) / Mathf.Max(1, dayIntervalIncrease));
            targetForNight = Mathf.Clamp(baseAnomaliesPerNight + bonus, 1, anomalyPool.Count);

            int initialCount = Mathf.Clamp(Mathf.RoundToInt(targetForNight * initialSpawnFraction), 1, targetForNight);
            ActivateRandomDistinct(initialCount);

            spawnTimer = spawnInterval;
            if (initialCount > 0)
            {
                if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, (audioPoint ? audioPoint : transform).position);
                EventBus.Publish(new AnomalySpawnBatchEvent(initialCount, ActiveAnomalyCount, targetForNight));
            }

            Debug.Log($"[AnomalyManager] Night started Target={targetForNight} Initial={initialCount} RemainToActivate={RemainingToActivate}");
        }

        private void ResetAll()
        {
            activeIds.Clear();
            foreach (var a in anomalyPool)
                a.Deactivate();
            nightRunning = false;
            targetForNight = 0;
            spawnTimer = 0f;
        }

        private void SpawnBatch()
        {
            if (!nightRunning) return;
            if (RemainingToActivate <= 0) return;
            int toSpawn = Mathf.Min(spawnBatchSize, RemainingToActivate);
            ActivateRandomDistinct(toSpawn);

            if (toSpawn > 0)
            {
                if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, (audioPoint ? audioPoint : transform).position);
                EventBus.Publish(new AnomalySpawnBatchEvent(toSpawn, ActiveAnomalyCount, targetForNight));
                Debug.Log($"[AnomalyManager] Spawn batch +{toSpawn} Active={ActiveAnomalyCount}/{targetForNight}");
            }

            TryCompleteNight();
        }

        private void ActivateRandomDistinct(int count)
        {
            List<Anomaly> candidates = new();
            foreach (var a in anomalyPool)
            {
                if (a.data == null) continue;
                if (activeIds.Contains(a.data.anomalyId)) continue;
                candidates.Add(a);
            }
            if (candidates.Count == 0) return;

            for (int i = 0; i < candidates.Count; i++)
            {
                int j = Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int spawn = Mathf.Min(count, candidates.Count);
            for (int i = 0; i < spawn; i++)
            {
                var a = candidates[i];
                a.Activate();
                if (a.data != null)
                {
                    activeIds.Add(a.data.anomalyId);
                    spawnTimes[a.data.anomalyId] = Time.time;
                }
            }
        }

        private void OnAnomalyResolved(AnomalyResolvedEvent evt)
        {
            if (!activeIds.Contains(evt.Id)) return;
            resolvedCount++;

            if (spawnTimes.TryGetValue(evt.Id, out float tSpawn))
            {
                float duration = Time.time - tSpawn;
                LastResolveTime = duration;
                resolveDurations.Add(duration);
                TotalResolveTime += duration;
                if (logResolveTimes)
                    Debug.Log($"[Metrics] Resolved {evt.Id} in {duration:0.00}s (Avg {AverageResolveTime:0.00}s)");
                spawnTimes.Remove(evt.Id);
            }
            else if (logResolveTimes)
            {
                Debug.Log($"[Metrics] Resolved {evt.Id} (no spawn time recorded)");
            }

            EventBus.Publish(new AnomalyProgressEvent(resolvedCount, targetForNight));
            TryCompleteNight();
        }

        private void TryCompleteNight()
        {
            if (nightRunning && resolvedCount >= targetForNight && ActiveAnomalyCount >= targetForNight)
            {
                nightRunning = false;
                if (logSummaryAtNightEnd)
                    Debug.Log($"[Metrics] Night done. Resolved={resolvedCount} Avg={AverageResolveTime:0.00}s Total={TotalResolveTime:0.0}s");
                EventBus.Publish(new NightCompletedEvent());
            }
        }
    }
}