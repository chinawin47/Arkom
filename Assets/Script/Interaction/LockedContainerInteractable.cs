using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;
using ARKOM.UI;

[AddComponentMenu("Interactable/Locked Container")]
public class LockedContainerInteractable : Interactable
{
    [Header("Lock")]
    [Tooltip("ใช้รหัส4 หลัก (เว้นว่างเพื่อใช้กุญแจ)")] public string pinCode = "";
    [Tooltip("คีย์ทางเลือก ถ้าไม่ได้ใช้ PIN")] public string requiredKeyId = "UpstairsBoxKey";
    public bool oneShot = true; // open once
    [Tooltip("ต้องอยู่ในสถานะ OpenMysteryBox จึงให้ใส่รหัสได้")] public bool requireOpenMysteryBoxState = true;

    [Header("Refs")] public GameObject lockedVisual;
    public GameObject openedVisual;
    [Tooltip("UI ป้อนรหัส")] public PinCodeUI pinUI;

    [Header("SFX")] public AudioClip lockedSfx; public AudioClip openSfx; public float volume =1f;

    private bool opened;
    private bool awaitingPin;

    public override bool CanInteract(object interactor)
    {
        if (opened && oneShot) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (opened && oneShot) return;

        // If using PIN flow
        if (!string.IsNullOrEmpty(pinCode))
        {
            if (requireOpenMysteryBoxState && SequenceController.Instance && SequenceController.Instance.CurrentState != SequenceController.StoryState.OpenMysteryBox)
            {
                if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume);
                return; // not ready to input yet
            }
            if (!pinUI)
            {
                Debug.LogWarning("[LockedContainerInteractable] PinUI is not assigned.");
                if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume);
                return;
            }
            if (awaitingPin) return; // already showing
            awaitingPin = true;
            pinUI.Show(pinCode, (ok) =>
            {
                awaitingPin = false;
                if (ok) OpenNow();
                else if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume);
            });
            return;
        }

        // Key fallback
        if (!Keyring.Has(requiredKeyId))
        {
            if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume);
            return; // still locked
        }
        OpenNow();
    }

    private void OpenNow()
    {
        opened = true;
        if (lockedVisual) lockedVisual.SetActive(false);
        if (openedVisual) openedVisual.SetActive(true);
        if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position, volume);
        EventBus.Publish(new BoxUnlockedEvent());
    }
}
