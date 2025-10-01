using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Plate Shard Pickup")]
public class PlateShardPickup : Interactable
{
    public static int TotalNeeded = 0; // กำหนดรวมก่อนเริ่ม (หรือคำนวณ runtime)
    private static int collected = 0;
    private bool picked;

    [Header("Visibility")]
    [Tooltip("ซ่อน renderer / collider จนกว่าจะเข้าสู่สเตจเก็บเศษจาน")] public bool hideUntilCleanPlates = true;

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    void Awake()
    {
        CacheComponents();
        if (hideUntilCleanPlates)
        {
            // ซ่อนตั้งแต่เริ่ม (ไม่ปิด GameObject เพื่อให้ FindObjectsOfType เจอ)
            SetVisible(false);
        }
    }

    private void CacheComponents()
    {
        if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedColliders == null) cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private void SetVisible(bool visible)
    {
        CacheComponents();
        foreach (var r in cachedRenderers)
            if (r) r.enabled = visible && !picked;
        foreach (var c in cachedColliders)
            if (c) c.enabled = visible && !picked;
    }

    private void OnEnable()
    {
        // ถ้าเคยเก็บแล้วให้ซ่อนต่อ
        if (picked)
        {
            SetVisible(false);
        }
        else if (hideUntilCleanPlates)
        {
            // ยังไม่ใช่สเตจ ? ซ่อน (เผื่อถูก enable ด้วยเหตุผลอื่น)
            if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.CleanPlates)
                SetVisible(false);
        }
    }

    public override bool CanInteract(object interactor)
    {
        if (picked) return false;
        // ต้องอยู่ในสถานะ CleanPlates เท่านั้น
        if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.CleanPlates)
            return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (picked) return;
        picked = true;
        collected++;
        StoryDebug.Log("Plate shard picked: " + name + " (" + collected + "/" + TotalNeeded + ")", this);
        SetVisible(false);

        if (collected >= TotalNeeded && TotalNeeded > 0)
        {
            StoryDebug.Log("All plate shards collected", this);
            EventBus.Publish(new PlatesCleanedEvent(TotalNeeded));
        }
    }

    public static void ResetCounter(int total)
    {
        TotalNeeded = total;
        collected = 0;
        StoryDebug.Log("PlateShard counter reset total=" + total);
    }

    // เรียกตอนเริ่มสเตจ CleanPlates เพื่อให้แต่ละชิ้นโผล่
    internal void RevealForCleanPlates()
    {
        if (picked) return;
        hideUntilCleanPlates = false; // ไม่ต้องซ่อนอีก
        SetVisible(true);
    }
}
