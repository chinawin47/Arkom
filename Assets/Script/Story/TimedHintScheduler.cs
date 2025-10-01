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

        private SequenceController.StoryState currentState;
        private Coroutine runCo;

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
            if (config == null || config.hints == null) return;
            runCo = StartCoroutine(RunHintsForState(e.Current));
        }

        private IEnumerator RunHintsForState(SequenceController.StoryState st)
        {
            // collect hints for this state ordered by timeFromEnter
            var list = System.Array.FindAll(config.hints, h => h != null && h.state == st);
            if (list.Length == 0) yield break;
            System.Array.Sort(list, (a,b)=> a.timeFromEnter.CompareTo(b.timeFromEnter));
            float elapsed = 0f;
            int index = 0;
            while (index < list.Length && currentState == st)
            {
                var target = list[index];
                if (elapsed < target.timeFromEnter)
                {
                    float wait = target.timeFromEnter - elapsed;
                    while (wait > 0f && currentState == st)
                    {
                        wait -= Time.deltaTime; elapsed += Time.deltaTime; yield return null;
                    }
                    if (currentState != st) yield break;
                }
                // show hint
                if (hintPresenter && currentState == st)
                {
                    hintPresenter.Show(target.text, target.duration);
                }
                elapsed = target.timeFromEnter; // ensure alignment
                index++;
                yield return null; // allow a frame
            }
        }
    }
}
