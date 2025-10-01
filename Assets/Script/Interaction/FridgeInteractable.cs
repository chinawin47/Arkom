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

    [Header("Door Open Settings")] // ใหม่: ระบบเปิดประตู
    public bool requireStoryState = true; // ถ้า false เปิดได้ตลอด (ยังคง one-time jump scare)
    public Transform door;                // ใส่ transform ของบานประตู (หมุนแกน Y)
    public Vector3 doorOpenEuler = new Vector3(0f, 110f, 0f); // มุม relative จากตอนเริ่ม
    public float doorOpenDuration = 0.6f;
    public AnimationCurve doorOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool playDoorAnimBeforeScare = true; // ถ้า true รอเปิดจบก่อนปล่อยของร่วง

    private bool done;
    private Quaternion doorClosedRot;
    private bool doorCached;

    public override bool CanInteract(object interactor)
    {
        if (done) return false;
        if (requireStoryState)
        {
            if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.FridgeSequence)
                return false; // ยังไม่ถึงขั้นตู้เย็น
        }
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (done) return;
        done = true;
        CacheDoor();
        StoryDebug.Log("Fridge Interacted", this);
        if (playDoorAnimBeforeScare && door)
        {
            StartCoroutine(OpenDoorAndScare());
        }
        else
        {
            if (door) StartCoroutine(OpenDoorRoutine());
            TriggerScare();
        }
    }

    private void TriggerScare()
    {
        // เปิดของร่วง
        if (itemsContainer)
        {
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

    private System.Collections.IEnumerator OpenDoorAndScare()
    {
        yield return OpenDoorRoutine();
        TriggerScare();
    }

    private void CacheDoor()
    {
        if (doorCached) return;
        if (door) doorClosedRot = door.localRotation;
        doorCached = true;
    }

    private System.Collections.IEnumerator OpenDoorRoutine()
    {
        if (!door) yield break;
        CacheDoor();
        Quaternion target = doorClosedRot * Quaternion.Euler(doorOpenEuler);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, doorOpenDuration);
            float k = doorOpenCurve.Evaluate(Mathf.Clamp01(t));
            door.localRotation = Quaternion.Slerp(doorClosedRot, target, k);
            yield return null;
        }
        door.localRotation = target;
    }

    private System.Collections.IEnumerator HideFoot()
    {
        yield return new WaitForSeconds(autoHideFootAfter);
        if (jumpScareFoot) { jumpScareFoot.SetActive(false); StoryDebug.Log("Jump scare foot hidden", jumpScareFoot); }
    }
}
