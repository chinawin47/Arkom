using UnityEngine;
using ARKOM.Core;

[AddComponentMenu("Interactable/Tool Gate (Requires Tool)")]
public class ToolGateInteractable : Interactable
{
    [Header("Requirement - Tool")] public string requiredToolId = "Pliers"; // คีม
    [Tooltip("ข้อความเตือนเมื่อผู้เล่นยังไม่มีเครื่องมือ")] public string needToolHint = "ตามหาคีมเพื่อปลดโซ่";

    [Header("Requirement - Key")] public bool requiresKey = true;
    [Tooltip("ID กุญแจสำหรับเปิดประตูหลังตัดโซ่แล้ว")] public string requiredKeyId = "UpstairsDoorKey";
    [Tooltip("ข้อความเตือนเมื่อยังไม่มีกุญแจ")] public string needKeyHint = "ตามหากุญแจเพื่อไขขึ้นไปข้างบน";

    [Header("Door Control")] public DoorInteractable doorToOpen;
    [Tooltip("เปิดประตูทันทีเมื่อปลดล็อคสำเร็จ")] public bool autoOpenDoorOnUnlock = true;
    [Tooltip("หลังปลดล็อคแล้ว เมื่อผู้เล่นกดที่เดิม ให้ส่งต่อไปยัง DoorInteractable")]
    public bool forwardToDoorWhenUnlocked = true;

    [Header("Visual Swap")] 
    [Tooltip("ชิ้นส่วนโซ่/กุญแจล่าม (จะถูกปิดเมื่อใช้คีม)")] public GameObject chainVisual;
    [Tooltip("สภาพประตูปิด (จะถูกปิดเมื่อเปิดสำเร็จ)")] public GameObject lockedVisual;
    [Tooltip("สภาพประตูเปิดแล้ว")] public GameObject unlockedVisual;

    [Header("Audio")] public AudioClip unlockSfx; public float sfxVolume = 1f;

    private bool chainRemoved; // ใช้คีมแล้ว
    private bool unlocked;     // เปิดสำเร็จแล้ว

    void OnEnable()
    {
        EventBus.Subscribe<ARKOM.Story.KeyPickedEvent>(OnKeyPicked);
    }
    void OnDisable()
    {
        EventBus.Unsubscribe<ARKOM.Story.KeyPickedEvent>(OnKeyPicked);
    }

    public override bool CanInteract(object interactor)
    {
        if (oneTime && unlocked) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (unlocked)
        {
            if (forwardToDoorWhenUnlocked && doorToOpen)
            {
                doorToOpen.Interact(interactor);
            }
            return;
        }

        // ขั้นที่ 1: ต้องมีคีมเพื่อตัดโซ่
        if (!chainRemoved)
        {
            if (!Keyring.Has(requiredToolId))
            {
                ARKOM.Story.SequenceController.Instance?.ShowTempHint(needToolHint, 2.5f);
                return;
            }
            // ตัดโซ่
            chainRemoved = true;
            if (chainVisual) chainVisual.SetActive(false);
            // หลังตัดโซ่เสร็จ ถ้าไม่ต้องใช้กุญแจ -> เปิดเลย
            if (!requiresKey)
            {
                OpenNow(interactor);
                return;
            }
            // ต้องใช้กุญแจต่อ
            ARKOM.Story.SequenceController.Instance?.ShowTempHint(needKeyHint, 2.5f);
            return;
        }

        // ขั้นที่ 2: ต้องมีกุญแจ
        if (requiresKey)
        {
            if (!Keyring.Has(requiredKeyId))
            {
                ARKOM.Story.SequenceController.Instance?.ShowTempHint(needKeyHint, 2.5f);
                return;
            }
            OpenNow(interactor);
            return;
        }

        // เผื่อกรณีไม่ต้องใช้กุญแจและตัดโซ่ไปแล้ว แต่ยังไม่เปิด
        OpenNow(interactor);
    }

    private void OnKeyPicked(ARKOM.Story.KeyPickedEvent e)
    {
        if (unlocked) return;
        if (!requiresKey) return;
        if (!chainRemoved) return; // ยังไม่ตัดโซ่ ไม่เปิดอัตโนมัติ
        if (string.IsNullOrEmpty(requiredKeyId)) return;
        if (e.KeyId != requiredKeyId) return;
        OpenNow(null); // เปิดอัตโนมัติเมื่อได้กุญแจที่ถูกต้อง
    }

    private void OpenNow(object interactor)
    {
        if (unlocked) return;
        unlocked = true;
        if (unlockSfx) AudioSource.PlayClipAtPoint(unlockSfx, transform.position, sfxVolume);
        if (lockedVisual) lockedVisual.SetActive(false);
        if (unlockedVisual) unlockedVisual.SetActive(true);
        EventBus.Publish(new ARKOM.Story.UpstairsDoorUnlockedEvent());

        if (autoOpenDoorOnUnlock && doorToOpen)
        {
            if (!doorToOpen.isOpen)
                doorToOpen.ToggleDoor();
        }

        // หลังจากนี้ส่งต่อการกดไปให้ DoorInteractable ได้ (ถ้าเลือกไว้)
        if (forwardToDoorWhenUnlocked && doorToOpen && interactor != null)
        {
            doorToOpen.Interact(interactor);
        }
    }
}
