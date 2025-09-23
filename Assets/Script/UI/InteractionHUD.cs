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

    [Header("Crosshair Donut (���͡���ҧ����ҧ˹��)")]
    [Tooltip("�Ը� A: ��� Image ǧ��ǹ��͹�Ѻ �Դ੾�е͹���ⴹ")]
    public Image donutOverlay;
    [Tooltip("�Ը� B: ��� Sprite ⴹѷ������Ѻ᷹��õ컡��")]
    public Sprite normalSprite;
    public Sprite donutSprite;
    [Tooltip("��Ѻ��Ҵ������Ѻ��õ� (�������ҧ = �������¹)")]
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

        // �Դ overlay �͹�����
        if (donutOverlay) donutOverlay.enabled = false;

        // ��ҡ�˹� normalSprite ��� crosshair ���繤�������
        if (crosshair && normalSprite)
            crosshair.sprite = normalSprite;

        // ��駢�Ҵ����������
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

        // �� crosshair (�ѧ���ĵԡ������)
        if (crosshair)
            crosshair.color = has ? highlightColor : normalColor;

        // �Ը� A: Overlay ǧ��ǹ
        if (donutOverlay)
            donutOverlay.enabled = has;

        // �Ը� B: Swap Sprite
        if (crosshair && donutSprite && normalSprite && donutOverlay == null)
        {
            crosshair.sprite = has ? donutSprite : normalSprite;

            // ��Ѻ��Ҵ�������Ѻ (��ҡ�˹�)
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