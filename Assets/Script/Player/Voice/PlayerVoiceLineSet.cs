using System.Collections.Generic;
using UnityEngine;

namespace ARKOM.Player.Voice
{
    [CreateAssetMenu(menuName = "PlayerVoice/VoiceLineSet", fileName = "VoiceLineSet_Asset")]
    public class PlayerVoiceLineSet : ScriptableObject
    {
        [Tooltip("??? Voice Line ???????????????")] public List<PlayerVoiceLine> lines = new();

        public PlayerVoiceLine Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                if (l && l.lineId == id) return l;
            }
            return null;
        }

        public IEnumerable<PlayerVoiceLine> All => lines;
    }
}
