using UnityEngine;
using ARKOM.Core;
using ARKOM.Player;
using ARKOM.Story;

[AddComponentMenu("Interactable/Fuse Pickup")]
public class FusePickup : PickupInteractable
{
    [Header("Fuse Pickup")]
    public int amount = 1;
    [Tooltip("ตำแหน่งของฟิวส์ชิ้นนี้")] public FuseLocation location;

    protected override void ApplyPickup(PlayerController player)
    {
        // เก็บเข้าคลังพร้อมบันทึก origin
        FuseInventory.Add(location, Mathf.Max(1, amount));
        EventBus.Publish(new FuseFoundEvent(location));
    }
}
