using UnityEngine;

namespace ARKOM.Story
{
 public readonly struct InvestigatePointEvent
 {
 public readonly Vector3 Position;
 public InvestigatePointEvent(Vector3 position)
 {
 Position = position;
 }
 }
}
