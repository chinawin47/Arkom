using UnityEngine;
using ARKOM.Core; // for EventBus & events

public class LightSwitch : Interactable
{
    [Header("Light Settings")]
    public GameObject lightObject;      // ไฟที่ควบคุม (Mesh / Light / Group Root)
    public float switchDuration = 0.2f; // เวลา transition (เผื่อเอาไปทำ fade)
    public bool isOn = true;            // สถานะเริ่มต้นไฟ (ก่อน blackout)

    [Header("Audio")]
    [Tooltip("เสียงตอนเปิดไฟ")] public AudioClip toggleOnSfx;
    [Tooltip("เสียงตอนปิดไฟ")] public AudioClip toggleOffSfx;
    [Tooltip("เสียงตอนพยายามกดช่วงไฟดับ (ถูกบล็อค)")] public AudioClip blockedSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    [Header("Global Power Integration")]
    [Tooltip("ให้สวิตช์ตอบสนองตอนเกิด Blackout / PowerRestored")] public bool reactToGlobalPower = true;
    [Tooltip("ดับไฟบังคับเมื่อ Blackout (ไม่ให้เปิดได้)")] public bool forceOffOnBlackout = true;
    [Tooltip("ฟื้นคืนสถานะเดิมก่อนดับเมื่อไฟกลับ")] public bool restorePreviousOnPower = true;

    private bool isSwitching;
    private float switchTimer;
    private bool blackoutActive;          // อยู่ระหว่างไฟดับ
    private bool lastUserStateBeforeBlackout; // ค่าที่ผู้ใช้ตั้งไว้ก่อน blackout

    private void OnEnable()
    {
        if (reactToGlobalPower)
        {
            EventBus.Subscribe<BlackoutStartedEvent>(OnBlackout);
            EventBus.Subscribe<PowerRestoredEvent>(OnPowerRestored);
        }
    }

    private void OnDisable()
    {
        if (reactToGlobalPower)
        {
            EventBus.Unsubscribe<BlackoutStartedEvent>(OnBlackout);
            EventBus.Unsubscribe<PowerRestoredEvent>(OnPowerRestored);
        }
    }

    private void Start()
    {
        ApplyLightImmediate(isOn);
    }

    public override bool CanInteract(object interactor)
    {
        if (blackoutActive && forceOffOnBlackout) return false; // ระหว่าง blackout ห้ามเปิด (จะให้เล่น blockedSfx ใน ToggleLight)
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (isSwitching) return; // กัน spam ระหว่าง transition
        ToggleLight();
    }

    public void ToggleLight()
    {
        // ถ้า blackout และบังคับปิด ไม่ให้ toggle แต่เล่นเสียง blocked (ถ้ามี)
        if (blackoutActive && forceOffOnBlackout)
        {
            if (blockedSfx) AudioSource.PlayClipAtPoint(blockedSfx, transform.position, sfxVolume);
            return;
        }

        bool newState = !isOn;
        isOn = newState;
        ApplyLightImmediate(isOn);

        // เล่นเสียงตามสถานะใหม่
        if (isOn && toggleOnSfx)
            AudioSource.PlayClipAtPoint(toggleOnSfx, transform.position, sfxVolume);
        else if (!isOn && toggleOffSfx)
            AudioSource.PlayClipAtPoint(toggleOffSfx, transform.position, sfxVolume);

        switchTimer = 0f;
        isSwitching = true;
    }

    private void Update()
    {
        if (!isSwitching) return;
        switchTimer += Time.deltaTime;
        if (switchTimer >= switchDuration)
            isSwitching = false;
    }

    private void ApplyLightImmediate(bool on)
    {
        if (lightObject) lightObject.SetActive(on);
    }

    // ==== Global Power Events ====
    private void OnBlackout(BlackoutStartedEvent _)
    {
        if (!reactToGlobalPower) return;
        blackoutActive = true;
        lastUserStateBeforeBlackout = isOn; // จำสถานะผู้ใช้
        if (forceOffOnBlackout)
        {
            if (lightObject) lightObject.SetActive(false);
        }
    }

    private void OnPowerRestored(PowerRestoredEvent _)
    {
        if (!reactToGlobalPower) return;
        blackoutActive = false;
        if (restorePreviousOnPower)
        {
            if (forceOffOnBlackout)
            {
                isOn = lastUserStateBeforeBlackout;
                ApplyLightImmediate(isOn);
            }
        }
        else
        {
            if (forceOffOnBlackout)
            {
                isOn = true;
                ApplyLightImmediate(true);
            }
        }
    }
}

