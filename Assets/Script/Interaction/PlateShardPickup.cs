using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Plate Shard Pickup")]
public class PlateShardPickup : Interactable
{
    public static int TotalNeeded = 0; // กำหนดรวมก่อนเริ่ม (หรือคำนวณ runtime)
    private static int collected = 0;
    private bool picked;

    private void OnEnable()
    {
        if (picked)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
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
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

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
}
