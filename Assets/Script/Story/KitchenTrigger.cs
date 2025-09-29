using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    [RequireComponent(typeof(Collider))]
    public class KitchenTrigger : MonoBehaviour
    {
        private bool fired;
        private void OnTriggerEnter(Collider other)
        {
            if (fired) return;
            if (!other.CompareTag("Player")) return;
            fired = true;
            EventBus.Publish(new KitchenEnteredEvent());
        }
    }
}
