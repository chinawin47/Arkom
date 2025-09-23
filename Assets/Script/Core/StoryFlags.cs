using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    // ��ʶҹи� (Flag) ��������ͧ��ҧ��
    public class StoryFlags : MonoBehaviour
    {
        public static StoryFlags Instance { get; private set; }

        [Tooltip("�ʴ� Flag ����� (debug)")]
        public List<string> acquired = new List<string>();

        public IReadOnlyCollection<string> All => acquired.AsReadOnly();

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool Has(string flag) => acquired.Contains(flag);

        public bool Add(string flag)
        {
            if (string.IsNullOrEmpty(flag) || acquired.Contains(flag)) return false;
            acquired.Add(flag);
            EventBus.Publish(new StoryFlagAddedEvent(flag));
            return true;
        }

        // �������ʹ��ҧ������ (��੾�е͹ Restart)
        public void ClearAll()
        {
            acquired.Clear();
        }
    }

    public readonly struct StoryFlagAddedEvent
    {
        public readonly string Flag;
        public StoryFlagAddedEvent(string flag) { Flag = flag; }
    }
}