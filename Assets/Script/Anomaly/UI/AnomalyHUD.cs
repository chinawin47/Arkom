using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;

public class AnomalyHUD : MonoBehaviour
{
    [Tooltip("Drag AnomalyManager หรือปล่อยว่างให้หาเอง / Assign or auto-find")]
    public AnomalyManager anomalyManager;
    [Tooltip("UI Text สำหรับแสดงผล / Text component to display progress")]
    public Text statusText;

    private void Awake()
    {
        if (!anomalyManager)
            anomalyManager = FindObjectOfType<AnomalyManager>();

        statusText.text = "";
        EventBus.Subscribe<GameStateChangedEvent>(OnState);
        EventBus.Subscribe<AnomalyResolvedEvent>(_ => Refresh());
        EventBus.Subscribe<NightCompletedEvent>(_ => Refresh());
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
        EventBus.Unsubscribe<AnomalyResolvedEvent>(_ => Refresh()); // NOTE: for production keep delegate ref
        EventBus.Unsubscribe<NightCompletedEvent>(_ => Refresh());
    }

    private void OnState(GameStateChangedEvent e)
    {
        if (e.State == GameState.NightAnomaly)
        {
            // Delay a frame so manager finishes activation / หน่วง 1 เฟรมกันยังไม่ Activate
            Invoke(nameof(Refresh), 0.05f);
        }
        else if (e.State == GameState.DayExploration || e.State == GameState.GameOver)
        {
            statusText.text = "";
        }
    }

    private void Refresh()
    {
        if (!anomalyManager || anomalyManager.ActiveAnomalyCount == 0)
        {
            statusText.text = "";
            return;
        }
        statusText.text = $"Anomaly: {anomalyManager.ResolvedCount}/{anomalyManager.ActiveAnomalyCount}";
        // ภาษาไทยเพิ่ม:
        // statusText.text = $"ความผิดปกติ: {anomalyManager.ResolvedCount}/{anomalyManager.ActiveAnomalyCount}";
    }
}