using UnityEngine;
using ARKOM.Anomalies.Data;

namespace ARKOM.Anomalies.Runtime
{
    // �ش Anomaly ˹�觨ش����� "������͡ 3 Ẻ" ��Ф����Ŵ�ǹ��ͨش
    public class AnomalyPoint : MonoBehaviour
    {
        [Header("Point Id / ���ʨش (��ͧ����ӷ�駫չ)")]
        [Tooltip("����������ѡ�дѺ '�ش' ���͡ѹ���Ѻ�ش��� (����͹�Ѻ/Resolve ���绶Ѵ�)")]
        public string pointId;

        [Header("Anomaly Component")]
        [Tooltip("����๹�� Anomaly ���ѵ�بش��� (��������� ����� GameObject ���ǡѹ)")]
        public Anomaly anomaly;

        [Header("Variants (3 Ẻ��ͨش)")]
        [Tooltip("��ʵ� AnomalyData ��������ͧ�ش��� (������ 3 ��¡��)")]
        public AnomalyData[] variants = new AnomalyData[3];

        [Tooltip("���˹ѡ�����ͧ����Ẻ (������Ҩ�������ѷ��). ������ǵ�ͧ��ҡѺ variants. ��� 0 ���ͤ�ҹ�������Ŵ�͡��")]
        public float[] variantWeights = new float[3];

        [Header("Cooldown / ��Ŵ�ǹ��ͨش")]
        [Tooltip("���Ҥ�Ŵ�ǹ���ѧ Resolve (�Թҷ�) ��͹���ش���ж١���͡�Դ���ա����")]
        public float cooldownSeconds = 20f;

        [Header("Random Policy")]
        [Tooltip("����§����������Ẻ����ѹ�� (�ҡ�յ�����͡���)")]
        public bool avoidImmediateRepeat = true;
        [Tooltip("�ѧ�Ѻ���͡ index Ẻ (����Ѻ debug): -1 = �Դ, 0..N-1 = �ѧ�Ѻ")]
        public int forceVariantIndex = -1;

        // Runtime state
        [SerializeField, Tooltip("ʶҹ����� (��ҹ���ҧ����)")] private bool isActive;
        [SerializeField, Tooltip("�Ѫ��Ẻ����ش (-1 = �ѧ�����)")] private int lastPickedVariantIndex = -1;
        [SerializeField, Tooltip("���ҷ�� Resolve ����ش (Time.time)")] private float lastResolvedAt = -99999f;

        public bool IsActive => isActive;
        public int LastVariantIndex => lastPickedVariantIndex;
        public float LastResolvedAt => lastResolvedAt;

        private void Awake()
        {
            if (!anomaly)
                anomaly = GetComponent<Anomaly>();
        }

        public bool CanActivate(float now)
        {
            if (isActive) return false;
            return (now - lastResolvedAt) >= cooldownSeconds;
        }

        public float CooldownRemaining(float now)
        {
            float remain = cooldownSeconds - (now - lastResolvedAt);
            return remain > 0f ? remain : 0f;
        }

        public int PickVariantIndex()
        {
            int count = (variants != null) ? variants.Length : 0;
            if (count <= 0) return -1;

            // Force ����Ѻ debug
            if (forceVariantIndex >= 0 && forceVariantIndex < count)
                return forceVariantIndex;

            // Weighted pick
            int picked = WeightedPick(excludeIndex: (avoidImmediateRepeat && count > 1) ? lastPickedVariantIndex : -1);
            if (picked < 0)
            {
                // fallback uniform
                int idx = Random.Range(0, count);
                if (avoidImmediateRepeat && count > 1 && idx == lastPickedVariantIndex)
                    idx = (idx + 1 + Random.Range(0, count - 1)) % count;
                return idx;
            }
            return picked;
        }

        private int WeightedPick(int excludeIndex = -1)
        {
            int count = variants?.Length ?? 0;
            if (count == 0) return -1;

            // ���ҧ��������˹ѡ �µѴ index ��� exclude �͡ (���˹ѡ = 0)
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                float w = (variantWeights != null && i < variantWeights.Length) ? Mathf.Max(0f, variantWeights[i]) : 1f;
                if (i == excludeIndex) w = 0f;
                total += w;
            }
            if (total <= 0f) return -1; // �ء���˹ѡ�� 0

            float r = Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < count; i++)
            {
                float w = (variantWeights != null && i < variantWeights.Length) ? Mathf.Max(0f, variantWeights[i]) : 1f;
                if (i == excludeIndex) w = 0f;
                acc += w;
                if (r <= acc) return i;
            }
            return count - 1;
        }

        public void ActivateRandom()
        {
            if (!anomaly)
            {
                Debug.LogWarning($"[AnomalyPoint] Missing Anomaly component on '{name}'");
                return;
            }
            if (isActive) return;

            int idx = PickVariantIndex();
            if (idx < 0 || idx >= (variants?.Length ?? 0))
            {
                Debug.LogWarning($"[AnomalyPoint] No valid variant to activate on '{name}'");
                return;
            }

            var data = variants[idx];
            if (!data)
            {
                Debug.LogWarning($"[AnomalyPoint] Variant index {idx} is null on '{name}'");
                return;
            }

            anomaly.data = data;
            lastPickedVariantIndex = idx;
            isActive = true;
            anomaly.Activate();

            Debug.Log($"[AnomalyPoint] Activate '{pointId}' -> Variant[{idx}] '{data.displayName}'");
        }

        public void OnResolved()
        {
            isActive = false;
            lastResolvedAt = Time.time;
        }

        public void ResetPointState(bool resetCooldown = false)
        {
            isActive = false;
            if (resetCooldown)
                lastResolvedAt = -99999f;
        }

        public void ApplyCooldownRemaining(float remainingSeconds)
        {
            if (remainingSeconds <= 0f)
            {
                lastResolvedAt = Time.time - cooldownSeconds;
            }
            else
            {
                lastResolvedAt = Time.time - (cooldownSeconds - remainingSeconds);
            }
        }

        private void OnValidate()
        {
            // �ѡ�Ҥ������ variants = 3 (��䫹����)
            if (variants == null || variants.Length != 3)
            {
                if (variants == null) variants = new AnomalyData[3];
                else if (variants.Length != 3)
                {
                    var arr = new AnomalyData[3];
                    for (int i = 0; i < Mathf.Min(variants.Length, 3); i++)
                        arr[i] = variants[i];
                    variants = arr;
                }
            }

            // ��������ǹ��˹ѡ�ç�Ѻ variants
            if (variantWeights == null || variantWeights.Length != variants.Length)
            {
                var w = new float[variants.Length];
                for (int i = 0; i < w.Length; i++)
                    w[i] = (variantWeights != null && i < variantWeights.Length) ? variantWeights[i] : 1f;
                variantWeights = w;
            }

            if (cooldownSeconds < 0f) cooldownSeconds = 0f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float now = Application.isPlaying ? Time.time : 0f;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, $"Point: {pointId}\nActive: {isActive}\nCD Remain: {CooldownRemaining(now):0.0}s");
        }
#endif
    }
}