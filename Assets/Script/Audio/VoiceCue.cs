using UnityEngine;

namespace ARKOM.Audio
{
 [CreateAssetMenu(menuName = "Audio/Voice Cue", fileName = "VoiceCue")] 
 public class VoiceCue : ScriptableObject
 {
 [System.Serializable]
 public struct SubtitleLine
 {
 [TextArea(2,3)] public string text;
 [Tooltip("เวลาเริ่ม (วินาที) นับจากต้นคลิป")] public float start;
 [Tooltip("ความยาวบรรทัด (วินาที)")] public float duration;
 }
 
 [Header("Audio")]
 public AudioClip clip;
 [Tooltip("เล่นแบบ3D หรือไม่ (ปกติ VO ใช้2D)")] public bool play3D = false;
 [Range(0f,1f)] public float volume =1f;
 public float minDistance =1f;
 public float maxDistance =15f;
 
 [Header("Subtitles")]
 public SubtitleLine[] lines;
 }
}
