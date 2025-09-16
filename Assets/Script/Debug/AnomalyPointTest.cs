using UnityEngine;
using UnityEngine.InputSystem;
using ARKOM.Anomalies.Runtime;

public class AnomalyPointTest : MonoBehaviour
{
    public Key selectNextKey = Key.Tab;
    public Key activateKey = Key.P;
    public Key resolveKey = Key.L;
    public Key activateAllEligibleKey = Key.Z;

    [Tooltip("รีเฟรชรายการจุดอัตโนมัติเมื่อมีการเพิ่ม/ลบในซีน")]
    public bool autoRefreshPoints = true;
    public float autoRefreshInterval = 2f;

    private AnomalyPoint[] points = System.Array.Empty<AnomalyPoint>();
    private int selectedIndex = 0;
    private float refreshTimer;

    void Start()
    {
        RefreshPoints();
        Debug.Log("[AnomalyPointTest] Loaded points = " + points.Length);
        Debug.Log("[AnomalyPointTest] Keys: TAB=เลือกจุด, P=Activate จุดที่เลือก, L=Resolve จุดที่เลือก, Z=Activate ทุกจุดที่พ้นคูลดาวน์");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[selectNextKey].wasPressedThisFrame)
            NextSelection(1);

        if (kb[activateKey].wasPressedThisFrame)
            ActivateSelected();

        if (kb[resolveKey].wasPressedThisFrame)
            ResolveSelected();

        if (kb[activateAllEligibleKey].wasPressedThisFrame)
            ActivateAllEligible();

        // เลือกด้วยเลข 1..9
        if (kb.digit1Key.wasPressedThisFrame) SelectByNumber(1);
        if (kb.digit2Key.wasPressedThisFrame) SelectByNumber(2);
        if (kb.digit3Key.wasPressedThisFrame) SelectByNumber(3);
        if (kb.digit4Key.wasPressedThisFrame) SelectByNumber(4);
        if (kb.digit5Key.wasPressedThisFrame) SelectByNumber(5);
        if (kb.digit6Key.wasPressedThisFrame) SelectByNumber(6);
        if (kb.digit7Key.wasPressedThisFrame) SelectByNumber(7);
        if (kb.digit8Key.wasPressedThisFrame) SelectByNumber(8);
        if (kb.digit9Key.wasPressedThisFrame) SelectByNumber(9);

        if (autoRefreshPoints)
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = autoRefreshInterval;
                RefreshPoints();
            }
        }
    }

    private void RefreshPoints()
    {
        points = FindObjectsOfType<AnomalyPoint>(includeInactive: false);
        if (points.Length == 0) selectedIndex = 0;
        else selectedIndex = Mathf.Clamp(selectedIndex, 0, points.Length - 1);
    }

    private void NextSelection(int step)
    {
        if (points.Length == 0) return;
        selectedIndex = (selectedIndex + step) % points.Length;
        if (selectedIndex < 0) selectedIndex += points.Length;
        AnnounceSelection();
    }

    private void SelectByNumber(int n)
    {
        if (points.Length == 0) return;
        int idx = n - 1;
        if (idx >= 0 && idx < points.Length)
        {
            selectedIndex = idx;
            AnnounceSelection();
        }
    }

    private void AnnounceSelection()
    {
        if (points.Length == 0) return;
        var p = points[selectedIndex];
        float cd = p.CooldownRemaining(Time.time);
        Debug.Log($"[AnomalyPointTest] Select {selectedIndex + 1}/{points.Length}: '{p.name}' (pointId={p.pointId}) Active={p.IsActive} CD={cd:0.0}s LastVar={p.LastVariantIndex}");
    }

    private void ActivateSelected()
    {
        if (points.Length == 0) return;
        var p = points[selectedIndex];
        if (!p.CanActivate(Time.time))
        {
            Debug.Log($"[AnomalyPointTest] '{p.pointId}' ยังไม่พ้นคูลดาวน์หรือกำลัง Active (CD={p.CooldownRemaining(Time.time):0.0}s)");
            return;
        }
        p.ActivateRandom();
    }

    private void ResolveSelected()
    {
        if (points.Length == 0) return;
        var p = points[selectedIndex];
        if (!p.IsActive)
        {
            Debug.Log($"[AnomalyPointTest] '{p.pointId}' ไม่ได้ Active");
            return;
        }

        // สำหรับทดสอบ: Deactivate วัตถุให้คืนสภาพ และเริ่มคูลดาวน์ของ Point
        var anomaly = p.GetComponent<Anomaly>();
        if (anomaly) anomaly.Deactivate();
        p.OnResolved();

        Debug.Log($"[AnomalyPointTest] Resolve '{p.pointId}' -> เริ่มคูลดาวน์ {p.CooldownRemaining(Time.time):0.0}s");
    }

    private void ActivateAllEligible()
    {
        int count = 0;
        foreach (var p in points)
        {
            if (!p) continue;
            if (p.CanActivate(Time.time))
            {
                p.ActivateRandom();
                count++;
            }
        }
        Debug.Log($"[AnomalyPointTest] ActivateAll: สั่งเกิดได้ {count} จุด");
    }

    void OnGUI()
    {
        if (points == null || points.Length == 0) return;

        GUILayout.BeginArea(new Rect(12, 12, 360, Screen.height - 24), GUI.skin.box);
        GUILayout.Label("AnomalyPoint Test");
        GUILayout.Label($"TAB: เลือกจุด | P: Activate | L: Resolve | Z: Activate All Eligible");
        GUILayout.Space(6);

        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            if (!p) continue;
            string marker = (i == selectedIndex) ? ">" : " ";
            float cd = p.CooldownRemaining(Time.time);
            GUILayout.Label($"{marker} [{i + 1}] {p.pointId} | Active={p.IsActive} | CD={cd:0.0}s | LastVar={p.LastVariantIndex}");
        }
        GUILayout.EndArea();
    }
}