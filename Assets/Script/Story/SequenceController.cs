using System.Collections;
using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;
using ARKOM.UI; // added for TMP-based HintPresenter

namespace ARKOM.Story
{
    public class SequenceController : MonoBehaviour
    {
        public static SequenceController Instance { get; private set; } // singleton reference for gating

        [Header("References")]
        public PlayerController player;
        public SeatInteractable introSeat; // ที่นั่งเริ่ม
        public SeatInteractable reuseSeat; // เก้าอี้ตัวเดิมกลับมานั่งหลังเปิดไฟ (ถ้าไม่ตั้งใช้ introSeat)
        public FlashlightPickupInteractable flashlightPickup;
        public Interactable breakerInteractable; // จะใช้เป็นคัตเอ้าท์/เบรกเกอร์
        public TVController tv; // OPTIONAL: ตัวควบคุมทีวี (ยังไม่สร้าง ให้เว้นได้)
        public PowerManager powerManager; // OPTIONAL

        [Header("Config Timings")]
        public float introNewsDuration = 4f; // เล่นข่าวก่อนดับไฟ
        public float blackoutDelay = 0.5f;   // หน่วงก่อนตัดไฟหลัง intro
        public float timeSkipFadeOut = 1.2f;
        public float timeSkipBlackHold = 1.5f;
        public float timeSkipFadeIn = 1.2f;

        [Header("IDs / Seat")]
        public string introSeatId = "IntroSeat";

        [Header("UI / Hints (Optional external manager)")]
        public HintPresenter hint; // Optional helper ที่คุณอาจสร้าง

        [Header("Post TimeSkip References")] // new
        public GhostSpawner ghostSpawner; // optional
        public PraySequenceController prayController; // optional
        public HouseSweepManager sweepManager; // optional
        public FinalBedTrigger finalBedTrigger; // optional

        [Header("Auto Anomaly After Sweep")] // config
        public bool autoAnomalyAfterSweep = true;
        public float anomalyAutoDelay = 6f; // seconds to wait after SweepComplete before auto trigger
        public bool spawnGhostDirectOnAuto = true; // if false, just raise anomaly event

        [Header("Hint System")]
        public bool useProgressiveHints = true; // ถ้า true จะไม่โชว์ hint เริ่มต้น (ให้ระบบ ProgressiveHintController จัดการ)

        [Header("Flow Options")] // new
        public bool autoTriggerKitchenEntered = true; // ให้ KitchenEnteredEvent ออโต้ (ไม่ต้องมี trigger collider)

        [Header("Audio Loops")] // new persistent loop settings
        [Tooltip("เสียง loop สำหรับช่วง CleanPlates (จะเล่นต่อไปจนกว่าจะหยุดด้วยโค้ด)")] public AudioClip cleanPlatesLoopClip;
        [Range(0f,1f)] public float cleanPlatesLoopVolume = 0.7f;
        [Tooltip("เล่นเฉพาะครั้งแรก (ไม่เริ่มซ้ำถ้า state เข้า CleanPlates อีก)")] public bool cleanPlatesLoopPlayOnce = true;
        private AudioSource persistentLoopSource; // internal audio source
        private bool cleanPlatesLoopStarted;

        [Header("Global Game Loop Audio")] // NEW global loop across whole game
        [Tooltip("คลิปเสียงที่เล่นตั้งแต่เริ่มเกมจนถึง SleepEnd")] public AudioClip globalLoopClip;
        [Range(0f,1f)] public float globalLoopVolume = 1f;
        [Tooltip("เริ่มเล่น global loop ตอน Awake")] public bool startGlobalLoopOnAwake = true;
        [Tooltip("ถ้าไม่เล่นตอน Awake ให้เริ่มเมื่อเข้า IntroSeated")] public bool startGlobalLoopOnIntro = true;
        [Tooltip("หยุดทุกเสียง + mute ตอนเข้าสู่ SleepEnd")] public bool stopAllAudioAtSleepEnd = true;
        private AudioSource globalLoopSource; // internal audio source
        private bool globalLoopStarted;

