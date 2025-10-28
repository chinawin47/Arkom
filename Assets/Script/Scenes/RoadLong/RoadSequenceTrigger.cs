using UnityEngine;

namespace ARKOM.Scenes.Road
{
 [RequireComponent(typeof(BoxCollider))]
 public class RoadSequenceTrigger : MonoBehaviour
 {
 public int nodeIndex =1; //1..4
 public RoadSequenceController controller;

 private void Reset()
 {
 var bc = GetComponent<BoxCollider>();
 bc.isTrigger = true;
 // Ensure trigger has a kinematic Rigidbody so it can detect CharacterController without RB
 var rb = GetComponent<Rigidbody>();
 if (!rb) rb = gameObject.AddComponent<Rigidbody>();
 rb.isKinematic = true;
 rb.useGravity = false;
 }

 private void Awake()
 {
 // Safety ensure Rigidbody is configured
 var rb = GetComponent<Rigidbody>();
 if (!rb)
 {
 rb = gameObject.AddComponent<Rigidbody>();
 rb.isKinematic = true;
 rb.useGravity = false;
 }
 else
 {
 rb.isKinematic = true;
 rb.useGravity = false;
 }
 }

 private void OnTriggerEnter(Collider other)
 {
 if (!controller) return;
 var player = other.GetComponentInParent<ARKOM.Player.PlayerController>();
 if (player)
 controller.OnNodeTriggered(nodeIndex);
 }
 }
}
