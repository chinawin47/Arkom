using UnityEngine;
using UnityEngine.UI;
using ARKOM.Player;

public class FlashlightHUD : MonoBehaviour
{
    public Flashlight flashlight;
    public Slider batteryBar;
    public Text percentText;
    public bool hideWhenMissing = true;

    void Awake()
    {
        if (!flashlight) flashlight = FindObjectOfType<Flashlight>();
        if (batteryBar)
        {
            batteryBar.minValue = 0f;
            batteryBar.maxValue = 1f;
        }
        Refresh();
    }

    void Update()
    {
        if (!flashlight)
        {
            if (hideWhenMissing && batteryBar) batteryBar.gameObject.SetActive(false);
            if (percentText) percentText.text = "";
            return;
        }

        float p = flashlight.BatteryPercent;
        if (batteryBar)
        {
            batteryBar.gameObject.SetActive(true);
            batteryBar.value = p;
        }
        if (percentText)
            percentText.text = $"{Mathf.RoundToInt(p * 100f)}%";
    }

    private void Refresh()
    {
        if (flashlight == null)
        {
            if (batteryBar) batteryBar.gameObject.SetActive(false);
            if (percentText) percentText.text = "";
        }
    }
}