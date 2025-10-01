using UnityEngine;

namespace ARKOM.Story
{
    [CreateAssetMenu(menuName = "Story/Story Hints Config", fileName = "StoryHintsConfig")]
    public class StoryHintsConfig : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public SequenceController.StoryState state;
            [TextArea] public string hintText;
            public float duration = 4f;
        }

        public Entry[] entries;

        public bool TryGet(SequenceController.StoryState s, out Entry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null && entries[i].state == s)
                    {
                        entry = entries[i];
                        return true;
                    }
                }
            }
            entry = null;
            return false;
        }
    }
}
