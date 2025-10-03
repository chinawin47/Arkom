using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Flashlight Pickup")]
public class FlashlightPickupInteractable : PickupInteractable
{
    [Header("Flashlight Ref")]
    public Flashlight flashlightPrefabOrInstance;
    [Tooltip("เปิดทันทีหลังเก็บหรือไม่")] public bool turnOnAfterPickup = false;

    [Header("Flashlight Pickup Audio")] // เพิ่มเสียงเฉพาะตอนรับไฟฉาย
    [Tooltip("เสียงตอนเก็บแล้วเปิดไฟฉาย (ใช้เมื่อ turnOnAfterPickup = true)")] public AudioClip pickupTurnOnSfx;
    [Tooltip("เสียงตอนเก็บแต่ยังไม่เปิดไฟฉาย (turnOnAfterPickup = false)")] public AudioClip pickupTurnOffSfx;
    [Range(0f,1f)] public float flashlightTogglePickupVolume = 1f;

    protected override void ApplyPickup(PlayerController player)
    {
        if (!flashlightPrefabOrInstance)
        {
            Debug.LogWarning("[FlashlightPickup] No flashlight assigned.");
            return;
        }
        Flashlight toGive = flashlightPrefabOrInstance;
        // ถ้าเป็น prefab (ไม่อยู่ใน scene) ให้ instantiate
        if (!toGive.gameObject.scene.IsValid())
        {
            toGive = Instantiate(toGive);
        }
        player.AcquireFlashlight(toGive, turnOnAfterPickup);
        EventBus.Publish(new FlashlightAcquiredEvent());

        // เล่นเสียงเฉพาะสถานะเปิด/ยังไม่เปิด (นอกเหนือจาก pickupSfx ของฐาน)
        if (turnOnAfterPickup && pickupTurnOnSfx)
        {
            AudioSource.PlayClipAtPoint(pickupTurnOnSfx, transform.position, flashlightTogglePickupVolume);
        }
        else if (!turnOnAfterPickup && pickupTurnOffSfx)
        {
            AudioSource.PlayClipAtPoint(pickupTurnOffSfx, transform.position, flashlightTogglePickupVolume);
        }
    }
}
