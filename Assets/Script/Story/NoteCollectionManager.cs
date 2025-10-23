using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Note Collection Manager")]
 public class NoteCollectionManager : MonoBehaviour
 {
 [Header("Drag Notes (optional)")]
 [Tooltip("ลากโน้ตที่ต้องการนับมาวางตรงนี้ แทนการพิมพ์ชื่อ flag")] public PaperInteractable[] requiredNotes;

 [Header("Required Flags (3 notes)")]
 [Tooltip("ถ้าไม่ลากโน้ต ระบบจะใช้รายชื่อ flag นี้")] public string[] requiredFlags = new string[] { "NoteA", "NoteB", "NoteC" };
 public bool autoPublishOnComplete = true;
 public bool debugLog = false;

 private int collected;
 private bool completed;

 void OnEnable()
 {
 RefreshFlagsFromNotes();
 EventBus.Subscribe<StoryFlagAddedEvent>(OnFlagAdded);
 Recount();
 }
 void OnDisable()
 {
 EventBus.Unsubscribe<StoryFlagAddedEvent>(OnFlagAdded);
 }

 private void OnValidate()
 {
 // อัปเดตอัตโนมัติเมื่อแก้ค่าบน Inspector (Editor เท่านั้น)
 RefreshFlagsFromNotes();
 }

 private void RefreshFlagsFromNotes()
 {
 if (requiredNotes == null || requiredNotes.Length ==0) return;
 int n = requiredNotes.Length;
 var flags = new System.Collections.Generic.List<string>(n);
 for (int i =0; i < n; i++)
 {
 var p = requiredNotes[i];
 if (!p || p.note == null) continue;
 var f = p.note.flagOnRead;
 if (!string.IsNullOrEmpty(f)) flags.Add(f);
 }
 if (flags.Count >0)
 {
 requiredFlags = flags.ToArray();
 if (debugLog) Debug.Log($"[NoteCollectionManager] Derived flags from notes: {string.Join(",", requiredFlags)}", this);
 }
 }

 private void OnFlagAdded(StoryFlagAddedEvent e)
 {
 if (completed) return;
 for (int i =0; i < requiredFlags.Length; i++)
 {
 if (!string.IsNullOrEmpty(requiredFlags[i]) && e.Flag == requiredFlags[i])
 {
 if (debugLog) Debug.Log($"[NoteCollectionManager] Flag set: {e.Flag}", this);
 Recount();
 return;
 }
 }
 }

 private void Recount()
 {
 int count =0;
 for (int i =0; i < requiredFlags.Length; i++)
 {
 var f = requiredFlags[i];
 if (string.IsNullOrEmpty(f)) continue;
 if (StoryFlags.Instance && StoryFlags.Instance.Has(f)) count++;
 }
 collected = count;
 if (!completed && requiredFlags != null && requiredFlags.Length >0 && collected >= requiredFlags.Length)
 {
 completed = true;
 if (debugLog) Debug.Log("[NoteCollectionManager] All notes collected", this);
 if (autoPublishOnComplete)
 {
 EventBus.Publish(new AllNotesFoundEvent());
 }
 }
 }
 }
}
