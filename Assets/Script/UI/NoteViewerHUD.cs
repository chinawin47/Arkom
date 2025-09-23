using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ARKOM.Story;
using TMPro; // add

public class NoteViewerHUD : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    public TMP_Text titleText;  // Text -> TMP_Text
    public TMP_Text bodyText;   // Text -> TMP_Text
    public Image pictureImage;
    public Button closeButton;

    [Header("Behavior")]
    public bool pauseGameOnOpen = true;
    public bool unlockCursorOnOpen = true;

    private float prevTimeScale = 1f;
    private bool prevCursorVisible;
    private CursorLockMode prevLockState;
    private bool isOpen;
    private NoteData currentNote;

    void Awake()
    {
        if (!panel) panel = gameObject;
        panel.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (closeButton) closeButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (!isOpen) return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Show(NoteData note)
    {
        currentNote = note;
        if (titleText) titleText.text = note ? note.title : "";
        if (bodyText) bodyText.text = note ? note.body : "";
        if (pictureImage)
        {
            bool has = note && note.image;
            pictureImage.enabled = has;
            if (has) pictureImage.sprite = note.image;
        }

        if (pauseGameOnOpen)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        if (unlockCursorOnOpen)
        {
            prevCursorVisible = Cursor.visible;
            prevLockState = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        panel.SetActive(true);
        isOpen = true;

        if (note && !string.IsNullOrEmpty(note.flagOnRead))
        {
            var sf = ARKOM.Story.StoryFlags.Instance;
            if (sf) sf.Add(note.flagOnRead);
        }
    }

    public void Close()
    {
        if (!isOpen) return;
        panel.SetActive(false);

        if (pauseGameOnOpen)
            Time.timeScale = prevTimeScale;

        if (unlockCursorOnOpen)
        {
            Cursor.lockState = prevLockState;
            Cursor.visible = prevCursorVisible;
        }

        isOpen = false;
        currentNote = null;
    }
}