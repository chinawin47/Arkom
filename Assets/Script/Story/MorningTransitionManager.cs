using System.Collections;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Audio; // optional if you want ambience switch

namespace ARKOM.Story
{
    [AddComponentMenu("Story/Morning Transition Manager")]
    public class MorningTransitionManager : MonoBehaviour
    {
        [Header("Scene References")]
        public Light sunLight;                   // Directional Light representing the sun
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 0f, 1, 1f);
        public Gradient colorGradient;           // Light color over progress
        public float transitionDuration = 5f;    // Seconds for night -> morning blend

        [Header("Ambient Settings")]            // Optional ambient lighting
        public bool adjustAmbientLight = true;
        public Gradient ambientColorGradient;

        [Header("Skybox")]                      // Optional skybox material switching
        public Material nightSkybox;
        public Material morningSkybox;
        public float skyboxBlendDelay = 0.5f;

        [Header("Prop Switching")]              // Activate / deactivate sets
        public GameObject[] nightOnlyObjects;
        public GameObject[] morningOnlyObjects;

        [Header("Audio / Ambience")]            // Optional ambience change
        public AmbienceManager ambienceManager;
        public SequenceController.StoryState morningState; // set if you added a Morning state in profile

        [Header("Player Fade (Optional)")]
        public ScreenFader screenFader;          // reuse existing fader if present
        public float fadeOutTime = 1.2f;
        public float holdBlackTime = 0.8f;
        public float fadeInTime = 1.2f;

        private bool running;
        private float startIntensity;
        private Color startColor;
        private Color startAmbient;

        public void BeginMorningTransition()
        {
            if (running) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            running = true;
            // Fade out
            if (screenFader) yield return screenFader.FadeOut(fadeOutTime);

            // Capture start values
            if (sunLight)
            {
                startIntensity = sunLight.intensity;
                startColor = sunLight.color;
            }
            if (adjustAmbientLight)
            {
                startAmbient = RenderSettings.ambientLight;
            }

            // Switch prop sets
            SetActiveArray(nightOnlyObjects, false);
            SetActiveArray(morningOnlyObjects, true);

            // Optionally change skybox
            if (morningSkybox)
            {
                RenderSettings.skybox = morningSkybox;
                DynamicGI.UpdateEnvironment();
            }

            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / transitionDuration);
                if (sunLight)
                {
                    sunLight.intensity = intensityCurve.Evaluate(p);
                    if (colorGradient != null) sunLight.color = colorGradient.Evaluate(p);
                }
                if (adjustAmbientLight && ambientColorGradient != null)
                {
                    RenderSettings.ambientLight = Color.Lerp(startAmbient, ambientColorGradient.Evaluate(p), p);
                }
                yield return null;
            }

            // Ambience switch
            if (ambienceManager && morningState != 0)
            {
                ambienceManager.PlayForState(morningState, immediate:true);
            }

            // Hold black then fade in
            if (screenFader)
            {
                yield return new WaitForSeconds(holdBlackTime);
                yield return screenFader.FadeIn(fadeInTime);
            }

            EventBus.Publish(new MorningStartedEvent());
            running = false;
        }

        private void SetActiveArray(GameObject[] arr, bool active)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i]) arr[i].SetActive(active);
            }
        }
    }
}
