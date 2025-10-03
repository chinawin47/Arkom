using UnityEngine;
using ARKOM.Core; // EventBus
using ARKOM.Story; // for GhostSpawnedEvent
using ARKOM.Player; // for player camera search
using System.Collections; // for IEnumerator

// วางสคริปต์นี้บน GameObject ผู้จัดการ (หรือบน root ผี) แล้วตั้งค่า ghostRoot ให้เป็นโมเดลผีในฉาก (ปิดไว้ก่อน)
// เมื่อเรียก Reveal() หรือเมื่อ Publish AnomalyFirstSeenEvent (ถ้าใช้ autoTriggerOnAnomaly) จะเปิดผี เล่นเสียง และส่ง GhostSpawnedEvent
// จากนั้น (ถ้าเปิด lookDespawn) จะรอให้ผู้เล่นมองครบกำหนดแล้วปิดเอง
[AddComponentMenu("Story/Manual Ghost Reveal")] 
public class ManualGhostReveal : MonoBehaviour
{
    [Header("Ghost Reference")] 
    public GameObject ghostRoot;          // โมเดลผี (วางในฉาก ปิด active เริ่มต้น)
    public bool disableOnStart = true;    // ปิดทันทีตอน Start

    [Header("Trigger Options")] 
    public bool autoTriggerOnAnomalyFound = true; // ทริกเกอร์เมื่อมี AnomalyFirstSeenEvent (SequenceController จะตาม flow เอง)
    public bool triggerOnce = true;               // แสดงครั้งเดียว

    [Header("Audio")] 
    public AudioClip revealSfx;           // เสียงตอนโผล่
    public AudioClip loopSfx;             // เสียงวนหลอน (optional)
    public bool attachAudioSource = true; // ถ้า true จะสร้าง AudioSource บน ghostRoot เล่น loop

    [Header("Look Despawn (Optional)")] 
    public bool lookDespawn = true;       // ปิดผีเมื่อถูกจ้องครบเวลาที่กำหนด
    public float lookTime = 3f;           // เวลาต้องจ้อง (วินาที) (<=0 = มองปุ๊บหาย)
    public float lookAngle = 20f;         // มุมที่ถือว่า “จ้อง”
    public bool continuousLook = true;    // ต้องจ้องต่อเนื่อง

    [Header("Animation (Optional)")] 
    public Animator animator;
    public string appearTrigger = "Appear"; // Trigger ตอนโผล่
    public string vanishTrigger = "Vanish"; // Trigger ตอนหาย
    public float vanishAnimExtraDelay = 0.5f; // รออนิเมก่อนปิด

    private bool revealed;
    private bool finished;
    private AudioSource loopSource;
    private Camera playerCam;
    private float lookAccum;

    void Awake()
    {
        if (ghostRoot && disableOnStart) ghostRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (autoTriggerOnAnomalyFound)
            EventBus.Subscribe<AnomalyFirstSeenEvent>(OnAnomalyFound);
    }

    void OnDisable()
    {
        if (autoTriggerOnAnomalyFound)
            EventBus.Unsubscribe<AnomalyFirstSeenEvent>(OnAnomalyFound);
    }

    private void OnAnomalyFound(AnomalyFirstSeenEvent e)
    {
        Reveal();
    }

    public void Reveal()
    {
        if (finished) return;
        if (triggerOnce && revealed) return;
        revealed = true;

        if (ghostRoot && !ghostRoot.activeSelf)
            ghostRoot.SetActive(true);

        if (animator && !string.IsNullOrEmpty(appearTrigger)) animator.SetTrigger(appearTrigger);

        if (revealSfx)
            AudioSource.PlayClipAtPoint(revealSfx, ghostRoot ? ghostRoot.transform.position : transform.position);

        if (loopSfx && attachAudioSource && ghostRoot)
        {
            loopSource = ghostRoot.GetComponent<AudioSource>();
            if (!loopSource) loopSource = ghostRoot.AddComponent<AudioSource>();
            loopSource.clip = loopSfx;
            loopSource.loop = true;
            loopSource.playOnAwake = false;
            loopSource.spatialBlend = 1f; // 3D
            loopSource.Play();
        }

        // ส่ง Event ให้ SequenceController เดินต่อ (แทน GhostSpawner)
        EventBus.Publish(new GhostSpawnedEvent(-1));

        if (lookDespawn)
        {
            playerCam = FindPlayerCamera();
            lookAccum = 0f;
        }
    }

    void Update()
    {
        if (!revealed || finished) return;
        if (!lookDespawn) return;
        if (!playerCam || !ghostRoot) return;

        Vector3 toGhost = ghostRoot.transform.position - playerCam.transform.position;
        float angle = Vector3.Angle(playerCam.transform.forward, toGhost.normalized);
        bool looking = angle <= lookAngle;

        if (lookTime <= 0f)
        {
            if (looking)
            {
                StartCoroutine(VanishRoutine());
                return;
            }
        }
        else
        {
            if (looking)
            {
                lookAccum += Time.deltaTime;
                if (lookAccum >= lookTime)
                {
                    StartCoroutine(VanishRoutine());
                    return;
                }
            }
            else if (continuousLook)
            {
                lookAccum = 0f; // รีเซ็ตถ้าหลุด
            }
        }
    }

    private IEnumerator VanishRoutine()
    {
        finished = true;
        if (animator && !string.IsNullOrEmpty(vanishTrigger)) animator.SetTrigger(vanishTrigger);
        if (loopSource) loopSource.Stop();
        if (vanishAnimExtraDelay > 0f) yield return new WaitForSeconds(vanishAnimExtraDelay);
        if (ghostRoot) ghostRoot.SetActive(false);
    }

    private Camera FindPlayerCamera()
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc)
        {
            var cam = pc.GetComponentInChildren<Camera>();
            if (cam) return cam;
        }
        return Camera.main;
    }
}
