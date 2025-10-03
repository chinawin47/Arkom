using UnityEngine;
using ARKOM.Core;

[AddComponentMenu("Interactable/Circuit Breaker")]
public class BreakerInteractable : Interactable
{
    [Header("Breaker")]
    public bool powerOn = false;
    public bool singleUse = true; // ถ้า true เปิดครั้งเดียว (ไม่ปิดได้)

    [Header("Audio")] 
    [Tooltip("เสียงตอนเปิดไฟสำเร็จ")] public AudioClip powerOnSfx;
    [Tooltip("เสียงตอนปิดไฟ (ใช้ได้เมื่อ singleUse = false)")] public AudioClip powerOffSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    protected override void OnInteract(object interactor)
    {
        // เปิดครั้งเดียว (singleUse)
        if (singleUse)
        {
            if (powerOn) return; // เปิดแล้ว ทำอะไรต่อไม่ได้
            powerOn = true;
            if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
            EventBus.Publish(new PowerRestoredEvent());
            return;
        }

        // โหมด toggle (singleUse = false)
        powerOn = !powerOn;
        if (powerOn)
        {
            if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
            EventBus.Publish(new PowerRestoredEvent());
        }
        else
        {
            if (powerOffSfx) AudioSource.PlayClipAtPoint(powerOffSfx, transform.position, sfxVolume);
            EventBus.Publish(new BlackoutStartedEvent()); // แจ้งว่าไฟดับอีกครั้ง
        }
    }
}
