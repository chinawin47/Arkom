using UnityEngine;
using ARKOM.Story;

[CreateAssetMenu(fileName = "Evidence_", menuName = "ARKOM/Story/Evidence")]
public class EvidenceItem : ScriptableObject
{
    [Tooltip("���� Flag �������������")]
    public string flagId;
    [Tooltip("�����ʴ� (debug / UI)")]
    public string displayName;
}