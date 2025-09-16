using UnityEngine;
using UnityEngine.UI;
using ARKOM.Player;

public class InteractionHUD : MonoBehaviour
{
    public PlayerController player;

    [Header("Crosshair (Base)")]
    public Image crosshair;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.cyan;

    [Header("Crosshair Donut (เลือกอย่างใดอย่างหนึ่ง)")]
    [Tooltip("วิธี A: ใส่ Image วงแหวนซ้อนทับ เปิดเฉพาะตอนเล็งโดน")]
    public Image donutOverlay;
    [Tooltip("วิธี B: ใส่ Sprite โดนัทเพื่อสลับแทนสไปรต์ปกติ")]
    public Sprite normalSprite;
    public Sprite donutSprite;
    [Tooltip("ปรับขนาดเวลาสลับสไปรต์ (ปล่อยว่าง = ไม่เปลี่ยน)")]
    public Vector2 normalSize;
    public Vector2 donutSize;

    [Header("Prompt")]
    public Text promptText;
    public string keyHint = "E";

    private void Awake()
    {
        if (!player)
            player = FindObjectOfType<PlayerController>();

        if (promptText) promptText.text = "";

        // ปิด overlay ตอนเริ่ม
        if (donutOverlay) donutOverlay.enabled = false;

        // ถ้ากำหนด normalSprite ให้ crosshair ใช้เป็นค่าเริ่ม
        if (crosshair && normalSprite)
            crosshair.sprite = normalSprite;

        // ตั้งขนาดเริ่มถ้ามี
        if (crosshair && normalSize != Vector2.zero)
            crosshair.rectTransform.sizeDelta = normalSize;
    }

    private void OnEnable()
    {
        if (player != null)
            player.FocusChanged += OnFocusChanged;
    }

    private void OnDisable()
    {
        if (player != null)
            player.FocusChanged -= OnFocusChanged;
    }

    private void OnFocusChanged(IInteractable target)
    {
        bool has = target != null;

        // สี crosshair (ยังคงพฤติกรรมเดิม)
        if (crosshair)
            crosshair.color = has ? highlightColor : normalColor;

        // วิธี A: Overlay วงแหวน
        if (donutOverlay)
            donutOverlay.enabled = has;

        // วิธี B: Swap Sprite
        if (crosshair && donutSprite && normalSprite && donutOverlay == null)
        {
            crosshair.sprite = has ? donutSprite : normalSprite;

            // ปรับขนาดเมื่อสลับ (ถ้ากำหนด)
            if (has && donutSize != Vector2.zero)
                crosshair.rectTransform.sizeDelta = donutSize;
            else if (!has && normalSize != Vector2.zero)
                crosshair.rectTransform.sizeDelta = normalSize;
        }

        // Prompt
        if (promptText)
            promptText.text = has ? $"{keyHint}: {target.DisplayName}" : "";
    }
}