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

        [Header("Points Mode / โหมดจุด")]
        [Tooltip("รายการจุด (AnomalyPoint) ถ้าตั้งค่านี้ ระบบจะสลับไปใช้โหมดจุด + คูลดาวน์ + จำกัดจำนวนพร้อมกัน")]
        public List<AnomalyPoint> pointPool = new();
        [Tooltip("จำนวนจุดที่ Active พร้อมกันได้สูงสุด (เฉพาะโหมดจุด)")]
        public int maxConcurrentActive = 2;

        [Header("Spawning Toggles")]
        [Tooltip("ให้เกิดชุดแรกทันทีเมื่อเริ่มคืนหรือไม่ (โหมดจุดและโหมดเดิม)")]
        public bool initialSpawnOnNightStart = true;
        [Tooltip("ให้เกิดตามเวลา spawnInterval หรือไม่ (ถ้าปิด จะเหลือเฉพาะสั่งเกิดด้วย TriggerZone/โค้ด)")]
        public bool timedSpawningEnabled = true;

        [Header("Point Options")]
        [Tooltip("พยายามไม่เลือก 'ตำแหน่งเดิม' ซ้ำในรอบถัดไป (ถ้ามีตัวเลือกอื่น)")]
        public bool avoidImmediateRepeatPoint = true;
        [Tooltip("รีเซ็ตคูลดาวน์ของทุกจุดเมื่อเริ่มคืนใหม่")]
        public bool resetCooldownOnStartNight = false;
        [Tooltip("เติมตัวใหม่อย่างไวหลัง Resolve (ไม่ต้องรอทั้ง spawnInterval)")]
        public bool spawnImmediatelyOnResolve = true;
        [Tooltip("ดีเลย์เติมหลัง Resolve (วินาที) ถ้าเปิด spawnImmediatelyOnResolve")]
        public float resolveRefillDelay = 0.5f;

        [Header("Difficulty / ความยาก")]
        public int baseAnomaliesPerNight = 3;
        public int dayIntervalIncrease = 2;

        [Header("Progressive Spawning / ทยอยเกิด")]
        [Range(0f,1f)] public float initialSpawnFraction = 0.5f;
        public float spawnInterval = 25f;
        [Tooltip("แปรผันเวลาระหว่างสปอว์น ±สัดส่วน (เช่น 0.25 = ±25%) เพื่อไม่ให้เป็นจังหวะคงที่เกินไป")]
        [Range(0f, 0.95f)] public float spawnIntervalJitter = 0.25f;
        public int spawnBatchSize = 1;

        [Header("Spawn Feedback")]
        public AudioClip spawnSfx;
        public Transform audioPoint;

        [Header("Metrics")]
        [Tooltip("พิมพ์ Log เวลา resolve แต่ละ anomaly")]
        public bool logResolveTimes = true;
        [Tooltip("สรุปค่าเฉลี่ยเมื่อจบคืน")]
        public bool logSummaryAtNightEnd = true;

        [Header("Validation / Debug")]
        public bool validateIdsOnStartNight = true;

        private readonly HashSet<string> activeIds = new();
        private int resolvedCount;

        // Progressive runtime
        private bool nightRunning;
        private int targetForNight;
        private float spawnTimer;

        // Metrics
        private readonly Dictionary<string, float> spawnTimes = new(); // id -> Time.time ตอน spawn
        private readonly List<float> resolveDurations = new();        // เวลาที่ใช้หาแต่ละตัว
        public float AverageResolveTime => resolveDurations.Count == 0 ? 0f : TotalResolveTime / resolveDurations.Count;
        public float LastResolveTime { get; private set; }
        public float TotalResolveTime { get; private set; }

        // Public read-only info
        public int ActiveAnomalyCount => activeIds.Count;
        public int ResolvedCount => resolvedCount;
        public int RemainingCount => Mathf.Max(0, targetForNight - resolvedCount);
        public int RemainingToActivate => Mathf.Max(0, targetForNight - ActiveAnomalyCount);

        public int TargetForNight => targetForNight;
        public bool IsPointMode => pointPool != null && pointPool.Count > 0;

        // Point-mode memory
        private string lastActivatedPointId; // ใช้กันตำแหน่งเดิมซ้ำทันที

        private void OnEnable()  => EventBus.Subscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        private void OnDisable() => EventBus.Unsubscribe<AnomalyResolvedEvent>(OnAnomalyResolved);

        private void Update()
        {
            if (!nightRunning) return;

            bool pointMode = IsPointMode;

            if (pointMode)
            {
                if (ResolvedCount >= targetForNight) return;

                if (!timedSpawningEnabled) return; // ปิดสปอว์นตามเวลา

                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    int allow = Mathf.Max(0, maxConcurrentActive - ActiveAnomalyCount);
                    int remainToTarget = Mathf.Max(0, targetForNight - (resolvedCount + ActiveAnomalyCount));
                    int toSpawn = Mathf.Min(spawnBatchSize, allow, remainToTarget);
                    if (toSpawn > 0)
                        SpawnPointsBatch(toSpawn);

                    spawnTimer = NextSpawnInterval();
                }
            }
            else
            {
                if (ActiveAnomalyCount >= targetForNight) return;

                if (!timedSpawningEnabled) return;

                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    SpawnBatch();
                    spawnTimer = NextSpawnInterval();
                }
            }
        }

        private float NextSpawnInterval()
        {
            float j = Mathf.Clamp01(spawnIntervalJitter);
            float min = spawnInterval * (1f - j);
            float max = spawnInterval * (1f + j);
            return Random.Range(min, max);
        }

        // ===== Validation =====
        private bool ValidateAnomalyIds()
        {
            var seen = new HashSet<string>();
            bool ok = true;
            foreach (var a in anomalyPool)
            {
                if (!a) continue;
                var d = a.data;
                if (d == null)
                {
                    Debug.LogWarning("[AnomalyManager] Anomaly missing data asset.", a);
                    ok = false;
                    continue;
                }
                string id = (d.anomalyId ?? "").Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[AnomalyManager] Empty anomalyId on '{d.name}'", d);
                    ok = false;
                    continue;
                }
                if (!seen.Add(id))
                {
                    Debug.LogWarning($"[AnomalyManager] Duplicate anomalyId '{id}' ({d.name})", d);
                    ok = false;
                }
                d.anomalyId = id;
            }
            if (!ok) Debug.LogWarning("[AnomalyManager] Validation finished with issues.");
            return ok;
        }

        private bool ValidatePointIds()
        {
            var seen = new HashSet<string>();
            bool ok = true;
            foreach (var p in pointPool)
            {
                if (!p) continue;
                string id = (p.pointId ?? "").Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[AnomalyManager] Point with empty pointId on '{p.name}'", p);
                    ok = false;
                }
                else if (!seen.Add(id))
                {
                    Debug.LogWarning($"[AnomalyManager] Duplicate pointId '{id}' on '{p.name}'", p);
                    ok = false;
                }

                if (!p.anomaly)
                {
                    Debug.LogWarning($"[AnomalyManager] Point '{p.pointId}' missing Anomaly component reference.", p);
                    ok = false;
                }
            }
            if (!ok) Debug.LogWarning("[AnomalyManager] Point validation finished with issues.");
            return ok;
        }

        public void StartNight()
        {
            bool pointMode = IsPointMode;

            if (validateIdsOnStartNight)
            {
                if (pointMode) ValidatePointIds();
                else ValidateAnomalyIds();
            }

            resolvedCount = 0;
            ResetAll(pointMode);
            nightRunning = true;

            resolveDurations.Clear();
            spawnTimes.Clear();
            TotalResolveTime = 0f;
            LastResolveTime = 0f;
            lastActivatedPointId = null;

            int currentDay = FindObjectOfType<GameManager>()?.currentDay ?? 1;
            int bonus = Mathf.FloorToInt((currentDay - 1) / Mathf.Max(1, dayIntervalIncrease));
            targetForNight = pointMode
                ? Mathf.Clamp(baseAnomaliesPerNight + bonus, 1, Mathf.Max(1, pointPool.Count * 10))
                : Mathf.Clamp(baseAnomaliesPerNight + bonus, 1, anomalyPool.Count);

            if (pointMode)
            {
                int initialCount = Mathf.Clamp(Mathf.Min(maxConcurrentActive, targetForNight), 1, Mathf.Max(1, maxConcurrentActive));

                if (initialSpawnOnNightStart)
                {
                    int spawned = SpawnPointsBatch(initialCount);
                    if (spawned > 0)
                    {
                        if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, (audioPoint ? audioPoint : transform).position);
                        EventBus.Publish(new AnomalySpawnBatchEvent(spawned, ActiveAnomalyCount, targetForNight));
                    }
                }

                spawnTimer = NextSpawnInterval();

                Debug.Log($"[AnomalyManager] Night started (PointMode) Target={targetForNight} Initial={(initialSpawnOnNightStart ? Mathf.Min(initialCount, ActiveAnomalyCount) : 0)} Active={ActiveAnomalyCount} MaxConcurrent={maxConcurrentActive}");
            }
            else
            {
                int initialCount = Mathf.Clamp(Mathf.RoundToInt(targetForNight * initialSpawnFraction), 1, targetForNight);

                if (initialSpawnOnNightStart)
                {
                    ActivateRandomDistinct(initialCount);
                    if (initialCount > 0)
                    {
                        if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, (audioPoint ? audioPoint : transform).position);
                        EventBus.Publish(new AnomalySpawnBatchEvent(initialCount, ActiveAnomalyCount, targetForNight));
                    }
                }

                spawnTimer = NextSpawnInterval();

                Debug.Log($"[AnomalyManager] Night started Target={targetForNight} Initial={(initialSpawnOnNightStart ? initialCount : 0)} RemainToActivate={RemainingToActivate}");
            }
        }

        private void ResetAll(bool pointMode)
        {
            activeIds.Clear();

            if (pointMode)
            {
                foreach (var p in pointPool)
                {
                    if (!p) continue;
                    if (p.anomaly) p.anomaly.Deactivate();
                    p.ResetPointState(resetCooldown: resetCooldownOnStartNight);
                }
            }
            else
            {
                foreach (var a in anomalyPool)
                    if (a) a.Deactivate();
            }

            nightRunning = false;
            spawnTimer = 0f;
        }

        // ===== โหมดจุด (Point Mode) =====
        private int SpawnPointsBatch(int count)
        {
            if (!nightRunning || count <= 0) return 0;

            List<AnomalyPoint> candidates = new();
            float now = Time.time;

            foreach (var p in pointPool)
            {
                if (!p) continue;
                if (p.IsActive) continue;
                if (!p.CanActivate(now)) continue;
                if (activeIds.Contains(p.pointId)) continue; // กันซ้ำในชุด Active ปัจจุบัน
                candidates.Add(p);
            }
            if (candidates.Count == 0) return 0;

            // เลี่ยงตำแหน่งเดิมซ้ำทันที (ถ้ามีตัวเลือกอื่น)
            if (avoidImmediateRepeatPoint && !string.IsNullOrEmpty(lastActivatedPointId))
            {
                bool hasOthers = false;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].pointId != lastActivatedPointId) { hasOthers = true; break; }
                }
                if (hasOthers)
                {
                    candidates.RemoveAll(p => p.pointId == lastActivatedPointId);
                    if (candidates.Count == 0) return 0;
                }
            }

            // สุ่มลำดับ candidates
            for (int i = 0; i < candidates.Count; i++)
            {
                int j = Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int spawned = 0;
            int allow = Mathf.Max(0, maxConcurrentActive - ActiveAnomalyCount);
            int remainToTarget = Mathf.Max(0, targetForNight - (resolvedCount + ActiveAnomalyCount));
            int toSpawn = Mathf.Min(count, allow, remainToTarget);

            for (int i = 0; i < candidates.Count && spawned < toSpawn; i++)
            {
                var p = candidates[i];
                p.ActivateRandom();
                activeIds.Add(p.pointId);
                spawnTimes[p.pointId] = Time.time;
                lastActivatedPointId = p.pointId;
                spawned++;
            }

            if (spawned > 0)
            {
                if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, (audioPoint ? audioPoint : transform).position);
                EventBus.Publish(new AnomalySpawnBatchEvent(spawned, ActiveAnomalyCount, targetForNight));
                Debug.Log($"[AnomalyManager] Spawn points +{spawned} Active={ActiveAnomalyCount}/{targetForNight} (Candidates={candidates.Count})");
            }

            TryCompleteNight_PointMode();
            return spawned;
        }

        // Public helper: spawn จากชุดจุดที่อนุญาต (เคารพกติกาทั้งหมด)
        public int TrySpawnFromAllowed(IList<AnomalyPoint> allowed, int count)
        {
            if (!nightRunning || count <= 0) return 0;
            if (!IsPointMode) return 0;
            if (allowed == null || allowed.Count == 0) return 0;

            // สร้าง candidates จาก allowed โดยใช้เงื่อนไขปัจจุบัน
            List<AnomalyPoint> candidates = new();
            float now = Time.time;

            foreach (var p in allowed)
            {
                if (!p) continue;
                if (p.IsActive) continue;
                if (!p.CanActivate(now)) continue;
                if (activeIds.Contains(p.pointId)) continue;
                candidates.Add(p);
            }
            if (candidates.Count == 0) return 0;

            // เลี่ยงตำแหน่งเดิมซ้ำทันทีถ้ามีตัวเลือกอื่น
            if (avoidImmediateRepeatPoint && !string.IsNullOrEmpty(lastActivatedPointId))
            {
                bool hasOthers = false;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].pointId != lastActivatedPointId) { hasOthers = true; break; }
                }
                if (hasOthers)
                {
                    candidates.RemoveAll(p => p.pointId == lastActivatedPointId);
                    if (candidates.Count == 0) return 0;
                }
            }

            // สุ่มลำดับ
            for (int i = 0; i < candidates.Count; i++)
            {
                int j = Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int spawned = 0;
            int allowSlots = Mathf.Max(0, maxConcurrentActive - ActiveAnomalyCount);
            int remainToTarget = Mathf.Max(0, targetForNight - (resolvedCount + ActiveAnomalyCount));
            int toSpawn = Mathf.Min(count, allowSlots, remainToTarget);

            for (int i = 0; i < candidates.Count && spawned < toSpawn; i++)
            {
                var p = candidates[i];
                p.ActivateRandom();
                activeIds.Add(p.pointId);
                spawnTimes[p.pointId] = Time.time;
                lastActivatedPointId = p.pointId;
                spawned++;
            }

            if (spawned > 0)
            {
                if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, (audioPoint ? audioPoint : transform).position);
                EventBus.Publish(new AnomalySpawnBatchEvent(spawned, ActiveAnomalyCount, targetForNight));
                Debug.Log($"[AnomalyManager] Triggered spawn +{spawned} Active={ActiveAnomalyCount}/{targetForNight} (Subset={allowed.Count})");
            }

            TryCompleteNight_PointMode();
            return spawned;
        }

        private void TryCompleteNight_PointMode()
        {
            if (nightRunning && resolvedCount >= targetForNight)
            {
                nightRunning = false;
                if (logSummaryAtNightEnd)
                    Debug.Log($"[Metrics] Night done. Resolved={resolvedCount} Avg={AverageResolveTime:0.00}s Total={TotalResolveTime:0.0}s");
                EventBus.Publish(new NightCompletedEvent());
            }
        }

        // ===== โหมดเดิม (Anomaly list) =====
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

            TryCompleteNight_Legacy();
        }

        private void TryCompleteNight_Legacy()
        {
            if (nightRunning && resolvedCount >= targetForNight && ActiveAnomalyCount >= targetForNight)
            {
                nightRunning = false;
                if (logSummaryAtNightEnd)
                    Debug.Log($"[Metrics] Night done. Resolved={resolvedCount} Avg={AverageResolveTime:0.00}s Total={TotalResolveTime:0.0}s");
                EventBus.Publish(new NightCompletedEvent());
            }
        }

        private void ActivateRandomDistinct(int count)
        {
            List<Anomaly> candidates = new();
            foreach (var a in anomalyPool)
            {
                if (a == null) continue;
                if (a.data == null) continue;
                if (activeIds.Contains(a.data.anomalyId)) continue;
                candidates.Add(a);
            }
            if (candidates.Count == 0) return;

            // สุ่มลำดับ candidates
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

        // ===== Event: Resolve =====
        private void OnAnomalyResolved(AnomalyResolvedEvent evt)
        {
            if (!activeIds.Contains(evt.Id)) return;

            resolvedCount++;
            activeIds.Remove(evt.Id);

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

            // เติมตัวใหม่ทันทีตามดีเลย์ (แม้จะปิด timedSpawningEnabled ก็ยังเติมได้เพราะเรียกตรง)
            if (spawnImmediatelyOnResolve && RemainingToActivate > 0)
            {
                Invoke(nameof(DelayedSpawnRefill), resolveRefillDelay);
            }

            if (IsPointMode)
            {
                TryCompleteNight_PointMode();
            }
            else
            {
                TryCompleteNight_Legacy();
            }
        }

        private void DelayedSpawnRefill()
        {
            if (!nightRunning) return;
            if (IsPointMode)
            {
                SpawnPointsBatch(1);
            }
            else
            {
                SpawnBatch();
            }
        }
    }
}