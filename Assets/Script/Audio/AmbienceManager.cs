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
        [Tooltip("ถ้าติ๊ก = ข้าม stingerDelay / loopDelay ครั้งแรก (พฤติกรรมเดิม)")] public bool skipDelaysOnFirstPlay = false;

        [Header("Global Options")] 
        [Tooltip("ปิดการเล่น stinger ทั้งหมด (enterStinger จะถูกข้าม)")] public bool disableAllStingers = false;
        [Tooltip("ล็อกไม่ให้เริ่มระบบจนกว่าจะถึง state ที่กำหนด")] public bool gateUntilActivationState = false;
        [Tooltip("เริ่มเปิดระบบ ambience ตอนเข้าสู่ state นี้ (ใช้คู่กับ gateUntilActivationState)")] public SequenceController.StoryState activationState = SequenceController.StoryState.ReturnToSeat;
        [Tooltip("เมื่อ activate แล้วเริ่มเล่น clip ของ state ปัจจุบันทันที")] public bool playCurrentOnActivate = true;

        [Header("Mixer Routing (Optional)")]
        public AudioMixerGroupReference mixerGroup; // optional lightweight reference (wrapper you can implement) or leave null

        [Header("Crossfade Settings")]
        public float defaultFadeIn = 2f;
        public float defaultFadeOut = 2f;

        [Header("Debug")] public bool debugLog = false;

        [Header("State Filters")] 
        [Tooltip("เปิดเพื่อให้ข้าม state ที่ระบุไม่ให้เล่น ambience")] public bool ignoreSelectedStates = false;
        [Tooltip("รายชื่อ state ที่จะไม่เล่น ambience")] public SequenceController.StoryState[] ignoredStates = new SequenceController.StoryState[] { SequenceController.StoryState.IntroSeated, SequenceController.StoryState.FindFlashlight };

        [Header("Startup Override")] 
        [Tooltip("บังคับให้เล่นเสียง ambience ของ state เริ่มต้นทันทีแม้จะถูก gating / filter")] public bool forceIntroPlay = true;
        [Tooltip("state ที่ถือเป็นจุดเริ่ม (ใช้กับ forceIntroPlay)")] public SequenceController.StoryState introStartupState = SequenceController.StoryState.IntroSeated;

        private AudioSource currentSource;
        private AudioSource fadingOutSource;
        private Coroutine fadeCo;
        private SequenceController.StoryState lastState;
        private bool activated; // gating flag
        private bool startedInit;

        void Awake()
        {
            currentSource = CreateSource("Ambience_Current");
            fadingOutSource = CreateSource("Ambience_FadingOut");

            if (!gateUntilActivationState)
            {
                activated = true;
                DebugMsg("Activated immediately (no gating)");
            }
        }

        void OnEnable() => EventBus.Subscribe<StoryStateChangedEvent>(OnStoryStateChanged);
        void OnDisable() => EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryStateChanged);

        void Start()
        {
            if (!playOnStart) { DebugMsg("playOnStart disabled"); return; }

            // SequenceController.Instance อาจยังไม่ Awake -> รอเฟรม
            if (SequenceController.Instance == null)
            {
                DebugMsg("SequenceController.Instance not ready, defer start");
                StartCoroutine(DeferredStartRoutine());
                return;
            }
            var current = SequenceController.Instance.CurrentState;
            if (forceIntroPlay && current == introStartupState && profile)
            {
                // override gating & filters for the very first play if we have entry
                DebugMsg("Force intro play bypass");
                var wasActivated = activated;
                activated = true; // temporarily ensure active
                PlayForState(current, immediate: skipDelaysOnFirstPlay, bypassFilters:true);
                activated = wasActivated || true; // keep activated after first play so crossfade works
                return; // already played
            }
            if (activated)
            {
                PlayForState(current, immediate: skipDelaysOnFirstPlay);
            }
            else DebugMsg("Not activated yet (gated)");
        }

        private IEnumerator DeferredStartRoutine()
        {
            if (startedInit) yield break;
            startedInit = true;
            int safety = 60; // max ~1 sec at 60fps
            while (SequenceController.Instance == null && safety-- > 0)
                yield return null;
            if (SequenceController.Instance == null)
            {
                DebugMsg("SequenceController still null after wait; abort start");
                yield break;
            }
            if (activated && playOnStart)
                PlayForState(SequenceController.Instance.CurrentState, immediate: skipDelaysOnFirstPlay);
            else DebugMsg("Deferred start reached but not activated");
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
            if (!reactToStateChanges) { DebugMsg("Ignore state change (reactToStateChanges=false)"); return; }

            if (gateUntilActivationState && !activated)
            {
                if (e.Current == activationState)
                {
                    activated = true;
                    DebugMsg($"Activated at state {e.Current}");
                    if (playCurrentOnActivate)
                        PlayForState(e.Current, immediate: skipDelaysOnFirstPlay);
                }
                else
                {
                    DebugMsg($"Gated: state {e.Current} < activation {activationState}");
                }
                return;
            }

            if (!activated)
            {
                DebugMsg("State change ignored (not activated)");
                return;
            }
            PlayForState(e.Current);
        }

        public void PlayForState(SequenceController.StoryState st, bool immediate = false, bool bypassFilters = false)
        {
            if (!activated && !bypassFilters) { DebugMsg($"PlayForState {st} ignored (not activated)"); return; }
            if (!bypassFilters && ignoreSelectedStates && ignoredStates != null)
            {
                for (int i = 0; i < ignoredStates.Length; i++)
                {
                    if (ignoredStates[i] == st)
                    {
                        DebugMsg($"State {st} ignored by filter");
                        return;
                    }
                }
            }
            if (!profile) { DebugMsg("No profile set"); return; }
            if (!profile.TryGet(st, out var amb)) { DebugMsg($"No entry for state {st}"); return; }

            lastState = st;
            DebugMsg($"PlayForState {st} immediate={immediate} loop={(amb.baseLoop?amb.baseLoop.name:"null")} stinger={(amb.enterStinger?amb.enterStinger.name:"none")}" );

            // auto un-pause if previously muted (e.g., after SleepEnd test)
            if (AudioListener.pause) { AudioListener.pause = false; DebugMsg("AudioListener.pause was true -> unpaused"); }

            // stinger (with optional delay) unless globally disabled
            if (!disableAllStingers && amb.enterStinger)
            {
                if (!immediate && amb.stingerDelay > 0f)
                {
                    StartCoroutine(PlayStingerDelayed(amb.enterStinger, amb.volume, amb.stingerDelay));
                }
                else
                {
                    AudioSource.PlayClipAtPoint(amb.enterStinger, Camera.main ? Camera.main.transform.position : Vector3.zero, amb.volume);
                }
            }
            if (amb.baseLoop == null) { DebugMsg("No baseLoop clip; stinger only"); return; }

            // swap sources (current -> fadingOut, new clip -> current)
            var temp = fadingOutSource; fadingOutSource = currentSource; currentSource = temp;
            currentSource.clip = amb.baseLoop;
            currentSource.volume = 0f;
            currentSource.loop = amb.loop;

            float fadeIn = amb.fadeIn > 0f ? amb.fadeIn : defaultFadeIn;
            float fadeOut = amb.fadeOut > 0f ? amb.fadeOut : defaultFadeOut;

            if (!immediate && amb.loopDelay > 0f)
            {
                if (fadeCo != null) StopCoroutine(fadeCo);
                fadeCo = StartCoroutine(CrossfadeRoutineDelayedStart(fadeIn, fadeOut, amb.volume, amb.loopDelay));
            }
            else
            {
                currentSource.Play();
                if (fadeCo != null) StopCoroutine(fadeCo);
                fadeCo = StartCoroutine(CrossfadeRoutine(fadeIn, fadeOut, amb.volume, immediate));
            }
        }

        private IEnumerator PlayStingerDelayed(AudioClip clip, float vol, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!activated) yield break;
            if (clip)
                AudioSource.PlayClipAtPoint(clip, Camera.main ? Camera.main.transform.position : Vector3.zero, vol);
        }

        private IEnumerator CrossfadeRoutineDelayedStart(float fadeIn, float fadeOut, float targetVol, float loopDelay)
        {
            float t = 0f;
            float startOut = fadingOutSource.isPlaying ? fadingOutSource.volume : 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                if (fadeOut > 0f && fadingOutSource.isPlaying)
                    fadingOutSource.volume = Mathf.Lerp(startOut, 0f, Mathf.Clamp01(t / fadeOut));
                yield return null;
            }
            if (fadingOutSource.isPlaying) fadingOutSource.Stop();

            if (loopDelay > 0f) yield return new WaitForSeconds(loopDelay);
            if (!activated) yield break;
            currentSource.Play();
            if (fadeIn <= 0f)
            {
                currentSource.volume = targetVol;
                yield break;
            }
            float tIn = 0f;
            while (tIn < fadeIn)
            {
                tIn += Time.deltaTime;
                currentSource.volume = Mathf.Lerp(0f, targetVol, Mathf.Clamp01(tIn / fadeIn));
                yield return null;
            }
            currentSource.volume = targetVol;
        }

        private IEnumerator CrossfadeRoutine(float fadeIn, float fadeOut, float targetVol, bool immediate)
        {
            if (immediate)
            {
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

        // Public method to manually activate if gating used
        public void ActivateAmbienceIfGated()
        {
            if (activated) return;
            activated = true;
            DebugMsg("Manually activated via ActivateAmbienceIfGated()");
            if (SequenceController.Instance && playCurrentOnActivate)
                PlayForState(SequenceController.Instance.CurrentState, immediate: skipDelaysOnFirstPlay);
        }

        public void StopAllAmbience()
        {
            if (currentSource && currentSource.isPlaying) currentSource.Stop();
            if (fadingOutSource && fadingOutSource.isPlaying) fadingOutSource.Stop();
            DebugMsg("StopAllAmbience called");
        }

        private void DebugMsg(string msg)
        {
            if (debugLog)
                Debug.Log($"[AmbienceManager] {msg}", this);
        }
    }

    [System.Serializable]
    public class AudioMixerGroupReference
    {
        public UnityEngine.Audio.AudioMixerGroup group;
    }
}
