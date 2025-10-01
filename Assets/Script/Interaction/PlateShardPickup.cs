using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Plate Shard Pickup")]
public class PlateShardPickup : Interactable
{
    public static int TotalNeeded = 0; // ��˹������͹����� (���ͤӹǳ runtime)
    private static int collected = 0;
    private bool picked;

    [Header("Visibility")]
    [Tooltip("��͹ renderer / collider �����Ҩ��������൨����ɨҹ")] public bool hideUntilCleanPlates = true;

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    void Awake()
    {
        CacheComponents();
        if (hideUntilCleanPlates)
        {
            // ��͹���������� (���Դ GameObject ������� FindObjectsOfType ��)
            SetVisible(false);
        }
    }

    private void CacheComponents()
    {
        if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedColliders == null) cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private void SetVisible(bool visible)
    {
        CacheComponents();
        foreach (var r in cachedRenderers)
            if (r) r.enabled = visible && !picked;
        foreach (var c in cachedColliders)
            if (c) c.enabled = visible && !picked;
    }

    private void OnEnable()
    {
        // ���������������͹���
        if (picked)
        {
            SetVisible(false);
        }
        else if (hideUntilCleanPlates)
        {
            // �ѧ������൨ ? ��͹ (���Ͷ١ enable �����˵ؼ����)
            if (!SequenceController.Instance || SequenceController.Instance.CurrentState != SequenceController.StoryState.CleanPlates)
                SetVisible(false);
        }
    }

    public override bool CanInteract(object interactor)
    {
        if (picked) return false;
        // ��ͧ�����ʶҹ� CleanPlates ��ҹ��
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
        SetVisible(false);

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

    // ���¡�͹������൨ CleanPlates ����������Ъ�����
    internal void RevealForCleanPlates()
    {
        if (picked) return;
        hideUntilCleanPlates = false; // ����ͧ��͹�ա
        SetVisible(true);
    }
}
