using UnityEngine;
using ARKOM.Story;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("���� Flag �е������ͼ��������ʹ�� (�����á��ҹ��)")]
    public string flagOnEnter;
    [Tooltip("���������")]
    public bool oneShot = true;
    private bool fired;

    private void OnTriggerEnter(Collider other)
    {
        if (fired && oneShot) return;
        if (!other.CompareTag("Player")) return;
        if (!string.IsNullOrEmpty(flagOnEnter))
        {
            if (StoryFlags.Instance.Add(flagOnEnter))
                Debug.Log($"[DialogueTrigger] Flag {flagOnEnter} set");
        }
        fired = true;
        // �ش������к� Dialogue ��ԧ�����ѧ (�� ���¡ DialogueSystem.Play(id))
    }
}