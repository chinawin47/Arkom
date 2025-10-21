using UnityEngine;

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Power Manager")]
 public class PowerManager : MonoBehaviour
 {
 public Light[] normalLights;
 public Light[] emergencyLights;
 public void SetPower(bool on)
 {
 if (normalLights != null)
 {
 foreach (var l in normalLights) if (l) l.enabled = on;
 }
 if (emergencyLights != null)
 {
 foreach (var l in emergencyLights) if (l) l.enabled = !on;
 }
 }
 }
}
