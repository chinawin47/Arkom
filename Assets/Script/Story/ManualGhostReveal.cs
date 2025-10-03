using UnityEngine;
using ARKOM.Core; // EventBus
using ARKOM.Story; // for GhostSpawnedEvent
using ARKOM.Player; // for player camera search
using System.Collections; // for IEnumerator

// �ҧʤ�Ի���麹 GameObject ���Ѵ��� (���ͺ� root ��) ���ǵ�駤�� ghostRoot ��������ż�㹩ҡ (�Դ����͹)
// ��������¡ Reveal() ��������� Publish AnomalyFirstSeenEvent (����� autoTriggerOnAnomaly) ���Դ�� ������§ ����� GhostSpawnedEvent
// �ҡ��� (����Դ lookDespawn) �������������ͧ�ú��˹����ǻԴ�ͧ
[AddComponentMenu("Story/Manual Ghost Reveal")] 
public class ManualGhostReveal : MonoBehaviour
{
    [Header("Ghost Reference")] 
    public GameObject ghostRoot;          // ���ż� (�ҧ㹩ҡ �Դ active �������)
    public bool disableOnStart = true;    // �Դ�ѹ�յ͹ Start

    [Header("Trigger Options")] 
    public bool autoTriggerOnAnomalyFound = true; // ��ԡ����������� AnomalyFirstSeenEvent (SequenceController �е�� flow �ͧ)
    public bool triggerOnce = true;               // �ʴ���������

    [Header("Audio")] 
    public AudioClip revealSfx;           // ���§�͹���
    public AudioClip loopSfx;             // ���§ǹ��͹ (optional)
    public bool attachAudioSource = true; // ��� true �����ҧ AudioSource �� ghostRoot ��� loop

    [Header("Look Despawn (Optional)")] 
    public bool lookDespawn = true;       // �Դ������Ͷ١��ͧ�ú���ҷ���˹�
    public float lookTime = 3f;           // ���ҵ�ͧ��ͧ (�Թҷ�) (<=0 = �ͧ������)
    public float lookAngle = 20f;         // ����������� ���ͧ�
    public bool continuousLook = true;    // ��ͧ��ͧ������ͧ

    [Header("Animation (Optional)")] 
    public Animator animator;
    public string appearTrigger = "Appear"; // Trigger �͹���
    public string vanishTrigger = "Vanish"; // Trigger �͹���
    public float vanishAnimExtraDelay = 0.5f; // ��͹�����͹�Դ

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

        // �� Event ��� SequenceController �Թ��� (᷹ GhostSpawner)
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
                lookAccum = 0f; // ���絶����ش
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
