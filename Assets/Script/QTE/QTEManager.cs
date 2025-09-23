using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ARKOM.Core;

namespace ARKOM.QTE
{
    public class QTEManager : MonoBehaviour
    {
        public List<Key> keyPool = new() { Key.W, Key.A, Key.S, Key.D, Key.Space }; // ��� Key.E �͡
        public float timePerKey = 1.2f;
        public int sequenceLength = 3;
        [Header("Input Grace")]
        [Tooltip("�ѹ���� Interact (E) 仡Թ�����á ��˹�ǧ��͹�������Ǩ")]
        public float inputGraceTime = 0.15f;

        private List<Key> currentSequence = new();
        private int index;
        private float timer;
        private bool active;
        private float graceTimer;

        public bool IsActive => active;
        public Key CurrentExpectedKey => active && index < currentSequence.Count ? currentSequence[index] : Key.None;
        public float RemainingTime => timer;
        public int CurrentIndex => index;
        public int TotalLength => currentSequence.Count;

        public void StartQTE()
        {
            GenerateSequence();
            index = 0;
            timer = timePerKey;
            graceTimer = inputGraceTime;
            active = true;
            EventBus.Publish(new GameStateChangedEvent(GameState.QTE));
        }

        void Update()
        {
            if (!active) return;

            // ˹�ǧ�Ѻ����������� HUD �ѹ�ʴ���͹ ��Сѹ E �����觡�
            if (graceTimer > 0f)
            {
                graceTimer -= Time.deltaTime;
                return; // �ѧ���������Ѻ����Ŵ��������ҡ (�͹���Ѻ��ѧ grace)
            }

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
                            Success();
                        else
                            timer = timePerKey;
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
                currentSequence.Add(keyPool[Random.Range(0, keyPool.Count)]);
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