        // internal flags
        private bool sweepDone;
        private bool anomalySeen;
        private bool ghostSpawned;
        private bool plateCrashTriggered; // prevent duplicate plate crash
        private bool upstairsFusePicked;  // picked upstairs fuse
        private bool stairsGhostSpawned;  // ghost at stairs already spawned
        private bool platesCleaned;       // finished cleaning plates
        private bool requireCleanPlatesBeforeFuse; // gate fuse insertion until cleaned
        private bool storageFuseTriggered; // ensure TV scare runs once
        [Header("TV Scare (Storage Fuse)")]
        [Tooltip("เวลาถือ Static บนทีวีก่อนปิด (วินาที)")] public float tvStaticHoldTime = 4f;

        // Public accessors for other scripts (e.g., Breaker)
        public bool RequireCleanPlatesBeforeFuse => requireCleanPlatesBeforeFuse;
        public bool PlatesCleaned => platesCleaned;
        [Tooltip("ข้อความเตือนเมื่อยังต้องเก็บเศษจานก่อนใส่ฟิวส์")] public string needCleanPlatesHint = "เก็บเศษจานในครัวให้หมดก่อน";

        [Header("Find Ooy Flow")] // NEW
        [Tooltip("ข้อความชี้เป้าให้ไปหาออยหลังไฟติด")] public string findOoyHint = "ไปหาออย";
        [Tooltip("เสียงบรรยายเมื่อหาไม่พบออย")] public AudioClip ooyNotFoundVoice;
        [Range(0f,1f)] public float ooyNotFoundVoiceVolume = 1f;
        [Tooltip("ใช้ข้อความแทนเสียง ถ้าไม่มีคลิป")] public string ooyNotFoundText = "นี่มันเกิดอะไรขึ้นวะ ลองไปปลุกออยหน่อยดีกว่า";
        [Tooltip("หน่วงก่อนตัดไฟอีกครั้งหลังจบบรรยาย (วินาที)")] public float blackoutAgainDelay = 0.25f;

        private StoryState state;
        private bool started;

        private Coroutine autoAnomalyCo;

        [Header("Sleep End Options")] // new
        [Tooltip("เข้าสู่ SleepEnd แล้วให้จอดำเลย")] public bool blackScreenOnSleepEnd = true;
        [Tooltip("เวลาค่อยๆ fade ไปดำ")] public float sleepEndFadeTime = 1f;
        [Tooltip("แสดง hint ตอนจอดำ (เว้นว่างถ้าไม่ต้องการ)")] public string sleepEndHintText = "";
        [Header("Sleep End Display")] // NEW
        [Tooltip("ข้อความใหญ่แสดงบนจอดำ (เช่น TO BE CONTINUED)")] public string sleepEndDisplayText = "TO BE CONTINUED";
        [Tooltip("ดีเลย์ก่อนโชว์ข้อความหลังเข้าสู่ SleepEnd")] public float sleepEndTextDelay = 0.6f;
        [Tooltip("ใช้ HintPresenter เดิมแshowข้อความสุดท้าย")] public bool useHintPresenterForSleepEndText = true;
        [Tooltip("ระยะเวลาค้างข้อความสุดท้าย")] public float sleepEndTextDuration = 9999f;
        [Header("Sleep End Background Audio")] public AudioClip sleepEndBackgroundClip;
        [Range(0f,1f)] public float sleepEndBackgroundVolume = 1f;
        [Tooltip("ให้ loop เสียง SleepEnd")] public bool sleepEndBackgroundLoop = true;
        private AudioSource sleepEndBgSource;

        [Header("Debug / Dev")] // NEW
        [Tooltip("ข้ามทุกอย่างไปฉากจบทันทีตอน Start")] public bool debugSkipToSleepEndOnStart = false;
        [Tooltip("กดปุ่มนี้เพื่อข้ามไป SleepEnd ระหว่างเล่น (0 = ปิด)")] public KeyCode debugSkipKey = KeyCode.F9;

