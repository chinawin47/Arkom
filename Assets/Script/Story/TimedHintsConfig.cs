using UnityEngine;

namespace ARKOM.Story
{
    [CreateAssetMenu(menuName = "Story/Timed Hints Config", fileName = "TimedHintsConfig")]
    public class TimedHintsConfig : ScriptableObject
    {
        public enum HintStrength { Subtle = 0, Medium = 1, Explicit = 2 }

        // Ẻ��� (�ѧ�ͧ�Ѻ���� ���ͤ�����ҡѹ����͹��ѧ)
        [System.Serializable]
        public class TimedHint
        {
            public SequenceController.StoryState state;
            [Tooltip("���ҵ������� state (�Թҷ�)")] public float timeFromEnter = 0f;
            [TextArea] public string text;
            public float duration = 3f;
            [Tooltip("�дѺ�����ç�ͧ���� (Subtle=��, Medium=�����, Explicit=�����ͺ���)")] public HintStrength strength = HintStrength.Subtle;
            [Tooltip("��� true ���ʴ�੾���ѧ���� state ��� (�ѹ����ҡ scheduler ������ future)")] public bool onlyIfStillInState = true;
        }

        // �ç����: ��������������ѹ���˹�� state ����Ŵ������ҧ��¡�� state ��� �
        [System.Serializable]
        public class GroupTimedHint
        {
            [Tooltip("���ҵ������� state (�Թҷ�)")] public float timeFromEnter = 0f;
            [TextArea] public string text;
            public float duration = 3f;
            public HintStrength strength = HintStrength.Subtle;
        }

        [System.Serializable]
        public class StateHintGroup
        {
            public SequenceController.StoryState state;
            [Tooltip("�������·������ͧ state ���")] public GroupTimedHint[] hints;
        }

        [Header("(Legacy) ��¡����� - �ж١��������� groupedHints")]
        public TimedHint[] hints; // unordered list (legacy)

        [Header("���������Ẻ����")]
        public StateHintGroup[] groupedHints; // ����ը����������᷹ hints ���
    }
}
