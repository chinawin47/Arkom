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

    private bool canPickup = false;

    private void OnEnable()
    {
        // ฟัง event จาก EventBus
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
            Debug.Log("❌ ยังเก็บกุญแจไม่ได้ ต้องเปิดไฟก่อน!");
            // ถ้ามีระบบ UIHint:
            // SequenceController.Instance?.ShowTempHint("ไฟยังไม่มา เก็บไม่ได้", 2.5f);
            return;
        }

        base.OnInteract(interactor);
    }

    protected override void ApplyPickup(PlayerController player)
    {
        // ✅ เช็กว่าไฟเปิดครบ 3 ฟิวส์หรือยังจาก SequenceController
        if (SequenceController.Instance != null && !SequenceController.Instance.PlatesCleaned)
        {
            SequenceController.Instance.ShowTempHint(needPowerHint, 2.5f);
            return; // ❌ หยุด ไม่ให้เก็บกุญแจ
        }

        // ✅ เก็บกุญแจปกติ
        Keyring.Add(keyId);
        SequenceController.Instance?.ShowTempHint("ได้กุญแจมาแล้ว", 2.5f);
    }



}