        public enum StoryState
        {
            IntroSeated,
            FindFlashlight,
            RestorePower,
            ReturnToSeat,
            TimeSkipCutscene,
            Finished,
            // Post TimeSkip placeholders
            PlateCrashStart,
            InvestigateKitchen,
            CleanPlates,
            FridgeSequence,
            CheckOoy,
            HouseSweep,
            AnomalyFound,
            GhostSpawn,
            RunToBed,
            PraySequence,
            SleepEnd,
            FindOoy // simplified path after power restored
        }

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning("Multiple SequenceController detected, destroying duplicate", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // create persistent audio source child
            var loopGo = new GameObject("PersistentLoopAudio");
            loopGo.transform.SetParent(transform);
            persistentLoopSource = loopGo.AddComponent<AudioSource>();
            persistentLoopSource.loop = true;
            persistentLoopSource.playOnAwake = false;
            persistentLoopSource.spatialBlend = 0f; // 2D
            persistentLoopSource.volume = cleanPlatesLoopVolume;

            // prepare global loop source (always create if clip assigned)
            if (globalLoopClip)
            {
                var globalGo = new GameObject("GlobalLoopAudio");
                globalGo.transform.SetParent(transform);
                globalLoopSource = globalGo.AddComponent<AudioSource>();
                globalLoopSource.loop = true;
                globalLoopSource.playOnAwake = false;
                globalLoopSource.spatialBlend = 0f;
                globalLoopSource.clip = globalLoopClip;
                globalLoopSource.volume = globalLoopVolume;
                if (startGlobalLoopOnAwake)
                {
                    globalLoopSource.Play();
                    globalLoopStarted = true;
                }
            }
        }

        void Start()
        {
            if (!player) player = FindObjectOfType<PlayerController>();
            if (!introSeat) introSeat = FindObjectOfType<SeatInteractable>();
            if (!reuseSeat) reuseSeat = introSeat;

            if (debugSkipToSleepEndOnStart)
            {
                // สร้างขั้นต่ำให้พร้อม แล้วเข้าฉากจบเลย
                SetupInitial();
                EnterSleepEnd();
                return;
            }

            SetupInitial();
        }

        void OnEnable()
        {
            EventBus.Subscribe<FlashlightAcquiredEvent>(OnFlashlight);
            EventBus.Subscribe<PowerRestoredEvent>(OnPowerRestored);
            EventBus.Subscribe<PlayerSeatedEvent>(OnPlayerSeated);
            EventBus.Subscribe<TimeSkipFinishedEvent>(OnTimeSkipFinished);
            EventBus.Subscribe<KitchenEnteredEvent>(OnKitchenEntered);
            EventBus.Subscribe<PlatesCleanedEvent>(OnPlatesCleaned);
            // Remove unrelated older flow subscriptions to simplify
            // EventBus.Subscribe<FridgeScareDoneEvent>(OnFridgeScareDone);
            // EventBus.Subscribe<OoyCheckedEvent>(OnOoyChecked);
            // EventBus.Subscribe<SweepCompleteEvent>(OnSweepComplete);
            // EventBus.Subscribe<AnomalyFirstSeenEvent>(OnAnomalyFirstSeen);
            // EventBus.Subscribe<GhostSpawnedEvent>(OnGhostSpawned);
            // EventBus.Subscribe<PlayerInBedEvent>(OnPlayerInBed);
            // EventBus.Subscribe<PrayerFinishedEvent>(OnPrayerFinished);
            EventBus.Subscribe<FuseFoundEvent>(OnFuseFound);
            EventBus.Subscribe<OoyCheckedEvent>(OnOoyChecked); // keep for new flow
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<FlashlightAcquiredEvent>(OnFlashlight);
            EventBus.Unsubscribe<PowerRestoredEvent>(OnPowerRestored);
            EventBus.Unsubscribe<PlayerSeatedEvent>(OnPlayerSeated);
            EventBus.Unsubscribe<TimeSkipFinishedEvent>(OnTimeSkipFinished);
            EventBus.Unsubscribe<KitchenEnteredEvent>(OnKitchenEntered);
            EventBus.Unsubscribe<PlatesCleanedEvent>(OnPlatesCleaned);
            // Removed older flow unsubs
            // EventBus.Unsubscribe<FridgeScareDoneEvent>(OnFridgeScareDone);
            // EventBus.Unsubscribe<OoyCheckedEvent>(OnOoyChecked);
            // EventBus.Unsubscribe<SweepCompleteEvent>(OnSweepComplete);
            // EventBus.Unsubscribe<AnomalyFirstSeenEvent>(OnAnomalyFirstSeen);
            // EventBus.Unsubscribe<GhostSpawnedEvent>(OnGhostSpawned);
            // EventBus.Unsubscribe<PlayerInBedEvent>(OnPlayerInBed);
            // EventBus.Unsubscribe<PrayerFinishedEvent>(OnPrayerFinished);
            EventBus.Unsubscribe<FuseFoundEvent>(OnFuseFound);
            EventBus.Unsubscribe<OoyCheckedEvent>(OnOoyChecked);
        }

