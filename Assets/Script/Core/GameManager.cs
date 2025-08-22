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
            EventBus.Subscribe<NightCompletedEvent>(_ => OnNightComplete());
            EventBus.Subscribe<LoudNoiseDetectedEvent>(_ => TriggerGameOver());
            EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NightCompletedEvent>(_ => OnNightComplete());
            EventBus.Unsubscribe<LoudNoiseDetectedEvent>(_ => TriggerGameOver());
            EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
        }

        public void BeginDay()
        {
            SetState(GameState.DayExploration);
            // Narrative triggers happen via separate systems.
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
            // Transition back to day
            SetState(GameState.Transition);
            Invoke(nameof(BeginDay), 2f); // simple delay
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
                // Return where we came from (simplified: to NightAnomaly)
                SetState(GameState.NightAnomaly);
            }
        }

        private void TriggerGameOver()
        {
            SetState(GameState.GameOver);
            // Reset after short delay (prototype)
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