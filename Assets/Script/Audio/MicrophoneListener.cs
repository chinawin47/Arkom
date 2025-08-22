using UnityEngine;
using ARKOM.Core;
using System.Linq;

namespace ARKOM.Audio
{
    public class MicrophoneListener : MonoBehaviour
    {
        public string deviceName;
        public int sampleLengthMs = 256;
        public int frequency = 44100;
        public float loudnessThreshold = 0.25f;
        public float sustainSeconds = 0.3f;
        public bool autoStart = true;

        private AudioClip micClip;
        private float loudTimer;

        void Start()
        {
            if (autoStart)
                StartCapture();
        }

        public void StartCapture()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone devices detected.");
                return;
            }

            if (string.IsNullOrEmpty(deviceName) || !Microphone.devices.Contains(deviceName))
                deviceName = Microphone.devices[0];

            micClip = Microphone.Start(deviceName, true, 1, frequency);
        }

        public void StopCapture()
        {
            if (!string.IsNullOrEmpty(deviceName))
                Microphone.End(deviceName);
        }

        void Update()
        {
            if (micClip == null || Microphone.GetPosition(deviceName) <= 0) return;

            float loudness = GetRMS(sampleLengthMs);
            if (loudness > loudnessThreshold)
            {
                loudTimer += Time.deltaTime;
                if (loudTimer >= sustainSeconds)
                {
                    EventBus.Publish(new LoudNoiseDetectedEvent(loudness));
                    loudTimer = 0f;
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
            float[] buffer = new float[samples];
            int micPos = Microphone.GetPosition(deviceName) - samples;
            if (micPos < 0) return 0f;
            micClip.GetData(buffer, micPos);
            double sum = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sum += buffer[i] * buffer[i];
            }
            return Mathf.Sqrt((float)(sum / samples));
        }
    }
}