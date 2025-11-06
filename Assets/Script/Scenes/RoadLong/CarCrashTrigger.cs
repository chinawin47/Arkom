using UnityEngine;
using ARKOM.Scenes.Road;

public class CarCrashTrigger : MonoBehaviour
{
    public RoadSequenceController controller;

    [Header("Collider Control")]
    [Tooltip("บังคับให้ Collider เป็น Trigger เสมอ")] public bool forceTriggerOn = true;
    [Tooltip("ตรวจและตั้งค่า isTrigger=true ทุกเฟรม (กันแอนิเมชันแก้กลับ)")] public bool reapplyEveryFrame = true;

    private Collider selfCollider;

    private void Awake()
    {
        selfCollider = GetComponent<Collider>();
        if (forceTriggerOn && selfCollider)
            selfCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (forceTriggerOn && selfCollider)
            selfCollider.isTrigger = true;
    }

    private void LateUpdate()
    {
        if (forceTriggerOn && reapplyEveryFrame && selfCollider && !selfCollider.isTrigger)
        {
            selfCollider.isTrigger = true; // กันโดนแอนิเมชัน/สคริปต์อื่นแก้กลับ
        }
    }

    // เผื่อเรียกจาก Animation Event ตอนจบคลิป
    public void EnsureTrigger()
    {
        if (selfCollider)
            selfCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(controller.ResetSequenceToStart());
        }
    }
}
