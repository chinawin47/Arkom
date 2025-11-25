using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using ARKOM.Core; // reset globals
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenu : MonoBehaviour
{
    void Awake()
    {
        // Reset runtime state to ensure menu is interactive
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    // ??????????????????? "Play"
    public void PlayGame()
    {
        // Force wipe progression before starting a new run (prevents door auto unlock)
        Keyring.Reset();
        FuseInventory.Reset();
        EvidenceRegistry.ResetAll(clearFlags: true);
        // Load first gameplay scene (build index 1)
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
