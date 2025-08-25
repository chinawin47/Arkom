using UnityEngine;
using ARKOM.Story;

public class EvidencePickup : Interactable
{
    public EvidenceItem evidence;
    [SerializeField] private bool destroyOnPickup = false; // ����¹������������ false
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

        // ��������ҡ������� ��� clearFlag=false
        if (clearFlag && evidence && StoryFlags.Instance)
        {
            // ��������ʹź� StoryFlags (�������͹ flag) ��ͧ�Ѵ�Թ��ͧ��Ҩ���ҧ StoryFlags.Instance ��駡�͹
        }
        collected = false;
        if (destroyOnPickup)
        {
            // ��ҷ���·�駨�ԧ ��ͧ Instantiate ���� (�� prefab ����ͧ��Ҩ��ͧ�Ѻ)
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