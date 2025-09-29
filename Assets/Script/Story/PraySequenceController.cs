using System.Collections;
using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
    public class PraySequenceController : MonoBehaviour
    {
        public AudioClip chantClip;
        public float delayBetween = 0.8f;
        public float chantLength = 2.5f; // �����������§�����Ҥ����
        public bool autoProgress = true; // ��� false �Ҩ����ʹ��顴����

        private bool running;

        public void BeginPrayer(int rounds)
        {
            if (running) return;
            StartCoroutine(PrayerRoutine(rounds));
        }

        private IEnumerator PrayerRoutine(int rounds)
        {
            running = true;
            for (int i = 1; i <= rounds; i++)
            {
                // ������§���ͨ��ͧ�Ǵ
                if (chantClip)
                {
                    AudioSource.PlayClipAtPoint(chantClip, transform.position);
                    yield return new WaitForSeconds(chantClip.length);
                }
                else
                {
                    yield return new WaitForSeconds(chantLength);
                }
                if (i < rounds)
                    yield return new WaitForSeconds(delayBetween);
            }
            running = false;
            EventBus.Publish(new PrayerFinishedEvent());
        }
    }
}
