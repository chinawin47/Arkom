using UnityEngine;
using ARKOM.Core;
using System.Linq;

namespace ARKOM.Audio
{
    public class MicrophoneListener : MonoBehaviour
    {
        [Header("Mic Device")]
        public string deviceName;
        public int sampleLengthMs = 256;
        public int frequency = 44100;

        [Header("Detection")]
        [Range(0f,1f)] public float loudnessThreshold = 0.25f;
        public float sustainSeconds = 0.3f;
        public float cooldownSeconds = 1.0f;

        [Header("Smoothing / Debug")]
        [Range(0f,1f)] public float smoothing = 0.4f;
        public bool autoStartAtNight = true;
        public bool debugLog;
        public float lastLoudness { get; private set; }
        public float smoothedLoudness { get; private set; }
        public bool IsCapturing { get; private set; }

        private AudioClip micClip;
        private float loudTimer;
        private float cooldownTimer;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnState);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
            StopCapture();
        }

        private void OnState(GameStateChangedEvent e)
        {
            if (!autoStartAtNight) return;

            if (e.State == GameState.NightAnomaly)
                StartCapture();
            else if (e.State == GameState.DayExploration || e.State == GameState.GameOver || e.State == GameState.Transition)
                StopCapture();
        }

        public void StartCapture()
        {
            if (IsCapturing) return;
            if (Microphone.devices.Length == 0)
            {
                if (debugLog) Debug.LogWarning("[Mic] No microphone devices.");
                return;
            }
            if (string.IsNullOrEmpty(deviceName) || !Microphone.devices.Contains(deviceName))
                deviceName = Microphone.devices[0];

            micClip = Microphone.Start(deviceName, true, 1, frequency);
            IsCapturing = true;
            loudTimer = 0f;
            cooldownTimer = 0f;
            if (debugLog) Debug.Log($"[Mic] Start {deviceName}");
        }

        public void StopCapture()
        {
            if (!IsCapturing) return;
            if (!string.IsNullOrEmpty(deviceName))
                Microphone.End(deviceName);
            micClip = null;
            IsCapturing = false;
            if (debugLog) Debug.Log("[Mic] Stop");
        }

        private void Update()
        {
            if (!IsCapturing || micClip == null) return;
            if (Microphone.GetPosition(deviceName) <= 0) return;

            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.unscaledDeltaTime);

            lastLoudness = GetRMS(sampleLengthMs);
            smoothedLoudness = Mathf.Lerp(smoothedLoudness, lastLoudness, 1f - smoothing);

            if (smoothedLoudness > loudnessThreshold)
            {
                loudTimer += Time.unscaledDeltaTime;
                if (loudTimer >= sustainSeconds && cooldownTimer <= 0f)
                {
                    if (debugLog) Debug.Log($"[Mic] Loud {smoothedLoudness:F3}");
                    EventBus.Publish(new LoudNoiseDetectedEvent(smoothedLoudness));
                    loudTimer = 0f;
                    cooldownTimer = cooldownSeconds;
                }
            }
            else
            {
                loudTimer = 0f;
            }
        }

        private float GetRMS(int lengthMs)
        {
            int samples = Mathf.CeilToInt(frequency * (lengthMs / 1000f));
            if (samples <= 0) return 0f;
            int pos = Microphone.GetPosition(deviceName) - samples;
            if (pos < 0) return 0f;
            var buffer = new float[samples];
            micClip.GetData(buffer, pos);
            double sum = 0;
            for (int i = 0; i < buffer.Length; i++) sum += buffer[i] * buffer[i];
            return Mathf.Sqrt((float)(sum / samples));
        }
    }
}