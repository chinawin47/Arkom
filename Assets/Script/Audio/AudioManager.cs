using UnityEngine;

namespace ARKOM.Audio
{
 [AddComponentMenu("Audio/Audio Manager (Pool)")]
 public class AudioManager : MonoBehaviour
 {
 public static AudioManager Instance { get; private set; }

 [Header("Pool Settings")] public int poolSize2D =8; public int poolSize3D =24;
 [Range(0f,1f)] public float masterVolume =1f;
 [Tooltip("ปิด AutoDontDestroyOnLoad ถ้าไม่ต้องการคงอยู่ทุกซีน")] public bool persistAcrossScenes = true;

 private AudioSource[] pool2D; private AudioSource[] pool3D;
 private int idx2D; private int idx3D;

 void Awake()
 {
 if (Instance && Instance != this) { Destroy(gameObject); return; }
 Instance = this;
 if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
 BuildPools();
 }

 private void BuildPools()
 {
 pool2D = new AudioSource[Mathf.Max(1, poolSize2D)];
 for (int i =0; i < pool2D.Length; i++)
 {
 var a = new GameObject("Audio2D_" + i); a.transform.SetParent(transform);
 var src = a.AddComponent<AudioSource>(); src.playOnAwake = false; src.loop = false; src.spatialBlend =0f; src.rolloffMode = AudioRolloffMode.Linear; pool2D[i] = src;
 }
 pool3D = new AudioSource[Mathf.Max(1, poolSize3D)];
 for (int i =0; i < pool3D.Length; i++)
 {
 var a = new GameObject("Audio3D_" + i); a.transform.SetParent(transform);
 var src = a.AddComponent<AudioSource>(); src.playOnAwake = false; src.loop = false; src.spatialBlend =1f; src.rolloffMode = AudioRolloffMode.Linear; pool3D[i] = src;
 }
 }

 private AudioSource Next2D() { idx2D = (idx2D +1) % pool2D.Length; return pool2D[idx2D]; }
 private AudioSource Next3D() { idx3D = (idx3D +1) % pool3D.Length; return pool3D[idx3D]; }

 public AudioSource Play2D(AudioClip clip, float volume =1f, float pitchMin =1f, float pitchMax =1f)
 {
 if (!clip || pool2D == null) return null; var s = Next2D(); s.Stop(); s.clip = clip; s.volume = volume * masterVolume; s.pitch = Random.Range(pitchMin, pitchMax); s.Play(); return s;
 }
 public AudioSource Play3D(AudioClip clip, Vector3 pos, float volume =1f, float pitchMin =1f, float pitchMax =1f, float minDist =1f, float maxDist =25f)
 {
 if (!clip || pool3D == null) return null; var s = Next3D(); s.transform.position = pos; s.Stop(); s.clip = clip; s.volume = volume * masterVolume; s.pitch = Random.Range(pitchMin, pitchMax); s.minDistance = minDist; s.maxDistance = maxDist; s.Play(); return s;
 }

 public void SetMasterVolume(float v)
 {
 masterVolume = Mathf.Clamp01(v);
 // realtime adjust current playing sources
 if (pool2D != null) foreach (var s in pool2D) if (s) s.volume = Mathf.Clamp01(s.volume) * masterVolume;
 if (pool3D != null) foreach (var s in pool3D) if (s) s.volume = Mathf.Clamp01(s.volume) * masterVolume;
 }
 }
}
