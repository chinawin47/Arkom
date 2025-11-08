using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

namespace ARKOM.Audio
{
 [AddComponentMenu("Audio/Story Audio Controller")] public class StoryAudioController : MonoBehaviour
 {
 [System.Serializable]
 public class StateConfig
 {
 public SequenceController.StoryState state;
 [Header("Emitters to Enable on Enter")] public List<RandomOneShotEmitter> enableEmitters = new List<RandomOneShotEmitter>();
 [Header("Emitters to Disable on Enter")] public List<RandomOneShotEmitter> disableEmitters = new List<RandomOneShotEmitter>();
 [Header("One-shot on Enter")] public AudioClip onEnterSfx; public bool sfx3D = false; [Range(0f,1f)] public float sfxVolume =1f;
 public Transform sfxAt; public float minDistance =1f; public float maxDistance =20f;
 }

 [Tooltip("แมปรายสถานะเนื้อเรื่อง -> การเปิด/ปิด Emitter + เล่น SFX ครั้งเดียวตอนเข้า")]
 public List<StateConfig> states = new List<StateConfig>();
 [Tooltip("ใช้คอนฟิกของสถานะที่ตรงกันตัวแรกที่พบ (ถ้า false จะใช้ทุกรายการที่ตรง)")]
 public bool firstMatchOnly = false;

 private void Reset()
 {
 // auto fill sfxAt to this transform
 if (states != null)
 {
 foreach (var s in states)
 {
 if (s != null && !s.sfxAt) s.sfxAt = transform;
 }
 }
 }
 private void Awake()
 {
 // ensure sfxAt defaults
 foreach (var s in states) if (s != null && !s.sfxAt) s.sfxAt = transform;
 }
 private void OnEnable()
 {
 EventBus.Subscribe<StoryStateChangedEvent>(OnStoryState);
 // apply current state immediately if available
 var seq = SequenceController.Instance;
 if (seq != null) Apply(seq.CurrentState);
 }
 private void OnDisable()
 {
 EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryState);
 }
 private void OnStoryState(StoryStateChangedEvent e) => Apply(e.Current);

 public void Apply(SequenceController.StoryState state)
 {
 if (states == null || states.Count ==0) return;
 bool appliedAny = false;
 for (int i =0; i < states.Count; i++)
 {
 var cfg = states[i]; if (cfg == null) continue;
 if (cfg.state != state) continue;
 appliedAny = true;
 // enable emitters
 if (cfg.enableEmitters != null)
 {
 foreach (var em in cfg.enableEmitters)
 {
 if (!em) continue;
 if (!em.gameObject.activeSelf) em.gameObject.SetActive(true);
 em.enabled = true;
 }
 }
 // disable emitters
 if (cfg.disableEmitters != null)
 {
 foreach (var em in cfg.disableEmitters)
 {
 if (!em) continue;
 em.enabled = false;
 }
 }
 // play one-shot
 if (cfg.onEnterSfx && AudioManager.Instance != null)
 {
 var pos = (cfg.sfxAt ? cfg.sfxAt.position : transform.position);
 if (cfg.sfx3D) AudioManager.Instance.Play3D(cfg.onEnterSfx, pos, cfg.sfxVolume,1f,1f, cfg.minDistance, cfg.maxDistance);
 else AudioManager.Instance.Play2D(cfg.onEnterSfx, cfg.sfxVolume,1f,1f);
 }
 if (firstMatchOnly) break;
 }
 // optional: if no config matched, do nothing
 }
 }
}
