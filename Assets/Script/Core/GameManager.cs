using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;
using ARKOM.Story;

namespace ARKOM.Game
{
    public class GameManager : MonoBehaviour
    {
        public int currentDay = 1;
        public int maxDays = 5;
        public AnomalyManager anomalyManager;
        public GameState State { get; private set; } = GameState.DayExploration;

        [Header("Night Timer")]
        public float nightDuration = 180f;
        public float NightTimeRemaining { get; private set; }

        [Header("Story Win")]
        public string[] requiredStoryFlags;

        private void Awake()
        {
            EventBus.Publish(new GameStateChangedEvent(State));
            ApplyCursorForState(State);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NightCompletedEvent>(HandleNightCompleted);
            EventBus.Subscribe<LoudNoiseDetectedEvent>(HandleLoudNoiseDetected);
            EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
            EventBus.Subscribe<StoryFlagAddedEvent>(OnStoryFlagAdded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NightCompletedEvent>(HandleNightCompleted);
            EventBus.Unsubscribe<LoudNoiseDetectedEvent>(HandleLoudNoiseDetected);
            EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
            EventBus.Unsubscribe<StoryFlagAddedEvent>(OnStoryFlagAdded);
        }

        private void Update()
        {
            if (State == GameState.NightAnomaly && NightTimeRemaining > 0f)
            {
                NightTimeRemaining -= Time.deltaTime;
                if (NightTimeRemaining <= 0f)
                {
                    NightTimeRemaining = 0f;
                    if (anomalyManager && anomalyManager.RemainingCount > 0)
                        TriggerGameOver();
                }
            }
        }

        private void HandleNightCompleted(NightCompletedEvent _) => OnNightComplete();
        private void HandleLoudNoiseDetected(LoudNoiseDetectedEvent _) => TriggerGameOver();

        public void BeginDay()
        {
            if (State == GameState.Victory || State == GameState.GameOver) return;
            NightTimeRemaining = 0f;
            SetState(GameState.DayExploration);
        }

        public void BeginNight()
        {
            if (State == GameState.Victory) return;
            SetState(GameState.NightAnomaly);
            NightTimeRemaining = nightDuration;
            anomalyManager.StartNight();
        }

        private void OnNightComplete()
        {
            NightTimeRemaining = 0f;
            currentDay++;
            if (CheckStoryVictory()) return;
            if (currentDay > maxDays)
            {
                TriggerVictory();
                return;
            }
            SetState(GameState.Transition);
            Invoke(nameof(BeginDay), 2f);
        }

        private void OnQTEResult(QTEResultEvent evt)
        {
            if (State != GameState.QTE) return;
            if (!evt.Success) TriggerGameOver();
            else SetState(GameState.NightAnomaly);
        }

        private void OnStoryFlagAdded(StoryFlagAddedEvent evt)
        {
            if (CheckStoryVictory())
                Debug.Log($"[Story] Victory triggered by flag '{evt.Flag}'");
        }

        private bool CheckStoryVictory()
        {
            if (requiredStoryFlags == null || requiredStoryFlags.Length == 0) return false;
            if (StoryFlags.Instance == null) return false;
            foreach (var f in requiredStoryFlags)
            {
                if (string.IsNullOrEmpty(f)) continue;
                if (!StoryFlags.Instance.Has(f)) return false;
            }
            TriggerVictory();
            return true;
        }

        private void TriggerGameOver()
        {
            if (State == GameState.GameOver || State == GameState.Victory) return;
            SetState(GameState.GameOver);
        }

        private void TriggerVictory()
        {
            if (State == GameState.Victory) return;
            SetState(GameState.Victory);
            EventBus.Publish(new VictoryEvent());
        }

        public void ResetGame()
        {
            Time.timeScale = 1f;
            currentDay = 1;
            NightTimeRemaining = 0f;

            // ��ҧ flags ������� evidence ��Ѻ�������� (����繴�䫹����ͧ�纫��)
            if (StoryFlags.Instance) StoryFlags.Instance.ClearAll();

            // ���� evidence ����͹ (����Ѻ destroyOnPickup=false)
            EvidenceRegistry.ResetAll(clearFlags: false);

            SetState(GameState.DayExploration);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetState(GameState newState)
        {
            State = newState;
            EventBus.Publish(new GameStateChangedEvent(newState));

            bool uiMode = newState == GameState.GameOver || newState == GameState.Victory;
            Cursor.lockState = uiMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = uiMode;
        }

        private void ApplyCursorForState(GameState st)
        {
            bool uiMode = st == GameState.GameOver || st == GameState.Victory || st == GameState.QTE;
            if (uiMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}