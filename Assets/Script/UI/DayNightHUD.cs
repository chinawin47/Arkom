using UnityEngine;
using UnityEngine.UI;
using ARKOM.Game;
using ARKOM.Core;

public class DayNightHUD : MonoBehaviour
{
    public Text dayText;
    public Text phaseText;

    private GameManager gm;

    void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        if (!dayText || !phaseText)
        {
            Debug.LogWarning("DayNightHUD: Assign both dayText and phaseText in Inspector.");
            enabled = false;
            return;
        }
        EventBus.Subscribe<GameStateChangedEvent>(OnState);
        UpdateHUD();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
    }

    private void OnState(GameStateChangedEvent e)
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (!gm) return;
        dayText.text = $"Day {gm.currentDay}";
        switch (gm.State)
        {
            case GameState.DayExploration:
                phaseText.text = "Daytime";
                break;
            case GameState.NightAnomaly:
                phaseText.text = "Night";
                break;
            case GameState.QTE:
                phaseText.text = "QTE";
                break;
            case GameState.GameOver:
                phaseText.text = "Game Over";
                break;
            case GameState.Victory:
                phaseText.text = "Victory";
                break;
            case GameState.Transition:
                phaseText.text = "Transition";
                break;
            default:
                phaseText.text = "";
                break;
        }
    }
}