using UnityEngine;
using ARKOM.Core;

[AddComponentMenu("Interactable/Circuit Breaker")]
public class BreakerInteractable : Interactable
{
    [Header("Breaker")]
    public bool powerOn = false;
    public bool singleUse = true; // ��� true �Դ�������� (���Դ��)

    [Header("Audio")] 
    [Tooltip("���§�͹�Դ������")] public AudioClip powerOnSfx;
    [Tooltip("���§�͹�Դ� (��������� singleUse = false)")] public AudioClip powerOffSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    protected override void OnInteract(object interactor)
    {
        // �Դ�������� (singleUse)
        if (singleUse)
        {
            if (powerOn) return; // �Դ���� �����õ�������
            powerOn = true;
            if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
            EventBus.Publish(new PowerRestoredEvent());
            return;
        }

        // ���� toggle (singleUse = false)
        powerOn = !powerOn;
        if (powerOn)
        {
            if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
            EventBus.Publish(new PowerRestoredEvent());
        }
        else
        {
            if (powerOffSfx) AudioSource.PlayClipAtPoint(powerOffSfx, transform.position, sfxVolume);
            EventBus.Publish(new BlackoutStartedEvent()); // �����俴Ѻ�ա����
        }
    }
}
