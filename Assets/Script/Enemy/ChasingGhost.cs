using UnityEngine;
using UnityEngine.AI;
using ARKOM.Player;

namespace ARKOM.Enemy
{
    [AddComponentMenu("Enemy/Chasing Ghost AI")]
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChasingGhost : MonoBehaviour
    {
        public enum GhostState { Patrol, Search, Chase, Return }

        [Header("Refs")] public Transform target; // player
        public AudioSource sfxSource;
        [Tooltip("Animator ของโมเดลผี (ถ้าเว้นว่างจะหาในลูกหลาน)")] public Animator animator;

        [Header("Speed")] public float walkSpeed = 2.8f; public float runSpeed = 4.2f;

        [Header("Animation Params")]
        [Tooltip("โหมด 2D (ใช้พารามิเตอร์ X/Y) – เหมาะกับ BlendTree Idle/Walk/Strafe")] public bool use2DLocomotionParams = false;
        [Tooltip("ชื่อพารามิเตอร์แกน X (เช่น X หรือ Idle ถ้าตั้งตามโปรเจกต์)")] public string xParam2D = "X";
        [Tooltip("ชื่อพารามิเตอร์แกน Y (เช่น Y หรือ walk ถ้าตั้งตามโปรเจกต์)")] public string yParam2D = "Y";
        [Tooltip("ใช้พารามิเตอร์แบบ Float (เช่น Speed) เพื่อคุม BlendTree Idle/Walk")] public bool useFloatSpeedParam = true;
        [Tooltip("ชื่อพารามิเตอร์ Float สำหรับความเร็วเดิน/วิ่ง")] public string speedParam = "Speed";
        [Tooltip("ชื่อพารามิเตอร์ Bool ถ้าไม่ได้ใช้แบบ Float")] public string walkBoolParam = "IsWalking";
        [Tooltip("ความเร็วขั้นต่ำที่ถือว่าเริ่มเดิน (ใช้กับ Bool)")] public float walkStartThreshold = 0.1f;
        [Tooltip("คูณสเกลความเร็ว (ส่งเข้า Animator)")] public float speedAnimScale = 1.0f;

        [Header("Detect (Vision)")]
        [Tooltip("ระยะมองเห็นสูงสุด")] public float detectionRadius = 12f;
        [Tooltip("มุมมอง (องศา) ครึ่งหนึ่งของ FOV")] public float fovHalfAngle = 55f;
        [Tooltip("เลเยอร์ที่บังสายตา (กำแพง/เฟอร์นิเจอร์)")] public LayerMask losBlockMask = ~0;
        [Tooltip("เวลาที่ต้องเห็นต่อเนื่องเพื่อยืนยันการไล่")] public float confirmTime = 0.2f;

        [Header("Detect (Misc)")]
        [Tooltip("ระยะที่ได้ยินเสียง (ยังไม่เชื่อมระบบเสียงจริง: ใช้สำหรับทดสอบ)")] public float hearRadius = 0f;

        [Header("Chase/Leash")]
        [Tooltip("ระยะเริ่มไล่ (fallback ถ้าเข้าใกล้มากๆ)")] public float chaseStartDistance = 8f;
        [Tooltip("ระยะยอมแพ้ถ้าห่างจากฐานเกินค่านี้")] public float leashRadius = 25f;
        [Tooltip("เวลาที่มองไม่เห็นเป้าหมายจนจะยอมแพ้ (วินาที)")] public float lostTargetTimeout = 3.0f;
        [Tooltip("ระยะที่ถือว่าจับผู้เล่นได้")] public float catchDistance = 1.4f;
        [Tooltip("เช็คเส้นทางใหม่ทุกกี่วินาที เพื่อลดอาการหน่วง")] public float repathInterval = 0.25f;
        [Tooltip("สุ่ม offset จุดหมายเล็กน้อยให้การเคลื่อนที่ดูเป็นธรรมชาติ")] public float wanderOffset = 0.5f;

        [Header("Patrol")]
        public Transform homeAnchor; // ถ้าว่าง จะตั้งจากตำแหน่งเริ่มต้น
        public Transform[] patrolWaypoints;
        [Tooltip("วนลูปเวย์พอยต์")] public bool patrolLoop = true;
        [Tooltip("รอคอยย์พอยต์กี่วินาที")] public float patrolWaitAtPoint = 0.5f;
        [Tooltip("ระยะที่ถือว่าไปถึงเวย์พอยต์แล้ว")] public float waypointArriveDistance = 0.5f;

        private NavMeshAgent agent; private PlayerController player;
        private float lastRepath; private float seenAccum;
        private Vector3 lastKnownPos; private Vector3 spawnPos;
        private GhostState state;
        private int wpIndex; private float wpWaitTimer; private float lastSeenTime;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = true; agent.autoRepath = true; agent.stoppingDistance = 0f;
            if (!sfxSource) sfxSource = GetComponent<AudioSource>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            spawnPos = transform.position;
            if (!homeAnchor)
            {
                var go = new GameObject(name + "_Home");
                go.transform.position = spawnPos; go.transform.rotation = transform.rotation;
                homeAnchor = go.transform; // lightweight anchor
            }
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
            state = (patrolWaypoints != null && patrolWaypoints.Length > 0) ? GhostState.Patrol : GhostState.Return;
        }
        void Update()
        {
            if (!target || !agent || !agent.isOnNavMesh) return;

            switch (state)
            {
                case GhostState.Patrol: TickPatrol(); break;
                case GhostState.Search: TickSearch(); break;
                case GhostState.Chase:  TickChase();  break;
                case GhostState.Return: TickReturn(); break;
            }

            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            if (!animator || !agent) return;

            if (use2DLocomotionParams)
            {
                // ใช้ความเร็ว local (ขวา/หน้า) เพื่อขับ 2D BlendTree X/Y หรือ Idle/Walk
                Vector3 lv = transform.InverseTransformDirection(agent.velocity);
                float x = lv.x * speedAnimScale;
                float y = lv.z * speedAnimScale;
                if (!string.IsNullOrEmpty(xParam2D)) animator.SetFloat(xParam2D, x);
                if (!string.IsNullOrEmpty(yParam2D)) animator.SetFloat(yParam2D, y);
                return;
            }

            float spd = agent.velocity.magnitude * speedAnimScale;
            if (useFloatSpeedParam)
            {
                if (!string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, spd);
            }
            else
            {
                if (!string.IsNullOrEmpty(walkBoolParam)) animator.SetBool(walkBoolParam, spd > walkStartThreshold);
            }
        }

