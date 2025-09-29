using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    [RequireComponent(typeof(Collider))]
    public class FinalBedTrigger : MonoBehaviour
    {
        public bool initiallyDisabled = true;
        private bool enabledBed;
        private bool fired;

        void Start()
        {
            if (initiallyDisabled)
            {
                enabledBed = false;
                gameObject.SetActive(false);
            }
            else
            {
                enabledBed = true;
            }
        }

        public void EnableBed()
        {
            if (enabledBed) return;
            enabledBed = true;
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enabledBed || fired) return;
            if (!other.CompareTag("Player")) return;
            fired = true;
            EventBus.Publish(new PlayerInBedEvent());
        }
    }
}
