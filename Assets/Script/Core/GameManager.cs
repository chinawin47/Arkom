using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;

namespace ARKOM.Game
{
    public class GameManager : MonoBehaviour
    {
        public int currentDay = 1;
        public int maxDays = 5;
        public AnomalyManager anomalyManager;
        public GameState State { get; private set; } = GameState.DayExploration;

        [Header("Night Timer")]
        [Tooltip("Total seconds for a night before fail if anomalies remain")]
        public float nightDuration = 180f;
        public float NightTimeRemaining { get; private set; }

        private void Awake()
        {
            EventBus.Publish(new GameStateChangedEvent(State));
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NightCompletedEvent>(HandleNightCompleted);
            EventBus.Subscribe<LoudNoiseDetectedEvent>(HandleLoudNoiseDetected);
            EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NightCompletedEvent>(HandleNightCompleted);
            EventBus.Unsubscribe<LoudNoiseDetectedEvent>(HandleLoudNoiseDetected);
            EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
        }

        private void Update()
        {
            if (State == GameState.NightAnomaly)
            {
                if (NightTimeRemaining > 0f)
                {
                    NightTimeRemaining -= Time.deltaTime;
                    if (NightTimeRemaining <= 0f)
                    {
                        NightTimeRemaining = 0f;
                        // If still unresolved anomalies ? fail
                        if (anomalyManager != null && anomalyManager.RemainingCount > 0)
                            TriggerGameOver();
                    }
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

        private void TriggerGameOver()
        {
            if (State == GameState.GameOver || State == GameState.Victory) return;
            SetState(GameState.GameOver);
            Invoke(nameof(ResetGame), 3f);
        }

        private void TriggerVictory()
        {
            if (State == GameState.Victory) return;
            SetState(GameState.Victory);
            EventBus.Publish(new VictoryEvent());
        }

        private void ResetGame()
        {
            currentDay = 1;
            NightTimeRemaining = 0f;
            SetState(GameState.DayExploration);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            EventBus.Publish(new GameStateChangedEvent(newState));
        }
    }
}