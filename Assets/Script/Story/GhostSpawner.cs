using UnityEngine;
using ARKOM.Core;
using ARKOM.Player;
using ARKOM.Enemy; // added
using System.Collections.Generic;

namespace ARKOM.Story
{
    public class GhostSpawner : MonoBehaviour
    {
        public enum GhostKind { Any, NonChasing, Chasing }

        [Tooltip("??????? (Transform) ?????????????????????????")] public Transform[] spawnPoints; // spawn locations
        [Tooltip("Prefab ????????? (??????????????? 1) ?????????????????????????")]
        public GameObject[] ghostPrefabs; // at least1
        [Tooltip("????????????????????")]
        public AudioClip spawnSfx;

        [Header("Despawn Settings")]
        [Tooltip("????????????????????????????????????????? (??????) (<=0 = ?????????????????????????????)")] public float lookDespawnTime = 2f;
        [Tooltip("??????????????? (????) ???????????? (forward) ???????????? '??????????'")] public float lookAngleThreshold = 18f;
        [Tooltip("??????? true ?????????????????????????????????????????? (????????????? lookDespawnTime > 0)")] public bool requireContinuousLook = true;
        [Tooltip("????????????????????????????????????????????? (0 = ??????)")] public float hardTimeout = 0f;

        [Header("Player Voice")]
        [Tooltip("lineId ???????????????????????????????????? (???? despawn)")] public string voiceOnDespawnLineId = "";
        [Tooltip("Override priority ??????????????? (??????????)")] public int voiceOnDespawnPriority = 0;
        [Tooltip("??? override priority ????????????????????????????")] public bool useVoiceOnDespawnPriority = false;

        private bool spawned;
        private GameObject activeGhost;
        private float accumulatedLook;
        private float aliveTime;
        private Camera playerCam;

        public void SpawnRandom() => SpawnRandom(GhostKind.Any);

        public void SpawnRandom(GhostKind kind)
        {
            if (spawned) return;
            spawned = true;
            if (spawnPoints == null || spawnPoints.Length == 0 || ghostPrefabs == null || ghostPrefabs.Length == 0)
            {
                EventBus.Publish(new GhostSpawnedEvent(-1));
                return;
            }
            int pointIndex = Random.Range(0, spawnPoints.Length);
            var prefab = PickPrefab(kind);
            if (!prefab)
            {
                EventBus.Publish(new GhostSpawnedEvent(-1));
                return;
            }
            var p = spawnPoints[pointIndex];
            activeGhost = Instantiate(prefab, p.position, p.rotation);
            if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, p.position);
            EventBus.Publish(new GhostSpawnedEvent(pointIndex));
            playerCam = FindPlayerCamera();

            var chaser = activeGhost ? activeGhost.GetComponent<ChasingGhost>() : null;
            if (!chaser && activeGhost)
                StartCoroutine(LookDespawnRoutine());
        }

        public void SpawnAtIndex(int pointIndex) => SpawnAtIndex(pointIndex, GhostKind.Any);

        public void SpawnAtIndex(int pointIndex, GhostKind kind)
        {
            if (spawned) return;
            spawned = true;
            if (spawnPoints == null || spawnPoints.Length == 0 || ghostPrefabs == null || ghostPrefabs.Length == 0)
            {
                EventBus.Publish(new GhostSpawnedEvent(-1));
                return;
            }
            if (pointIndex < 0 || pointIndex >= spawnPoints.Length)
                pointIndex = 0;
            var prefab = PickPrefab(kind);
            if (!prefab)
            {
                EventBus.Publish(new GhostSpawnedEvent(-1));
                return;
            }
            var p = spawnPoints[pointIndex];
            activeGhost = Instantiate(prefab, p.position, p.rotation);
            if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, p.position);
            EventBus.Publish(new GhostSpawnedEvent(pointIndex));
            playerCam = FindPlayerCamera();

            var chaser = activeGhost ? activeGhost.GetComponent<ChasingGhost>() : null;
            if (!chaser && activeGhost)
                StartCoroutine(LookDespawnRoutine());
        }

        private GameObject PickPrefab(GhostKind kind)
        {
            if (ghostPrefabs == null || ghostPrefabs.Length == 0) return null;
            if (kind == GhostKind.Any)
            {
                int i = Random.Range(0, ghostPrefabs.Length);
                return ghostPrefabs[i];
            }
            bool wantChaser = kind == GhostKind.Chasing;
            var pool = new List<GameObject>(ghostPrefabs.Length);
            foreach (var g in ghostPrefabs)
            {
                if (!g) continue;
                bool hasChaser = g.GetComponent<ChasingGhost>() != null;
                if (wantChaser && hasChaser) pool.Add(g);
                if (!wantChaser && !hasChaser) pool.Add(g);
            }
            if (pool.Count == 0)
            {
                // fallback
                int i = Random.Range(0, ghostPrefabs.Length);
                return ghostPrefabs[i];
            }
            int idx = Random.Range(0, pool.Count);
            return pool[idx];
        }

        private Camera FindPlayerCamera()
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc)
            {
                var cam = pc.GetComponentInChildren<Camera>();
                if (cam) return cam;
            }
            return Camera.main;
        }

        private System.Collections.IEnumerator LookDespawnRoutine()
        {
            accumulatedLook = 0f;
            aliveTime = 0f;
            bool instantMode = lookDespawnTime <= 0f;
            bool willDespawn = false;
            while (activeGhost)
            {
                aliveTime += Time.deltaTime;
                if (hardTimeout > 0f && aliveTime >= hardTimeout) { willDespawn = true; break; }

                if (playerCam)
                {
                    bool looking = IsLookingAtGhost();
                    if (instantMode)
                    {
                        if (looking) { willDespawn = true; break; }
                    }
                    else
                    {
                        if (looking)
                        {
                            accumulatedLook += Time.deltaTime;
                            if (accumulatedLook >= lookDespawnTime) { willDespawn = true; break; }
                        }
                        else if (requireContinuousLook)
                        {
                            accumulatedLook = 0f;
                        }
                    }
                }
                yield return null;
            }

            if (willDespawn)
            {
                // ????????????????????????????
                if (!string.IsNullOrEmpty(voiceOnDespawnLineId))
                {
                    if (useVoiceOnDespawnPriority)
                        EventBus.Publish(new PlayerVoiceRequestEvent(voiceOnDespawnLineId, voiceOnDespawnPriority));
                    else
                        EventBus.Publish(new PlayerVoiceRequestEvent(voiceOnDespawnLineId));
                }
            }

            if (activeGhost)
                Destroy(activeGhost);
        }

        private bool IsLookingAtGhost()
        {
            if (!playerCam || !activeGhost) return false;
            Vector3 toGhost = activeGhost.transform.position - playerCam.transform.position;
            float angle = Vector3.Angle(playerCam.transform.forward, toGhost.normalized);
            return angle <= lookAngleThreshold;
        }
    }
}
