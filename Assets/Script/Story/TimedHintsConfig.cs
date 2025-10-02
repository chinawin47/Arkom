using UnityEngine;

namespace ARKOM.Story
{
    [CreateAssetMenu(menuName = "Story/Timed Hints Config", fileName = "TimedHintsConfig")]
    public class TimedHintsConfig : ScriptableObject
    {
        public enum HintStrength { Subtle = 0, Medium = 1, Explicit = 2 }

        // แบบเดิม (ยังรองรับอยู่ เพื่อความเข้ากันได้ย้อนหลัง)
        [System.Serializable]
        public class TimedHint
        {
            public SequenceController.StoryState state;
            [Tooltip("เวลาตั้งแต่เข้า state (วินาที)")] public float timeFromEnter = 0f;
            [TextArea] public string text;
            public float duration = 3f;
            [Tooltip("ระดับความตรงของคำใบ้ (Subtle=เบา, Medium=ชี้ทิศ, Explicit=เฉลยเกือบหมด)")] public HintStrength strength = HintStrength.Subtle;
            [Tooltip("ถ้า true จะแสดงเฉพาะยังอยู่ state เดิม (กันไว้หาก scheduler ใช้ร่วม future)")] public bool onlyIfStillInState = true;
        }

        // โครงใหม่: กลุ่มคำใบ้หลายอันต่อหนึ่ง state เพื่อลดการสร้างรายการ state ซ้ำ ๆ
        [System.Serializable]
        public class GroupTimedHint
        {
            [Tooltip("เวลาตั้งแต่เข้า state (วินาที)")] public float timeFromEnter = 0f;
            [TextArea] public string text;
            public float duration = 3f;
            public HintStrength strength = HintStrength.Subtle;
        }

        [System.Serializable]
        public class StateHintGroup
        {
            public SequenceController.StoryState state;
            [Tooltip("คำใบ้ย่อยทั้งหมดของ state นี้")] public GroupTimedHint[] hints;
        }

        [Header("(Legacy) รายการเดิม - จะถูกใช้ถ้าไม่ตั้ง groupedHints")]
        public TimedHint[] hints; // unordered list (legacy)

        [Header("กลุ่มคำใบ้แบบใหม่")]
        public StateHintGroup[] groupedHints; // ถ้ามีจะใช้กลุ่มนี้แทน hints เดิม
    }
}
