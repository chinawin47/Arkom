using UnityEngine;
using ARKOM.Story;

[CreateAssetMenu(fileName = "Evidence_", menuName = "ARKOM/Story/Evidence")]
public class EvidenceItem : ScriptableObject
{
    [Tooltip("รหัส Flag ที่จะเซ็ตเมื่อเก็บ")]
    public string flagId;
    [Tooltip("ชื่อแสดง (debug / UI)")]
    public string displayName;
}