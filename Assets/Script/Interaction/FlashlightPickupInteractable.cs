using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Flashlight Pickup")]
public class FlashlightPickupInteractable : Interactable
{
    [Header("Flashlight Ref")]
    public Flashlight flashlightPrefabOrInstance;
    [Tooltip("เปิดทันทีหลังเก็บหรือไม่")] public bool turnOnAfterPickup = false;
    [Tooltip("ซ่อน Mesh / ปิดคอลไลเดอร์หลังเก็บ")] public bool hideAfterPickup = true;

    private bool picked;

    public override bool CanInteract(object interactor)
    {
        if (picked) return false;
        return interactor is PlayerController && base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (picked) return;
        if (!(interactor is PlayerController pc)) return;

        Flashlight toGive = flashlightPrefabOrInstance;
        if (!toGive)
        {
            Debug.LogWarning("[FlashlightPickup] No flashlight assigned.");
            return;
        }

        // ถ้าไฟฉายเป็น prefab (ไม่มี parent ในซีน) -> สร้าง Instance
        if (!toGive.gameObject.scene.IsValid())
        {
            toGive = Object.Instantiate(toGive);
        }

        pc.AcquireFlashlight(toGive, turnOnAfterPickup);
        EventBus.Publish(new FlashlightAcquiredEvent());

        if (hideAfterPickup)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }
        picked = true;
    }
}
