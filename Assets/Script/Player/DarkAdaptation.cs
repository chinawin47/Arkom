using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARKOM.Player
{
 [AddComponentMenu("Player/Dark Adaptation (Eye Adjust)")]
 public class DarkAdaptation : MonoBehaviour
 {
 [Header("Targets")]
 [Tooltip("กล้องหลัก (เว้นว่างจะหา Camera.main)")] public Camera targetCamera;
 [Tooltip("ภาพทับหน้าจอ (สีดำ) ถ้าเว้นว่างจะสร้างให้อัตโนมัติ")] public Image overlayImage;

 [Header("Behavior")]
 [Tooltip("ค่าความสว่างอ้างอิงสำหรับ normalize (ประมาณรวม ambient+lights)")] public float referenceIntensity =1.0f;
 [Tooltip("ค่านี้ขึ้นไปถือว่าสว่างพอ (รีเซ็ตการปรับตา)")] public float brightThreshold =0.6f;
 [Tooltip("ค่านี้ลงไปถือว่ามืด (เริ่มปรับตา)")] public float darkThreshold =0.3f;
 [Tooltip("ความเร็วเพิ่มการปรับตาในที่มืด (ต่อวินาที)")] public float adaptUpSpeed =0.25f;
 [Tooltip("ความเร็วลดการปรับตาเมื่อสว่าง (ต่อวินาที)")] public float adaptDownSpeed =1.0f;

 [Header("Overlay Alpha Mapping")] 
 [Tooltip("ความทึบของ overlay ตอนเข้าสู่ความมืดใหม่ๆ (ยังไม่ปรับตา)")] public float maxOverlayAlpha =0.7f;
 [Tooltip("ความทึบ overlay หลังปรับตาสุด (เห็นมืดชัดขึ้น)")] public float minOverlayAlpha =0.1f;

 [Header("Sampling")] 
 [Tooltip("คำนวณความสว่างทุกๆ กี่วินาที")] public float sampleInterval =0.25f;
 [Tooltip("ระยะค้นหา Light เพื่อประเมินความสว่างใกล้ผู้เล่น")] public float lightCheckRadius =12f;
 [Tooltip("ตัวคูณสำหรับไฟทิศ (Directional Light)")] public float directionalWeight =0.5f;
 [Tooltip("ลดน้ำหนักไฟตามระยะทาง (1/(1+d^2))")] public float distanceFalloff =1f;

 private float timer;
 private float brightness; //0..~
 private float adapt; //0..1 (0=ยังไม่ปรับ,1=ปรับสุด)
 private Canvas overlayCanvas;

 void Awake()
 {
 if (!targetCamera) targetCamera = Camera.main;
 EnsureOverlay();
 }

 void OnEnable()
 {
 timer =0f;
 }

 void Update()
 {
 // sample brightness
 timer -= Time.unscaledDeltaTime; // ไม่ผูกกับ timescale เผื่อเปิด UI pause
 if (timer <=0f)
 {
 brightness = SampleApproxBrightness();
 timer = sampleInterval;
 }

 // normalize
 float norm = Mathf.Clamp01(referenceIntensity >0f ? (brightness / referenceIntensity) :1f);

 // update adaptation
 if (norm <= darkThreshold)
 adapt = Mathf.MoveTowards(adapt,1f, adaptUpSpeed * Time.unscaledDeltaTime);
 else if (norm >= brightThreshold)
 adapt = Mathf.MoveTowards(adapt,0f, adaptDownSpeed * Time.unscaledDeltaTime);
 else
 {
 // zone ระหว่างกลาง ค่อยๆ เอียงเข้าหา1 หากใกล้มืด,0 หากใกล้สว่าง
 float t = Mathf.InverseLerp(brightThreshold, darkThreshold, norm);
 float target = Mathf.Lerp(0f,1f, t);
 float speed = Mathf.Lerp(adaptDownSpeed, adaptUpSpeed, t);
 adapt = Mathf.MoveTowards(adapt, target, speed * Time.unscaledDeltaTime);
 }

 // map to overlay alpha (ปรับตาเพิ่ม -> overlay จางลง)
 float alpha = Mathf.Lerp(maxOverlayAlpha, minOverlayAlpha, adapt);
 ApplyOverlay(alpha);
 }

 private float SampleApproxBrightness()
 {
 float sum =0f;

 // ambient (ถ้าใช้ค่า ambientIntensity)
 sum += RenderSettings.ambientIntensity;

 // directional (ดวงอาทิตย์)
 if (RenderSettings.sun && RenderSettings.sun.enabled)
 sum += RenderSettings.sun.intensity * directionalWeight;

 // nearby lights
 Vector3 pos = targetCamera ? targetCamera.transform.position : transform.position;
 Light[] all = FindObjectsOfType<Light>();
 float r2 = lightCheckRadius * lightCheckRadius;
 for (int i =0; i < all.Length; i++)
 {
 var l = all[i]; if (!l || !l.enabled) continue;
 if (l.type == LightType.Directional) continue; // นับไปแล้ว
 float d2 = (l.transform.position - pos).sqrMagnitude;
 if (d2 > r2) continue;
 float fall =1f / (1f + d2 * distanceFalloff);
 sum += l.intensity * fall;
 }

 return sum;
 }

 private void EnsureOverlay()
 {
 if (overlayImage) return;

 // สร้าง Canvas + Image ลูกของกล้องหรือ object นี้
 GameObject root;
 if (targetCamera)
 {
 root = new GameObject("DarkAdaptationOverlay");
 root.transform.SetParent(targetCamera.transform, false);
 }
 else
 {
 root = new GameObject("DarkAdaptationOverlay");
 root.transform.SetParent(transform, false);
 }

 overlayCanvas = root.AddComponent<Canvas>();
 overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
 overlayCanvas.sortingOrder =9999;
 var cg = root.AddComponent<CanvasGroup>();
 cg.blocksRaycasts = false; cg.interactable = false;

 var imgGO = new GameObject("Image");
 imgGO.transform.SetParent(root.transform, false);
 overlayImage = imgGO.AddComponent<Image>();
 overlayImage.color = new Color(0f,0f,0f,0f);
 overlayImage.raycastTarget = false;

 var rt = overlayImage.rectTransform;
 rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
 }

 private void ApplyOverlay(float a)
 {
 if (!overlayImage) return;
 var c = overlayImage.color;
 c.a = Mathf.Clamp01(a);
 overlayImage.color = c;
 }
 }
}