        private void SetState(StoryState newState)
        {
            if (state == newState) return;
            var prev = state;
            state = newState;
            StoryDebug.Log($"State -> {newState}", this);
            EventBus.Publish(new StoryStateChangedEvent(prev, newState));
        }

        private void SetupInitial()
        {
            if (started) return;
            started = true;
            state = StoryState.IntroSeated;
            StoryDebug.Log("State -> IntroSeated", this);
            EventBus.Publish(new StoryStateChangedEvent(StoryState.IntroSeated, StoryState.IntroSeated));

            // start global loop here if configured to begin at intro instead of Awake
            if (!globalLoopStarted && globalLoopSource && !startGlobalLoopOnAwake && startGlobalLoopOnIntro)
            {
                globalLoopSource.volume = globalLoopVolume;
                globalLoopSource.Play();
                globalLoopStarted = true;
            }

            // บังคับให้นั่ง (ถ้า seatInteractable เรียก EnterSeat)
            if (introSeat && player && !player.IsSeated)
                player.EnterSeat(introSeat.seatAnchor, introSeat.cameraPoint);

            // ปิด interact อื่น ๆ
            if (flashlightPickup) flashlightPickup.gameObject.SetActive(false); // จะเปิดตอนหาไฟฉาย
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(false);

            // เริ่มเล่นข่าว
            if (tv) tv.PlayIntro();
            ShowHint("ชมข่าว...", introNewsDuration);
            if (introAutoCamera) StartCoroutine(IntroCameraFocusRoutine());
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(introNewsDuration);
            yield return new WaitForSeconds(blackoutDelay);
            TriggerBlackout();
        }

        private void TriggerBlackout()
        {
            StoryDebug.Log("TriggerBlackout", this);
            if (tv) tv.PowerOff();
            if (powerManager) powerManager.SetPower(false);
            EventBus.Publish(new BlackoutStartedEvent());
            if (player && player.IsSeated) player.ExitSeat();
            if (flashlightPickup) flashlightPickup.gameObject.SetActive(true);
            SetState(StoryState.FindFlashlight);
            if(!useProgressiveHints) ShowHint("ไฟดับ... หาไฟฉายก่อน", 4f);
        }

        private void OnFlashlight(FlashlightAcquiredEvent _)
        {
            if (state != StoryState.FindFlashlight) return;
            StoryDebug.Log("Flashlight acquired", this);
            SetState(StoryState.RestorePower);
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
            if(!useProgressiveHints) ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์", 4f);
        }

        private void OnPowerRestored(PowerRestoredEvent _)
        {
            if (state != StoryState.RestorePower) return;
            StoryDebug.Log("Power restored", this);

            // เปิดไฟบ้าน
            if (powerManager) powerManager.SetPower(true);
            if (tv) tv.PreparePostRestoreNews();

            // เปลี่ยน flow: ให้ผู้เล่นไปหาออย (แทนการกลับไปนั่ง/ตัดฉาก)
            SetState(StoryState.FindOoy);
            if (!string.IsNullOrEmpty(findOoyHint)) ShowHint(findOoyHint, 4f);

            // ไม่ใช้ reuseSeat / ReturnToSeat / TimeSkip อีกใน flow นี้
            if (reuseSeat) reuseSeat.gameObject.SetActive(false);
        }

        private void OnPlayerSeated(PlayerSeatedEvent e)
        {
            if (state != StoryState.ReturnToSeat) return;
            if (e.SeatId != introSeatId) return;
            StoryDebug.Log("Player seated at intro seat", this);
            StartCoroutine(TimeSkipRoutine());
        }

        private IEnumerator TimeSkipRoutine()
        {
            SetState(StoryState.TimeSkipCutscene);
            LockPlayer(true);
            if (hint) hint.HideImmediate();
            var fader = FindObjectOfType<ScreenFader>();
            if (fader) yield return fader.FadeOut(timeSkipFadeOut);
            if (tv) tv.PlayTimeSkipNews();
            yield return new WaitForSeconds(timeSkipBlackHold);
            if (fader) yield return fader.FadeIn(timeSkipFadeIn);
            LockPlayer(false);
            SetState(StoryState.Finished);
            EventBus.Publish(new TimeSkipFinishedEvent());
            ShowHint("กด F เพื่อลุก", 3f);
        }

