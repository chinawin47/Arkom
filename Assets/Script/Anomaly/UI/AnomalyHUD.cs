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
        EventBus.Subscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        EventBus.Subscribe<NightCompletedEvent>(OnNightCompleted);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
        EventBus.Unsubscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        EventBus.Unsubscribe<NightCompletedEvent>(OnNightCompleted);
    }

    private void OnAnomalyResolved(AnomalyResolvedEvent _)
    {
        Refresh();
    }

    private void OnNightCompleted(NightCompletedEvent _)
    {
        Refresh();
    }

    private void OnState(GameStateChangedEvent e)
    {
        if (e.State == GameState.NightAnomaly)
        {
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
    }
}