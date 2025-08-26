using UnityEngine;
using ARKOM.Story;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("ชื่อ Flag จะตั้งเมื่อผู้เล่นเข้าสนาม (ครั้งแรกเท่านั้น)")]
    public string flagOnEnter;
    public string dialogueId;
    [Tooltip("ใช้ครั้งเดียว")]
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