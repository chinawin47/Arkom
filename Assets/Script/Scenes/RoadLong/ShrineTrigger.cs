using UnityEngine;
using ARKOM.Player;

namespace ARKOM.Scenes.Road
{
 [RequireComponent(typeof(Collider))]
 public class ShrineTrigger : MonoBehaviour
 {
 public RoadSequenceController controller;
 private void Reset()
 {
 var col = GetComponent<Collider>();
 col.isTrigger = true;
 }
 private void OnTriggerEnter(Collider other)
 {
 if (!controller) return;
 var player = other.GetComponentInParent<PlayerController>();
 if (player)
 {
 controller.OnShrineReached();
 }
 }
 }
}
