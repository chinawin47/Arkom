using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Note Reveal Controller")]
 public class NoteRevealController : MonoBehaviour
 {
 [Header("Targets")]
 [Tooltip("ถ้าเว้นว่าง ระบบจะค้นหา PaperInteractable ทั้งซีนตาม flag ด้านล่าง")] public PaperInteractable[] specificNotes;
 [Tooltip("ค้นหาโน้ตที่มี flagOnRead ตรงชื่อในลิสต์นี้")] public string[] targetFlags = new string[] { "NoteA", "NoteB", "NoteC" };
 [Tooltip("ซ่อนทันทีเมื่อเริ่มซีน")] public bool hideOnStart = true;

 [Header("Reveal Triggers")]
 [Tooltip("แสดงโน้ตเมื่อเริ่มฉากผีไล่ (หลังคัตเอ้าท์ใช้ไม่ได้)")] public bool revealOnBreakerFailed = true;
 [Tooltip("แสดงโน้ตเมื่อเข้าสู่สถานะ FindNotes")] public bool revealOnFindNotesState = true;

 [Header("Optional")] public float revealDelay =0f;

 private bool revealed;

 void OnEnable()
 {
 EventBus.Subscribe<BreakerFailedEvent>(OnBreakerFailed);
 EventBus.Subscribe<StoryStateChangedEvent>(OnStoryStateChanged);
 }
 void OnDisable()
 {
 EventBus.Unsubscribe<BreakerFailedEvent>(OnBreakerFailed);
 EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryStateChanged);
 }

 void Start()
 {
 if (hideOnStart) HideTargets();
 }

 private void OnBreakerFailed(BreakerFailedEvent _)
 {
 if (!revealOnBreakerFailed) return;
 RevealTargets();
 }

 private void OnStoryStateChanged(StoryStateChangedEvent e)
 {
 if (!revealOnFindNotesState) return;
 if (e.Current == SequenceController.StoryState.FindNotes)
 {
 RevealTargets();
 }
 }

 private IEnumerable<PaperInteractable> GetTargets()
 {
 if (specificNotes != null && specificNotes.Length >0)
 {
 foreach (var p in specificNotes) if (p) yield return p;
 yield break;
 }
 var all = FindObjectsOfType<PaperInteractable>(includeInactive: true);
 if (targetFlags == null || targetFlags.Length ==0)
 {
 foreach (var p in all) if (p && p.note) yield return p;
 yield break;
 }
 var flagsSet = new System.Collections.Generic.HashSet<string>(targetFlags);
 foreach (var p in all)
 {
 if (!p || !p.note) continue;
 if (!string.IsNullOrEmpty(p.note.flagOnRead) && flagsSet.Contains(p.note.flagOnRead))
 yield return p;
 }
 }

 private void HideTargets()
 {
 foreach (var p in GetTargets())
 {
 if (p && p.gameObject.activeSelf)
 p.gameObject.SetActive(false);
 }
 revealed = false;
 }

 private void RevealTargets()
 {
 if (revealed) return;
 if (revealDelay <=0f)
 {
 DoReveal();
 }
 else
 {
 StopAllCoroutines();
 StartCoroutine(DelayedReveal());
 }
 }

 private System.Collections.IEnumerator DelayedReveal()
 {
 yield return new WaitForSeconds(revealDelay);
 DoReveal();
 }

 private void DoReveal()
 {
 foreach (var p in GetTargets())
 {
 if (p && !p.gameObject.activeSelf)
 p.gameObject.SetActive(true);
 }
 revealed = true;
 }
 }
}
