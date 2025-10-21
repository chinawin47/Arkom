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
    [Tooltip("ติ๊กเพื่อแจ้งลำดับเรื่องว่าอ่านไดอารี่แล้ว (ใช้คู่กับ requireDiaryBeforeOoy)")]
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

        // แจ้ง SequenceController ว่าอ่านโน้ต/ไดอารี่แล้ว เพื่อปลดล็อคไป FindOoy ถ้าถูกตั้งค่าไว้
        if (notifyDiaryRead && SequenceController.Instance)
        {
            SequenceController.Instance.NotifyDiaryRead();
        }
    }
}