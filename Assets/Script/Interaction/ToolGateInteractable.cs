using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Tool Gate (Requires Tool)")]
public class ToolGateInteractable : Interactable
{
    [Header("Requirement - Tool")] public string requiredToolId = "Pliers"; // คีม
    [Tooltip("ข้อความเตือนเมื่อผู้เล่นยังไม่มีเครื่องมือ")] public string needToolHint = "ตามหาคีมเพื่อปลดโซ่";

    [Header("Requirement - Key")] public bool requiresKey = true;
    [Tooltip("ID กุญแจสำหรับเปิดประตูหลังตัดโซ่แล้ว")]
    public string requiredKeyId = "UpstairsDoorKey";
    [Tooltip("ไอดีกุญแจที่ยอมรับเพิ่มเติม (ใช้ได้ควบคู่กัน)")]
    public string[] additionalAcceptedKeyIds;
    [Tooltip("ต้องการกุญแจครบทุกอันหรือไม่ (ถ้าไม่ ตรงกับอันใดอันหนึ่งก็พอ)")]
    public bool requireAllKeys = false;
    [Tooltip("ข้อความเตือนเมื่อยังไม่มีกุญแจ")] public string needKeyHint = "ตามหากุญแจเพื่อไขขึ้นไปข้างบน";

    [Header("Story Gate")]
    [Tooltip("เปิดใช้การล็อกด้วยเนื้อเรื่อง (ถึงสถานะนี้ก่อน จึงจะเปิดได้)")] public bool requireStoryGate = false;
    [Tooltip("สถานะเนื้อเรื่องขั้นต่ำที่ต้องถึงก่อน จึงจะใช้ประตูนี้ได้")]
    public SequenceController.StoryState requiredStoryState = SequenceController.StoryState.BreakerFail;
    [Tooltip("ข้อความเตือนเมื่อยังไม่ถึงขั้นเนื้อเรื่อง")]
    public string notReadyHint = "ยังไม่ถึงเวลาไปชั้นสอง";
    [Tooltip("เวลาที่แสดงข้อความ 'ยังไม่ถึงเวลาไปชั้นสอง' (วินาที)")] public float notReadyHintDuration =2.5f;

    [Header("Fuse Gate")]
    [Tooltip("เช็คว่าต้องใส่ฟิวส์ให้ครบก่อนถึงจะเริ่มขั้นตอนปลดโซ่")]
    public bool checkFusesBeforeUnlock = true;
    [Tooltip("ข้อความเตือนเมื่อฟิวส์ยังไม่ครบ")]
    public string needFusesHint = "ไปใส่ฟิวก่อน";
    [Tooltip("เวลาที่แสดงข้อความเตือนฟิวส์ (วินาที)")]
    public float fuseHintDuration =2.5f;

    [Header("Door Control")] public DoorInteractable doorToOpen;
    [Tooltip("เปิดประตูทันทีเมื่อปลดล็อคสำเร็จ")] public bool autoOpenDoorOnUnlock = true;
    [Tooltip("หลังปลดล็อคแล้ว เมื่อผู้เล่นกดที่เดิม ให้ส่งต่อไปยัง DoorInteractable")]
    public bool forwardToDoorWhenUnlocked = true;

    [Header("Visual Swap")]
    [Tooltip("ชิ้นส่วนโซ่/กุญแจล่าม (จะถูกปิดเมื่อใช้คีม)")] public GameObject chainVisual;
    [Tooltip("สภาพประตูปิด (จะถูกปิดเมื่อเปิดสำเร็จ)")] public GameObject lockedVisual;
    [Tooltip("สภาพประตูเปิดแล้ว")] public GameObject unlockedVisual;

    [Header("Audio")]
    [Tooltip("เสียงตอนปลดล็อคสำเร็จ (ประตูเปิด)")] public AudioClip unlockSfx; public float sfxVolume =1f;
    [Tooltip("เสียงตอนใช้คีมตัดโซ่")] public AudioClip chainCutSfx; [Range(0f,1f)] public float chainCutVolume =1f;

    private bool chainRemoved; // ใช้คีมแล้ว
    private bool unlocked; // เปิดสำเร็จแล้ว

