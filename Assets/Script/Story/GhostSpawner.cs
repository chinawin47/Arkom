using UnityEngine;
using ARKOM.Core;
using ARKOM.Player;

namespace ARKOM.Story
{
    public class GhostSpawner : MonoBehaviour
    {
        public Transform[] spawnPoints; // 8 จุด
        public GameObject[] ghostPrefabs; // อย่างน้อย 1
        public AudioClip spawnSfx;

        [Header("Despawn Settings")]
        [Tooltip("เวลาที่ผู้เล่นต้องจ้อง (วินาที) ก่อนผีจะหายไป (<=0 = หายทันทีเมื่อถูกมอง)")] public float lookDespawnTime = 5f;
        [Tooltip("องศาสูงสุดระหว่าง forward กล้อง กับทิศไปยังผี")] public float lookAngleThreshold = 18f;
        [Tooltip("ถ้า true ต้องจ้องต่อเนื่องไม่หลุด (เฉพาะเมื่อ lookDespawnTime > 0)")] public bool requireContinuousLook = true;
        [Tooltip("กำหนดเวลาสูงสุดที่ผีอยู่ได้ (0 = ไม่จำกัด)")] public float hardTimeout = 0f;

        private bool spawned;
        private GameObject activeGhost;
        private float accumulatedLook;
        private float aliveTime;
        private Camera playerCam;

        public void SpawnRandom()
        {
            if (spawned) return;
            spawned = true;
            if (spawnPoints.Length == 0 || ghostPrefabs.Length == 0)
            {
                EventBus.Publish(new GhostSpawnedEvent(-1));
                return;
            }
            int pointIndex = Random.Range(0, spawnPoints.Length);
            int prefabIndex = Random.Range(0, ghostPrefabs.Length);
            var p = spawnPoints[pointIndex];
            activeGhost = Instantiate(ghostPrefabs[prefabIndex], p.position, p.rotation);
            if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, p.position);
            EventBus.Publish(new GhostSpawnedEvent(pointIndex));
            playerCam = FindPlayerCamera();
            if (activeGhost) StartCoroutine(LookDespawnRoutine());
        }

        private Camera FindPlayerCamera()
        {
            // พยายามจาก PlayerController ก่อน ถ้าไม่มีใช้ Camera.main
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
            bool instantMode = lookDespawnTime <= 0f; // หายทันทีเมื่อถูกมอง
            while (activeGhost)
            {
                aliveTime += Time.deltaTime;
                if (hardTimeout > 0f && aliveTime >= hardTimeout) break; // timeout hard

                if (playerCam)
                {
                    bool looking = IsLookingAtGhost();
                    if (instantMode)
                    {
                        if (looking) break; // มองปุ๊บหาย
                    }
                    else
                    {
                        if (looking)
                        {
                            accumulatedLook += Time.deltaTime;
                            if (accumulatedLook >= lookDespawnTime) break;
                        }
                        else if (requireContinuousLook)
                        {
                            accumulatedLook = 0f; // รีเซ็ตถ้าต้องต่อเนื่องและหลุดมอง
                        }
                    }
                }
                yield return null;
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
