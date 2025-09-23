using UnityEngine;
using UnityEngine.UI;
using ARKOM.Audio;

public class MicrophoneHUD : MonoBehaviour
{
    public MicrophoneListener mic;
    public Slider loudnessBar;
    public Gradient colorGradient;
    public bool hideWhenInactive = true;

    void Awake()
    {
        if (!mic) mic = FindObjectOfType<MicrophoneListener>();
        if (loudnessBar)
            loudnessBar.minValue = 0f;
        if (loudnessBar)
            loudnessBar.maxValue = 1f;
    }

    void Update()
    {
        if (!mic || !loudnessBar) return;

        if (hideWhenInactive)
            loudnessBar.gameObject.SetActive(mic.IsCapturing);
        if (!mic.IsCapturing) return;

        loudnessBar.value = mic.smoothedLoudness;
        if (loudnessBar.fillRect)
        {
            var img = loudnessBar.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = colorGradient.Evaluate(mic.smoothedLoudness);
        }
    }
}