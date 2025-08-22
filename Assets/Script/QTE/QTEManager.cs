using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ARKOM.Core;

namespace ARKOM.QTE
{
    public class QTEManager : MonoBehaviour
    {
        [Tooltip("Allowed keys for QTE prompts")]
        public List<Key> keyPool = new() { Key.W, Key.A, Key.S, Key.D, Key.Space, Key.E };
        public float timePerKey = 1.2f;
        public int sequenceLength = 3;

        private List<Key> currentSequence = new();
        private int index;
        private float timer;
        private bool active;

        public void StartQTE()
        {
            GenerateSequence();
            index = 0;
            timer = timePerKey;
            active = true;
            EventBus.Publish(new GameStateChangedEvent(GameState.QTE));
            // UI system should read currentSequence[index]
        }

        void Update()
        {
            if (!active) return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Fail();
                return;
            }

            foreach (var key in keyPool)
            {
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    if (key == currentSequence[index])
                    {
                        index++;
                        if (index >= currentSequence.Count)
                        {
                            Success();
                        }
                        else
                        {
                            timer = timePerKey;
                        }
                    }
                    else
                    {
                        Fail();
                    }
                    break;
                }
            }
        }

        private void GenerateSequence()
        {
            currentSequence.Clear();
            for (int i = 0; i < sequenceLength; i++)
            {
                currentSequence.Add(keyPool[Random.Range(0, keyPool.Count)]);
            }
        }

        private void Success()
        {
            active = false;
            EventBus.Publish(new QTEResultEvent(true));
        }

        private void Fail()
        {
            active = false;
            EventBus.Publish(new QTEResultEvent(false));
        }
    }
}