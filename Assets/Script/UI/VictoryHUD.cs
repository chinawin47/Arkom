using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;

public class VictoryHUD : MonoBehaviour
{
    public Text victoryText;
    public GameObject panel;

    void Awake()
    {
        if (panel) panel.SetActive(false);
        EventBus.Subscribe<GameStateChangedEvent>(OnState);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
    }

    private void OnState(GameStateChangedEvent e)
    {
        if (panel)
            panel.SetActive(e.State == GameState.Victory);
        if (e.State == GameState.Victory && victoryText)
            victoryText.text = "VICTORY (Placeholder)\nผ่านครบวันกำหนด";
    }
}