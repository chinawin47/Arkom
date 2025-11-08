using UnityEngine;
using System.Collections;

namespace ARKOM.Audio
{
 [AddComponentMenu("Audio/Voice Manager")] public class VoiceManager : MonoBehaviour
 {
 public static VoiceManager Instance { get; private set; }
 public AudioSource voiceSource; [Range(0f,1f)] public float defaultVolume =1f; public bool persistAcrossScenes = true;
 private Coroutine playRoutine;
 void Awake()
 {
 if (Instance && Instance != this) { Destroy(gameObject); return; }
 Instance = this; if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
 if (!voiceSource)
 {
 voiceSource = gameObject.AddComponent<AudioSource>(); voiceSource.playOnAwake = false; voiceSource.loop = false; voiceSource.spatialBlend =0f;
 }
 }
 public void StopVoice() { if (playRoutine != null) StopCoroutine(playRoutine); voiceSource.Stop(); playRoutine = null; }
 public void PlayOneShot(AudioClip clip, float volume = -1f)
 {
 if (!clip) return; StopVoice(); voiceSource.clip = clip; voiceSource.volume = (volume <0f ? defaultVolume : volume); voiceSource.Play();
 }
 public void PlayFrom(Transform worldPos, AudioClip clip, float volume =1f, float minDist =1f, float maxDist =15f)
 {
 if (!clip) return; StopVoice(); voiceSource.spatialBlend =1f; voiceSource.transform.position = worldPos.position; voiceSource.minDistance = minDist; voiceSource.maxDistance = maxDist; voiceSource.clip = clip; voiceSource.volume = volume; voiceSource.Play(); voiceSource.spatialBlend =0f; // reset to2D for next
 }
 }
}
