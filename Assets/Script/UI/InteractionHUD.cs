using UnityEngine;
using UnityEngine.UI;
using ARKOM.Player;

public class InteractionHUD : MonoBehaviour
{
    public PlayerController player;
    public Image crosshair;
    public Text promptText;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.cyan;
    public string keyHint = "E";

    private void Awake()
    {
        if (!player)
            player = FindObjectOfType<PlayerController>();

        if (promptText) promptText.text = "";
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
        if (crosshair)
            crosshair.color = has ? highlightColor : normalColor;

        if (promptText)
            promptText.text = has ? $"{keyHint}: {target.DisplayName}" : "";
    }
}