    void OnEnable()
    {
        EventBus.Subscribe<KeyPickedEvent>(OnKeyPicked);
        EventBus.Subscribe<StoryStateChangedEvent>(OnStoryState);
        // รีเฟรชชาตามสถานะปัจจุบัน (เช่น กลับเข้าฉาก)
        RefreshByStory();
    }
    void OnDisable()
    {
        EventBus.Unsubscribe<KeyPickedEvent>(OnKeyPicked);
        EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryState);
    }

    private void OnStoryState(StoryStateChangedEvent e)
    {
        // ถ้าผ่านเกณฑ์เนื้อเรื่องแล้ว และมีของครบ ให้เปิดอัตโนมัติ (กรณีผู้เล่นเก็บไว้ก่อน)
        RefreshByStory();
    }

    private void RefreshByStory()
    {
        if (unlocked) return;
        if (!requireStoryGate || IsStoryAllowed())
        {
            // ถึงขั้นเนื้อเรื่องแล้ว ? ถ้าตัดโซ่แล้วและมีคีย์ครบ (หรือไม่ต้องใช้คีย์) ให้เปิดได้ทันทีเมื่อกด
            // ถ้าต้องการเปิดอัตโนมัติเมื่อครบทุกอย่างแล้ว (กลับเข้าฉาก) ทำได้ดังนี้:
            if (chainRemoved)
            {
                if (!requiresKey || HasRequiredKeys())
                {
                    // เปิดอัตโนมัติเมื่อทุกอย่างครบ (ไม่บังคับ ถ้าไม่ต้องการ auto เปิด ให้คอมเมนต์บรรทัดถัดไป)
                    // OpenNow(null);
                }
            }
        }
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

        // Gate ด้วยเนื้อเรื่องก่อน
        if (requireStoryGate && !IsStoryAllowed())
        {
            SequenceController.Instance?.ShowTempHint(notReadyHint, notReadyHintDuration);
            return;
        }

        // เพิ่ม: ถ้ายังใส่ฟิวส์ไม่ครบ ให้เตือนและไม่ไปต่อขั้นคีม
        if (checkFusesBeforeUnlock && !FuseInventory.HasEnough)
        {
            SequenceController.Instance?.ShowTempHint(needFusesHint, fuseHintDuration);
            return;
        }

        // ขั้นที่1: ต้องมีคีมเพื่อตัดโซ่
        if (!chainRemoved)
        {
            if (!Keyring.Has(requiredToolId))
            {
                SequenceController.Instance?.ShowTempHint(needToolHint,2.5f);
                return;
            }
            // ตัดโซ่
            chainRemoved = true;
            if (chainVisual) chainVisual.SetActive(false);
            if (chainCutSfx) AudioSource.PlayClipAtPoint(chainCutSfx, transform.position, chainCutVolume);

            // หลังตัดโซ่เสร็จ ถ้าไม่ต้องใช้กุญแจ -> เปิดเลย
            if (!requiresKey)
            {
                OpenNow(interactor);
                return;
            }
            // ต้องใช้กุญแจต่อ
            SequenceController.Instance?.ShowTempHint(needKeyHint,2.5f);
            return;
        }

        // ขั้นที่2: ต้องมีกุญแจ
        if (requiresKey)
        {
            if (!HasRequiredKeys())
            {
                SequenceController.Instance?.ShowTempHint(needKeyHint,2.5f);
                return;
            }
            OpenNow(interactor);
            return;
        }

        // เผื่อกรณีไม่ต้องใช้กุญแจและตัดโซ่ไปแล้ว แต่ยังไม่เปิด
        OpenNow(interactor);
    }

    private void OnKeyPicked(KeyPickedEvent e)
    {
        if (unlocked) return;
        if (!requiresKey) return;
        if (!chainRemoved) return; // ยังไม่ตัดโซ่ ไม่เปิดอัตโนมัติ
        if (requireStoryGate && !IsStoryAllowed()) return; // ยังไม่ถึงขั้นเนื้อเรื่อง
        if (checkFusesBeforeUnlock && !FuseInventory.HasEnough) return; // ฟิวส์ยังไม่ครบ
        if (HasRequiredKeys())
        {
            OpenNow(null); // เปิดอัตโนมัติเมื่อมีครบตามเงื่อนไข
        }
    }

    private bool IsStoryAllowed()
    {
        var seq = SequenceController.Instance;
        if (!seq) return true; // ถ้าไม่มีคอนโทรลเลอร์ ให้ผ่าน
        // อนุญาตเมื่อ CurrentState >= requiredStoryState (เปรียบเทียบตามค่า enum)
        return (int)seq.CurrentState >= (int)requiredStoryState;
    }

    private bool HasRequiredKeys()
    {
        // รวมรายการ key ids ที่ยอมรับ
        int countRequired =0;
        int countOwned =0;
        if (!string.IsNullOrEmpty(requiredKeyId)) { countRequired++; if (Keyring.Has(requiredKeyId)) countOwned++; }
        if (additionalAcceptedKeyIds != null && additionalAcceptedKeyIds.Length >0)
        {
            foreach (var id in additionalAcceptedKeyIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                countRequired++;
                if (Keyring.Has(id)) countOwned++;
            }
        }
        if (countRequired ==0)
        {
            // ถ้าไม่ได้กำหนด id ใดเลย ถือว่าไม่ต้องใช้กุญแจ
            return true;
        }
        if (requireAllKeys)
        {
            return countOwned == countRequired;
        }
        else
        {
            return countOwned >0; // มีอันใดอันหนึ่งก็พอ
        }
    }

    private void OpenNow(object interactor)
    {
        if (unlocked) return;
        unlocked = true;
        if (unlockSfx) AudioSource.PlayClipAtPoint(unlockSfx, transform.position, sfxVolume);
        if (lockedVisual) lockedVisual.SetActive(false);
        if (unlockedVisual) unlockedVisual.SetActive(true);
        EventBus.Publish(new UpstairsDoorUnlockedEvent());

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
