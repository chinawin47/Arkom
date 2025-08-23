using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;

namespace ARKOM.Game
{
    public class GameManager : MonoBehaviour
    {
        public int currentDay = 1;
        public AnomalyManager anomalyManager;
        public GameState State { get; private set; } = GameState.DayExploration;

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

        private void HandleNightCompleted(NightCompletedEvent _)
        {
            OnNightComplete();
        }

        private void HandleLoudNoiseDetected(LoudNoiseDetectedEvent _)
        {
            TriggerGameOver();
        }

        public void BeginDay()
        {
            SetState(GameState.DayExploration);
        }

        public void BeginNight()
        {
            SetState(GameState.NightAnomaly);
            anomalyManager.StartNight();
        }

        public void BeginQTE()
        {
            SetState(GameState.QTE);
        }

        private void OnNightComplete()
        {
            currentDay++;
            SetState(GameState.Transition);
            Invoke(nameof(BeginDay), 2f);
        }

        private void OnQTEResult(QTEResultEvent evt)
        {
            if (State != GameState.QTE) return;
            if (!evt.Success)
            {
                TriggerGameOver();
            }
            else
            {
                SetState(GameState.NightAnomaly);
            }
        }

        private void TriggerGameOver()
        {
            SetState(GameState.GameOver);
            Invoke(nameof(ResetGame), 3f);
        }

        private void ResetGame()
        {
            currentDay = 1;
            BeginDay();
        }

        private void SetState(GameState newState)
        {
            State = newState;
            EventBus.Publish(new GameStateChangedEvent(newState));
        }
    }
}