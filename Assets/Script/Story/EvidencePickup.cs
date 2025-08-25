using UnityEngine;
using ARKOM.Story;

public class EvidencePickup : Interactable
{
    public EvidenceItem evidence;
    [SerializeField] private bool destroyOnPickup = false; // เปลี่ยนค่าเริ่มต้นเป็น false
    private bool collected;
    private Vector3 initialPos;
    private Quaternion initialRot;

    void Start()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
        EvidenceRegistry.Register(this);
    }

    protected override void OnInteract(object interactor)
    {
        if (collected) return;
        if (evidence && !string.IsNullOrEmpty(evidence.flagId))
        {
            if (StoryFlags.Instance.Add(evidence.flagId))
                Debug.Log($"[Evidence] Collected {evidence.displayName} -> Flag {evidence.flagId}");
        }

        collected = true;
        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    public void ResetEvidence(bool clearFlag)
    {
        if (destroyOnPickup && this == null) return;
        if (!gameObject) return;

        // ถ้าไม่อยากให้ได้ซ้ำ ให้ clearFlag=false
        if (clearFlag && evidence && StoryFlags.Instance)
        {
            // ไม่มีเมธอดลบใน StoryFlags (จะไม่ย้อน flag) ต้องตัดสินใจเองว่าจะล้าง StoryFlags.Instance ทั้งก้อน
        }
        collected = false;
        if (destroyOnPickup)
        {
            // ถ้าทำลายทิ้งจริง ต้อง Instantiate ใหม่ (เก็บ prefab ไว้เองถ้าจะรองรับ)
        }
        else
        {
            transform.position = initialPos;
            transform.rotation = initialRot;
            gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        EvidenceRegistry.Unregister(this);
    }
}