using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.Game;

public class OnboardingHUD : MonoBehaviour
{
    public Text tipText;
    public float fadeDelay = 4f;
    public float fadeTime = 1f;

    [Header("Input Pass-through")]
    public bool allowClickThrough = true;
    public Graphic background;

    private float timer;
    private bool fading;
    private CanvasGroup cg;
    private GameManager gm;

    void Awake()
    {
        if (!tipText)
        {
            Debug.LogWarning("OnboardingHUD needs tipText");
            enabled = false;
            return;
        }
        cg = tipText.GetComponent<CanvasGroup>();
        if (!cg) cg = tipText.gameObject.AddComponent<CanvasGroup>();
        gm = FindObjectOfType<GameManager>();
        tipText.text = "";
        cg.alpha = 0f;
        ApplyInputFlags();
        EventBus.Subscribe<GameStateChangedEvent>(OnState);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) fading = true;
        }
        else if (fading)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 0f, Time.deltaTime / fadeTime);
            if (cg.alpha <= 0f)
            {
                fading = false;
                tipText.text = "";
            }
        }
    }

    private void ShowTip(string msg, float hold)
    {
        tipText.text = msg;
        cg.alpha = 1f;
        timer = hold;
        fading = false;
        ApplyInputFlags();
    }

    private void ApplyInputFlags()
    {
        if (allowClickThrough)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
            tipText.raycastTarget = false;
            if (background) background.raycastTarget = false;
        }
        else
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
            tipText.raycastTarget = true;
            if (background) background.raycastTarget = true;
        }
    }

    private void OnState(GameStateChangedEvent e)
    {
        if (!gm) return;
        if (gm.currentDay == 1 && e.State == GameState.DayExploration)
            ShowTip("สำรวจบ้านเพื่อเก็บเบาะแส (เริ่มกลางคืนเมื่อพร้อม)", fadeDelay);
        else if (gm.currentDay == 1 && e.State == GameState.NightAnomaly)
            ShowTip("กลางคืน: มองหาความผิดปกติ กด E เพื่อตรวจ", fadeDelay);
        else if (e.State == GameState.QTE)
            ShowTip("QTE: กดปุ่มตามที่แสดงบนหน้าจอ", fadeDelay);
    }
}