        private void LockPlayer(bool locked)
        {
            if (!player) return;
            if (locked)
            {
                // ปิด input map
                player.enabled = false; // หรือใช้ flag ภายใน ถ้าอยากให้ Update บางอย่างยังทำงาน
            }
            else
            {
                player.enabled = true;
            }
        }

        private void ShowHint(string text, float duration)
        {
            if (hint) hint.Show(text, duration);
        }

        private void OnTimeSkipFinished(TimeSkipFinishedEvent _)
        {
            if (state != StoryState.Finished) return;
            StoryDebug.Log("TimeSkipFinishedEvent received", this);
            StartCoroutine(PlateCrashSequence());
        }

        private IEnumerator PlateCrashSequence()
        {
            SetState(StoryState.PlateCrashStart);
            ShowHint("เกิดเสียงดังจากครัว...", 3f);
            yield return new WaitForSeconds(2f);
            SetState(StoryState.InvestigateKitchen);
            ShowHint("ไปดูที่ครัว", 4f);
            if (autoTriggerKitchenEntered)
            {
                StoryDebug.Log("Auto publish KitchenEnteredEvent", this);
                EventBus.Publish(new KitchenEnteredEvent());
            }
        }

        private void OnKitchenEntered(KitchenEnteredEvent _)
        {
            if (state != StoryState.InvestigateKitchen) return;
            StoryDebug.Log("KitchenEnteredEvent", this);
            SetState(StoryState.CleanPlates);
            var shards = FindObjectsOfType<PlateShardPickup>();
            PlateShardPickup.ResetCounter(shards.Length);
            foreach (var s in shards)
            {
                if (!s) continue;
                s.RevealForCleanPlates();
            }
            // start persistent loop for CleanPlates
            if (cleanPlatesLoopClip && persistentLoopSource && (!cleanPlatesLoopPlayOnce || !cleanPlatesLoopStarted))
            {
                persistentLoopSource.clip = cleanPlatesLoopClip;
                persistentLoopSource.volume = cleanPlatesLoopVolume;
                persistentLoopSource.Play();
                cleanPlatesLoopStarted = true;
            }
            if(!useProgressiveHints) ShowHint("เก็บเศษจานให้หมด", 4f);
        }

        private void OnPlatesCleaned(PlatesCleanedEvent e)
        {
            if (state != StoryState.CleanPlates) return;
            StoryDebug.Log("PlatesCleanedEvent (Total=" + e.Total + ")", this);
            platesCleaned = true;
            // allow fuse insertion now; guide player to breaker
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
            SetState(StoryState.RestorePower);
            ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์", 4f);
        }

        // Public helper for hint
        public void ShowTempHint(string text, float duration = 2f) => ShowHint(text, duration);

        private void OnFridgeScareDone(FridgeScareDoneEvent _)
        {
            if (state != StoryState.FridgeSequence) return;
            StoryDebug.Log("FridgeScareDoneEvent", this);
            SetState(StoryState.CheckOoy);
            if(!useProgressiveHints) ShowHint("ไปดูออย", 4f);
        }

        private void OnOoyChecked(OoyCheckedEvent _)
        {
            // รองรับทั้ง flow เก่า (CheckOoy) และ flow ใหม่ (FindOoy)
            if (state != StoryState.CheckOoy && state != StoryState.FindOoy) return;
            StoryDebug.Log("OoyCheckedEvent", this);

            // เล่นเสียง/โชว์ข้อความว่าไม่เจอ และเตรียมตัดไฟอีกครั้ง
            if (ooyNotFoundVoice)
                AudioSource.PlayClipAtPoint(ooyNotFoundVoice, player ? player.transform.position : transform.position, ooyNotFoundVoiceVolume);
            else if (!string.IsNullOrEmpty(ooyNotFoundText))
                ShowHint(ooyNotFoundText, 3.5f);

            StartCoroutine(SecondBlackoutRoutine());
        }

        private IEnumerator SecondBlackoutRoutine()
        {
            // รอความยาวเสียงคร่าวๆ ถ้ามี
            float wait = 0f;
            if (ooyNotFoundVoice)
                wait = Mathf.Max(wait, ooyNotFoundVoice.length);
            if (blackoutAgainDelay > 0f)
                wait += blackoutAgainDelay;
            if (wait > 0f) yield return new WaitForSeconds(wait);

            // ดับไฟอีกครั้ง
            if (tv) tv.PowerOff();
            if (powerManager) powerManager.SetPower(false);
            EventBus.Publish(new BlackoutStartedEvent());
            StoryDebug.Log("Second blackout triggered after Ooy not found", this);

            // ถ้าต้องการไป flow ต่อไปค่อยเติมที่นี่
        }

