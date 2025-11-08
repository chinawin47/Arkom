using UnityEngine;

namespace ARKOM.Audio
{
 // Ensure AudioManager/VoiceManager exist even when starting Play from any scene
 public static class AudioBootstrap
 {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 private static void EnsureAudioManagers()
 {
 // AudioManager
 if (Object.FindObjectOfType<AudioManager>() == null)
 {
 var go = new GameObject("GameSystems_AudioManager");
 go.AddComponent<AudioManager>();
 }
 // VoiceManager
 if (Object.FindObjectOfType<VoiceManager>() == null)
 {
 var go = new GameObject("GameSystems_VoiceManager");
 go.AddComponent<VoiceManager>();
 }
 }
 }
}
