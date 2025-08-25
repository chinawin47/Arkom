using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    // เก็บสถานะธง (Flag) เนื้อเรื่องกลางเกม
    public class StoryFlags : MonoBehaviour
    {
        public static StoryFlags Instance { get; private set; }

        [Tooltip("แสดง Flag ที่ได้ (debug)")]
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

        // เพิ่มเมธอดล้างทั้งหมด (ใช้เฉพาะตอน Restart)
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