        private void EnterSleepEnd()
        {
            StoryDebug.Log("EnterSleepEnd (debug=" + debugSkipToSleepEndOnStart + ")", this);
            SetState(StoryState.SleepEnd);
            ShowHint("...",1f);
            if (!string.IsNullOrEmpty(sleepEndHintText)) ShowHint(sleepEndHintText,1f);
            if (blackScreenOnSleepEnd)
            {
                var fader = FindObjectOfType<ScreenFader>();
                if (fader) StartCoroutine(fader.FadeOut(Mathf.Max(0.01f, sleepEndFadeTime)));
                LockPlayer(true);
            }
            if (stopAllAudioAtSleepEnd)
            {
                if (globalLoopSource) globalLoopSource.Stop();
                if (persistentLoopSource) persistentLoopSource.Stop();
                if (!sleepEndBackgroundClip)
                    AudioListener.pause = true;
            }
            if (sleepEndBackgroundClip)
            {
                if (!sleepEndBgSource)
                {
                    var bg = new GameObject("SleepEndBackgroundAudio");
                    bg.transform.SetParent(transform);
                    sleepEndBgSource = bg.AddComponent<AudioSource>();
                    sleepEndBgSource.loop = sleepEndBackgroundLoop;
                    sleepEndBgSource.playOnAwake = false;
                    sleepEndBgSource.spatialBlend = 0f;
                }
                sleepEndBgSource.clip = sleepEndBackgroundClip;
                sleepEndBgSource.volume = sleepEndBackgroundVolume;
                sleepEndBgSource.Play();
            }
            StartCoroutine(SleepEndTextRoutine());
        }

        private IEnumerator SleepEndTextRoutine()
        {
            if (!useHintPresenterForSleepEndText) yield break;
            if (string.IsNullOrEmpty(sleepEndDisplayText)) yield break;
            if (sleepEndTextDelay > 0f) yield return new WaitForSeconds(sleepEndTextDelay);
            ShowHint(sleepEndDisplayText, sleepEndTextDuration);
        }

        public StoryState CurrentState => state; // public read-only accessor

        void Update()
        {
            if (debugSkipKey != KeyCode.None && Input.GetKeyDown(debugSkipKey))
            {
                if (state != StoryState.SleepEnd)
                {
                    EnterSleepEnd();
                }
            }
        }

        [Header("Intro Camera Focus")] // NEW
        [Tooltip("ระหว่างนั่งช่วง Intro ให้กล้องหันไปที่ TV และซูมเข้าออกอัตโนมัติ")] public bool introAutoCamera = true;
        [Tooltip("จุดที่ให้มองระหว่าง Intro (ว่าง = ใช้ Transform ของ TV)")] public Transform tvLookTarget;
        [Tooltip("เวลาหันกล้องไปหา TV")] public float introLookTime = 0.8f;
        [Tooltip("FOV ตอนซูมเข้า (ค่าต่ำ = ซูมมาก)")] public float introZoomFov = 35f;
        [Tooltip("เวลาในการซูมเข้า")] public float introZoomInTime = 0.8f;
        [Tooltip("ค้างซูมไว้กี่วินาที") ] public float introZoomHoldTime = 1.0f;
        [Tooltip("เวลาในการซูมออกกลับค่าปกติ")] public float introZoomOutTime = 0.8f;
        [Tooltip("ล็อก PlayerController ชั่วคราวระหว่างหมุนและซูมกล้อง Intro")] public bool introLockPlayerDuringCamera = true;

