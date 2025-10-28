using UnityEngine;

namespace ARKOM.Scenes.Road
{
 [DisallowMultipleComponent]
 public class StreetLampController : MonoBehaviour
 {
 [Header("Components")]
 [Tooltip("โหนดที่มี Light เป็นลูก (ถ้าเว้นว่างจะใช้ GameObject นี้)")] public Transform lightsRoot;
 private Light[] lights;
 private float[] baseIntensity;

 [Header("Behavior - Base")] 
 [Tooltip("ความน่าจะเป็นที่หลอดนี้จะกระพริบเองเป็นครั้งคราว (0-1 ต่อวินาที)")] [Range(0f,2f)] public float idleFlickerRate =0.15f;
 [Tooltip("โอกาสที่กระพริบนั้นจะดับมิดสั้นๆ")] [Range(0,1)] public float idleHardBlinkChance =0.1f;
 [Tooltip("ความเร็วการกระพริบโดยรวมของหลอดนี้")] public Vector2 flickerSpeedRange = new Vector2(6f,12f);
 [Tooltip("ช่วงความสว่าง (คูณกับ intensity เดิม)")] public Vector2 intensityMulRange = new Vector2(0.6f,1.1f);

 [Header("Behavior - Distance Activation")] 
 [Tooltip("จะทำงานเต็มรูปแบบเมื่ออยู่ใกล้ผู้เล่นภายในระยะนี้ (เมตร)")] public float activeRadius =35f;
 [Tooltip("นอกระยะนี้จะค่อยๆ ลดกิจกรรมลงเพื่อประหยัดทรัพยากร")] public float farLodRadius =60f;

 [Header("Behavior - Surge Event (รถผ่าน)")]
 [Tooltip("บวกเพิ่มโอกาสดับมิดระหว่าง Surge")] public float surgeHardBlinkBonus =0.35f;
 [Tooltip("คูณความเร็วกระพริบระหว่าง Surge")][Range(1f,4f)] public float surgeSpeedMul =1.6f;

 private Transform player;
 private float surgeTimer; // เหลือเวลา Surge
 private float idleNextTime; // เวลานัดกระพริบครั้งต่อไป
 private float perLampPhase; // ทำให้แต่ละหลอดไม่ตรงกัน
 private bool inActiveRange;
 private bool inLodRange;

 private void Awake()
 {
 if (!lightsRoot) lightsRoot = transform;
 lights = lightsRoot.GetComponentsInChildren<Light>(includeInactive:true);
 baseIntensity = new float[lights.Length];
 for (int i=0;i<lights.Length;i++) baseIntensity[i] = lights[i].intensity;
 perLampPhase = Random.value *10f;
 // ลงทะเบียนกับ Manager
 var mgr = StreetLightingManager.Instance;
 if (!mgr) mgr = FindObjectOfType<StreetLightingManager>();
 if (mgr) mgr.Register(this);
 }

 private void OnDestroy()
 {
 var mgr = StreetLightingManager.Instance;
 if (mgr) mgr.Unregister(this);
 }

 public void SetPlayer(Transform t)
 { player = t; }

 public void TriggerSurgeFlicker(float duration, float extraHardBlink)
 {
 surgeTimer = Mathf.Max(surgeTimer, duration);
 surgeHardBlinkBonus = Mathf.Max(surgeHardBlinkBonus, extraHardBlink);
 }

 private void Update()
 {
 UpdateRanges();
 if (!inLodRange) return; // ไกลมาก ไม่ต้องอัปเดต

 float t = Time.time + perLampPhase;
 float speedBase = Mathf.Lerp(flickerSpeedRange.x, flickerSpeedRange.y, Mathf.PerlinNoise(0.1f, t*0.1f));
 float speed = speedBase * (surgeTimer >0f ? surgeSpeedMul :1f);
 float hardBlink = idleHardBlinkChance + (surgeTimer >0f ? surgeHardBlinkBonus :0f);
 hardBlink = Mathf.Clamp01(hardBlink);

 // idle scheduling: โอกาสกระพริบเองเพิ่มเล็กน้อยถ้าอยู่ใน active range
 float rate = idleFlickerRate * (inActiveRange ?1f :0.4f);
 if (Time.time >= idleNextTime)
 {
 // นัดครั้งใหม่แบบสุ่ม
 float interval = rate <=0.0001f ?999f : Random.Range(0.6f,1.6f) / rate;
 idleNextTime = Time.time + interval;
 // ทำหนึ่ง burst สั้นๆ
 DoFlickerStep(t, speed, hardBlink);
 }

 // ระหว่าง Surge เร่งจังหวะแบบต่อเนื่องเล็กน้อย
 if (surgeTimer >0f)
 {
 DoFlickerStep(t, speed, hardBlink);
 surgeTimer -= Time.deltaTime;
 }
 }

 private void UpdateRanges()
 {
 if (!player) return;
 float d2 = (player.position - transform.position).sqrMagnitude;
 inActiveRange = d2 <= activeRadius * activeRadius;
 inLodRange = d2 <= farLodRadius * farLodRadius;
 }

 private void DoFlickerStep(float timeSeed, float speed, float hardBlink)
 {
 // ใช้ Perlin ทำให้แต่ละดวงในเสาเดียวกันแกว่งไม่เท่ากัน แต่ค่อนข้างสอดคล้องกัน
 for (int i=0;i<lights.Length;i++)
 {
 if (!lights[i]) continue;
 float n = Mathf.PerlinNoise(i*0.27f +3.1f, timeSeed *0.7f);
 float sin = Mathf.Abs(Mathf.Sin((timeSeed + i*0.13f) * (speed * (0.8f +0.4f*n))));
 float mul = Mathf.Lerp(intensityMulRange.x, intensityMulRange.y, sin);
 if (Random.value < hardBlink * Time.deltaTime * (0.6f +0.8f*n)) mul =0f; // ดับวูบเป็นบางครั้ง
 lights[i].intensity = baseIntensity[i] * mul;
 }
 }
 }
}
