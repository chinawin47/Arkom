using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private Resolution[] resolutions;
    private int currentResolutionIndex = 0;
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
        // ---------- GRAPHICS ----------
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        // หาค่า default16:9
        int default16_9Index = -1;
        for (int i =0; i < resolutions.Length; i++)
        {
            float aspect = (float)resolutions[i].width / resolutions[i].height;
            if (Mathf.Abs(aspect - (16f /9f)) <0.01f)
            {
                default16_9Index = i;
                break;
            }
        }
        // ถ้ายังไม่เคยบันทึก resolution ให้ใช้16:9 เป็นค่าเริ่มต้น
        if (!PlayerPrefs.HasKey(KEY_RESOLUTION) && default16_9Index != -1)
        {
            PlayerPrefs.SetInt(KEY_RESOLUTION, default16_9Index);
            PlayerPrefs.Save();
        }

        // กรองเฉพาะขนาดที่ไม่ซ้ำ (เลือก refresh rate สูงสุด)
        var uniqueRes = new System.Collections.Generic.Dictionary<string, Resolution>();
        for (int i =0; i < resolutions.Length; i++)
        {
            string key = resolutions[i].width + "x" + resolutions[i].height;
            if (!uniqueRes.ContainsKey(key) || resolutions[i].refreshRate > uniqueRes[key].refreshRate)
            {
                uniqueRes[key] = resolutions[i];
            }
        }
        // สร้าง list ของ resolution ที่ไม่ซ้ำ
        var filteredRes = new System.Collections.Generic.List<Resolution>(uniqueRes.Values);
        // Sort จาก property โดยตรง
        filteredRes.Sort((a, b) => {
            if (a.width != b.width) return a.width.CompareTo(b.width);
            return a.height.CompareTo(b.height);
        });
        // สร้าง options string หลังจาก sort
        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex =0;
        foreach (var res in filteredRes)
        {
            string option = res.width + " x " + res.height + " @ " + res.refreshRate + "Hz";
            options.Add(option);
        }
        resolutions = filteredRes.ToArray();
        int savedResolutionIndex = PlayerPrefs.GetInt(KEY_RESOLUTION, GetCurrentResolutionIndex());
        // หาค่า index ปัจจุบัน
        currentResolutionIndex =0;
        for (int i =0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(savedResolutionIndex,0, resolutions.Length -1);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        fullscreenToggle.isOn = Screen.fullScreen;
        bool savedFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN,1) ==1;
        fullscreenToggle.isOn = savedFullscreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);

        // ---------- LOAD SETTINGS ----------
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume",1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume",1f);
        sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity",1f);
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume",1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume",1f);
        sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity",1f);
        float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL,1f);
        float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL,1f);
        float mouseSens = PlayerPrefs.GetFloat(KEY_MOUSE_SENS,1f);
        masterSlider.value = masterVol;
        sfxSlider.value = sfxVol;
        sensitivitySlider.value = mouseSens;
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        // Apply all settings
        ApplyAllSettings();
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
        // TODO: Apply SFX volume to your SFX AudioSources or AudioMixer
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        AudioListener.volume = masterSlider.value;
        // SFX volume: implement on your SFX sources or AudioMixer if available
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
        int index = PlayerPrefs.GetInt(KEY_RESOLUTION, GetCurrentResolutionIndex());
        if (resolutions == null || resolutions.Length == 0) resolutions = Screen.resolutions;
        var res = resolutions[Mathf.Clamp(index, 0, resolutions.Length - 1)];
        bool isFull = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        Screen.SetResolution(res.width, res.height, isFull);
    }

    public void SetFullScreen(bool isFullscreen)
    {
        if (isApplying) return;
        PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullscreen ? 1 : 0);
        Screen.fullScreen = isFullscreen;
        ApplyResolution();
    }

    // ---------- MOUSE ----------
    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(KEY_MOUSE_SENS, value);
        // ให้ PlayerController หรือสคริปต์อื่นอ่านค่าจาก SettingsManager.Instance.sensitivitySlider.value
    }

    // ---------- HELPERS ----------
    public int GetCurrentResolutionIndex()
    {
        resolutions = Screen.resolutions;
        var cur = Screen.currentResolution;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == cur.width && resolutions[i].height == cur.height)
                return i;
        }
        return 0;
    }

    public void BackToMenu()
    {
        Debug.Log("Back to menu...");
    }
}
