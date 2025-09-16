using UnityEngine;
using ARKOM.Story;

public class PaperInteractable : Interactable
{
    [Header("Note Data")]
    public NoteData note;

    [Header("UI")]
    [Tooltip("ปล่อยว่างเพื่อตามหาในซีนอัตโนมัติ")]
    public NoteViewerHUD viewer;

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
    }
}