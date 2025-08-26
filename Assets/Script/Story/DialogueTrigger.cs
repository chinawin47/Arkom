using UnityEngine;
using ARKOM.Story;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("���� Flag �е������ͼ��������ʹ�� (�����á��ҹ��)")]
    public string flagOnEnter;
    public string dialogueId;
    [Tooltip("���������")]
    public bool oneShot = true;
    private bool fired;

    private void OnTriggerEnter(Collider other)
    {
        if (fired && oneShot) return;
        if (!other.CompareTag("Player")) return;
        if (!string.IsNullOrEmpty(flagOnEnter))
            StoryFlags.Instance.Add(flagOnEnter);
        if (!string.IsNullOrEmpty(dialogueId))
            DialogueSystem.Instance.ShowDialogue(dialogueId);
        fired = true;
    }
}