using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Graphic Settings")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Mouse Settings")]
    public Slider sensitivitySlider;

    [Header("Audio Settings")]
    public Slider masterSlider;
    public Slider sfxSlider;

    // All available system resolutions
    private Resolution[] systemResolutions;
    // Filtered, unique-by WxH with highest refresh, sorted ascending; used for dropdown and saved index mapping
    private Resolution[] filteredResolutions;

    private bool isApplying = false;

    // PlayerPrefs keys
    private const string KEY_RESOLUTION = "settings_resolution";
    private const string KEY_FULLSCREEN = "settings_fullscreen";
    private const string KEY_MOUSE_SENS = "settings_mouse_sens";
    private const string KEY_MASTER_VOL = "settings_master_vol";
    private const string KEY_SFX_VOL = "settings_sfx_vol";

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BuildResolutionOptions();

        // Fullscreen toggle
        bool savedFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        fullscreenToggle.isOn = savedFullscreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);

        // ---------- LOAD SETTINGS ----------
        float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
        float mouseSens = PlayerPrefs.GetFloat(KEY_MOUSE_SENS, 1f);
        if (masterSlider) { masterSlider.value = masterVol; masterSlider.onValueChanged.AddListener(SetMasterVolume); }
        if (sfxSlider) { sfxSlider.value = sfxVol; sfxSlider.onValueChanged.AddListener(SetSFXVolume); }
        if (sensitivitySlider) { sensitivitySlider.value = mouseSens; sensitivitySlider.onValueChanged.AddListener(SetSensitivity); }

        // Apply all settings
        ApplyAllSettings();
    }

    private void BuildResolutionOptions()
    {
        systemResolutions = Screen.resolutions;
        var unique = new Dictionary<string, Resolution>();
        for (int i = 0; i < systemResolutions.Length; i++)
        {
            var r = systemResolutions[i];
            string key = r.width + "x" + r.height;
            if (!unique.ContainsKey(key) || r.refreshRate > unique[key].refreshRate)
                unique[key] = r;
        }
        var list = new List<Resolution>(unique.Values);
        list.Sort((a, b) =>
        {
            int c = a.width.CompareTo(b.width);
            return c != 0 ? c : a.height.CompareTo(b.height);
        });
        filteredResolutions = list.ToArray();

        // Default to a 16:9 entry if no saved index yet
        if (!PlayerPrefs.HasKey(KEY_RESOLUTION))
        {
            int idx16x9 = -1;
            for (int i = 0; i < filteredResolutions.Length; i++)
            {
                float aspect = (float)filteredResolutions[i].width / filteredResolutions[i].height;
                if (Mathf.Abs(aspect - (16f / 9f)) < 0.01f) { idx16x9 = i; break; }
            }
            if (idx16x9 >= 0) { PlayerPrefs.SetInt(KEY_RESOLUTION, idx16x9); PlayerPrefs.Save(); }
        }

        // Build dropdown options from filteredResolutions
        if (resolutionDropdown)
        {
            resolutionDropdown.ClearOptions();
            var options = new List<string>(filteredResolutions.Length);
            for (int i = 0; i < filteredResolutions.Length; i++)
            {
                var r = filteredResolutions[i];
                options.Add($"{r.width} x {r.height} @ {r.refreshRate}Hz");
            }
            resolutionDropdown.AddOptions(options);
            int saved = Mathf.Clamp(PlayerPrefs.GetInt(KEY_RESOLUTION, GetCurrentResolutionIndex()), 0, filteredResolutions.Length - 1);
            resolutionDropdown.value = saved;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
    }

    // ---------- APPLY ALL ----------
    public void ApplyAllSettings()
    {
        isApplying = true;
        ApplyResolution();
        ApplyVolumes();
        isApplying = false;
    }

    // ---------- AUDIO ----------
    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, volume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(KEY_SFX_VOL, volume);
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        if (masterSlider)
            AudioListener.volume = masterSlider.value;
        else
            AudioListener.volume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        // TODO: route SFX volume via mixer or SFX sources if available
    }

    // ---------- GRAPHIC ----------
    public void SetResolution(int index)
    {
        if (isApplying) return;
        PlayerPrefs.SetInt(KEY_RESOLUTION, index);
        ApplyResolution();
    }

    void ApplyResolution()
    {
        if (filteredResolutions == null || filteredResolutions.Length == 0)
            BuildResolutionOptions();
        int index = Mathf.Clamp(PlayerPrefs.GetInt(KEY_RESOLUTION, GetCurrentResolutionIndex()), 0, filteredResolutions.Length - 1);
        var res = filteredResolutions[index];
        bool isFull = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        var mode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreenMode = mode;
        // apply with preferred refresh rate
        Screen.SetResolution(res.width, res.height, mode, res.refreshRate);
    }

    public void SetFullScreen(bool isFullscreen)
    {
        if (isApplying) return;
        PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullscreen ? 1 : 0);
        Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        ApplyResolution();
    }

    // ---------- MOUSE ----------
    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(KEY_MOUSE_SENS, value);
        // ผู้เล่นอ่านจาก SettingsManager.Instance.sensitivitySlider.value หรือ PlayerPrefs
    }

    // ---------- HELPERS ----------
    public int GetCurrentResolutionIndex()
    {
        if (filteredResolutions == null || filteredResolutions.Length == 0)
        {
            BuildResolutionOptions();
        }
        var curW = Screen.currentResolution.width;
        var curH = Screen.currentResolution.height;
        for (int i = 0; i < filteredResolutions.Length; i++)
        {
            if (filteredResolutions[i].width == curW && filteredResolutions[i].height == curH)
                return i;
        }
        // fallback to closest larger/smaller
        int closest = 0; int bestDiff = int.MaxValue;
        for (int i = 0; i < filteredResolutions.Length; i++)
        {
            int diff = Mathf.Abs(filteredResolutions[i].width * filteredResolutions[i].height - curW * curH);
            if (diff < bestDiff) { bestDiff = diff; closest = i; }
        }
        return closest;
    }

    public void BackToMenu()
    {
        Debug.Log("Back to menu...");
    }
}
