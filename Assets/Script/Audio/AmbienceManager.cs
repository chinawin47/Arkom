using System.Collections;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

namespace ARKOM.Audio
{
    [AddComponentMenu("Audio/Ambience Manager")]
    public class AmbienceManager : MonoBehaviour
    {
        public AmbienceStateProfile profile;
        public bool playOnStart = true;
        public bool reactToStateChanges = true;
        [Header("Mixer Routing (Optional)")]
        public AudioMixerGroupReference mixerGroup; // optional lightweight reference (wrapper you can implement) or leave null

        [Header("Crossfade Settings")]
        public float defaultFadeIn = 2f;
        public float defaultFadeOut = 2f;

        private AudioSource currentSource;
        private AudioSource fadingOutSource;
        private Coroutine fadeCo;
        private SequenceController.StoryState lastState;

        void Awake()
        {
            currentSource = CreateSource("Ambience_Current");
            fadingOutSource = CreateSource("Ambience_FadingOut");
        }

        void OnEnable()
        {
            EventBus.Subscribe<StoryStateChangedEvent>(OnStoryStateChanged);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryStateChanged);
        }

        void Start()
        {
            if (playOnStart && SequenceController.Instance)
            {
                PlayForState(SequenceController.Instance.CurrentState, immediate:true);
            }
        }

        private AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            if (mixerGroup != null && mixerGroup.group != null) src.outputAudioMixerGroup = mixerGroup.group;
            return src;
        }

        private void OnStoryStateChanged(StoryStateChangedEvent e)
        {
            if (!reactToStateChanges) return;
            PlayForState(e.Current);
        }

        public void PlayForState(SequenceController.StoryState st, bool immediate = false)
        {
            if (!profile) return;
            if (!profile.TryGet(st, out var amb)) return;

            // play stinger
            if (amb.enterStinger)
            {
                AudioSource.PlayClipAtPoint(amb.enterStinger, Camera.main ? Camera.main.transform.position : Vector3.zero, amb.volume);
            }

            if (amb.baseLoop == null)
                return; // nothing to loop

            // swap sources (current -> fadingOut, new clip -> current)
            var temp = fadingOutSource;
            fadingOutSource = currentSource;
            currentSource = temp;

            currentSource.clip = amb.baseLoop;
            currentSource.volume = 0f;
            currentSource.loop = amb.loop;
            currentSource.Play();

            float fadeIn = amb.fadeIn > 0f ? amb.fadeIn : defaultFadeIn;
            float fadeOut = amb.fadeOut > 0f ? amb.fadeOut : defaultFadeOut;

            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(CrossfadeRoutine(fadeIn, fadeOut, amb.volume, immediate));
            lastState = st;
        }

        private IEnumerator CrossfadeRoutine(float fadeIn, float fadeOut, float targetVol, bool immediate)
        {
            if (immediate)
            {
                // stop fadingOut instantly
                if (fadingOutSource.isPlaying) fadingOutSource.Stop();
                currentSource.volume = targetVol;
                yield break;
            }

            float t = 0f;
            float startOut = fadingOutSource.isPlaying ? fadingOutSource.volume : 0f;
            while (t < Mathf.Max(fadeIn, fadeOut))
            {
                t += Time.deltaTime;
                if (fadeIn > 0f)
                    currentSource.volume = Mathf.Lerp(0f, targetVol, Mathf.Clamp01(t / fadeIn));
                else currentSource.volume = targetVol;
                if (fadeOut > 0f && fadingOutSource.isPlaying)
                    fadingOutSource.volume = Mathf.Lerp(startOut, 0f, Mathf.Clamp01(t / fadeOut));
                yield return null;
            }
            currentSource.volume = targetVol;
            if (fadingOutSource.isPlaying) fadingOutSource.Stop();
        }
    }

    // Optional wrapper (so project compiles even if you don't have a mixer group yet)
    [System.Serializable]
    public class AudioMixerGroupReference
    {
        public UnityEngine.Audio.AudioMixerGroup group;
    }
}
