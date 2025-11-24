using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class EscMenuUI : MonoBehaviour
{
    public static EscMenuUI Instance { get; private set; }

    [Header("Panels")] public GameObject pausePanel; public GameObject settingsPanelProxy;
    [Header("Buttons")] public Button resumeButton; public Button settingsButton; public Button backMenuButton; public Button quitButton;

    [Header("Auto Bind")] public bool autoBindOnAwake = true; public bool autoCreateIfMissing = true; public string pausePanelName = "PausePanel"; public string settingsPanelName = "OptionPanel";

    [Header("Behavior")] public bool lockCursorOnResume = true; public bool unlockCursorOnPause = true; public bool pauseTimeScale = true;
    [Header("Persistence")] public bool persistAcrossScenes = true;

    private bool open; private float prevTimeScale = 1f; private CursorLockMode prevLock; private bool prevCursorVisible;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (persistAcrossScenes) { DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += OnSceneLoaded; }
        EnsureEventSystem();
        if (!settingsPanelProxy && SettingsManager.Instance) settingsPanelProxy = SettingsManager.Instance.settingsPanel;
        if (autoBindOnAwake) AutoBind();
        AttachButtonListeners();
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanelProxy) settingsPanelProxy.SetActive(false);
    }
    void OnDestroy()
    {
        if (resumeButton) resumeButton.onClick.RemoveListener(Resume);
        if (settingsButton) settingsButton.onClick.RemoveListener(OpenSettings);
        if (backMenuButton) backMenuButton.onClick.RemoveListener(BackToMenu);
        if (quitButton) quitButton.onClick.RemoveListener(QuitGame);
        if (persistAcrossScenes) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        var kb = Keyboard.current; if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (!open) OpenPause(); else if (settingsPanelProxy && settingsPanelProxy.activeSelf) CloseSettings(); else Resume();
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
        if (persistAcrossScenes) DontDestroyOnLoad(es);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem(); AutoBind(); AttachButtonListeners();
        open = false; if (settingsPanelProxy) settingsPanelProxy.SetActive(false); if (pausePanel) pausePanel.SetActive(false);
        if (pauseTimeScale && Time.timeScale == 0f) Time.timeScale = prevTimeScale;
    }

    private void OpenPause()
    {
        open = true; if (pausePanel) pausePanel.SetActive(true);
        if (pauseTimeScale) { prevTimeScale = Time.timeScale; Time.timeScale = 0f; }
        if (unlockCursorOnPause) { prevLock = Cursor.lockState; prevCursorVisible = Cursor.visible; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    public void Resume()
    {
        if (!open) return; if (settingsPanelProxy && settingsPanelProxy.activeSelf) CloseSettings();
        if (pausePanel) pausePanel.SetActive(false);
        if (pauseTimeScale) Time.timeScale = prevTimeScale;
        if (unlockCursorOnPause) { Cursor.lockState = lockCursorOnResume ? CursorLockMode.Locked : prevLock; Cursor.visible = lockCursorOnResume ? false : prevCursorVisible; }
        open = false;
    }

    private void OpenSettings()
    {
        if (!open) return; if (settingsPanelProxy) settingsPanelProxy.SetActive(true); var sm = SettingsManager.Instance; if (sm) sm.ShowSettings(true); if (pausePanel) pausePanel.SetActive(false);
    }

    private void CloseSettings()
    {
        var sm = SettingsManager.Instance; if (sm) sm.ShowSettings(false); if (settingsPanelProxy) settingsPanelProxy.SetActive(false); if (pausePanel && open) pausePanel.SetActive(true);
    }

    public void SettingsBack() => CloseSettings();
    private void BackToMenu() { var sm = SettingsManager.Instance; if (sm) sm.BackToMenu(); }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void AttachButtonListeners()
    {
        if (resumeButton) { resumeButton.onClick.RemoveListener(Resume); resumeButton.onClick.AddListener(Resume); }
        if (settingsButton) { settingsButton.onClick.RemoveListener(OpenSettings); settingsButton.onClick.AddListener(OpenSettings); }
        if (backMenuButton) { backMenuButton.onClick.RemoveListener(BackToMenu); backMenuButton.onClick.AddListener(BackToMenu); }
        if (quitButton) { quitButton.onClick.RemoveListener(QuitGame); quitButton.onClick.AddListener(QuitGame); }
    }

    private void AutoBind()
    {
        var canvas = GetComponentInParent<Canvas>(); if (!canvas) { canvas = FindObjectOfType<Canvas>(); if (canvas && transform.parent != canvas.transform) transform.SetParent(canvas.transform, false); }
        if (canvas && !canvas.GetComponent<GraphicRaycaster>()) canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (!pausePanel)
        {
            var found = GameObject.Find(pausePanelName);
            if (!found && autoCreateIfMissing)
            {
                found = new GameObject(pausePanelName); found.transform.SetParent(canvas ? canvas.transform : transform, false);
                var cg = found.AddComponent<CanvasGroup>(); cg.interactable = true; cg.blocksRaycasts = true;
            }
            pausePanel = found;
        }
        if (!settingsPanelProxy)
        {
            var opt = GameObject.Find(settingsPanelName); if (opt) settingsPanelProxy = opt;
        }
        if (pausePanel)
        {
            var allButtons = pausePanel.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                var n = b.gameObject.name.ToLowerInvariant();
                if (!resumeButton && (n.Contains("resume") || n.Contains("continue"))) resumeButton = b;
                else if (!settingsButton && n.Contains("setting")) settingsButton = b;
                else if (!backMenuButton && (n.Contains("menu") || n.Contains("back"))) backMenuButton = b;
                else if (!quitButton && (n.Contains("quit") || n.Contains("exit"))) quitButton = b;
            }
        }
        if (pausePanel && !resumeButton) resumeButton = pausePanel.GetComponentInChildren<Button>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawnIfNeeded()
    {
        if (FindObjectOfType<EscMenuUI>()) return; var canvas = FindObjectOfType<Canvas>(); if (!canvas) return;
        var go = new GameObject("EscMenuUI_Auto"); go.transform.SetParent(canvas.transform, false);
        var ui = go.AddComponent<EscMenuUI>(); ui.persistAcrossScenes = false; ui.autoBindOnAwake = true; ui.autoCreateIfMissing = true;
    }
}
