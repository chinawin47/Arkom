using UnityEngine;

/// <summary>
/// ใส่บนวัตถุที่เป็น Interactable เพื่อรองรับการ Highlight เมื่อผู้เล่นเล็ง
/// ใช้ MaterialPropertyBlock เพื่อลดการสร้าง Material ซ้ำ
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = Color.cyan;
    [Tooltip("ค้นหา Renderer ลูกทั้งหมดหรือเฉพาะตัวเอง")]
    public bool includeChildren = true;
    [Tooltip("เพิ่มความสว่าง (Emission) ถ้า Shader รองรับ _EmissionColor")]
    public bool useEmission = true;
    [Range(0f,5f)] public float emissionBoost = 1.5f;

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Color[] originalColors;
    private bool highlighted;

    void Awake()
    {
        renderers = includeChildren ? GetComponentsInChildren<Renderer>() : new[] { GetComponent<Renderer>() };
        block = new MaterialPropertyBlock();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i]) continue;
            var mat = renderers[i].sharedMaterial;
            if (mat && mat.HasProperty("_Color"))
                originalColors[i] = mat.color;
            else
                originalColors[i] = Color.white;
        }
    }

    public void SetHighlight(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(block);

            if (on)
            {
                block.SetColor("_Color", highlightColor);
                if (useEmission && r.sharedMaterial && r.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    block.SetColor("_EmissionColor", highlightColor * emissionBoost);
                    r.sharedMaterial.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                block.SetColor("_Color", originalColors[i]);
                if (useEmission && r.sharedMaterial && r.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    block.SetColor("_EmissionColor", Color.black);
                }
            }

            r.SetPropertyBlock(block);
        }
    }
}