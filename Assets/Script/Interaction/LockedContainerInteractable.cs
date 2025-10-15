using UnityEngine;
using ARKOM.Core;

[AddComponentMenu("Interactable/Locked Container")]
public class LockedContainerInteractable : Interactable
{
    [Header("Lock")] public string requiredKeyId = "UpstairsBoxKey";
    public bool oneShot = true; // open once

    [Header("Refs")] public GameObject lockedVisual;
    public GameObject openedVisual;

    [Header("SFX")] public AudioClip lockedSfx; public AudioClip openSfx; public float volume = 1f;

    private bool opened;

    public override bool CanInteract(object interactor)
    {
        if (opened && oneShot) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (opened && oneShot) return;
        if (!Keyring.Has(requiredKeyId))
        {
            if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume);
            return; // still locked
        }
        // open
        opened = true;
        if (lockedVisual) lockedVisual.SetActive(false);
        if (openedVisual) openedVisual.SetActive(true);
        if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position, volume);
    }
}
