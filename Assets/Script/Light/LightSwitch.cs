using UnityEngine;

public class LightSwitch : Interactable
{
    [Header("Light Settings")]
    public GameObject lightObject;      // ไฟที่ควบคุม
    public float switchDuration = 0.2f; // เผื่อใส่ effect fade
    public bool isOn = true;            // สถานะเริ่มต้นไฟ

    private bool isSwitching = false;
    private float switchTimer = 0f;

    private void Start()
    {
        if (lightObject != null)
            lightObject.SetActive(isOn);
    }

    protected override void OnInteract(object interactor)
    {
        if (isSwitching) return; // กัน spam
        ToggleLight();
    }

    public void ToggleLight()
    {
        isOn = !isOn;
        if (lightObject != null)
            lightObject.SetActive(isOn);

        switchTimer = 0f;
        isSwitching = true;
    }

    private void Update()
    {
        if (!isSwitching) return;

        switchTimer += Time.deltaTime;
        float t = Mathf.Clamp01(switchTimer / switchDuration);

        // เผื่ออนาคตใส่ fade-in / flicker
        if (t >= 1f)
            isSwitching = false;
    }
}

