using UnityEngine;

public class LightSwitch : Interactable
{
    [Header("Light Settings")]
    public GameObject lightObject;      // 俷��Ǻ���
    public float switchDuration = 0.2f; // ������� effect fade
    public bool isOn = true;            // ʶҹ���������

    private bool isSwitching = false;
    private float switchTimer = 0f;

    private void Start()
    {
        if (lightObject != null)
            lightObject.SetActive(isOn);
    }

    protected override void OnInteract(object interactor)
    {
        if (isSwitching) return; // �ѹ spam
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

        // ����͹Ҥ���� fade-in / flicker
        if (t >= 1f)
            isSwitching = false;
    }
}

