using System.Collections;
using UnityEngine;

namespace ARKOM.Scenes.Road
{
 public class StreetLightFlicker : MonoBehaviour
 {
 // โหนดพาเรนต์ที่ใช้ค้นหาไฟทั้งหมดสำหรับกระพริบ (ถ้าไม่ตั้งค่า จะใช้โหนดของสคริปต์นี้)
 [Tooltip("Lights under this root will be flickered (if null, search children)")] public Transform lightsRoot;
 
 // ค่าคูณความสว่างขั้นต่ำ-สูงสุด (คูณกับ intensity เดิมของหลอดไฟ)
 [Tooltip("Randomize intensity range multipliers")] public Vector2 intensityMulRange = new Vector2(0.4f,1.2f);
 
 // ช่วงความเร็วการกระพริบ (ต่อดวง แรนดอมด้วย PerlinNoise ให้ต่างกัน)
 [Tooltip("Flicker speed range")] public Vector2 speedRange = new Vector2(6f,14f);
 
 // โอกาสดับสนิทชั่วครู่ต่อเฟรม (สเกลด้วย deltaTime และ speed)
 [Tooltip("Chance that a light fully blinks off at samples")] [Range(0,1)] public float hardBlinkChance =0.15f;
 
 // ทำให้แต่ละดวงไม่ตรงจังหวะกัน (สุ่มเฟสต่อดวง)
 [Tooltip("Per-light desync offset")] public float phaseJitter =10f;
 
 // ระยะเวลามาตรฐานของการกระพริบแบบครั้งเดียว เมื่อเรียก StartFlicker() ไม่ส่งพารามิเตอร์
 [Tooltip("Default duration for StartFlicker() without parameters")] public float defaultDuration =3f;
 
 [Header("Looped Bursts")]
 // ถ้าเปิด จะเริ่มโหมดกระพริบเป็นรอบอัตโนมัติใน Start()
 [Tooltip("If true, auto start looped flicker bursts with cooldowns on Start()")] public bool autoStartLoopOnStart = false;
 
 // ระยะเวลาที่ไฟจะกระพริบในแต่ละรอบ (burst)
 [Tooltip("Duration of each flicker burst when looping")] public float loopBurstDuration =3f;
 
 // ช่วงเวลาพักระหว่างรอบกระพริบ (สุ่มในช่วงนี้ทุกครั้ง)
 [Tooltip("Cooldown range (seconds) between bursts")] public Vector2 loopCooldownRange = new Vector2(3f,6f);
 
 // แคชรายการไฟและค่า intensity เดิม
 private Light[] lights;
 private float[] baseIntensity;
 
 // คอร์รูทีนสำหรับรอบกระพริบครั้งเดียว (burst)
 private Coroutine routine;
 
 // คอร์รูทีนสำหรับโหมดลูป (burst -> cooldown -> ซ้ำ)
 private Coroutine loopRoutine;

 private void Awake()
 {
 // ถ้าไม่กำหนด lightsRoot ให้ใช้โหนดเดียวกับสคริปต์ แล้วสแกนหาไฟลูกทั้งหมด
 if (!lightsRoot) lightsRoot = transform;
 CacheLights();
 }

 private void Start()
 {
 // เริ่มโหมดกระพริบเป็นรอบโดยอัตโนมัติถ้าเปิดใช้งานไว้
 if (autoStartLoopOnStart)
 StartFlickerLoop();
 }

 private void OnDisable()
 {
 // เมื่อปิด/ถูกปิดการทำงาน: หยุดคอร์รูทีนทั้งหมด และคืนค่า intensity เดิมให้ทุกดวง
 if (routine != null) { StopCoroutine(routine); routine = null; }
 if (loopRoutine != null) { StopCoroutine(loopRoutine); loopRoutine = null; }
 RestoreIntensity();
 }

 private void OnValidate()
 {
 // บังคับค่าในอินสเปกเตอร์ให้อยู่ในช่วงที่ถูกต้อง ป้องกันค่าเพี้ยน
 hardBlinkChance = Mathf.Clamp01(hardBlinkChance);
 intensityMulRange.x = Mathf.Max(0f, intensityMulRange.x);
 intensityMulRange.y = Mathf.Max(intensityMulRange.x, intensityMulRange.y);
 speedRange.x = Mathf.Max(0f, speedRange.x);
 speedRange.y = Mathf.Max(speedRange.x, speedRange.y);
 defaultDuration = Mathf.Max(0f, defaultDuration);
 loopBurstDuration = Mathf.Max(0f, loopBurstDuration);
 if (loopCooldownRange.x <0f) loopCooldownRange.x =0f;
 if (loopCooldownRange.y < loopCooldownRange.x) loopCooldownRange.y = loopCooldownRange.x;
 }

 // สแกนหาไฟลูกทั้งหมดใต้ lightsRoot และจำค่า intensity เดิมไว้
 private void CacheLights()
 {
 lights = lightsRoot.GetComponentsInChildren<Light>(includeInactive:true);
 baseIntensity = new float[lights.Length];
 for (int i=0;i<lights.Length;i++) baseIntensity[i] = lights[i].intensity;
 }

 // คืนค่า intensity ของทุกดวงกลับค่าเดิม
 private void RestoreIntensity()
 {
 if (lights == null) return;
 for (int i=0;i<lights.Length;i++) if (i < baseIntensity.Length && lights[i]) lights[i].intensity = baseIntensity[i];
 }

