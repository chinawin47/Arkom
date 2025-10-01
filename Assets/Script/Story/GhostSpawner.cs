using UnityEngine;
using ARKOM.Core;
using ARKOM.Player;

namespace ARKOM.Story
{
    public class GhostSpawner : MonoBehaviour
    {
        public Transform[] spawnPoints; // 8 �ش
        public GameObject[] ghostPrefabs; // ���ҧ���� 1
        public AudioClip spawnSfx;

        [Header("Despawn Settings")]
        [Tooltip("���ҷ������蹵�ͧ��ͧ (�Թҷ�) ��͹�ը����� (<=0 = ��·ѹ������Ͷ١�ͧ)")] public float lookDespawnTime = 5f;
        [Tooltip("ͧ���٧�ش�����ҧ forward ���ͧ �Ѻ�����ѧ��")] public float lookAngleThreshold = 18f;
        [Tooltip("��� true ��ͧ��ͧ������ͧ�����ش (੾������� lookDespawnTime > 0)")] public bool requireContinuousLook = true;
        [Tooltip("��˹������٧�ش���������� (0 = ���ӡѴ)")] public float hardTimeout = 0f;

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
            // �������ҡ PlayerController ��͹ ���������� Camera.main
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
            bool instantMode = lookDespawnTime <= 0f; // ��·ѹ������Ͷ١�ͧ
            while (activeGhost)
            {
                aliveTime += Time.deltaTime;
                if (hardTimeout > 0f && aliveTime >= hardTimeout) break; // timeout hard

                if (playerCam)
                {
                    bool looking = IsLookingAtGhost();
                    if (instantMode)
                    {
                        if (looking) break; // �ͧ������
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
                            accumulatedLook = 0f; // ���絶�ҵ�ͧ������ͧ�����ش�ͧ
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
