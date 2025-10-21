using UnityEngine;

namespace ARKOM.Player
{
 // Global helper for player hidden state (closet/cabinet)
 public static class PlayerStealth
 {
 public static bool IsHidden { get; private set; }
 public static Transform CurrentHideSpot { get; private set; }

 public static void SetHidden(Transform hideSpot)
 {
 IsHidden = hideSpot != null;
 CurrentHideSpot = hideSpot;
 }

 public static void Clear()
 {
 IsHidden = false;
 CurrentHideSpot = null;
 }
 }
}
