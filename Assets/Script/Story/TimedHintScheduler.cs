using System.Collections;
using UnityEngine;
using ARKOM.Core;
using ARKOM.UI;

namespace ARKOM.Story
{
    [AddComponentMenu("Story/Timed Hint Scheduler")]
    public class TimedHintScheduler : MonoBehaviour
    {
        public TimedHintsConfig config;
        public HintPresenter hintPresenter; // if null will try FindObjectOfType
        public bool enabledScheduler = true;
        public bool cancelOnStateExit = true;
        public bool clearOnStateChange = true;

        [Header("Strength Control")] // ใหม่: คุมไม่ให้สปอยมากไป
        [Tooltip("ระดับสูงสุดที่ยอมให้บอกอัตโนมัติ (Explicit = ทั้งหมด)")] public TimedHintsConfig.HintStrength maxAutoStrength = TimedHintsConfig.HintStrength.Explicit;
        [Tooltip("ถ้า true เมื่อแสดง hint ระดับสูงกว่าแล้ว จะไม่แสดงระดับที่ต่ำกว่าที่เหลือ")] public bool suppressLowerAfterEscalation = true;
        [Tooltip("ดีเลย์ขั้นต่ำระหว่าง hint สองอัน (ป้องกันถี่เกิน)")] public float minGapBetweenHints = 1.5f;

        private SequenceController.StoryState currentState;
        private Coroutine runCo;
        private float lastHintTimeRealtime;
        private TimedHintsConfig.HintStrength highestShownThisState = TimedHintsConfig.HintStrength.Subtle;

        void Awake()
        {
            if (!hintPresenter) hintPresenter = FindObjectOfType<HintPresenter>();
        }

        void OnEnable()
        {
            EventBus.Subscribe<StoryStateChangedEvent>(OnStateChanged);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<StoryStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(StoryStateChangedEvent e)
        {
            if (!enabledScheduler) return;
            if (runCo != null)
            {
                if (cancelOnStateExit)
                {
                    StopCoroutine(runCo);
                    runCo = null;
                }
                if (clearOnStateChange && hintPresenter)
                    hintPresenter.HideImmediate();
            }
            currentState = e.Current;
            highestShownThisState = TimedHintsConfig.HintStrength.Subtle; // reset tracking
            lastHintTimeRealtime = Time.realtimeSinceStartup;
            if (config == null) return;
            runCo = StartCoroutine(RunHintsForState(e.Current));
        }

        private struct RuntimeHint
        {
            public float timeFromEnter;
            public string text;
            public float duration;
            public TimedHintsConfig.HintStrength strength;
        }

        private IEnumerator RunHintsForState(SequenceController.StoryState st)
        {
            // สร้าง list runtime จาก groupedHints ถ้ามี ไม่งั้นใช้ legacy hints
            System.Collections.Generic.List<RuntimeHint> buffer = new System.Collections.Generic.List<RuntimeHint>();
            if (config.groupedHints != null && config.groupedHints.Length > 0)
            {
                for (int i = 0; i < config.groupedHints.Length; i++)
                {
                    var g = config.groupedHints[i];
                    if (g == null || g.hints == null || g.state != st) continue;
                    foreach (var h in g.hints)
                    {
                        if (h == null) continue;
                        buffer.Add(new RuntimeHint { timeFromEnter = h.timeFromEnter, text = h.text, duration = h.duration, strength = h.strength });
                    }
                }
            }
            else if (config.hints != null && config.hints.Length > 0)
            {
                for (int i = 0; i < config.hints.Length; i++)
                {
                    var h = config.hints[i];
                    if (h == null || h.state != st) continue;
                    buffer.Add(new RuntimeHint { timeFromEnter = h.timeFromEnter, text = h.text, duration = h.duration, strength = h.strength });
                }
            }

            if (buffer.Count == 0) yield break;
            buffer.Sort((a,b)=> a.timeFromEnter.CompareTo(b.timeFromEnter));

            float elapsed = 0f;
            int index = 0;
            while (index < buffer.Count && currentState == st)
            {
                var target = buffer[index];
                if (target.strength > maxAutoStrength)
                { index++; continue; }
                if (suppressLowerAfterEscalation && target.strength < highestShownThisState)
                { index++; continue; }

                if (elapsed < target.timeFromEnter)
                {
                    float wait = target.timeFromEnter - elapsed;
                    while (wait > 0f && currentState == st)
                    {
                        wait -= Time.deltaTime; elapsed += Time.deltaTime; yield return null;
                    }
                    if (currentState != st) yield break;
                }

                float since = Time.realtimeSinceStartup - lastHintTimeRealtime;
                if (since < minGapBetweenHints)
                {
                    yield return new WaitForSeconds(minGapBetweenHints - since);
                }
                if (currentState != st) yield break;

                if (hintPresenter && currentState == st)
                {
                    hintPresenter.Show(target.text, target.duration);
                    lastHintTimeRealtime = Time.realtimeSinceStartup;
                    if (target.strength > highestShownThisState)
                        highestShownThisState = target.strength;
                }
                elapsed = target.timeFromEnter;
                index++;
                yield return null;
            }
        }

        public void ForceHint(string text, float duration, TimedHintsConfig.HintStrength strength)
        {
            if (!hintPresenter) return;
            hintPresenter.Show(text, duration);
            if (strength > highestShownThisState)
                highestShownThisState = strength;
            lastHintTimeRealtime = Time.realtimeSinceStartup;
        }
    }
}
