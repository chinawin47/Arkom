using UnityEngine;

namespace ARKOM.Anomalies.Data
{
    [CreateAssetMenu(fileName = "New Anomaly", menuName = "ARKOM/Anomaly Data")]
    public class AnomalyData : ScriptableObject
    {
        [Header("�����ž�鹰ҹ")]
        [Tooltip("�������������Ѻ���Ǩ�ͺ/��ҧ�ԧ (��ͧ�����)")]
        public string anomalyId;
        [Tooltip("���ͷ����ʴ��� UI ���� Log")]
        public string displayName;

        [Header("�����������Դ����")]
        [Tooltip("�ٻẺ�������¹�ŧ������")]
        public AnomalyType type;

        [Header("Transform Offsets (��Ѻ Position / Rotation / Scale)")]
        [Tooltip("���з�������͹ (���Ѻ���˹����)")]
        public Vector3 positionOffset;
        [Tooltip("ͧ�ҷ�����ع (���Ѻ��ع���)")]
        public Vector3 rotationOffset;
        [Tooltip("��Ǥٳ��Ҵ (1 = ������)")]
        public float scaleMultiplier = 1f;

        [Header("Color Change (��Ѻ Color)")]
        [Tooltip("�շ���᷹�����ʴ� (Material._Color)")]
        public Color colorChange = Color.red;

        [Header("Prefab / �Ϳ࿡�������")]
        [Tooltip("Prefab ���� Spawn ������������ ShadowMovement ���� SpawnPrefab")]
        public GameObject effectPrefab;
        [Tooltip("������������ effectPrefab")]
        public bool useEffectPrefab;
        [Tooltip("���§������蹵͹ Anomaly �١�Դ��ҹ")]
        public AudioClip soundEffect;

        public enum AnomalyType
        {
            Position,       // ����͹���˹�
            Rotation,       // ��ع
            Scale,          // ����¹��Ҵ
            Color,          // ����¹��
            Disappear,      // ��͹�ѵ�� (Renderer �Դ �� Collider �ѧ������顴��)
            PictureFlip,    // ��ԡ�ٻ/��Ѻ��ҹ (��ع 180 ͧ��)
            ShadowMovement, // ���ҧ������͹��� (Prefab)
            SpawnPrefab     // ���ҧ Prefab ���� (��觢ͧ�������)
        }
    }
}
