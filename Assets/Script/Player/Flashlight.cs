using UnityEngine;
using UnityEngine.InputSystem;

namespace ARKOM.Player
{
    // 俩�¾�����к�ẵ/���� ��л�Ѻ�������ҧ���ẵ
    public class Flashlight : MonoBehaviour
    {
        [Header("Light Source")]
        [Tooltip("�ҡ Spot Light ��������俩�� (������١�ͧ���ͧ)")]
        public Light lightSource;

        [Header("Power / Battery")]
        [Tooltip("��������Դ俩���������")]
        public bool initiallyOn = false;
        [Tooltip("������ẵ�٧�ش (˹������ص�)")]
        public float batteryCapacity = 100f;
        [Tooltip("�ѵ��Ŵẵ����Թҷ�������Դ�")]
        public float drainPerSecondOn = 6f;
        [Tooltip("�ѵ�Ҫ��쨵���Թҷ�����ͻԴ� (0 = ������)")]
        public float rechargePerSecondOff = 2f;
        [Tooltip("�������ҧ��鹵�������ẵ������ (�Ѵ��ǹ 0..1 �ͧ�������ҧ��鹰ҹ)")]
        [Range(0f, 1f)] public float minIntensityAtEmpty = 0.2f;
        [Tooltip("������о�Ժ�����ẵ��ӡ��Ҥ�ҹ�� (�� 0.15 = 15%)")]
        [Range(0f, 0.5f)] public float lowBatteryThreshold = 0.15f;

        [Header("FX")]
        public AudioClip toggleOnSfx;
        public AudioClip toggleOffSfx;
        public AudioClip noPowerSfx;
        [Tooltip("�������ǡ�áо�Ժ�����ẵ���")]
        public float lowBatteryFlickerSpeed = 5f;
        [Tooltip("�����Ԩٴ��áо�Ժ (0..1 �ͧ�������ҧ��鹰ҹ)")]
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
            // �ѻവẵ
            if (IsOn && Battery > 0f)
                Battery -= drainPerSecondOn * Time.deltaTime;
            else if (!IsOn && rechargePerSecondOff > 0f)
                Battery += rechargePerSecondOff * Time.deltaTime;

            Battery = Mathf.Clamp(Battery, 0f, batteryCapacity);

            // �Դ�ͧ���ẵ���
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
            // �������ҧ��Ѻ���ẵ
            float intensity = baseIntensity * Mathf.Lerp(minIntensityAtEmpty, 1f, p);

            // �о�Ժ�����ẵ���
            if (IsOn && p <= lowBatteryThreshold && Battery > 0f && lowBatteryFlickerAmount > 0f)
            {
                float flicker = 1f + (Mathf.PerlinNoise(Time.time * lowBatteryFlickerSpeed, 0.123f) - 0.5f) * 2f * lowBatteryFlickerAmount;
                intensity *= Mathf.Max(0.05f, flicker);
            }

            lightSource.intensity = intensity;
            lightSource.range = baseRange; // ��Ѻ����������ͧ���
        }
    }
}