 // ===== Public helpers =====

 // รีสแกนไฟใหม่ (กรณีเพิ่ม/ลบไฟหลังจากเริ่มซีน หรือเปลี่ยน lightsRoot)
 public void RescanLights()
 {
 if (!lightsRoot) lightsRoot = transform;
 CacheLights();
 }

 // ตั้งค่าโอกาสดับสนิทชั่วคราว และระยะเฟสสุ่ม ต่อการทำงานครั้งถัดไป
 public void SetFlickerParams(float newHardBlinkChance, float newPhaseJitter)
 {
 hardBlinkChance = Mathf.Clamp01(newHardBlinkChance);
 phaseJitter = newPhaseJitter;
 }

 // ===== One-shot (กระพริบครั้งเดียว) =====

 // เริ่มกระพริบด้วยระยะเวลามาตรฐาน
 public void StartFlicker()
 {
 StartFlicker(defaultDuration);
 }

 // เริ่มกระพริบด้วยระยะเวลาที่กำหนด (หยุดลูปไว้ชั่วคราวถ้ามี)
 public void StartFlicker(float duration)
 {
 if (loopRoutine != null) { StopCoroutine(loopRoutine); loopRoutine = null; }
 if (routine != null) StopCoroutine(routine);
 routine = StartCoroutine(FlickerRoutine(duration));
 }

 // เริ่มกระพริบด้วยระยะเวลาที่กำหนด พร้อมตั้งค่า hardBlinkChance และ phaseJitter ทันที
 public void StartFlicker(float duration, float newHardBlinkChance, float newPhaseJitter)
 {
 hardBlinkChance = Mathf.Clamp01(newHardBlinkChance);
 phaseJitter = newPhaseJitter;
 StartFlicker(duration);
 }

 // ===== Looped bursts (กระพริบเป็นรอบๆ มีคูลดาวน์) =====

 // เริ่มโหมดลูป โดยใช้ค่าจากอินสเปกเตอร์
 public void StartFlickerLoop()
 {
 StartFlickerLoop(loopBurstDuration, loopCooldownRange.x, loopCooldownRange.y);
 }

 // เริ่มโหมดลูป พร้อมกำหนดระยะเวลาบัสต์และคูลดาวน์เอง
 public void StartFlickerLoop(float burstDuration, float cooldownMin, float cooldownMax)
 {
 if (routine != null) { StopCoroutine(routine); routine = null; }
 if (loopRoutine != null) StopCoroutine(loopRoutine);
 loopRoutine = StartCoroutine(FlickerLoopRoutine(Mathf.Max(0f, burstDuration), Mathf.Max(0f, cooldownMin), Mathf.Max(cooldownMin, cooldownMax)));
 }

 // หยุดโหมดลูป และคืนค่าไฟ
 public void StopFlickerLoop()
 {
 if (loopRoutine != null) { StopCoroutine(loopRoutine); loopRoutine = null; }
 RestoreIntensity();
 }

 // คอร์รูทีนโหมดลูป: ทำงานเป็นรอบๆ = กระพริบ (burst) -> พัก (cooldown) -> ซ้ำ
 private IEnumerator FlickerLoopRoutine(float burstDuration, float cooldownMin, float cooldownMax)
 {
 while (true)
 {
 // กระพริบช่วงหนึ่ง
 yield return FlickerRoutine(burstDuration);
 RestoreIntensity();
 // พักตามคูลดาวน์แบบสุ่มในช่วง
 float wait = cooldownMax <= cooldownMin ? cooldownMin : Random.Range(cooldownMin, cooldownMax);
 if (wait >0f) yield return new WaitForSeconds(wait);
 }
 }

 // คอร์รูทีนกระพริบจริง: คำนวณความสว่างต่อดวงด้วย sin และสุ่มความเร็ว/เฟส + hard blink
 private IEnumerator FlickerRoutine(float duration)
 {
 float t=0f; var phases = new float[lights.Length];
 // สุ่มเฟสต่อดวงเพื่อให้ไม่ตรงกัน
 for (int i=0;i<phases.Length;i++) phases[i] = Random.value * phaseJitter;
 
 while (t < duration)
 {
 t += Time.deltaTime;
 for (int i=0;i<lights.Length;i++)
 {
 if (!lights[i]) continue;
 
 // สุ่มความเร็วแบบนุ่มนวลด้วย PerlinNoise ต่อดวง
 float speed = Mathf.Lerp(speedRange.x, speedRange.y, Mathf.PerlinNoise(i*0.17f, t*0.1f));
 
 // ใช้รูปคลื่นเพื่อขึ้นลงของความสว่าง
 float sin = Mathf.Abs(Mathf.Sin((t + phases[i]) * speed));
 float mul = Mathf.Lerp(intensityMulRange.x, intensityMulRange.y, sin);
 
 // โอกาสดับสนิทชั่วคราว (แฟลชดับ)
 if (Random.value < hardBlinkChance * Time.deltaTime * speed)
 mul =0f;
 
 // เซ็ตความสว่างใหม่ โดยอิงจากค่าเดิมของแต่ละดวง
 lights[i].intensity = baseIntensity[i] * mul;
 }
 yield return null;
 }
 
 // จบแล้ว คืนค่าเดิมทุกดวง
 for (int i=0;i<lights.Length;i++) if (lights[i]) lights[i].intensity = baseIntensity[i];
 routine = null;
 }
 }
}
