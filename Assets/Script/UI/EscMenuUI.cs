using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // add for sceneLoaded
using UnityEngine.EventSystems; // for EventSystem check

// Simple ESC pause + settings + resume + back to menu
public class EscMenuUI : MonoBehaviour
{
    public static EscMenuUI Instance { get; private set; }

    [Header("Panels")] public GameObject pausePanel; // root pause panel (buttons only)
    public GameObject settingsPanelProxy; // optional: assign same as SettingsManager.settingsPanel if you want separate reference
    [Tooltip("Main menu panel that should only be visible in main menu scene")] public GameObject mainMenuPanel; // optional

    [Header("Buttons")] public Button resumeButton; public Button settingsButton; public Button backMenuButton; public Button quitButton;

    [Header("Behavior")] public bool lockCursorOnResume = true; public bool unlockCursorOnPause = true; public bool pauseTimeScale = true;
    [Header("Persistence")] public bool persistAcrossScenes = true; // ให้เมนูตามไปทุกซีน
    [Tooltip("Name of the main menu scene (hide mainMenuPanel in other scenes)")] public string mainMenuSceneName = "MainMenu"; // adjust to your real scene name
    [Tooltip("ถ้าออกจากซีนเมนูหลักแล้วลบ MainMenuPanel ทิ้งเลย (ไม่กลับไปใช้) ")] public bool destroyMainMenuOnLeave = false;

    private bool open;
    private float prevTimeScale = 1f; private CursorLockMode prevLock; private bool prevCursorVisible;
    private bool leftMenuScene;

    void Awake()
    {
        // กันซ้ำ + Persist
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // subscribe
        }

        // Ensure there is an EventSystem soปุ่มจะคลิกได้ทุกซีน
        EnsureEventSystem();

        if (!settingsPanelProxy && SettingsManager.Instance)
            settingsPanelProxy = SettingsManager.Instance.settingsPanel; // auto bind ถ้าไม่ได้ลาก

        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanelProxy) settingsPanelProxy.SetActive(false);
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
        if (backMenuButton) backMenuButton.onClick.AddListener(BackToMenu);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);

        // ตรวจซีนปัจจุบันทันที (กรณีเริ่มจากซีนที่ไม่ใช่เมนู)
        EnsureMainMenuVisibility(SceneManager.GetActiveScene().name);
    }
    void OnDestroy()
    {
        if (resumeButton) resumeButton.onClick.RemoveListener(Resume);
        if (settingsButton) settingsButton.onClick.RemoveListener(OpenSettings);
        if (backMenuButton) backMenuButton.onClick.RemoveListener(BackToMenu);
        if (quitButton) quitButton.onClick.RemoveListener(QuitGame);
        if (persistAcrossScenes) SceneManager.sceneLoaded -= OnSceneLoaded; // unsubscribe
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
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            if (persistAcrossScenes) DontDestroyOnLoad(es);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem(); // ซีนใหม่อาจไม่มี EventSystem
        EnsureMainMenuVisibility(scene.name);
        // Close any open state when switching scenes
        open = false;
        if (settingsPanelProxy) settingsPanelProxy.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (pauseTimeScale && Time.timeScale == 0f) Time.timeScale = prevTimeScale;
    }

    private void EnsureMainMenuVisibility(string sceneName)
    {
        if (!mainMenuPanel) return;
        bool isMenu = !string.IsNullOrEmpty(mainMenuSceneName) && sceneName == mainMenuSceneName;
        if (!isMenu)
        {
            mainMenuPanel.SetActive(false);
            if (!leftMenuScene)
            {
                leftMenuScene = true;
                if (destroyMainMenuOnLeave)
                {
                    Destroy(mainMenuPanel); // ลบทิ้งเลยถ้าไม่ต้องใช้กลับ
                }
            }
        }
        else if (!leftMenuScene) // ยังอยู่ในซีนเมนูแรก
        {
            mainMenuPanel.SetActive(true);
        }
        else // กลับมาซีนเมนูอีก (โหลดย้อนกลับ)
        {
            if (!destroyMainMenuOnLeave) mainMenuPanel.SetActive(true);
        }
    }

    private void OpenPause()
    {
        open = true;
        if (pausePanel) pausePanel.SetActive(true);
        if (pauseTimeScale) { prevTimeScale = Time.timeScale; Time.timeScale = 0f; }
        if (unlockCursorOnPause)
        {
            prevLock = Cursor.lockState; prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }

    public void Resume()
    {
        if (!open) return;
        if (settingsPanelProxy && settingsPanelProxy.activeSelf) CloseSettings();
        if (pausePanel) pausePanel.SetActive(false);
        if (pauseTimeScale) Time.timeScale = prevTimeScale;
        if (unlockCursorOnPause)
        {
            Cursor.lockState = lockCursorOnResume ? CursorLockMode.Locked : prevLock;
            Cursor.visible = lockCursorOnResume ? false : prevCursorVisible;
        }
        open = false;
    }

    private void OpenSettings()
    {
        if (!open) return;
        if (settingsPanelProxy) settingsPanelProxy.SetActive(true);
        var sm = SettingsManager.Instance; if (sm) sm.ShowSettings(true);
        if (pausePanel) pausePanel.SetActive(false);
    }

    private void CloseSettings()
    {
        var sm = SettingsManager.Instance; if (sm) sm.ShowSettings(false);
        if (settingsPanelProxy) settingsPanelProxy.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    // Back button on OptionPanel (use this for both main menu and in-game)
    public void SettingsBack()
    {
        var sm = SettingsManager.Instance; if (sm) sm.ShowSettings(false);
        if (settingsPanelProxy) settingsPanelProxy.SetActive(false);
        string cur = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(mainMenuSceneName) && cur == mainMenuSceneName)
        {
            // เราอยู่ในซีนเมนูหลัก → กลับไป mainMenuPanel
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            // ไม่ต้องยุ่งกับ pausePanel / open flag
        }
        else
        {
            // อยู่ในเกม → กลับไป pausePanel ถ้ากำลัง pause อยู่
            if (open && pausePanel) pausePanel.SetActive(true);
        }
    }

    private void BackToMenu()
    {
        var sm = SettingsManager.Instance; if (sm) sm.BackToMenu();
        // implement scene load e.g. SceneManager.LoadScene(mainMenuSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
