using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSetting : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixers;
    [SerializeField] private Slider musicSlider;

    private void Start()
    {
        // ตั้งค่าเริ่มต้น (ถ้าอยากให้จำค่าที่เคยปรับ ใช้ PlayerPrefs ได้)
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicSlider.value = savedVolume;
        SetMusicVolume(savedVolume);
    }

    public void SetMusicVolume(float value)
    {
        // Clamp ป้องกันค่าเป็น 0
        float volume = Mathf.Clamp(value, 0.0001f, 1f);

        // แปลงจาก 0–1 → dB
        float dB = Mathf.Log10(volume) * 20f;
        myMixers.SetFloat("Music", dB);

        // เซฟค่าไว้
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
}
