using UnityEngine;
using ARKOM.Story;

namespace ARKOM.Audio
{
    [CreateAssetMenu(menuName = "Audio/Ambience State Profile", fileName = "AmbienceStateProfile")]
    public class AmbienceStateProfile : ScriptableObject
    {
        [System.Serializable]
        public class StateAmbience
        {
            public SequenceController.StoryState state;
            [Tooltip("คลิป loop หลักสำหรับ state นี้")] public AudioClip baseLoop;
            [Tooltip("Stinger (เล่นครั้งเดียวตอนเข้า)")] public AudioClip enterStinger;
            [Range(0f,10f)] public float fadeIn = 2f;
            [Range(0f,10f)] public float fadeOut = 2f;
            public bool loop = true;
            [Range(0f,1f)] public float volume = 1f;
        }

        public StateAmbience[] entries;

        public bool TryGet(SequenceController.StoryState st, out StateAmbience ambience)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e != null && e.state == st)
                    {
                        ambience = e;
                        return true;
                    }
                }
            }
            ambience = null;
            return false;
        }
    }
}
