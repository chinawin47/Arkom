using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Anomalies.Runtime;

[RequireComponent(typeof(Collider))]
public class TriggerZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("กำหนดรหัสโซน (เพื่อ Debug/Log)")]
    public string zoneId;

    [Tooltip("ทำงานเฉพาะตอนกลางคืนเท่านั้น")]
    public bool requireNightOnly = true;

    [Range(0f, 1f)]
    [Tooltip("โอกาสเกิดต่อการเดินผ่าน (0..1)")]
    public float probability = 1f;

    [Tooltip("จำนวนที่จะพยายามสั่งเกิดต่อครั้ง")]
    public int requestCount = 1;

    [Header("Cooldown / Limit")]
    [Tooltip("คูลดาวน์ของโซน (วินาที) หลังสั่งเกิดสำเร็จ")]
    public float zoneCooldownSeconds = 20f;

    [Tooltip("จำนวนครั้งสูงสุดต่อคืน (0 = ไม่จำกัด)")]
    public int maxTriggersPerNight = 0;

    [Header("Allowed Points")]
    [Tooltip("ถ้ากำหนด จะพยายามเลือกจากรายการนี้ก่อน (เว้นว่าง = ใช้ pointPool ของ Manager)")]
    public List<AnomalyPoint> allowedPoints = new();

    [Header("Manager Reference (optional)")]
    [Tooltip("ปล่อยว่างเพื่อหาอัตโนมัติ")]
    public AnomalyManager manager;

    // Runtime
    private float lastTriggeredAt = -99999f;
    private int usedThisNight = 0;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col && !col.isTrigger)
            Debug.LogWarning($"[TriggerZone] Collider on '{name}' should be isTrigger = true", this);
        if (!manager) manager = FindObjectOfType<AnomalyManager>();
        if (string.IsNullOrEmpty(zoneId)) zoneId = name;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnState);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnState);
    }

    private void OnState(GameStateChangedEvent e)
    {
        // เริ่มคืนใหม่ -> รีเซ็ตตัวนับต่อคืน
        if (e.State == ARKOM.Core.GameState.NightAnomaly)
            usedThisNight = 0;
    }

    private bool CoolingDown => (Time.time - lastTriggeredAt) < zoneCooldownSeconds;

    private void OnTriggerEnter(Collider other)
    {
        if (!other || !other.CompareTag("Player")) return;

        // เงื่อนไขเบื้องต้น
        if (!manager)
        {
            Debug.LogWarning($"[TriggerZone] No AnomalyManager found for zone '{zoneId}'", this);
            return;
        }

        // ทำงานเฉพาะกลางคืน
        var gm = FindObjectOfType<ARKOM.Game.GameManager>();
        if (requireNightOnly && (gm == null || gm.State != ARKOM.Core.GameState.NightAnomaly))
            return;

        if (!manager.IsPointMode)
        {
            // ใช้เฉพาะ Point Mode เท่านั้น
            return;
        }

        if (CoolingDown) return;
        if (maxTriggersPerNight > 0 && usedThisNight >= maxTriggersPerNight) return;

        if (Random.value > probability) return;

        // เตรียมชุดผู้สมัคร
        List<AnomalyPoint> pool = null;
        if (allowedPoints != null && allowedPoints.Count > 0)
            pool = allowedPoints;
        else
            pool = manager.pointPool;

        if (pool == null || pool.Count == 0) return;

        // พยายามขอให้ Manager สปอว์นจากชุดนี้
        int spawned = manager.TrySpawnFromAllowed(pool, Mathf.Max(1, requestCount));
        if (spawned > 0)
        {
            lastTriggeredAt = Time.time;
            usedThisNight++;
#if UNITY_EDITOR
            Debug.Log($"[TriggerZone] '{zoneId}' triggered -> spawned {spawned}");
#endif
        }
        // ถ้า 0: อาจเต็มเพดาน/ถึงเป้า/ทุกจุดติดคูลดาวน์ -> เงียบไว้เพื่อไม่กวน
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        var c = GetComponent<Collider>();
        if (c is BoxCollider bc)
        {
            var m = bc.transform.localToWorldMatrix;
            var size = Vector3.Scale(bc.size, bc.transform.lossyScale);
            var center = bc.center;
            Gizmos.matrix = m;
            Gizmos.DrawCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (c is SphereCollider sc)
        {
            Gizmos.DrawSphere(sc.transform.TransformPoint(sc.center), sc.radius * Mathf.Max(sc.transform.lossyScale.x, Mathf.Max(sc.transform.lossyScale.y, sc.transform.lossyScale.z)));
        }
        else if (c is CapsuleCollider cc)
        {
            // วาดสเฟียร์คร่าวๆ แทน
            Gizmos.DrawSphere(cc.transform.TransformPoint(cc.center), cc.radius * Mathf.Max(cc.transform.lossyScale.x, cc.transform.lossyScale.z));
        }

        float remain = Mathf.Max(0f, zoneCooldownSeconds - (Time.time - lastTriggeredAt));
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Zone: {zoneId}\nCD: {remain:0.0}s  Used: {usedThisNight}/{(maxTriggersPerNight>0?maxTriggersPerNight:999)}");
    }
#endif
}