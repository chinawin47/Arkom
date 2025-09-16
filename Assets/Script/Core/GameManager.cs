using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;
using ARKOM.Story;

namespace ARKOM.Game
{
    public enum GameOverReason
    {
        None,
        Timeout,
        LoudNoise,
        QTEFail
    }

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

        public GameOverReason LastGameOverReason { get; private set; } = GameOverReason.None;

        private void Awake()
        {
            EventBus.Publish(new GameStateChangedEvent(State));
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NightCompletedEvent>(HandleNightCompleted);
            EventBus.Subscribe<LoudNoiseDetectedEvent>(HandleLoudNoiseDetected);
            EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
            EventBus.Subscribe<StoryFlagAddedEvent>(OnStoryFlagAdded);

            // NEW: �Ѻ�ѭ�ҳ੾�Сó� QTE fail ����ͧ Game Over
            EventBus.Subscribe<QTEFailGameOverEvent>(OnQTEFailGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NightCompletedEvent>(HandleNightCompleted);
            EventBus.Unsubscribe<LoudNoiseDetectedEvent>(HandleLoudNoiseDetected);
            EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
            EventBus.Unsubscribe<StoryFlagAddedEvent>(OnStoryFlagAdded);

            // NEW: ¡��ԡ sub
            EventBus.Unsubscribe<QTEFailGameOverEvent>(OnQTEFailGameOver);
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
                        TriggerGameOver(GameOverReason.Timeout);
                }
            }
        }

        private void HandleNightCompleted(NightCompletedEvent _) => OnNightComplete();
        private void HandleLoudNoiseDetected(LoudNoiseDetectedEvent _)
        {
            TriggerGameOver(GameOverReason.LoudNoise);
        }

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

            // ����¹��Ѻ�������� NightAnomaly ��������� QTE �� (�� Game Over �����������˵ء�ó�����繤��Ѵ�Թ)
            SetState(GameState.NightAnomaly);
        }

        // NEW: �Ѻ੾�Сó� QTE fail ����ͧ Game Over
        private void OnQTEFailGameOver(QTEFailGameOverEvent _)
        {
            TriggerGameOver(GameOverReason.QTEFail);
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

        private void TriggerGameOver(GameOverReason reason)
        {
            if (State == GameState.GameOver || State == GameState.Victory) return;
            LastGameOverReason = reason;
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
            LastGameOverReason = GameOverReason.None;
            if (StoryFlags.Instance) StoryFlags.Instance.ClearAll();
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
    }
}