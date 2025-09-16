using UnityEngine;
using UnityEngine.InputSystem;

namespace ARKOM.Player
{
    // ไฟฉายพร้อมระบบแบต/ชาร์จ และปรับความสว่างตามแบต
    public class Flashlight : MonoBehaviour
    {
        [Header("Light Source")]
        [Tooltip("ลาก Spot Light ที่จะใช้เป็นไฟฉาย (ควรเป็นลูกของกล้อง)")]
        public Light lightSource;

        [Header("Power / Battery")]
        [Tooltip("เริ่มต้นเปิดไฟฉายหรือไม่")]
        public bool initiallyOn = false;
        [Tooltip("ความจุแบตสูงสุด (หน่วยสมมุติ)")]
        public float batteryCapacity = 100f;
        [Tooltip("อัตราลดแบตต่อวินาทีเมื่อเปิดไฟ")]
        public float drainPerSecondOn = 6f;
        [Tooltip("อัตราชาร์จต่อวินาทีเมื่อปิดไฟ (0 = ไม่ชาร์จ)")]
        public float rechargePerSecondOff = 2f;
        [Tooltip("ความสว่างขั้นต่ำเมื่อแบตใกล้หมด (สัดส่วน 0..1 ของความสว่างพื้นฐาน)")]
        [Range(0f, 1f)] public float minIntensityAtEmpty = 0.2f;
        [Tooltip("เริ่มกะพริบเมื่อแบตต่ำกว่าค่านี้ (เช่น 0.15 = 15%)")]
        [Range(0f, 0.5f)] public float lowBatteryThreshold = 0.15f;

        [Header("FX")]
        public AudioClip toggleOnSfx;
        public AudioClip toggleOffSfx;
        public AudioClip noPowerSfx;
        [Tooltip("ความเร็วการกะพริบเมื่อแบตต่ำ")]
        public float lowBatteryFlickerSpeed = 5f;
        [Tooltip("แอมพลิจูดการกะพริบ (0..1 ของความสว่างพื้นฐาน)")]
        [Range(0f, 0.5f)] public float lowBatteryFlickerAmount = 0.15f;

        public bool IsOn { get; private set; }
        public float Battery { get; private set; }
        public float BatteryPercent => Mathf.Clamp01(Battery / Mathf.Max(0.0001f, batteryCapacity));

        private float baseIntensity;
        private float baseRange;

        private void Awake()
        {
            if (!lightSource)
                lightSource = GetComponentInChildren<Light>();
            if (lightSource)
            {
                baseIntensity = lightSource.intensity;
                baseRange = lightSource.range;
            }
            Battery = batteryCapacity;
            SetOn(initiallyOn, playSfx: false);
            ApplyLightParams();
        }

        private void Update()
        {
            // อัปเดตแบต
            if (IsOn && Battery > 0f)
                Battery -= drainPerSecondOn * Time.deltaTime;
            else if (!IsOn && rechargePerSecondOff > 0f)
                Battery += rechargePerSecondOff * Time.deltaTime;

            Battery = Mathf.Clamp(Battery, 0f, batteryCapacity);

            // ปิดเองถ้าแบตหมด
            if (IsOn && Battery <= 0f)
                SetOn(false);

            ApplyLightParams();
        }

        public void Toggle()
        {
            if (!IsOn)
            {
                if (Battery <= 0f)
                {
                    if (noPowerSfx) AudioSource.PlayClipAtPoint(noPowerSfx, transform.position);
                    return;
                }
                SetOn(true);
            }
            else
            {
                SetOn(false);
            }
        }

        public void SetOn(bool on, bool playSfx = true)
        {
            if (IsOn == on) return;
            IsOn = on;
            if (lightSource) lightSource.enabled = on;

            if (playSfx)
            {
                var clip = on ? toggleOnSfx : toggleOffSfx;
                if (clip) AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        private void ApplyLightParams()
        {
            if (!lightSource) return;

            float p = BatteryPercent;
            // ความสว่างปรับตามแบต
            float intensity = baseIntensity * Mathf.Lerp(minIntensityAtEmpty, 1f, p);

            // กะพริบเมื่อแบตต่ำ
            if (IsOn && p <= lowBatteryThreshold && Battery > 0f && lowBatteryFlickerAmount > 0f)
            {
                float flicker = 1f + (Mathf.PerlinNoise(Time.time * lowBatteryFlickerSpeed, 0.123f) - 0.5f) * 2f * lowBatteryFlickerAmount;
                intensity *= Mathf.Max(0.05f, flicker);
            }

            lightSource.intensity = intensity;
            lightSource.range = baseRange; // ปรับเพิ่มได้ตามต้องการ
        }
    }
}