using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;
using ARKOM.Game;

public class AnomalyHUD : MonoBehaviour
{
    public AnomalyManager anomalyManager;
    public Text statusText;
    public GameManager gameManager;

    private GameState lastState = GameState.DayExploration;
    private int lastResolved = -1;
    private int lastTotal = -1;

    void Awake()
    {
        if (!anomalyManager) anomalyManager = FindObjectOfType<AnomalyManager>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (statusText) statusText.text = "";
        EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(GameStateChangedEvent e)
    {
        lastState = e.State;
        if (e.State != GameState.NightAnomaly && statusText)
            statusText.text = "";
    }

    void Update()
    {
        if (!anomalyManager || !statusText) return;
        if (gameManager && gameManager.State != GameState.NightAnomaly) return;

        int total = anomalyManager.ActiveAnomalyCount;
        int resolved = anomalyManager.ResolvedCount;

        // อัปเดตเฉพาะเมื่อมีการเปลี่ยน ลด GC / Update only when changed
        if (resolved != lastResolved || total != lastTotal)
        {
            if (total > 0)
                statusText.text = $"Anomaly: {resolved}/{total}";
            else
                statusText.text = "";
            lastResolved = resolved;
            lastTotal = total;
        }
    }
}