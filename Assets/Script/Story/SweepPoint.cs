using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    [RequireComponent(typeof(Collider))]
    public class SweepPoint : MonoBehaviour
    {
        [HideInInspector] public bool active;
        private HouseSweepManager manager;
        public void Activate(HouseSweepManager mgr){ manager = mgr; active = true; gameObject.SetActive(true); StoryDebug.Log("SweepPoint activated: " + name, this); }
        private void OnTriggerEnter(Collider other)
        {
            if (!active) return;
            if (!other.CompareTag("Player")) return;
            StoryDebug.Log("SweepPoint trigger by Player: " + name, this);
            manager?.ReportVisited(this);
        }
    }
}
