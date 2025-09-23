using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.QTE;

public class QTEHUD : MonoBehaviour
{
    public QTEManager qteManager;
    public Text keyText;
    public Image timerFill;

    public string promptPrefix = "PRESS";
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    public bool fallbackIfNoText = true; // ถ้า keyText ไม่มี ให้สร้าง GUI ชั่วคราว

    private bool showing;

    void Awake()
    {
        if (!qteManager) qteManager = FindObjectOfType<QTEManager>();
        if (keyText) keyText.text = "";
        EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
    }

    void Update()
    {
        if (!qteManager || !qteManager.IsActive) return;

        if (!showing)
            SetVisible(true);

        if (keyText)
        {
            var key = qteManager.CurrentExpectedKey;
            keyText.color = normalColor;
            keyText.text = $"{promptPrefix} {FormatKey(key)} ({qteManager.CurrentIndex + 1}/{qteManager.TotalLength})";
        }

        if (timerFill)
        {
            float t = Mathf.Clamp01(qteManager.RemainingTime / qteManager.timePerKey);
            timerFill.fillAmount = t;
        }
    }

    private void OnStateChanged(GameStateChangedEvent e)
    {
        if (e.State == GameState.QTE)
            ; // รอ Update โชว์
        else
            SetVisible(false);
    }

    private void OnQTEResult(QTEResultEvent e)
    {
        if (keyText)
        {
            keyText.color = e.Success ? successColor : failColor;
            keyText.text = e.Success ? "SUCCESS" : "FAILED";
        }
        Invoke(nameof(HideAfterResult), 1f);
    }

    private void HideAfterResult() => SetVisible(false);

    private void SetVisible(bool v)
    {
        showing = v;
        if (keyText) keyText.enabled = v;
        if (timerFill) timerFill.enabled = v;
    }

    private string FormatKey(UnityEngine.InputSystem.Key key)
    {
        if (key == UnityEngine.InputSystem.Key.None) return "-";
        if (key == UnityEngine.InputSystem.Key.Space) return "SPACE";
        return key.ToString().ToUpperInvariant();
    }

    // Fallback debug GUI ถ้า keyText ไม่ได้ผูก
    void OnGUI()
    {
        if (!fallbackIfNoText) return;
        if (keyText) return;
        if (!qteManager || !qteManager.IsActive) return;

        var k = qteManager.CurrentExpectedKey;
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 24;
        style.alignment = TextAnchor.MiddleCenter;
        GUILayout.BeginArea(new Rect(Screen.width / 2f - 100, 40, 200, 60));
        GUILayout.Box($"{promptPrefix} {FormatKey(k)}\n({qteManager.CurrentIndex + 1}/{qteManager.TotalLength})", style);
        GUILayout.EndArea();
    }
}