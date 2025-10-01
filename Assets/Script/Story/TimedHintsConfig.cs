using UnityEngine;

namespace ARKOM.Story
{
    [CreateAssetMenu(menuName = "Story/Timed Hints Config", fileName = "TimedHintsConfig")]
    public class TimedHintsConfig : ScriptableObject
    {
        [System.Serializable]
        public class TimedHint
        {
            public SequenceController.StoryState state;
            [Tooltip("���ҵ������� state (�Թҷ�)")] public float timeFromEnter = 0f;
            [TextArea] public string text;
            public float duration = 3f;
        }

        public TimedHint[] hints; // unordered list
    }
}
