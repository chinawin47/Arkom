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

    [Header("Carry Rules")]
    [Tooltip("ข้อความเมื่อพกฟิวส์อยู่แล้ว")] public string alreadyCarryingHint = "พกฟิวส์ได้ทีละอัน นำไปใส่ก่อน";

    public override bool CanInteract(object interactor)
    {
        if (!base.CanInteract(interactor)) return false;
        // อนุญาตให้พกได้ทีละ1 เท่านั้น ต้องนำไปใส่ก่อนถึงจะเก็บชิ้นใหม่ได้
        if (FuseInventory.Count > 0)
        {
            SequenceController.Instance?.ShowTempHint(alreadyCarryingHint, 2.0f);
            return false;
        }
        return true;
    }

    protected override void ApplyPickup(PlayerController player)
    {
        // เก็บเข้าคลังเป็นครั้งละ1 พร้อมบันทึก origin
        FuseInventory.Add(location, 1);
        EventBus.Publish(new FuseFoundEvent(location));
    }
}
