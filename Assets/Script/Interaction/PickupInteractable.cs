using UnityEngine;
using ARKOM.Core; // For optional global event
using ARKOM.Player;

/// <summary>
/// Base class for simple pick up style interactables (keys, notes, generic items, flashlight, etc.)
/// Handles: one-time pickup gating, playing SFX, hiding renderers & colliders, and publishing a generic event.
/// Derive and implement ApplyPickup to grant the item effect to the player.
/// </summary>
[AddComponentMenu("Interactable/Base Pickup Interactable")] 
public abstract class PickupInteractable : Interactable
{
    [Header("Pickup Base Settings")] 
    [Tooltip("รหัสสำหรับอ้างอิง (เช่น ใช้เก็บลง Inventory / Log)")] public string pickupId;
    [Tooltip("ซ่อน Renderer / Collider หลังเก็บ")] public bool hideAfter = true;
    [Tooltip("วัตถุเพิ่มเติมที่ต้องปิดหลังเก็บ")] public GameObject[] extraHide;

    [Header("Audio")] 
    [Tooltip("เสียงตอนเก็บสำเร็จ")] public AudioClip pickupSfx;
    [Tooltip("เสียงหลังซ่อน (optional)")] public AudioClip hideSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    protected bool picked;

    public override bool CanInteract(object interactor)
    {
        if (picked) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (picked) return;
        // Allow PlayerController only by default; override if broader
        if (interactor is PlayerController player)
        {
            picked = true;
            if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);
            ApplyPickup(player);
            PublishEvent();
            if (hideAfter) HideVisuals();
        }
    }

    private void HideVisuals()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (extraHide != null)
        {
            foreach (var go in extraHide) if (go) go.SetActive(false);
        }
        if (hideSfx) AudioSource.PlayClipAtPoint(hideSfx, transform.position, sfxVolume);
    }

    protected virtual void PublishEvent()
    {
        if (!string.IsNullOrEmpty(pickupId))
            EventBus.Publish(new PickupEvent(pickupId, this));
    }

    /// <summary>
    /// Grant the item effect to the player (inventory / ability / stat change).
    /// </summary>
    protected abstract void ApplyPickup(PlayerController player);
}

/// <summary>
/// Generic event for any pickup (id + reference)
/// </summary>
public struct PickupEvent
{
    public readonly string Id;
    public readonly PickupInteractable Source;
    public PickupEvent(string id, PickupInteractable src)
    {
        Id = id;
        Source = src;
    }
}
