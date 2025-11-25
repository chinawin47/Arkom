using ARKOM.Core;
using ARKOM.Player;
using ARKOM.Story;
using ARKOM.UI;
using UnityEngine;

[AddComponentMenu("Interactable/Key Pickup")]
public class KeyPickup : PickupInteractable
{
    [Header("Key Settings")]
    public string keyId = "UpstairsBoxKey";
    public string needPowerHint = "ไฟยังไม่มา อย่าเพิ่งเก็บกุญแจตอนนี้";
    [Tooltip("ข้อความที่แสดงเมื่อเก็บสำเร็จ")] public string pickupHint = "ได้กุญแจมาแล้ว";

    // ปลดล็อกได้เมื่อได้รับ PowerRestoredEvent
    private bool canPickup = false;

    private void OnEnable()
    {
        EventBus.Subscribe<PowerRestoredEvent>(OnPowerRestored);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PowerRestoredEvent>(OnPowerRestored);
    }

    private void OnPowerRestored(PowerRestoredEvent e)
    {
        canPickup = true;
        Debug.Log("⚡ ไฟมาแล้ว ปลดล็อกให้เก็บกุญแจได้");
    }

    // ต้องเป็น protected override (ไม่ใช่ public)
    protected override void OnInteract(object interactor)
    {
        if (!canPickup)
        {
            // ยังไฟไม่มา แสดงฮินต์และบล็อก
            SequenceController.Instance?.ShowTempHint(needPowerHint, 2.0f);
            Debug.Log("❌ ยังเก็บกุญแจไม่ได้ ต้องเปิดไฟก่อน!");
            return;
        }

        base.OnInteract(interactor);
    }

    protected override void ApplyPickup(PlayerController player)
    {
        // อนุญาตให้เก็บได้เลยเพราะตรวจ canPickup ไปแล้วใน OnInteract
        Keyring.Add(keyId);

        // แจ้งระบบเนื้อเรื่องให้ไปแสดงฮินต์เป้าหมายต่อไป (เช่น ไปที่วิทยุ) แทนที่ฮินต์ "ได้กุญแจมาแล้ว"
        EventBus.Publish(new KeyPickedEvent(keyId));

        // ไม่แสดงฮินต์ "ได้กุญแจมาแล้ว" เพื่อไม่ให้ค้างทับเป้าหมายต่อไปบน HintText
        // ถ้าต้องการ feedback ให้ใช้เสียงหรือ VFX แทน
    }
}

