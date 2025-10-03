using UnityEngine;
using ARKOM.Core; // for EventBus & events

public class LightSwitch : Interactable
{
    [Header("Light Settings")]
    public GameObject lightObject;      // 俷��Ǻ��� (Mesh / Light / Group Root)
    public float switchDuration = 0.2f; // ���� transition (�������价� fade)
    public bool isOn = true;            // ʶҹ��������� (��͹ blackout)

    [Header("Audio")]
    [Tooltip("���§�͹�Դ�")] public AudioClip toggleOnSfx;
    [Tooltip("���§�͹�Դ�")] public AudioClip toggleOffSfx;
    [Tooltip("���§�͹����������ǧ俴Ѻ (�١���ͤ)")] public AudioClip blockedSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    [Header("Global Power Integration")]
    [Tooltip("�����Ե��ͺʹͧ�͹�Դ Blackout / PowerRestored")] public bool reactToGlobalPower = true;
    [Tooltip("�Ѻ俺ѧ�Ѻ����� Blackout (�������Դ��)")] public bool forceOffOnBlackout = true;
    [Tooltip("��鹤׹ʶҹ������͹�Ѻ�����信�Ѻ")] public bool restorePreviousOnPower = true;

    private bool isSwitching;
    private float switchTimer;
    private bool blackoutActive;          // ���������ҧ俴Ѻ
    private bool lastUserStateBeforeBlackout; // ��ҷ������������͹ blackout

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
        if (blackoutActive && forceOffOnBlackout) return false; // �����ҧ blackout �����Դ (�������� blockedSfx � ToggleLight)
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (isSwitching) return; // �ѹ spam �����ҧ transition
        ToggleLight();
    }

    public void ToggleLight()
    {
        // ��� blackout ��кѧ�Ѻ�Դ ������ toggle ��������§ blocked (�����)
        if (blackoutActive && forceOffOnBlackout)
        {
            if (blockedSfx) AudioSource.PlayClipAtPoint(blockedSfx, transform.position, sfxVolume);
            return;
        }

        bool newState = !isOn;
        isOn = newState;
        ApplyLightImmediate(isOn);

        // ������§���ʶҹ�����
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
        lastUserStateBeforeBlackout = isOn; // ��ʶҹм����
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

