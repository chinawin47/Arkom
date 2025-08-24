using UnityEngine;
using ARKOM.Game;
using ARKOM.Core;
using UnityEngine.InputSystem;

public class NightTestBootstrap : MonoBehaviour
{
    public GameManager gameManager;

    void Start()
    {
        if (!gameManager)
            gameManager = FindObjectOfType<GameManager>();

        Debug.Log("[Test] N=Start Night, V=Force Victory (placeholder), R=Restart.");
        EventBus.Subscribe<AnomalyResolvedEvent>(e => Debug.Log($"[Anomaly] Resolved {e.Id}"));
        EventBus.Subscribe<NightCompletedEvent>(_ => Debug.Log("[Night] Completed!"));
        EventBus.Subscribe<VictoryEvent>(_ => Debug.Log("[Game] VICTORY (placeholder)."));
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.nKey.wasPressedThisFrame)
            gameManager.BeginNight();

        if (Keyboard.current.vKey.wasPressedThisFrame) // force win
        {
            // จำลองผ่านคืนจนเกิน maxDays
            gameManager.GetType().GetMethod("TriggerVictory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(gameManager, null);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            // รีเซ็ตเร็ว
            gameManager.GetType().GetMethod("ResetGame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(gameManager, null);
        }
    }
}