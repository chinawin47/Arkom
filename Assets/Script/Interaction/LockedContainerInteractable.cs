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

    [Header("Lid Opening (Optional)")]
    [Tooltip("ตัว Transform ของฝากล่องที่จะหมุนเปิด (ทิศแกน = local)")]
    public Transform lid;
    [Tooltip("การหมุนเพิ่มจากมุมปิด (local euler) ตอนเปิดฝากล่อง")]
    public Vector3 lidOpenOffsetEuler = new Vector3(-110f,0f,0f);
    [Tooltip("เวลาเปิดฝากล่อง (วินาที)")]
    public float lidOpenTime =0.7f;
    public AnimationCurve lidOpenCurve = AnimationCurve.EaseInOut(0f,0f,1f,1f);

    private bool opened;
    private bool awaitingPin;
    private Quaternion lidClosedLocalRot;
    private bool lidCached;

    public override bool CanInteract(object interactor)
    {
        if (opened && oneShot) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (opened && oneShot) return;

        // ???? SequenceController ??????????? Hint (???????????? GhostSpawn ???????? Hint)
        if (SequenceController.Instance) SequenceController.Instance.NotifyMysteryBoxAttempt();
        if (SequenceController.Instance && SequenceController.Instance.CurrentState == SequenceController.StoryState.FindNotes)
        {
            // ?????? re-show hint ????????????????????????
            SequenceController.Instance.ForceFindNotesHint();
        }

        // ===== PIN =====
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
            if (awaitingPin) return;
            awaitingPin = true;
            pinUI.Show(pinCode, (ok) =>
            {
                awaitingPin = false;
                if (ok) OpenNow(); else {
                    if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume);
                    // ??? UI ???????? hint ??????????????????????
                    if (SequenceController.Instance) SequenceController.Instance.OnMysteryBoxClosed(false);
                }
            });
            return;
        }
        if (!Keyring.Has(requiredKeyId)) { if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position, volume); return; }
        OpenNow();
    }

    private void OpenNow()
    {
        opened = true;
        if (lockedVisual) lockedVisual.SetActive(false);
        if (openedVisual) openedVisual.SetActive(true);
        if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position, volume);
        EventBus.Publish(new BoxUnlockedEvent());

        if (SequenceController.Instance) SequenceController.Instance.OnMysteryBoxClosed(true);

        // Animate lid if provided
        if (lid)
        {
            if (!lidCached)
            {
                lidClosedLocalRot = lid.localRotation;
                lidCached = true;
            }
            StartCoroutine(OpenLidRoutine());
        }
    }

    private System.Collections.IEnumerator OpenLidRoutine()
    {
        float t =0f;
        Quaternion from = lidClosedLocalRot;
        Quaternion to = lidClosedLocalRot * Quaternion.Euler(lidOpenOffsetEuler);
        float dur = Mathf.Max(0.01f, lidOpenTime);
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float k = lidOpenCurve != null ? lidOpenCurve.Evaluate(p) : p;
            lid.localRotation = Quaternion.Slerp(from, to, k);
            yield return null;
        }
        lid.localRotation = to;
    }
}