        private void TickPatrol()
        {
            // detect
            if (CheckDetect(out var pos, out bool confirmed))
            {
                lastKnownPos = pos; lastSeenTime = Time.time;
                if (confirmed || Vector3.Distance(transform.position, target.position) <= chaseStartDistance)
                { state = GhostState.Chase; SetSpeed(runSpeed); return; }
                else
                { state = GhostState.Search; SetSpeed(walkSpeed); MoveTo(lastKnownPos); return; }
            }

            // simple waypoint patrol
            if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            {
                // idle near home
                if (Vector3.Distance(transform.position, homeAnchor.position) > waypointArriveDistance)
                    MoveTo(homeAnchor.position);
                return;
            }

            var wp = patrolWaypoints[wpIndex];
            if (wp)
            {
                float dist = Vector3.Distance(transform.position, wp.position);
                if (dist <= waypointArriveDistance)
                {
                    wpWaitTimer += Time.deltaTime;
                    if (wpWaitTimer >= patrolWaitAtPoint)
                    {
                        wpWaitTimer = 0f;
                        wpIndex++;
                        if (wpIndex >= patrolWaypoints.Length)
                            wpIndex = patrolLoop ? 0 : patrolWaypoints.Length - 1;
                    }
                }
                else
                {
                    if (Time.time - lastRepath >= repathInterval)
                    {
                        lastRepath = Time.time;
                        MoveTo(wp.position);
                        SetSpeed(walkSpeed);
                    }
                }
            }
        }

        private void TickSearch()
        {
            if (CheckDetect(out var pos, out bool confirmed))
            {
                lastKnownPos = pos; lastSeenTime = Time.time;
                if (confirmed || Vector3.Distance(transform.position, target.position) <= chaseStartDistance)
                { state = GhostState.Chase; SetSpeed(runSpeed); return; }
            }

            // go to last known and wait briefly
            if (Time.time - lastRepath >= repathInterval)
            {
                lastRepath = Time.time;
                MoveTo(lastKnownPos);
            }

            // if close enough to last known and still no sight -> return
            if (Vector3.Distance(transform.position, lastKnownPos) <= waypointArriveDistance * 1.5f)
            {
                state = GhostState.Return; SetSpeed(walkSpeed);
            }
        }

