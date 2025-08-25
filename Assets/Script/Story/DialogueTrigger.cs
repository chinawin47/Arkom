using UnityEngine;
using ARKOM.Story;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("ชื่อ Flag จะตั้งเมื่อผู้เล่นเข้าสนาม (ครั้งแรกเท่านั้น)")]
    public string flagOnEnter;
    [Tooltip("ใช้ครั้งเดียว")]
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
        // จุดนี้ต่อระบบ Dialogue จริงภายหลัง (เช่น เรียก DialogueSystem.Play(id))
    }
}