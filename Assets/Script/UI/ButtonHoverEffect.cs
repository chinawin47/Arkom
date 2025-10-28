using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Vector3 targetScale;
    [Header("ตั้งค่าความเด้ง")]
    [Range(1.0f, 1.5f)] public float hoverScale = 1.1f; // ขยายขึ้น 10%
    [Range(1f, 20f)] public float speed = 10f; // ความเร็วตอนเด้ง

    void Awake()
    {
        // จำค่า scale เดิมไว้ก่อนเริ่มเกม
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // ค่อย ๆ เปลี่ยน scale ให้ลื่น
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // เด้งขึ้นเมื่อชี้
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // กลับขนาดเดิม
        targetScale = originalScale;
    }
}
