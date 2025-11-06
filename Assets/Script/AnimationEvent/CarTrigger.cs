using UnityEngine;
using System.Collections;

public class CarTrigger : MonoBehaviour
{
    public Animator animator;
    [Tooltip("ชื่อ state ของแอนิเมชันรถที่ต้องรอให้จบก่อนปิด Trigger")]
    public string animStateName = "CarForGame"; // ตั้งให้ตรงกับชื่อ state/clip ใน Animator
    [Tooltip("หน่วงเวลาก่อนตรวจ normalizedTime (เผื่อ CrossFade)")] public float startCheckDelay =0.05f;
    [Tooltip("ปิด isTrigger เมื่อ normalizedTime ถึงค่านี้หรือมากกว่า")][Range(0.8f,1f)] public float completeThreshold =0.99f;
    [Tooltip("ใช้โหมดตรวจจับอัตโนมัติ (ไม่จำเป็นต้องใส่ Animation Event)")] public bool autoWatchAnimation = true;

    private bool triggered;
    private Collider selfCollider;

    void Awake()
    {
        selfCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.CompareTag("Player"))
        {
            if (animator)
                animator.SetTrigger("PlayAnim");
            triggered = true;
            if (autoWatchAnimation && animator)
                StartCoroutine(WatchAnimAndDisableTrigger());
        }
    }

    // เรียกจาก Animation Event ตอนท้ายคลิปได้ (ถ้าไม่ใช้ auto watch)
    public void OnCarAnimationFinished()
    {
        DisableTrigger();
    }

    private IEnumerator WatchAnimAndDisableTrigger()
    {
        // รอให้ Animator เข้า state เป้าหมาย
        yield return new WaitForSeconds(startCheckDelay);
        if (!animator) yield break;
        // รอจนเข้า state ที่ต้องการก่อน
        while (animator && !animator.GetCurrentAnimatorStateInfo(0).IsName(animStateName))
        {
            yield return null;
        }
        // ตอนนี้อยู่ใน state เป้าหมายแล้ว รอจนเล่นจบ
        while (animator)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (!st.IsName(animStateName)) break; // หลุดไป state อื่นก่อน ถือว่าไม่ต้องปิด
            if (st.normalizedTime >= completeThreshold)
            {
                DisableTrigger();
                break;
            }
            yield return null;
        }
    }

    private void DisableTrigger()
    {
        if (selfCollider && selfCollider.isTrigger)
        {
            selfCollider.isTrigger = false;
        }
    }
}
