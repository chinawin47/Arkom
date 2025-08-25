using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.Game;

public class GameOverHUD : MonoBehaviour
{
    public GameObject panel;
    public Text messageText;
    public Button restartButton;
    public Button quitButton;

    private GameManager gm;
    private bool panelShown;

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        if (!panel) panel = gameObject;
        panel.SetActive(false);
        if (messageText) messageText.text = "";
        if (restartButton) restartButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            gm?.ResetGame();
        });
        if (quitButton) quitButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            gm?.QuitGame();
        });
        EventBus.Subscribe<GameStateChangedEvent>(OnState);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
        if (restartButton) restartButton.onClick.RemoveAllListeners();
        if (quitButton) quitButton.onClick.RemoveAllListeners();
    }

    private void OnState(GameStateChangedEvent e)
    {
        bool show = e.State == GameState.GameOver;
        if (show)
        {
            panel.SetActive(true);
            if (messageText) messageText.text = "GAME OVER";
            if (!panelShown) Time.timeScale = 0f;
            panelShown = true;
        }
        else
        {
            if (panelShown) Time.timeScale = 1f;
            panelShown = false;
            panel.SetActive(false);
        }
    }
}