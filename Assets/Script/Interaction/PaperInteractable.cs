using UnityEngine;
using ARKOM.Story;

public class PaperInteractable : Interactable
{
    [Header("Note Data")]
    public NoteData note;

    [Header("UI")]
    [Tooltip("ปล่อยว่างเพื่อตามหาในซีนอัตโนมัติ")]
    public NoteViewerHUD viewer;

    [Header("Story Progression")]
    [Tooltip("ติ๊กเพื่อแจ้งลำดับเรื่องว่าอ่านไดอารี่อแล้ว (ใช้คู่กับ requireDiaryBeforeOoy)")]
    public bool notifyDiaryRead = true;

    public override bool CanInteract(object interactor)
    {
        return base.CanInteract(interactor) && note != null;
    }

    protected override void OnInteract(object interactor)
    {
        if (!note) return;

        if (!viewer) viewer = Object.FindObjectOfType<NoteViewerHUD>();
        if (!viewer)
        {
            Debug.LogWarning("[PaperInteractable] NoteViewerHUD not found in scene.");
            return;
        }

        viewer.Show(note);

        // ปลดล็อก player เมื่อปิดโน้ต
        viewer.OnClose += () =>
        {
            if (SequenceController.Instance.player != null)
                SequenceController.Instance.player.enabled = true;
        };

        // แจ้ง SequenceController ว่าอ่านโน้ตแล้ว
        if (notifyDiaryRead && SequenceController.Instance)
        {
            SequenceController.Instance.NotifyDiaryRead();
        }

        // อัปเดต story flag
        if (!string.IsNullOrEmpty(note.flagOnRead) && StoryFlags.Instance)
        {
            StoryFlags.Instance.Add(note.flagOnRead);
        }
    }
}