using UnityEngine;
using UnityEngine.AI;
using ARKOM.Player;

namespace ARKOM.Enemy
{
    [AddComponentMenu("Enemy/Chasing Ghost AI")]
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChasingGhost : MonoBehaviour
    {
        [Header("Refs")] public Transform target; // player
        public AudioSource sfxSource;
        [Header("Speed")] public float walkSpeed = 2.8f; public float runSpeed = 4.2f;
        [Header("Behavior")] public float chaseStartDistance = 15f; public float giveUpDistance = 25f;
        [Tooltip("ระยะที่ถือว่าจับผู้เล่นได้")] public float catchDistance = 1.4f;
        [Tooltip("เช็คเส้นทางใหม่ทุกกี่วินาที เพื่อลดอาการหน่วง")] public float repathInterval = 0.25f;
        [Tooltip("สุ่ม offset จุดหมายเล็กน้อยให้การเคลื่อนที่ดูเป็นธรรมชาติ")] public float wanderOffset = 0.5f;

        private NavMeshAgent agent; private float lastRepath; private PlayerController player;
        private Vector3 lastKnownPos; private bool chasing;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = true; agent.autoRepath = true; agent.stoppingDistance = 0f;
            if (!sfxSource) sfxSource = GetComponent<AudioSource>();
        }
        void Start()
        {
            if (!target)
            {
                player = FindObjectOfType<PlayerController>();
                if (player) target = player.transform;
            }
            SetSpeed(walkSpeed);
            lastRepath = Time.time;
        }
        void Update()
        {
            if (!target) return;
            float d = Vector3.Distance(transform.position, target.position);

            // state: start/stop chase
            if (!chasing && d <= chaseStartDistance) chasing = true;
            if (chasing && d >= giveUpDistance) chasing = false;

            // repath at interval
            if (Time.time - lastRepath >= repathInterval)
            {
                lastRepath = Time.time;
                Vector3 dest = target.position + Random.insideUnitSphere * wanderOffset; dest.y = target.position.y;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(dest, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetPath(path);
                    SetSpeed(chasing ? runSpeed : walkSpeed);
                }
            }

            // catch check
            if (d <= catchDistance)
            {
                OnCatchPlayer();
            }
        }

        private void SetSpeed(float s)
        {
            if (agent) agent.speed = s;
        }

        private void OnCatchPlayer()
        {
            // TODO: Hook GameOver or scare animation
            // For now, just log and stop
            agent.isStopped = true;
            Debug.Log("ChasingGhost: Caught player");
        }
    }
}
