using UnityEngine;
using TMPro;

namespace ARKOM.UI
{
    [AddComponentMenu("UI/Note Hint UI")]
    public class NoteHintUI : MonoBehaviour
    {
        public TMP_Text label;
        public CanvasGroup group;
        [TextArea] public string noteText = "...";
        public float fadeTime = 0.25f;

        void Awake(){ if (!group) group = GetComponent<CanvasGroup>(); if (group) group.alpha = 0f; }

        public void ShowNote(string text = null)
        {
            if (!string.IsNullOrEmpty(text)) noteText = text;
            if (label) label.text = noteText;
            StopAllCoroutines();
            StartCoroutine(Fade(1f));
        }
        public void Hide(){ StopAllCoroutines(); StartCoroutine(Fade(0f)); }

        private System.Collections.IEnumerator Fade(float target)
        {
            if (!group) yield break; float start = group.alpha; float t = 0f;
            while (t < fadeTime)
            { t += Time.deltaTime; group.alpha = Mathf.Lerp(start, target, t/fadeTime); yield return null; }
            group.alpha = target;
        }
    }
}
