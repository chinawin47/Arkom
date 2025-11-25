using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Global scene reset + full game state wipe when entering any starting scene
public static class SceneStartupReset
{
    // Scenes that should trigger a full progression wipe (menu + initial playable scenes)
    private static readonly string[] fullResetScenes = { "Start", "House1", "House2", "House3", "House4", "House5" }; // Adjust names if needed

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        // Base UI/input reset every scene
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();

        var sceneName = SceneManager.GetActiveScene().name;
        if (ShouldFullReset(sceneName))
        {
            WipeProgress();
        }
    }

    private static bool ShouldFullReset(string sceneName)
    {
        for (int i = 0; i < fullResetScenes.Length; i++)
        {
            if (string.Equals(sceneName, fullResetScenes[i])) return true;
        }
        return false;
    }

    private static void WipeProgress()
    {
        // Keys / fuses / evidence
        try { ARKOM.Core.Keyring.Reset(); } catch { }
        try { ARKOM.Core.FuseInventory.Reset(); } catch { }
        try { EvidenceRegistry.ResetAll(clearFlags: true); } catch { }
        // Stop lingering voice
        try { var vm = Object.FindObjectOfType<ARKOM.Audio.VoiceManager>(); if (vm) vm.StopVoice(); } catch { }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Object.DontDestroyOnLoad(go);
    }
}
