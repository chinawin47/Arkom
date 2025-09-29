using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Fridge Interactable")]
public class FridgeInteractable : Interactable
{
    [Header("Fridge Effects")]
    public GameObject itemsContainer; // ใส่ของที่จะร่วง (disable เริ่ม แล้ว enable + physics)
    public Rigidbody[] fallingItems;  // หรือเจาะจง Rigidbody ให้เปิด gravity
    public GameObject jumpScareFoot;  // วัตถุรูปเท้าที่โผล่ (enable เมื่อ interact)
    public AudioClip openSfx;
    public AudioClip dropSfx;
    public float autoHideFootAfter = 2f;

    private bool done;

    public override bool CanInteract(object interactor)
    {
        if (done) return false;
        if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.FridgeSequence)
            return false; // ยังไม่ถึงขั้นตู้เย็น
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (done) return;
        done = true;
        StoryDebug.Log("Fridge Interacted", this);
        // เปิดของร่วง
        if (itemsContainer) {
            itemsContainer.SetActive(true);
            StoryDebug.Log("Items container activated", itemsContainer);
        }
        foreach (var rb in fallingItems)
        {
            if (!rb) continue;
            rb.isKinematic = false;
            rb.useGravity = true;
            StoryDebug.Log("Item physics enabled: " + rb.name, rb);
        }
        if (jumpScareFoot) { jumpScareFoot.SetActive(true); StoryDebug.Log("Jump scare foot shown", jumpScareFoot); }
        if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position);
        if (dropSfx) AudioSource.PlayClipAtPoint(dropSfx, transform.position);
        EventBus.Publish(new FridgeScareDoneEvent());
        if (jumpScareFoot && autoHideFootAfter > 0f)
            StartCoroutine(HideFoot());
    }

    private System.Collections.IEnumerator HideFoot()
    {
        yield return new WaitForSeconds(autoHideFootAfter);
        if (jumpScareFoot) { jumpScareFoot.SetActive(false); StoryDebug.Log("Jump scare foot hidden", jumpScareFoot); }
    }
}
