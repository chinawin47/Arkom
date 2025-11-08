using UnityEngine;
using System.Collections.Generic;

namespace ARKOM.Audio
{
 [AddComponentMenu("Audio/Random OneShot Emitter")] public class RandomOneShotEmitter : MonoBehaviour
 {
 [System.Serializable]
 public class ClipEntry { public AudioClip clip; [Range(0f,1f)] public float weight =1f; public float volume =1f; public Vector2 pitchRange = new Vector2(0.98f,1.02f); }

 public enum SpawnMode { SinglePoint, SpawnPoints, BoxVolume, AroundPlayer }

 [Header("Clips")] public List<ClipEntry> clips = new List<ClipEntry>();

 [Header("Timing")] public Vector2 intervalRange = new Vector2(4f,12f); public bool playOnEnable = true; public float playProbability =1f; public float postPlaySilence =0f; public bool delayOnStart = true; public Vector2 initialDelayRange = new Vector2(5f,15f);
 public bool use3D = true; public float minDistance =1f; public float maxDistance =25f;

 [Header("Spawn Mode")] public SpawnMode spawnMode = SpawnMode.SinglePoint;
 [System.Serializable] public class SpawnPoint { public Transform point; [Range(0f,1f)] public float weight =1f; }
 [Tooltip("ใช้เมื่อ SpawnMode = SpawnPoints")] public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

 [Header("BoxVolume (SpawnMode)")] public Vector3 areaSize; public bool randomInsideArea = false;

 [Header("AroundPlayer (SpawnMode)")] public bool useAroundPlayer = false; // legacy toggle (ignored if spawnMode overrides)
 [Tooltip("รัศมีขั้นต่ำ-สูงสุดจากผู้เล่น")] public Vector2 aroundPlayerRadius = new Vector2(6f,10f);
 [Tooltip("เลเยอร์พื้นสำหรับปรับระดับ Y ด้วย Raycast (ถ้าเป็น0 จะไม่ยิง)")] public LayerMask groundRayMask = ~0;

 [Header("Filters")] public bool requirePlayerNear = false; public float playerRadius =15f;
 [Tooltip("ชื่อ Tag ผู้เล่น (เว้นว่างจะค้น PlayerController เอง)")] public string playerTag;

 [Header("Occlusion (3D)")] public bool occlusion = true; [Range(0f,1f)] public float occludedVolumeFactor =0.6f; public LayerMask occlusionMask = ~0;

 private float nextTime; private Transform player; private int lastClipIndex = -1; private float nextIntervalOverride;

 void OnEnable()
 {
 if (delayOnStart)
 {
 nextTime = Time.time + Random.Range(initialDelayRange.x, initialDelayRange.y);
 }
 else
 {
 if (playOnEnable) nextTime = Time.time; else Schedule();
 }
 }
 void Update()
 {
 if (Time.time >= nextTime) { TryPlay(); Schedule(); }
 }
 private void Schedule()
 {
 if (nextIntervalOverride >0f)
 {
 nextTime = Time.time + nextIntervalOverride;
 nextIntervalOverride =0f;
 return;
 }
 nextTime = Time.time + Random.Range(intervalRange.x, intervalRange.y);
 }

 private void TryPlay()
 {
 if (clips == null || clips.Count ==0) return;

 // player ref (optional)
 if (requirePlayerNear || spawnMode == SpawnMode.AroundPlayer || useAroundPlayer)
 {
 EnsurePlayerRef();
 if (!player) return;
 }

 if (requirePlayerNear && player)
 {
 if (Vector3.Distance(transform.position, player.position) > playerRadius) return;
 }

 // probability gate
 if (playProbability <1f && Random.value > Mathf.Clamp01(playProbability))
 {
 // skip this cycle but keep schedule based on intervalRange
 return;
 }

 var entry = PickClip(); if (entry == null || !entry.clip) return;

 // choose position by spawn mode
 float volMul =1f; Vector3 pos = GetSpawnPosition(ref volMul);

 if (AudioManager.Instance)
 {
 float vol = entry.volume * volMul;
 if (use3D) AudioManager.Instance.Play3D(entry.clip, pos, vol, entry.pitchRange.x, entry.pitchRange.y, minDistance, maxDistance);
 else AudioManager.Instance.Play2D(entry.clip, vol, entry.pitchRange.x, entry.pitchRange.y);
 }

 // enforce post-play silence if configured
 if (postPlaySilence >0f) nextIntervalOverride = postPlaySilence;
 }

 private void EnsurePlayerRef()
 {
 if (player) return;
 if (!string.IsNullOrEmpty(playerTag)) { var go = GameObject.FindGameObjectWithTag(playerTag); if (go) { player = go.transform; return; } }
 var pc = FindObjectOfType<ARKOM.Player.PlayerController>(); if (pc) player = pc.transform;
 }

 private ClipEntry PickClip()
 {
 float total =0f; for (int i =0; i < clips.Count; i++) { var c = clips[i]; if (c != null && c.clip) total += Mathf.Max(0f, c.weight); }
 if (total <=0f) return null;
 // weighted pick
 ClipEntry chosen = null; int safety =0; int chosenIndex = -1;
 do
 {
 float r = Random.value * total; float acc =0f; chosen = null; chosenIndex = -1;
 for (int i =0; i < clips.Count; i++)
 {
 var c = clips[i]; if (c == null || !c.clip) continue; acc += Mathf.Max(0f, c.weight); if (r <= acc) { chosen = c; chosenIndex = i; break; }
 }
 safety++;
 } while (clips.Count >1 && chosenIndex == lastClipIndex && safety <4);
 lastClipIndex = chosenIndex;
 return chosen ?? clips[clips.Count -1];
 }

 private Vector3 GetSpawnPosition(ref float volumeMul)
 {
 Vector3 pos = transform.position;
 var mode = spawnMode;
 if (mode == SpawnMode.SinglePoint && useAroundPlayer) mode = SpawnMode.AroundPlayer; // backward compat

 switch (mode)
 {
 case SpawnMode.SinglePoint:
 pos = transform.position; break;
 case SpawnMode.SpawnPoints:
 pos = PickSpawnPointPosition(); break;
 case SpawnMode.BoxVolume:
 pos = transform.position;
 if (randomInsideArea && areaSize != Vector3.zero)
 {
 pos += new Vector3(Random.Range(-areaSize.x *0.5f, areaSize.x *0.5f), Random.Range(-areaSize.y *0.5f, areaSize.y *0.5f), Random.Range(-areaSize.z *0.5f, areaSize.z *0.5f));
 }
 break;
 case SpawnMode.AroundPlayer:
 if (player)
 {
 float r = Random.Range(Mathf.Min(aroundPlayerRadius.x, aroundPlayerRadius.y), Mathf.Max(aroundPlayerRadius.x, aroundPlayerRadius.y));
 Vector2 dir2 = Random.insideUnitCircle.normalized; Vector3 dir = new Vector3(dir2.x,0f, dir2.y);
 pos = player.position + dir * r;
 // align to ground if mask provided
 if (groundRayMask.value !=0)
 {
 if (Physics.Raycast(pos + Vector3.up *2f, Vector3.down, out var hit,5f, groundRayMask, QueryTriggerInteraction.Ignore))
 pos = hit.point;
 }
 }
 break;
 }

 // simple occlusion -> reduce volume if blocked
 if (occlusion && use3D && player)
 {
 Vector3 origin = player.position + Vector3.up *1.6f;
 Vector3 to = pos - origin; float dist = to.magnitude;
 if (dist >0.1f)
 {
 if (Physics.Raycast(origin, to / dist, dist, occlusionMask, QueryTriggerInteraction.Ignore))
 {
 volumeMul *= occludedVolumeFactor;
 }
 }
 }
 return pos;
 }

 private Vector3 PickSpawnPointPosition()
 {
 if (spawnPoints == null || spawnPoints.Count ==0) return transform.position;
 float total =0f; for (int i =0; i < spawnPoints.Count; i++) { var p = spawnPoints[i]; if (p != null && p.point) total += Mathf.Max(0f, p.weight); }
 if (total <=0f) return transform.position;
 float r = Random.value * total; float acc =0f;
 for (int i =0; i < spawnPoints.Count; i++)
 {
 var p = spawnPoints[i]; if (p == null || !p.point) continue; acc += Mathf.Max(0f, p.weight); if (r <= acc) return p.point.position;
 }
 return transform.position;
 }

 #if UNITY_EDITOR
 private void OnDrawGizmosSelected()
 {
 Gizmos.color = Color.cyan;
 Gizmos.DrawWireSphere(transform.position,0.25f);

 if (spawnMode == SpawnMode.BoxVolume && areaSize != Vector3.zero)
 {
 Gizmos.color = new Color(0f,0.6f,1f,0.25f);
 Gizmos.DrawWireCube(transform.position, areaSize);
 }
 if (spawnMode == SpawnMode.SpawnPoints && spawnPoints != null)
 {
 foreach (var sp in spawnPoints)
 {
 if (sp == null || !sp.point) continue;
 float t = Mathf.Clamp01(sp.weight);
 Gizmos.color = Color.Lerp(Color.red, Color.green, t);
 Gizmos.DrawWireSphere(sp.point.position,0.3f);
 #if UNITY_EDITOR
 UnityEditor.Handles.color = Gizmos.color;
 UnityEditor.Handles.Label(sp.point.position + Vector3.up *0.35f, $"w={sp.weight:0.00}");
 #endif
 }
 }
 if ((spawnMode == SpawnMode.AroundPlayer || (spawnMode == SpawnMode.SinglePoint && useAroundPlayer)) && aroundPlayerRadius.x >0f && aroundPlayerRadius.y >0f)
 {
 float inner = Mathf.Min(aroundPlayerRadius.x, aroundPlayerRadius.y);
 float outer = Mathf.Max(aroundPlayerRadius.x, aroundPlayerRadius.y);
 Gizmos.color = new Color(1f,0.5f,0f,0.15f);
 DrawCircle(transform.position, inner,40);
 Gizmos.color = new Color(1f,0.2f,0f,0.25f);
 DrawCircle(transform.position, outer,40);
 }
 }
 private void DrawCircle(Vector3 center, float radius, int segments)
 {
 if (radius <=0f) return;
 float step =2f * Mathf.PI / segments; Vector3 prev = center + new Vector3(Mathf.Cos(0f),0f, Mathf.Sin(0f)) * radius;
 for (int i =1; i <= segments; i++)
 {
 float a = i * step; Vector3 next = center + new Vector3(Mathf.Cos(a),0f, Mathf.Sin(a)) * radius;
 Gizmos.DrawLine(prev, next); prev = next;
 }
 }
 #endif
 }
}
