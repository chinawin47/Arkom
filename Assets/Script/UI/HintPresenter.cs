using UnityEngine;
using TMPro;

namespace ARKOM.UI
{
    public class HintPresenter : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text label;
        public CanvasGroup group;
        public float fadeTime = 0.25f;

        private float hideTimer;
        private bool fading;

        void Awake()
        {
            if (!group) group = GetComponent<CanvasGroup>();
            if (group) group.alpha = 0f;
        }

        void Update()
        {
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                {
                    StartCoroutine(FadeOut());
                }
            }
        }

        public void Show(string text, float duration)
        {
            if (!label) return;
            label.text = text;
            hideTimer = duration;
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        public void HideImmediate()
        {
            StopAllCoroutines();
            if (group) group.alpha = 0f;
            if (label) label.text = string.Empty;
            hideTimer = 0f;
        }

        private System.Collections.IEnumerator FadeIn()
        {
            if (!group) yield break;
            fading = true;
            float t = 0f;
            float start = group.alpha;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, 1f, t / fadeTime);
                yield return null;
            }
            group.alpha = 1f;
            fading = false;
        }

        private System.Collections.IEnumerator FadeOut()
        {
            if (!group) yield break;
            fading = true;
            float t = 0f;
            float start = group.alpha;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, 0f, t / fadeTime);
                yield return null;
            }
            group.alpha = 0f;
            if (label) label.text = string.Empty;
            fading = false;
        }
    }
}
