using UnityEngine;
using ARKOM.Core; // for EventBus

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Power Manager")]
 public class PowerManager : MonoBehaviour
 {
 [Header("Light Groups")]
 public Light[] normalLights;
 public Light[] emergencyLights;

 [Header("Debug")] public bool logChanges = false;

 private bool currentOn;

 private void OnEnable()
 {
 EventBus.Subscribe<PowerRestoredEvent>(_ => SetPower(true));
 EventBus.Subscribe<BlackoutStartedEvent>(_ => SetPower(false));
 }
 private void OnDisable()
 {
 EventBus.Unsubscribe<PowerRestoredEvent>(_ => SetPower(true));
 EventBus.Unsubscribe<BlackoutStartedEvent>(_ => SetPower(false));
 }

 public void SetPower(bool on)
 {
 currentOn = on;
 if (normalLights != null)
 {
 for (int i =0; i < normalLights.Length; i++)
 {
 var l = normalLights[i]; if (!l) continue; l.enabled = on;
 }
 }
 if (emergencyLights != null)
 {
 for (int i =0; i < emergencyLights.Length; i++)
 {
 var l = emergencyLights[i]; if (!l) continue; l.enabled = !on;
 }
 }
 if (logChanges) Debug.Log($"[PowerManager] Power {(on ? "ON" : "OFF")} -> normal={normalLights?.Length ??0}, emergency={emergencyLights?.Length ??0}", this);
 }
 }
}
