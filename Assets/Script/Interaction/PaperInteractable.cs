using UnityEngine;
using ARKOM.Story;
using UnityEngine.InputSystem;

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

    [Header("Controls")]
    [Tooltip("กดปุ่มนี้เพื่อปิดโน้ต (ค่าเริ่มต้น E เหมือน Interact)")]
    public Key closeKey = Key.E;

    // remember cursor state in case HUD doesn't restore
    private CursorLockMode prevLock;
    private bool prevCursorVisible;

    // track current viewing state to allow toggle-close with E
    private bool isViewing;
    // require a release after opening before allowing close to avoid instant close on same key press
    private bool closeReady;

    public override bool CanInteract(object interactor)
    {
        return base.CanInteract(interactor) && note != null;
    }

    private void Update()
    {
        if (!isViewing || viewer == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        // become ready to close after the key is no longer held (release after open)
        if (!closeReady && !kb[closeKey].isPressed)
            closeReady = true;
        // allow pressing E again to close while viewer is open
        if (closeReady && kb[closeKey].wasPressedThisFrame)
        {
            viewer.Close();
        }
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

        // If already viewing, toggle to close on interact (only if ready)
        if (isViewing)
        {
            if (closeReady)
                viewer.Close();
            return;
        }

        // cache current cursor state (fallback)
        prevLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        // ปิดการควบคุมผู้เล่นระหว่างอ่านโน้ต
        if (SequenceController.Instance && SequenceController.Instance.player)
            SequenceController.Instance.player.enabled = false;

        // ensure single subscription to close event
        viewer.OnClose -= HandleNoteClosed;
        viewer.OnClose += HandleNoteClosed;

        viewer.Show(note);
        isViewing = true;
        closeReady = false; // wait for key release before allowing close

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

    private void HandleNoteClosed()
    {
        isViewing = false;
        closeReady = false;
        // ปลดล็อก player
        if (SequenceController.Instance && SequenceController.Instance.player)
            SequenceController.Instance.player.enabled = true;

        // บังคับ lock cursor กลับเพื่อให้เมาส์โฟกัสเกมและเดินได้
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ป้องกันเกมค้าง pause ถ้า HUD ไม่ได้คืนค่าเอง
        if (Time.timeScale < 0.99f)
            Time.timeScale = 1f;

        // ยกเลิก subscription เพื่อไม่ให้ซ้อน
        if (viewer)
            viewer.OnClose -= HandleNoteClosed;
    }
}