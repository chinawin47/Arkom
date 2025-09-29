using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    public class GhostSpawner : MonoBehaviour
    {
        public Transform[] spawnPoints; // 8 จุด
        public GameObject[] ghostPrefabs; // อย่างน้อย 1
        public AudioClip spawnSfx;
        public float autoDespawnAfter = 5f;

        private bool spawned;

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
            var g = Instantiate(ghostPrefabs[prefabIndex], p.position, p.rotation);
            if (spawnSfx) AudioSource.PlayClipAtPoint(spawnSfx, p.position);
            EventBus.Publish(new GhostSpawnedEvent(pointIndex));
            if (autoDespawnAfter > 0f)
                Destroy(g, autoDespawnAfter);
        }
    }
}
