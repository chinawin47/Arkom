using UnityEngine;
using UnityEngine.UI;
using ARKOM.Core;
using ARKOM.Story;
using ARKOM.Game;
using System.Text;

public class VictoryHUD : MonoBehaviour
{
    public GameObject panel;
    public Text victoryText;
    public Button restartButton;
    public Button quitButton;

    private GameManager gm;
    private bool panelShown;

    private void Awake()
    {
        if (!panel) panel = gameObject;
        panel.SetActive(false);
        if (victoryText) victoryText.text = "";
        gm = FindObjectOfType<GameManager>();
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
        bool show = e.State == GameState.Victory;
        if (show)
        {
            panel.SetActive(true);
            if (!panelShown) Time.timeScale = 0f;
            panelShown = true;
            if (victoryText)
            {
                var sb = new StringBuilder();
                sb.AppendLine("VICTORY");
                if (StoryFlags.Instance)
                {
                    sb.AppendLine("Flags:");
                    foreach (var f in StoryFlags.Instance.All)
                        sb.Append("- ").AppendLine(f);
                }
                victoryText.text = sb.ToString();
            }
        }
        else
        {
            if (panelShown) Time.timeScale = 1f;
            panelShown = false;
            panel.SetActive(false);
        }
    }
}