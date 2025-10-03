using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Fridge Interactable")]
public class FridgeInteractable : Interactable
{
    [Header("Fridge Effects")]
    public GameObject itemsContainer; // ���ͧ������ǧ (disable ����� ���� enable + physics)
    public Rigidbody[] fallingItems;  // ������Ш� Rigidbody ����Դ gravity
    public GameObject jumpScareFoot;  // �ѵ���ٻ��ҷ����� (enable ����� interact)
    [Header("Ghost (Fridge Jump Scare)")] 
    [Tooltip("��㹵����� (��� inactive �������)")] public GameObject fridgeGhost; // ���ż���������ͧ��ǧ
    [Tooltip("���ҷ�����������͹��͹")] public float ghostVisibleTime = 2f;
    [Tooltip("���§�͹�����")] public AudioClip ghostSfx; // ����: ���§���
    public AudioClip openSfx;         // ��蹷ѹ������͡� (��е�)
    public AudioClip dropSfx;         // ��蹵͹�ͧ��ǧ
    public AudioClip preOpenSfx;      // SFX ���� ��͹�Դ (�� �ç���) (optional)
    public float preOpenDelay = 0f;   // ˹�ǧ��͹������Դ��е�
    public float scareDelayAfterDoor = 0f; // ˹�ǧ��ѧ��е��Դ�ش��͹����¢ͧ��ǧ
    public float autoHideFootAfter = 2f;

    [Header("Door Open Settings")] // ����: �к��Դ��е�
    public bool requireStoryState = true; // ��� false �Դ���ʹ (�ѧ�� one-time jump scare)
    public Transform door;                // ��� transform �ͧ�ҹ��е� (��ع᡹ Y)
    public Vector3 doorOpenEuler = new Vector3(0f, 110f, 0f); // ��� relative �ҡ�͹�����
    public float doorOpenDuration = 0.6f;
    public AnimationCurve doorOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool playDoorAnimBeforeScare = true; // ��� true ���Դ����͹����¢ͧ��ǧ

    private bool done;
    private Quaternion doorClosedRot;
    private bool doorCached;

    void Awake()
    {
        // ��������� ghost �Դ�͹����� (�������Դ���㹩ҡ)
        if (fridgeGhost && fridgeGhost.activeSelf)
            fridgeGhost.SetActive(false);
    }

    public override bool CanInteract(object interactor)
    {
        if (done) return false;
        if (requireStoryState)
        {
            if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.FridgeSequence)
                return false; // �ѧ���֧��鹵�����
        }
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (done) return;
        done = true;
        CacheDoor();
        StoryDebug.Log("Fridge Interacted", this);
        if (preOpenSfx) AudioSource.PlayClipAtPoint(preOpenSfx, transform.position);
        if (preOpenDelay > 0f)
        {
            StartCoroutine(DelayedStart());
            return;
        }
        StartSequence();
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(preOpenDelay);
        StartSequence();
    }

    private void StartSequence()
    {
        if (playDoorAnimBeforeScare && door)
        {
            if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position);
            StartCoroutine(OpenDoorAndScare());
        }
        else
        {
            if (door) StartCoroutine(OpenDoorRoutine());
            if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position);
            TriggerScare();
        }
    }

    private void TriggerScare()
    {
        if (scareDelayAfterDoor > 0f)
        {
            StartCoroutine(DelayedScare());
            return;
        }
        DoScare();
    }

    private System.Collections.IEnumerator DelayedScare()
    {
        yield return new WaitForSeconds(scareDelayAfterDoor);
        DoScare();
    }

    private void DoScare()
    {
        // �Դ�ͧ��ǧ
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
        if (fridgeGhost)
        {
            fridgeGhost.SetActive(true);
            if (ghostSfx) AudioSource.PlayClipAtPoint(ghostSfx, fridgeGhost.transform.position);
            if (ghostVisibleTime > 0f) StartCoroutine(HideGhost());
        }
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

    private System.Collections.IEnumerator HideGhost()
    {
        yield return new WaitForSeconds(ghostVisibleTime);
        if (fridgeGhost) fridgeGhost.SetActive(false);
    }
}