        private void TickChase()
        {
            float distFromHome = Vector3.Distance(transform.position, homeAnchor.position);
            bool overLeash = leashRadius > 0f && distFromHome >= leashRadius;

            bool seeing = SeeTarget();
            if (seeing)
            {
                lastKnownPos = target.position; lastSeenTime = Time.time;
            }

            if (overLeash || (Time.time - lastSeenTime) >= lostTargetTimeout)
            {
                state = GhostState.Return; SetSpeed(walkSpeed); return;
            }

            if (Time.time - lastRepath >= repathInterval)
            {
                lastRepath = Time.time;
                Vector3 dest = target.position + Random.insideUnitSphere * wanderOffset; dest.y = target.position.y;
                MoveTo(dest);
                SetSpeed(runSpeed);
            }

            float d = Vector3.Distance(transform.position, target.position);
            if (d <= catchDistance)
                OnCatchPlayer();
        }

        private void TickReturn()
        {
            // if see target while returning, resume chase
            if (CheckDetect(out var pos, out bool confirmed))
            {
                lastKnownPos = pos; lastSeenTime = Time.time;
                state = confirmed ? GhostState.Chase : GhostState.Search;
                SetSpeed(confirmed ? runSpeed : walkSpeed);
                return;
            }

            if (Time.time - lastRepath >= repathInterval)
            {
                lastRepath = Time.time;
                MoveTo(homeAnchor.position);
                SetSpeed(walkSpeed);
            }

            if (Vector3.Distance(transform.position, homeAnchor.position) <= waypointArriveDistance * 1.2f)
            {
                state = (patrolWaypoints != null && patrolWaypoints.Length > 0) ? GhostState.Patrol : GhostState.Return;
            }
        }

        private bool CheckDetect(out Vector3 pos, out bool confirmed)
        {
            confirmed = false; pos = target ? target.position : transform.position;
            bool seen = SeeTarget();
            if (seen)
            {
                seenAccum += Time.deltaTime;
                if (seenAccum >= confirmTime) confirmed = true;
                return true;
            }
            else
            {
                seenAccum = 0f;
            }

            if (hearRadius > 0f && Vector3.Distance(transform.position, pos) <= hearRadius)
            {
                // simple hear stub -> not confirmed
                return true;
            }
            return false;
        }

        private bool SeeTarget()
        {
            if (!target) return false;
            Vector3 to = (target.position - transform.position);
            float dist = to.magnitude;
            if (dist > detectionRadius) return false;
            Vector3 dir = to / Mathf.Max(0.0001f, dist);
            float ang = Vector3.Angle(transform.forward, dir);
            if (ang > fovHalfAngle) return false;

            // line of sight
            if (Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, out var hit, dist + 0.1f, losBlockMask))
            {
                // blocked by something not the target
                if (hit.transform != target && !hit.transform.IsChildOf(target)) return false;
            }
            return true;
        }

        private void MoveTo(Vector3 dest)
        {
            if (!agent || !agent.isOnNavMesh) return;
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(dest, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
            }
        }

        private void SetSpeed(float s)
        {
            if (agent) agent.speed = s;
        }

        private void OnCatchPlayer()
        {
            agent.isStopped = true;
            Debug.Log("ChasingGhost: Caught player");
            // TODO: publish event / call game over
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = new Color(1,0,0,0.2f); UnityEditor.Handles.color = Gizmos.color;
            Vector3 fwd = transform.forward;
            UnityEditor.Handles.DrawSolidArc(transform.position + Vector3.up * 1.6f, Vector3.up, Quaternion.Euler(0,-fovHalfAngle,0) * fwd, fovHalfAngle*2f, 1.5f);
            if (homeAnchor)
            {
                Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(homeAnchor.position, 0.2f);
                if (leashRadius > 0f) { Gizmos.color = new Color(0,1,1,0.2f); Gizmos.DrawWireSphere(homeAnchor.position, leashRadius); }
            }
            if (patrolWaypoints != null)
            {
                Gizmos.color = Color.green;
                for (int i=0;i<patrolWaypoints.Length;i++)
                {
                    var p = patrolWaypoints[i]; if (!p) continue;
                    Gizmos.DrawWireSphere(p.position, 0.2f);
                    if (i+1 < patrolWaypoints.Length && patrolWaypoints[i+1])
                        Gizmos.DrawLine(p.position, patrolWaypoints[i+1].position);
                }
            }
        }
#endif
    }
}
