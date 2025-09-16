#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ARKOM.Anomalies.Runtime;

[CustomEditor(typeof(AnomalyManager))]
public class AnomalyManagerEditor : Editor
{
    private GUIStyle _headerStyle;
    private GUIStyle _helpStyle;
    private Vector2 _scroll;
    private double _lastRepaint;

    public override void OnInspectorGUI()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _helpStyle = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
        }

        DrawDefaultInspector();

        GUILayout.Space(6);
        EditorGUILayout.LabelField("ARKOM • Point Tools", _headerStyle);
        EditorGUILayout.BeginVertical(_helpStyle);
        EditorGUILayout.LabelField("เครื่องมือสำหรับโหมด Point: Auto-Fill รายการจุด, ตรวจ pointId ซ้ำ/ว่าง, และดูสถานะ Runtime");
        EditorGUILayout.EndVertical();

        var mgr = (AnomalyManager)target;

        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-Fill pointPool (จากซีน)"))
        {
            Undo.RecordObject(mgr, "Auto-Fill pointPool");
            mgr.pointPool = FindAllPointsInScene();
            EditorUtility.SetDirty(mgr);
            Debug.Log($"[AnomalyManagerEditor] Auto-Fill: พบ {mgr.pointPool.Count} จุดในซีน");
        }

        if (GUILayout.Button("Validate Points"))
        {
            var report = ValidatePointsReport(mgr);
            ShowValidationDialog(report);
        }
        EditorGUILayout.EndHorizontal();

        // Runtime monitor (Play Mode)
        if (Application.isPlaying)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Monitor", _headerStyle);
            EditorGUILayout.BeginVertical(_helpStyle);

            bool pointMode = mgr.IsPointMode;
            EditorGUILayout.LabelField($"Mode: {(pointMode ? "Point Mode" : "Legacy Mode")}");
            if (pointMode)
            {
                EditorGUILayout.LabelField($"Active: {mgr.ActiveAnomalyCount}/{mgr.maxConcurrentActive}");
                EditorGUILayout.LabelField($"Resolved: {mgr.ResolvedCount}/{mgr.TargetForNight}");
                EditorGUILayout.LabelField($"Remaining: {mgr.RemainingCount}");
            }
            else
            {
                EditorGUILayout.LabelField($"Active: {mgr.ActiveAnomalyCount}");
                EditorGUILayout.LabelField($"Resolved: {mgr.ResolvedCount}/{mgr.TargetForNight}");
            }

            // Active IDs (using reflection)
            var activeIds = GetPrivate<HashSet<string>>(mgr, "activeIds");
            if (activeIds != null && activeIds.Count > 0)
            {
                EditorGUILayout.LabelField("Active IDs:");
                foreach (var id in activeIds)
                    EditorGUILayout.LabelField($" • {id}");
            }

            // Cooldowns overview per point
            if (mgr.pointPool != null && mgr.pointPool.Count > 0)
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("Points Cooldown / Active:");
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(180));
                foreach (var p in mgr.pointPool)
                {
                    if (!p) continue;
                    float cd = p.CooldownRemaining(Time.time);
                    string status = p.IsActive ? "ACTIVE" : (cd > 0f ? $"CD {cd:0.0}s" : "READY");
                    EditorGUILayout.LabelField($" - {p.pointId}  [{status}]  LastVar={p.LastVariantIndex}");
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();

            // Repaint ช่วงๆ เพื่อให้ค่าขยับแบบ realtime
            if (EditorApplication.timeSinceStartup - _lastRepaint > 0.25f)
            {
                _lastRepaint = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
    }

    private static List<AnomalyPoint> FindAllPointsInScene()
    {
        // เอาเฉพาะวัตถุในซีน (ไม่รวม asset/prefab)
        var all = Resources.FindObjectsOfTypeAll<AnomalyPoint>();
        var list = new List<AnomalyPoint>();
        foreach (var p in all)
        {
            if (!p) continue;
            var go = p.gameObject;
            if (!go.scene.IsValid()) continue;            // ต้องอยู่ในซีนที่เปิดอยู่
            if (EditorUtility.IsPersistent(go)) continue; // ตัด prefab asset
            list.Add(p);
        }
        // ลบซ้ำโดยอ้างอิงตัวอ็อบเจ็กต์
        return list.Distinct().ToList();
    }

    private static string ValidatePointsReport(AnomalyManager mgr)
    {
        var lines = new List<string>();
        var seenIds = new HashSet<string>();
        var dupIds = new HashSet<string>();
        int nullRefs = 0, emptyIds = 0, nullPoints = 0, noAnomaly = 0, badVariants = 0;

        // ตรวจในซีนทั้งหมดเพื่อหา id ซ้ำข้าม pool
        var allPoints = FindAllPointsInScene();

        foreach (var p in allPoints)
        {
            if (!p) { nullPoints++; continue; }
            string id = (p.pointId ?? "").Trim();
            if (string.IsNullOrEmpty(id)) { emptyIds++; continue; }
            if (!seenIds.Add(id)) dupIds.Add(id);
            if (!p.anomaly) noAnomaly++;
            // ตรวจ variants
            if (p.variants == null || p.variants.Length == 0 || p.variants.All(v => v == null))
                badVariants++;
        }

        lines.Add($"Scene Points: {allPoints.Count}");
        lines.Add($" - Null point objects: {nullPoints}");
        lines.Add($" - Empty pointId: {emptyIds}");
        lines.Add($" - Duplicate pointId(s): {dupIds.Count}");
        if (dupIds.Count > 0)
            lines.Add("   • " + string.Join(", ", dupIds.Take(12)) + (dupIds.Count > 12 ? " ..." : ""));
        lines.Add($" - Missing Anomaly component refs: {noAnomaly}");
        lines.Add($" - Invalid variants (none or all null): {badVariants}");

        // ตรวจภายใน pointPool ด้วย (ความสอดคล้อง)
        if (mgr.pointPool != null)
        {
            var poolSet = new HashSet<AnomalyPoint>(mgr.pointPool.Where(p => p));
            if (poolSet.Count != mgr.pointPool.Count)
                lines.Add($" - pointPool has duplicate references or null entries: {mgr.pointPool.Count - poolSet.Count}");

            var poolIds = new HashSet<string>();
            int poolEmptyIds = 0, poolDupIds = 0;
            foreach (var p in poolSet)
            {
                if (!p) continue;
                string id = (p.pointId ?? "").Trim();
                if (string.IsNullOrEmpty(id)) { poolEmptyIds++; continue; }
                if (!poolIds.Add(id)) poolDupIds++;
            }
            if (poolEmptyIds > 0) lines.Add($" - pointPool entries with empty pointId: {poolEmptyIds}");
            if (poolDupIds > 0) lines.Add($" - pointPool duplicate pointId entries: {poolDupIds}");
        }

        return string.Join("\n", lines);
    }

    private static void ShowValidationDialog(string report)
    {
        Debug.Log($"[AnomalyManagerEditor] Validate Points\n{report}");
        EditorUtility.DisplayDialog("Validate Anomaly Points", report, "OK");
    }

    private static T GetPrivate<T>(object obj, string fieldName) where T : class
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var f = obj.GetType().GetField(fieldName, flags);
        return f?.GetValue(obj) as T;
    }

    [MenuItem("Tools/ARKOM/Validate Anomaly Points")]
    public static void MenuValidatePoints()
    {
        var mgr = FindObjectOfType<AnomalyManager>();
        if (!mgr)
        {
            EditorUtility.DisplayDialog("Validate Anomaly Points", "ไม่พบ AnomalyManager ในซีน", "OK");
            return;
        }
        var report = ValidatePointsReport(mgr);
        ShowValidationDialog(report);
    }
}
#endif