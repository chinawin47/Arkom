using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Data;
using ARKOM.Game;

namespace ARKOM.Anomalies.Runtime
{
    // ���Ѵ���������еԴ��� Anomaly ��ҧ�׹ (Random + Track) 
    public class AnomalyManager : MonoBehaviour
    {
        [Tooltip("��¡�� Anomaly (����๹��) �����������㹫չ������ / All anomaly components placed in scene")]
        public List<Anomaly> anomalyPool = new();

        [Header("Difficulty / �����ҡ")]
        [Tooltip("�ӹǹ��鹰ҹ��ͤ׹ / Base anomalies per night")]
        public int baseAnomaliesPerNight = 3;
        [Tooltip("���� +1 �ء � X �ѹ / Add +1 every X days")]
        public int dayIntervalIncrease = 2;

        private readonly HashSet<string> activeIds = new(); // id �����ѧ Active
        private int resolvedCount;                          // �ӹǹ�������蹵�Ǩ������

        // Public read-only info / ��������ҹ���ҧ������� UI ��
        public int ActiveAnomalyCount => activeIds.Count;          // �ӹǹ��������Դ������
        public int ResolvedCount => resolvedCount;                 // ��辺����
        public int RemainingCount => ActiveAnomalyCount - ResolvedCount; // ����ѧ�����

        private void OnEnable()  => EventBus.Subscribe<AnomalyResolvedEvent>(OnAnomalyResolved);
        private void OnDisable() => EventBus.Unsubscribe<AnomalyResolvedEvent>(OnAnomalyResolved);

        // ������׹���� / Start a night
        public void StartNight()
        {
            resolvedCount = 0;
            ResetAll();
            ActivateRandomSet();
            Debug.Log($"[AnomalyManager] Night started. Active = {ActiveAnomalyCount}"); // �͡�ӹǹ����Դ
        }

        // �Դ��������͹���� / Deactivate all
        private void ResetAll()
        {
            activeIds.Clear();
            foreach (var a in anomalyPool)
                a.Deactivate();
        }

        // �������͡����Ǩ� Active / Random pick which anomalies to activate
        private void ActivateRandomSet()
        {
            if (anomalyPool.Count == 0) return;

            int currentDay = FindObjectOfType<GameManager>()?.currentDay ?? 1;
            int bonus = Mathf.FloorToInt((currentDay - 1) / Mathf.Max(1, dayIntervalIncrease));
            int target = Mathf.Clamp(baseAnomaliesPerNight + bonus, 1, anomalyPool.Count);

            // Fisher-Yates shuffle / ������ʵ�
            List<Anomaly> shuffled = new(anomalyPool);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int j = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < target; i++)
            {
                var a = shuffled[i];
                a.Activate();
                if (a.data != null)
                    activeIds.Add(a.data.anomalyId);
            }
        }

        // ����ͼ����蹵�Ǩ�� / Player confirmed anomaly
        private void OnAnomalyResolved(AnomalyResolvedEvent evt)
        {
            if (!activeIds.Contains(evt.Id)) return;
            resolvedCount++;
            Debug.Log($"[AnomalyManager] Resolved {evt.Id} ({resolvedCount}/{ActiveAnomalyCount})");
            if (resolvedCount >= activeIds.Count)
                EventBus.Publish(new NightCompletedEvent());
        }
    }
}