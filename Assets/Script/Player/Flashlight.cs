using UnityEngine;
using UnityEngine.InputSystem;

namespace ARKOM.Player
{
    // ไฟฉาย พร้อมโหมดปิดระบบแบตเตอรี่
    public class Flashlight : MonoBehaviour
    {
        [Header("Light Source")]
        [Tooltip("ลาก Spot Light ที่จะใช้เป็นไฟฉาย (หากไม่ตั้งจะลองหาอัตโนมัติ)")]
        public Light lightSource;

        [Header("Power / Battery")]
        [Tooltip("เริ่มเปิดไฟฉายตั้งแต่แรกหรือไม่")] public bool initiallyOn = false;
        [Tooltip("ปิดระบบแบตเตอรี่ ให้ไฟฉายใช้งานได้ตลอด")] public bool infinitePower = true; // NEW
        [Tooltip("ความจุแบต (ใช้เมื่อ infinitePower=false)")] public float batteryCapacity = 100f;
        [Tooltip("อัตราการลดแบตขณะเปิด (ใช้เมื่อ infinitePower=false)")] public float drainPerSecondOn = 6f;
        [Tooltip("อัตราชาร์จแบตขณะปิด (0 = ไม่ชาร์จ) (ใช้เมื่อ infinitePower=false)")] public float rechargePerSecondOff = 2f;
        [Tooltip("ความสว่างขั้นต่ำเมื่อแบตหมด (อัตราส่วน 0..1) (ใช้เมื่อ infinitePower=false)")]
        [Range(0f, 1f)] public float minIntensityAtEmpty = 0.2f;
        [Tooltip("ค่าเปอร์เซ็นต์ที่ถือว่าแบตต่ำ (เช่น 0.15 = 15%) (ใช้เมื่อ infinitePower=false)")]
        [Range(0f, 0.5f)] public float lowBatteryThreshold = 0.15f;

        [Header("FX")]
        public AudioClip toggleOnSfx;
        public AudioClip toggleOffSfx;
        public AudioClip noPowerSfx;
        [Tooltip("ความเร็วการกระพริบเมื่อแบตต่ำ (ใช้เมื่อ infinitePower=false)")]
        public float lowBatteryFlickerSpeed = 5f;
        [Tooltip("ระดับการกระพริบเมื่อแบตต่ำ (0..1) (ใช้เมื่อ infinitePower=false)")]
        [Range(0f, 0.5f)] public float lowBatteryFlickerAmount = 0.15f;

        public bool IsOn { get; private set; }
        public float Battery { get; private set; }
        public float BatteryPercent => infinitePower ? 1f : Mathf.Clamp01(Battery / Mathf.Max(0.0001f, batteryCapacity));

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
            if (!infinitePower)
            {
                // โหมดมีแบตเตอรี่
                if (IsOn && Battery > 0f)
                    Battery -= drainPerSecondOn * Time.deltaTime;
                else if (!IsOn && rechargePerSecondOff > 0f)
                    Battery += rechargePerSecondOff * Time.deltaTime;

                Battery = Mathf.Clamp(Battery, 0f, batteryCapacity);

                // ปิดเมื่อแบตหมด
                if (IsOn && Battery <= 0f)
                    SetOn(false);
            }
            else
            {
                // โหมดไฟฉายใช้งานได้ตลอด: รักษาแบตเต็มไว้เสมอ
                Battery = batteryCapacity;
            }

            ApplyLightParams();
        }

        public void Toggle()
        {
            if (!IsOn)
            {
                if (!infinitePower && Battery <= 0f)
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

            if (infinitePower)
            {
                // ความสว่างเต็ม ปิดเอฟเฟ็กต์แบตต่ำ
                lightSource.intensity = baseIntensity;
                lightSource.range = baseRange;
                return;
            }

            float p = BatteryPercent;
            // ลดความสว่างตามแบต
            float intensity = baseIntensity * Mathf.Lerp(minIntensityAtEmpty, 1f, p);

            // กระพริบเมื่อแบตต่ำ
            if (IsOn && p <= lowBatteryThreshold && Battery > 0f && lowBatteryFlickerAmount > 0f)
            {
                float flicker = 1f + (Mathf.PerlinNoise(Time.time * lowBatteryFlickerSpeed, 0.123f) - 0.5f) * 2f * lowBatteryFlickerAmount;
                intensity *= Mathf.Max(0.05f, flicker);
            }

            lightSource.intensity = intensity;
            lightSource.range = baseRange;
        }
    }
}