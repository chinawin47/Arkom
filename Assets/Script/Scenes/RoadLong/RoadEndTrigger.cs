using UnityEngine;
using ARKOM.Player;

namespace ARKOM.Scenes.Road
{
 [RequireComponent(typeof(BoxCollider))]
 public class RoadEndTrigger : MonoBehaviour
 {
 public RoadSequenceController controller;
 private void Reset()
 {
 var bc = GetComponent<BoxCollider>();
 bc.isTrigger = true;
 var rb = GetComponent<Rigidbody>();
 if (!rb) rb = gameObject.AddComponent<Rigidbody>();
 rb.isKinematic = true; rb.useGravity = false;
 }
 private void OnTriggerEnter(Collider other)
 {
 if (!controller || !controller.enabled) return;
 var player = other.GetComponentInParent<PlayerController>();
 if (player)
 controller.OnEndOfPathReached(player);
 }
 }
}
