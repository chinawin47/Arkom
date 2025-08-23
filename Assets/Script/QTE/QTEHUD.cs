using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ARKOM.Core;
using ARKOM.QTE;

public class QTEHUD : MonoBehaviour
{
    [Header("References")]
    public QTEManager qteManager;
    public Text keyText;
    public Image timerFill; // optional radial/filled image

    [Header("Formatting")]
    public string promptPrefix = "PRESS";
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    private bool showing;

    private void Awake()
    {
        if (!qteManager)
            qteManager = FindObjectOfType<QTEManager>();

        SetVisible(false);
        EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
    }

    private void Update()
    {
        if (!qteManager || !qteManager.IsActive)
            return;

        if (!showing)
            SetVisible(true);

        // Update key label
        var key = qteManager.CurrentExpectedKey;
        keyText.color = normalColor;
        keyText.text = $"{promptPrefix} {FormatKey(key)} ({qteManager.CurrentIndex + 1}/{qteManager.TotalLength})";

        // Update timer UI
        if (timerFill)
        {
            float t = Mathf.Clamp01(qteManager.RemainingTime / qteManager.timePerKey);
            timerFill.fillAmount = t;
        }
    }

    private void OnStateChanged(GameStateChangedEvent e)
    {
        if (e.State == GameState.QTE)
        {
            // QTEManager.StartQTE() will handle activation; UI will show next Update
        }
        else
        {
            SetVisible(false);
        }
    }

    private void OnQTEResult(QTEResultEvent e)
    {
        if (!keyText) return;
        keyText.color = e.Success ? successColor : failColor;
        keyText.text = e.Success ? "SUCCESS" : "FAILED";
        Invoke(nameof(HideAfterResult), 1.0f);
    }

    private void HideAfterResult()
    {
        SetVisible(false);
    }

    private void SetVisible(bool value)
    {
        showing = value;
        if (keyText) keyText.enabled = value;
        if (timerFill) timerFill.enabled = value;
    }

    private string FormatKey(Key key)
    {
        if (key == Key.None) return "-";
        // Space special case
        if (key == Key.Space) return "SPACE";
        return key.ToString().ToUpperInvariant();
    }
}