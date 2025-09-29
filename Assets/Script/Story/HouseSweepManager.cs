using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    public class HouseSweepManager : MonoBehaviour
    {
        [Tooltip("�ش�������蹵�ͧ�Թ��ҹ���ú")] public global::ARKOM.Story.SweepPoint[] points; // fully-qualified
        private int visited;
        private bool completed;

        public void BeginSweep()
        {
            visited = 0; completed = false;
            StoryDebug.Log("BeginSweep: points=" + points.Length, this);
            foreach (var p in points) if (p) p.Activate(this);
        }

        public void ReportVisited(global::ARKOM.Story.SweepPoint p)
        {
            if (completed) return;
            if (!p || !p.active) return;
            p.active = false; // ��ͧ�ѹ�Ѻ���
            visited++;
            StoryDebug.Log("SweepPoint visited: " + p.name + " (" + visited + "/" + points.Length + ")", p);
            if (visited >= points.Length)
            {
                completed = true;
                StoryDebug.Log("All sweep points visited", this);
                EventBus.Publish(new SweepCompleteEvent());
            }
        }
    }
}
