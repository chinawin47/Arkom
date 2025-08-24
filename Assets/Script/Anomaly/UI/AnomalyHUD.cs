using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;
using ARKOM.Game;

public class AnomalyHUD : MonoBehaviour
{
    [Tooltip("Assign or auto-find")]
    public AnomalyManager anomalyManager;
    public Text statusText;

    [Header("Night Timer")]
    public GameManager gameManager;
    public Text timerText;
    public bool hideTimerOutsideNight = true;

    [Header("Spawn Flash")]
    public Text flashText;
    public float flashDuration = 1.0f;
    private float flashTimer;

    private void Awake()
    {
        if (!anomalyManager)
            anomalyManager = FindObjectOfType<AnomalyManager>();
        if (!gameManager)
            gameManager = FindObjectOfType<GameManager>();

        if (statusText) statusText.text = "";
        if (timerText) timerText.text = "";

        EventBus.Subscribe<GameStateChangedEvent>(OnState);
        EventBus.Subscribe<AnomalyResolvedEvent>(_ => Refresh());
        EventBus.Subscribe<NightCompletedEvent>(_ => Refresh());
        EventBus.Subscribe<AnomalySpawnBatchEvent>(OnSpawnBatch);
        EventBus.Subscribe<AnomalyProgressEvent>(_ => Refresh());
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
        EventBus.Unsubscribe<AnomalyResolvedEvent>(_ => Refresh());
        EventBus.Unsubscribe<NightCompletedEvent>(_ => Refresh());
        EventBus.Unsubscribe<AnomalySpawnBatchEvent>(OnSpawnBatch);
        EventBus.Unsubscribe<AnomalyProgressEvent>(_ => Refresh());
    }

    private void Update()
    {
        // Flash fade
        if (flashText && flashText.enabled)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
                flashText.enabled = false;
        }

        // Timer update (each frame)
        if (timerText && gameManager)
        {
            if (gameManager.State == GameState.NightAnomaly)
            {
                timerText.enabled = true;
                timerText.text = FormatTime(gameManager.NightTimeRemaining);
            }
            else if (hideTimerOutsideNight)
            {
                timerText.enabled = false;
            }
        }
    }

    private void OnState(GameStateChangedEvent e)
    {
        if (e.State == GameState.NightAnomaly)
        {
            Invoke(nameof(Refresh), 0.05f);
        }
        else if (e.State == GameState.DayExploration || e.State == GameState.GameOver || e.State == GameState.Victory)
        {
            if (statusText) statusText.text = "";
        }
    }

    private void OnSpawnBatch(AnomalySpawnBatchEvent e)
    {
        if (!flashText) return;
        flashText.enabled = true;
        flashText.text = $"+{e.Spawned} ({e.Active}/{e.Target})";
        flashTimer = flashDuration;
    }

    private void Refresh()
    {
        if (!statusText) return;

        if (!anomalyManager || anomalyManager.ActiveAnomalyCount == 0)
        {
            statusText.text = "";
            return;
        }
        statusText.text = $"Anomaly: {anomalyManager.ResolvedCount}/{anomalyManager.ActiveAnomalyCount}";
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}