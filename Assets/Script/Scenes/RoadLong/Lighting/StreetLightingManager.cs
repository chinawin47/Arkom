using System.Collections.Generic;
using UnityEngine;

namespace ARKOM.Scenes.Road
{
 public class StreetLightingManager : MonoBehaviour
 {
 public static StreetLightingManager Instance { get; private set; }

 [Header("References")]
 [Tooltip("ผู้เล่น ใช้สำหรับเปิดใช้งานหลอดไฟเฉพาะบริเวณใกล้ตัว")] public Transform player;
 [Tooltip("ค้นหา Player อัตโนมัติถ้า player ว่าง")] public bool autoFindPlayer = true;

 [Header("Car Pass / Surge")] 
 [Tooltip("รัศมีผลกระทบเมื่อมีรถวิ่งผ่าน (หลอดไฟในรัศมีจะกระพริบระยะสั้น)")] public float carPassSurgeRadius =25f;
 [Tooltip("ระยะเวลาที่หลอดไฟกระพริบจากคลื่นรถผ่าน")]
 public Vector2 carPassBurstDurationRange = new Vector2(1.2f,2.5f);
 [Tooltip("เพิ่มโอกาสดับมิดชั่วคราวระหว่างคลื่นรถผ่าน")]
 public float carPassExtraHardBlink =0.25f;

 private readonly HashSet<StreetLampController> lamps = new HashSet<StreetLampController>();

 private void Awake()
 {
 if (Instance && Instance != this) { Destroy(gameObject); return; }
 Instance = this;
 if (!player && autoFindPlayer)
 {
 var pc = FindObjectOfType<ARKOM.Player.PlayerController>();
 if (pc) player = pc.transform;
 }
 }

 public void Register(StreetLampController lamp)
 {
 if (!lamp) return;
 lamps.Add(lamp);
 if (player) lamp.SetPlayer(player);
 }

 public void Unregister(StreetLampController lamp)
 {
 if (!lamp) return;
 lamps.Remove(lamp);
 }

 public void SetPlayer(Transform t)
 {
 player = t;
 foreach (var l in lamps)
 if (l) l.SetPlayer(player);
 }

 // เรียกเมื่อมีรถวิ่งผ่านบริเวณ pos (หรือใช้ตำแหน่งผู้เล่น)
 public void TriggerCarPass(Vector3 pos)
 {
 float r2 = carPassSurgeRadius * carPassSurgeRadius;
 float dur = Random.Range(carPassBurstDurationRange.x, carPassBurstDurationRange.y);
 foreach (var l in lamps)
 {
 if (!l) continue;
 var d2 = (l.transform.position - pos).sqrMagnitude;
 if (d2 <= r2)
 {
 l.TriggerSurgeFlicker(dur, carPassExtraHardBlink);
 }
 }
 }
 }
}
