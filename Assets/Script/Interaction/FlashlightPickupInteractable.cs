using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Flashlight Pickup")]
public class FlashlightPickupInteractable : PickupInteractable
{
    [Header("Flashlight Ref")]
    public Flashlight flashlightPrefabOrInstance;
    [Tooltip("เปิดทันทีหลังเก็บหรือไม่")] public bool turnOnAfterPickup = false;

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
    }
}
