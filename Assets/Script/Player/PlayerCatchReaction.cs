using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Player
{
 [AddComponentMenu("Player/Catch Camera Reaction (Simple)")]
 public class PlayerCatchReaction : MonoBehaviour
 {
 [Header("Refs")]
 public PlayerController player;
 public CharacterController charController;

 [Header("Behaviour")]
 [Tooltip("ปิดการควบคุมและ CharacterController เมื่อถูกจับ")]
 public bool disablePlayerOnCatch = true;
 [Tooltip("ย้ายตัวผู้เล่นไปยังจุดกอด (holdAnchor) ถ้ามี เพื่อให้ชิดผีจริงๆ")]
 public bool snapPlayerToHoldAnchor = true;
 [Tooltip("แยกกล้องออกจาก Player ชั่วคราว เพื่อไม่ให้ถูกพากลับโดยการเคลื่อนที่ของผู้เล่น/แอนิเมชัน")]
 public bool detachCameraFromPlayer = true;
 [Tooltip("ชดเชยแกน Y ตอนสแน็ปผู้เล่นไปที่ holdAnchor ถ้า pivot อยู่ที่เท้า/อกไม่ตรงกัน")]
 public float snapPlayerYOffset =0f;

 private bool applied;
 private Transform savedCamParent; private Vector3 savedCamLocalPos; private Quaternion savedCamLocalRot;

 private void Awake()
 {
 if (!player) player = GetComponent<PlayerController>();
 if (!charController && player) charController = player.GetComponent<CharacterController>();
 }

 private void OnEnable()
 {
 EventBus.Subscribe<PlayerCaughtEvent>(OnPlayerCaught);
 }
 private void OnDisable()
 {
 EventBus.Unsubscribe<PlayerCaughtEvent>(OnPlayerCaught);
 }

 private void OnPlayerCaught(PlayerCaughtEvent e)
 {
 if (applied || player == null) return;
 var camRoot = player.cameraRoot ? player.cameraRoot : (player.mainCamera ? player.mainCamera.transform : null);
 if (!camRoot) return;
 var camAnchor = e.CameraAnchor ? e.CameraAnchor : e.Ghost;
 if (!camAnchor) return;

 //1) Disable controls if requested
 if (disablePlayerOnCatch)
 {
 player.enabled = false;
 if (charController) charController.enabled = false;
 }

 //2) Snap player to hold anchor and face ghost (horizontal)
 if (snapPlayerToHoldAnchor && e.HoldAnchor)
 {
 Vector3 p = e.HoldAnchor.position; p.y += snapPlayerYOffset;
 player.transform.position = p;
 if (e.Ghost)
 {
 Vector3 dir = e.Ghost.position - player.transform.position; dir.y =0f;
 if (dir.sqrMagnitude >0.0001f) player.transform.rotation = Quaternion.LookRotation(dir);
 }
 }

 //3) Detach camera from player to avoid being dragged back
 if (detachCameraFromPlayer)
 {
 savedCamParent = camRoot.parent; savedCamLocalPos = camRoot.localPosition; savedCamLocalRot = camRoot.localRotation;
 camRoot.SetParent(null, true);
 }

 //4) Place camera exactly at anchor (you adjust anchor in scene)
 camRoot.position = camAnchor.position;
 camRoot.rotation = camAnchor.rotation;

 applied = true;
 }

 // Public helper for checkpoint reset: restore camera parenting and re-enable controls
 public void RestoreToDefault()
 {
 if (!player) return;
 var camRoot = player.cameraRoot ? player.cameraRoot : (player.mainCamera ? player.mainCamera.transform : null);
 if (camRoot)
 {
 if (detachCameraFromPlayer)
 {
 camRoot.SetParent(savedCamParent, true);
 if (savedCamParent != null)
 {
 camRoot.localPosition = savedCamLocalPos;
 camRoot.localRotation = savedCamLocalRot;
 }
 }
 }
 if (charController) charController.enabled = true;
 player.enabled = true;
 applied = false;
 }
 }
}
