using System.Collections;
using UnityEngine;

namespace ARKOM.Story
{
 [AddComponentMenu("UI/Screen Fader")]
 public class ScreenFader : MonoBehaviour
 {
 public CanvasGroup group;

 private void Awake()
 {
 if (!group) group = GetComponentInChildren<CanvasGroup>();
 }

 public IEnumerator FadeOut(float t)
 {
 if (!group || t <=0f) yield break;
 float time =0f;
 while (time < t)
 {
 time += Time.deltaTime;
 group.alpha = Mathf.Clamp01(time / t);
 yield return null;
 }
 }

 public IEnumerator FadeIn(float t)
 {
 if (!group || t <=0f) yield break;
 float time =0f;
 while (time < t)
 {
 time += Time.deltaTime;
 group.alpha =1f - Mathf.Clamp01(time / t);
 yield return null;
 }
 }
 }
}