        private IEnumerator IntroCameraFocusRoutine()
        {
            if (!player) yield break;
            var cam = player.mainCamera ? player.mainCamera : Camera.main;
            if (!cam) yield break;

            Transform focus = tvLookTarget ? tvLookTarget : (tv ? tv.transform : null);
            if (!focus) yield break;

            if (introLockPlayerDuringCamera) LockPlayer(true);

            float startFov = cam.fieldOfView;
            Quaternion startRot = player.cameraRoot ? player.cameraRoot.rotation : cam.transform.rotation;

            // compute target rotation towards focus using current camera position
            Vector3 dirNow = (focus.position - cam.transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dirNow, Vector3.up);

            // 1) Ease rotate to TV
            if (introLookTime > 0f)
            {
                float t = 0f;
                while (t < introLookTime)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / introLookTime));
                    if (player.cameraRoot)
                        player.cameraRoot.rotation = Quaternion.Slerp(startRot, targetRot, k);
                    else
                        cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
                    yield return null;
                }
            }
            else
            {
                if (player.cameraRoot) player.cameraRoot.rotation = targetRot; else cam.transform.rotation = targetRot;
            }

            // update camera rig yaw/pitch to avoid snap-back when sequence ends
            if (player && player.cameraRoot)
            {
                Vector3 fwd = player.cameraRoot.forward;
                float yaw = Quaternion.LookRotation(new Vector3(fwd.x, 0f, fwd.z)).eulerAngles.y;
                float pitch = Quaternion.LookRotation(fwd).eulerAngles.x;
                // convert pitch from 0..360 to -180..180 then clamp
                if (pitch > 180f) pitch -= 360f;
                player.ForceLookYawPitch(yaw, pitch);
            }

            // 2) Ease zoom in
            if (introZoomInTime > 0f)
            {
                float t = 0f;
                while (t < introZoomInTime)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / introZoomInTime));
                    cam.fieldOfView = Mathf.Lerp(startFov, introZoomFov, k);
                    yield return null;
                }
            }
            else cam.fieldOfView = introZoomFov;

            if (introZoomHoldTime > 0f)
                yield return new WaitForSeconds(introZoomHoldTime);

            // 3) Ease zoom out
            if (introZoomOutTime > 0f)
            {
                float t = 0f;
                while (t < introZoomOutTime)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / introZoomOutTime));
                    cam.fieldOfView = Mathf.Lerp(introZoomFov, startFov, k);
                    yield return null;
                }
            }
            else cam.fieldOfView = startFov;

            if (introLockPlayerDuringCamera) LockPlayer(false);
        }

        // Handle fuse pickups to branch flows
        private void OnFuseFound(FuseFoundEvent e)
        {
            if (e.Location == FuseLocation.Upstairs)
            {
                if (plateCrashTriggered) return;
                plateCrashTriggered = true;
                upstairsFusePicked = true;
                requireCleanPlatesBeforeFuse = true;
                StoryDebug.Log("FuseFound (Upstairs) -> PlateCrashSequence", this);
                StartCoroutine(PlateCrashSequence());
                return;
            }

            if (e.Location == FuseLocation.StorageRoom)
            {
                if (storageFuseTriggered) return;
                storageFuseTriggered = true;
                StoryDebug.Log("FuseFound (StorageRoom) -> TV static scare", this);
                // Play TV static
                if (tv) { tv.PlayStatic(); StartCoroutine(StopTvStaticAfterDelay(tvStaticHoldTime)); }
                // Spawn ghost at TV area (assume index 1)
                if (ghostSpawner) ghostSpawner.SpawnAtIndex(1);
                // Optional hint
                ShowHint("เสียงทีวีซ่า...", 2.5f);
                return;
            }
        }

        private IEnumerator StopTvStaticAfterDelay(float delay)
        {
            if (delay <= 0f) { if (tv) tv.StopStatic(); yield break; }
            yield return new WaitForSeconds(delay);
            if (tv) tv.StopStatic();
        }
    }

    // Placeholder Power Manager interface (คุณจะสร้างจริงแยกไฟล์ก็ได้)
    public class PowerManager : MonoBehaviour
    {
        public Light[] normalLights;
        public Light[] emergencyLights;
        public void SetPower(bool on)
        {
            foreach (var l in normalLights) if (l) l.enabled = on;
            foreach (var l in emergencyLights) if (l) l.enabled = !on; // ไฟฉุกเฉินกลับด้าน
        }
    }

    public class ScreenFader : MonoBehaviour
    {
        public CanvasGroup group;
        public IEnumerator FadeOut(float t)
        {
            if (!group) yield break;
            float time = 0f;
            while (time < t)
            {
                time += Time.deltaTime;
                group.alpha = Mathf.Clamp01(time / t);
                yield return null;
            }
        }
        public IEnumerator FadeIn(float t)
        {
            if (!group) yield break;
            float time = 0f;
            while (time < t)
            {
                time += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(time / t);
                yield return null;
            }
        }
    }
}
