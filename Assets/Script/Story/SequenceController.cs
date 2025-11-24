using System.Collections;
using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;
using ARKOM.UI;
using ARKOM.Enemy;
using UnityEngine.SceneManagement; // load scenes at end
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ARKOM.Story
{
    [AddComponentMenu("Story/Sequence Controller")]
    public class SequenceController : MonoBehaviour
    {


        public static SequenceController Instance { get; private set; }

        [Header("References")]
        public PlayerController player;
        public SeatInteractable introSeat;
        public SeatInteractable reuseSeat;
        public FlashlightPickupInteractable flashlightPickup;
        public Interactable breakerInteractable;
        public TVController tv; // optional
        public PowerManager powerManager; // optional

        [Header("Checkpoint Spawns (Second Blackout)")]
        [Tooltip("จุดเกิดของผู้เล่นเมื่อรีเซ็ตไปยังด่านเช็คไฟรอบ2 (หน้าตู้ไฟ)")] public Transform breakerSpawnPoint;
        [Tooltip("จุดเกิดของผี (ถ้าต้องการกำหนดใหม่ตอนรีเซ็ต)")] public Transform ghostSpawnPoint;

        [Header("Config Timings")]
        public float introNewsDuration = 4f;
        public float blackoutDelay = 0.5f;
        public float timeSkipFadeOut = 1.2f;
        public float timeSkipBlackHold = 1.5f;
        public float timeSkipFadeIn = 1.2f;

        [Header("IDs / Seat")]
        public string introSeatId = "IntroSeat";

        [Header("UI / Hints")] public HintPresenter hint;

        [Header("Post TimeSkip References")]
        public GhostSpawner ghostSpawner; // optional
        public PraySequenceController prayController; // optional
        public HouseSweepManager sweepManager; // optional
        public FinalBedTrigger finalBedTrigger; // optional

        [Header("Chase (Hierarchy Option A)")]
        [Tooltip("ถ้ามี ให้ลาก ChasingGhost ที่วางไว้ในซีนเข้ามา จะใช้ตัวนี้แทนการ spawn")] public ChasingGhost sceneChasingGhost;
        [Tooltip("ปิดผีไล่เมื่อเปิดกล่อง4 หลักสำเร็จ")] public bool deactivateChaseGhostOnBoxOpen = true;

        [Header("Auto Anomaly After Sweep")] public bool autoAnomalyAfterSweep = true; public float anomalyAutoDelay = 6f; public bool spawnGhostDirectOnAuto = true;

        [Header("Hint System")] public bool useProgressiveHints = true;
        [Header("Flow Options")] public bool autoTriggerKitchenEntered = true;

        [Header("Global Game Loop Audio")] public AudioClip globalLoopClip; [Range(0f, 1f)] public float globalLoopVolume = 1f; public bool startGlobalLoopOnAwake = true; public bool startGlobalLoopOnIntro = true; public bool stopAllAudioAtSleepEnd = true;
        private AudioSource globalLoopSource; private bool globalLoopStarted;

        // Internal flags
        private bool platesCleaned; private bool requireCleanPlatesBeforeFuse; private bool storageFuseTriggered; private bool started;
        private bool secondBlackoutStarted;

        [Header("TV Scare (Storage Fuse)")]
        [Tooltip("เวลาถือ Static บนทีวีก่อนปิด (วินาที)")] public float tvStaticHoldTime = 4f;

        // Public accessors for other scripts
        public bool RequireCleanPlatesBeforeFuse => requireCleanPlatesBeforeFuse;
        public bool PlatesCleaned => platesCleaned;
        [Tooltip("ข้อความเตือนเมื่อยังต้องเก็บเศษจานก่อนใส่ฟิวส์")] public string needCleanPlatesHint = "เก็บเศษจานในครัวให้หมดก่อน";

        [Header("Find Ooy Flow")]
        public string findOoyHint = "ไปหาออย";
        public AudioClip ooyNotFoundVoice; [Range(0f, 1f)] public float ooyNotFoundVoiceVolume = 1f;
        public string ooyNotFoundText = "นี่มันเกิดอะไรขึ้นวะ ลองไปปลุกออยหน่อยดีกว่า";
        public float blackoutAgainDelay = 0.25f;
        [Tooltip("จำกัดเวลารอสูงสุดจากเสียง ooyNotFoundVoice ก่อนดับไฟรอบสอง (วินาที)")]
        public float maxVoiceWaitSeconds = 3f;

        [Header("Upstairs Unlock Flow")]
        public string pliersToolId = "Pliers"; public string needPliersHint = "ตามหาคีมเพื่อปลดโซ่"; // original hint
        [Tooltip("ข้อความแทน needPliersHint เมื่อต้องการบอกให้ผู้เล่นใส่ฟิวส์ให้ครบก่อนขึ้นชั้นบน")]
        public string restorePowerBlockHint = "ยังใส่ฟิวส์ไม่ครบ ลงไปใส่ให้ครบก่อน";
        private string originalNeedPliersHint; // backup
        [Tooltip("เสียงเท้าบนชั้นบนเมื่อ RestorePower สำเร็จ เพื่อดึงความสนใจขึ้นไป")] public AudioClip upstairsFootstepVoice; [Range(0f,1f)] public float upstairsFootstepVoiceVolume = 1f;
        public string objectiveFindNoiseSource = "ตามหาต้นตอของเสียง";
        public Transform upstairsDoor; public string needUpstairsKeyHint = "ตามหาวิทยุเพื่อปิด";
        public Transform prayerRoom; public string goTurnOffRadioHint = "เสียงดังมาจากวิทยุ"; public string readDiaryHint = "มีไดอารี่อของออย ลองอ่านดู";

        [Header("Upstairs Objects (Optional)")]
        [Tooltip("RadioInteractable ที่อยู่ในห้องพระ เพื่อสั่งเล่นอัตโนมัติหลังขึ้นชั้นสองได้")] public RadioInteractable upstairsRadio;
        [Tooltip("Collider ของไดอารี่อย่างบังคับให้อ่านก่อนอนุญาตให้ไปหาออย")] public Collider diaryInteractCollider;
        [Tooltip("ต้องอ่านไดอารี่ก่อนหรือไม่ (ถ้ามี collider)")] public bool requireDiaryBeforeOoy = false;
        private bool diaryRead;

        [Header("Post-Second Blackout Flow")]
        [Tooltip("ฮินต์หลังดับไฟรอบสองให้ไปเช็คคัตเอ้าท์")] public string checkBreakerHint = "ไปตรวจคัตเอ้าท์";
        [Tooltip("ฮินต์เริ่มหาโน้ต3 แผ่น")] public string findNotesHint = "อ่านโน้ต หาเลข 4 หลัก เพื่อเปิดกล่อง"; // ปรับข้อความตามคำขอ
        [Tooltip("ฮินต์ไปที่กล่อง4 หลักหลังหาโน้ตครบ")] public string openBoxHint = "หาและเปิดกล่อง4 หลัก";

        [Header("Sleep End Options")] public bool blackScreenOnSleepEnd = true; public float sleepEndFadeTime = 1f; public string sleepEndHintText = "";
        [Header("Sleep End Display")] public string sleepEndDisplayText = "TO BE CONTINUED"; public float sleepEndTextDelay = 0.6f; public bool useHintPresenterForSleepEndText = true; public float sleepEndTextDuration = 9999f; public AudioClip sleepEndBackgroundClip; [Range(0f, 1f)] public float sleepEndBackgroundVolume = 1f; public bool sleepEndBackgroundLoop = true; private AudioSource sleepEndBgSource;

        [Header("End Scene Transition")] [Tooltip("เมื่อเข้าสู่ SleepEnd ให้โหลดซีน Start หลังดีเลย์ (วินาที)")] public bool loadStartSceneAfterDelay = true; [Tooltip("ชื่อซีนเมนูแรกที่จะกลับไป")] public string startSceneName = "Start"; [Tooltip("เวลารอก่อนโหลดซีน Start (วินาที)")] public float loadStartDelay = 10f;

        [Header("Ending (After Box Unlock)")]
        [Tooltip("Animator ที่ใช้เล่นท่ามือปิดหน้าเมื่อจบเกม")] public Animator handCoverAnimator;
        [Tooltip("ชื่อ Trigger ใน Animator สำหรับเริ่มท่ามือปิดหน้า")] public string handCoverTrigger = "CoverFace";
        [Tooltip("เวลาถือท่ามือปิดหน้าก่อนตัดจบ (วินาที)")] public float handCoverDuration = 2.0f;
        [Tooltip("ค้นหา Animator ใต้ Player อัตโนมัติเมื่อไม่ได้เซ็ตใน Inspector")] public bool autoFindHandAnimatorUnderPlayer = true;
        [Tooltip("ค้นหาจากชื่อ GameObject/Animator ที่มีคำนี้ (ปล่อยว่างเพื่อหาทุกตัว)")] public string handAnimatorNameFilter = "Armature";

        [Header("Ghost Spawn Hide")] [Tooltip("วัตถุ/โมเดลที่ต้องการให้หายไปเมื่อเข้าสู่สถานะ GhostSpawn")] public GameObject hideOnGhostSpawn;

        [Header("Debug / Dev")] public bool debugSkipToSleepEndOnStart = false; public KeyCode debugSkipKey = KeyCode.F9;
#if ENABLE_INPUT_SYSTEM
        public Key debugSkipKeyInputSystem = Key.None;
#endif

        private AudioSource persistentLoopSource; private bool cleanPlatesLoopStarted;
        [Header("Audio Loops")] public AudioClip cleanPlatesLoopClip; [Range(0f, 1f)] public float cleanPlatesLoopVolume = 0.7f; public bool cleanPlatesLoopPlayOnce = true;

        // Catch reset options used by BreakerInteractable and others
        private Coroutine caughtRoutine;
        [Header("Catch Reset Options")][Tooltip("เวลาค้างหน้าจับก่อนรีเซ็ต (วินาที)")] public float catchHoldSeconds = 3.5f; [Tooltip("ให้รอผู้เล่นกดที่ตู้ไฟก่อน แล้วค่อยปล่อยผี (ไม่ spawn อัตโนมัติ)")] public bool requireBreakerInteractToSpawnGhost = true;

        [Header("Mystery Box Hint Options")] [Tooltip("ให้ขึ้น Hint หาโน้ตเมื่อผู้เล่นไปลองเปิดกล่องครั้งแรก (ไม่แสดงทันทีหลัง BreakerFail)")] public bool hintNotesOnBoxAttempt = true; private bool mysteryBoxAttempted;

        public enum StoryState
        {
            IntroSeated, FindFlashlight, RestorePower, ReturnToSeat, TimeSkipCutscene, Finished,
            PlateCrashStart, InvestigateKitchen, CleanPlates, FridgeSequence, CheckOoy, HouseSweep, AnomalyFound, GhostSpawn, RunToBed, PraySequence, SleepEnd,
            InvestigateUpstairs, FindOoy,
            // NEW post blackout
            BreakerFail, FindNotes, OpenMysteryBox
        }

        private StoryState state;
        public StoryState CurrentState => state;
        // Expose if box was attempted yet for external hint logic
        public bool HasMysteryBoxAttempted => mysteryBoxAttempted;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (globalLoopClip)
            {
                globalLoopSource = gameObject.AddComponent<AudioSource>();
                globalLoopSource.clip = globalLoopClip; globalLoopSource.loop = true; globalLoopSource.playOnAwake = false; globalLoopSource.volume = globalLoopVolume;
                if (startGlobalLoopOnAwake) { globalLoopSource.Play(); globalLoopStarted = true; }
            }
            var loopGo = new GameObject("PersistentLoopAudio"); loopGo.transform.SetParent(transform);
            persistentLoopSource = loopGo.AddComponent<AudioSource>();
            persistentLoopSource.loop = true; persistentLoopSource.playOnAwake = false; persistentLoopSource.spatialBlend = 0f; persistentLoopSource.volume = cleanPlatesLoopVolume;

            originalNeedPliersHint = needPliersHint;
        }

        void Start()
        {
            if (!player) player = FindObjectOfType<PlayerController>();
            if (!introSeat) introSeat = FindObjectOfType<SeatInteractable>();
            if (!reuseSeat) reuseSeat = introSeat;
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
            EventBus.Subscribe<FuseFoundEvent>(OnFuseFound);
            EventBus.Subscribe<OoyCheckedEvent>(OnOoyChecked);
            EventBus.Subscribe<UpstairsDoorUnlockedEvent>(OnUpstairsDoorUnlocked);
            EventBus.Subscribe<KeyPickedEvent>(OnKeyPickedGeneric);
            EventBus.Subscribe<RadioToggledEvent>(OnRadioToggled);
            // NEW events
            EventBus.Subscribe<BreakerFailedEvent>(OnBreakerFailed);
            EventBus.Subscribe<AllNotesFoundEvent>(OnAllNotesFound);
            EventBus.Subscribe<BoxUnlockedEvent>(OnBoxUnlocked);
            // Catch -> reset checkpoint
            EventBus.Subscribe<PlayerCaughtEvent>(OnPlayerCaughtReset);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<FlashlightAcquiredEvent>(OnFlashlight);
            EventBus.Unsubscribe<PowerRestoredEvent>(OnPowerRestored);
            EventBus.Unsubscribe<PlayerSeatedEvent>(OnPlayerSeated);
            EventBus.Unsubscribe<TimeSkipFinishedEvent>(OnTimeSkipFinished);
            EventBus.Unsubscribe<KitchenEnteredEvent>(OnKitchenEntered);
            EventBus.Unsubscribe<PlatesCleanedEvent>(OnPlatesCleaned);
            EventBus.Unsubscribe<FuseFoundEvent>(OnFuseFound);
            EventBus.Unsubscribe<OoyCheckedEvent>(OnOoyChecked);
            EventBus.Unsubscribe<UpstairsDoorUnlockedEvent>(OnUpstairsDoorUnlocked);
            EventBus.Unsubscribe<KeyPickedEvent>(OnKeyPickedGeneric);
            EventBus.Unsubscribe<RadioToggledEvent>(OnRadioToggled);
            // NEW events
            EventBus.Unsubscribe<BreakerFailedEvent>(OnBreakerFailed);
            EventBus.Unsubscribe<AllNotesFoundEvent>(OnAllNotesFound);
            EventBus.Unsubscribe<BoxUnlockedEvent>(OnBoxUnlocked);
            EventBus.Unsubscribe<PlayerCaughtEvent>(OnPlayerCaughtReset);
        }

        private void SetupInitial()
        {
            if (started) return; started = true;
            state = StoryState.IntroSeated;
            EventBus.Publish(new StoryStateChangedEvent(StoryState.IntroSeated, StoryState.IntroSeated));
            if (tv && tv.videoPlayer)
            {
                tv.videoPlayer.Play();
            }
            if (!globalLoopStarted && globalLoopSource && !startGlobalLoopOnAwake && startGlobalLoopOnIntro)
            { globalLoopSource.volume = globalLoopVolume; globalLoopSource.Play(); globalLoopStarted = true; }
            if (introSeat && player && !player.IsSeated) player.EnterSeat(introSeat.seatAnchor, introSeat.cameraPoint);
            if (flashlightPickup) flashlightPickup.gameObject.SetActive(false);
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(false);
            if (tv) tv.PlayIntro();
            ShowHint("", introNewsDuration);
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(introNewsDuration);
            yield return new WaitForSeconds(blackoutDelay);
            TriggerBlackout();
        }

        private bool debugLogVerbose = true;
        private void DLog(string msg)
        {
            if (debugLogVerbose) Debug.Log("[SequenceController] " + msg, this);
        }

        private void TriggerBlackout()
        {
            DLog("TriggerBlackout");
            if (tv)
            {
                tv.PowerOff();   // ปิดเสียง/คลิปเก่า
                tv.StopVideo();              // หยุด VideoPlayer
                tv.SetScreen(tv.staticTexture); // แสดงภาพ static
            }
            if (powerManager) powerManager.SetPower(false); else FallbackBlackout();
            EventBus.Publish(new BlackoutStartedEvent());
            if (player && player.IsSeated) player.ExitSeat();
            if (flashlightPickup) flashlightPickup.gameObject.SetActive(true);
            SetState(StoryState.FindFlashlight);
            if (!useProgressiveHints) ShowHint("ไฟดับ... หาไฟฉายก่อน", 4f);
        }

        private void OnFlashlight(FlashlightAcquiredEvent _)
        {
            if (state != StoryState.FindFlashlight) return;
            SetState(StoryState.RestorePower);
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
            if (!useProgressiveHints) ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์", 4f);
        }

        private void OnPowerRestored(PowerRestoredEvent _)
        {
            DLog("OnPowerRestored received (state=" + state + ")");

            if (state != StoryState.RestorePower)
            {
                DLog("Ignored PowerRestoredEvent because state != RestorePower");
                return;
            }
            if (powerManager) powerManager.SetPower(true); else FallbackRestore();
            if (tv) tv.PreparePostRestoreNews();
            if (upstairsFootstepVoice)
                AudioSource.PlayClipAtPoint(upstairsFootstepVoice, player ? player.transform.position : transform.position, upstairsFootstepVoiceVolume);

            // เข้าสู่ขั้น InvestigateUpstairs ก่อน (อย่าข้ามไป FindOoy ทันที เพื่อให้ขึ้นฮินต์ 'ตามหาต้นตอของเสียง')
            SetState(StoryState.InvestigateUpstairs);
            if (!string.IsNullOrEmpty(objectiveFindNoiseSource)) ShowHint(objectiveFindNoiseSource, 4f);

            if (startRadioOnPowerRestore)
                TryStartUpstairsRadioDelayed();
        }

        private void OnPlayerSeated(PlayerSeatedEvent e)
        {
            if (state != StoryState.ReturnToSeat) return;
            if (e.SeatId != introSeatId) return;
            StartCoroutine(TimeSkipRoutine());
        }

        private IEnumerator TimeSkipRoutine()
        {
            SetState(StoryState.TimeSkipCutscene);
            LockPlayer(true);
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

        private void OnTimeSkipFinished(TimeSkipFinishedEvent _)
        {
            if (state != StoryState.Finished) return;
            StartCoroutine(PlateCrashSequence());
        }

        private IEnumerator PlateCrashSequence()
        {
            SetState(StoryState.PlateCrashStart);
            ShowHint("", 3f);
            yield return new WaitForSeconds(2f);
            SetState(StoryState.InvestigateKitchen);
            ShowHint("", 4f);
            if (autoTriggerKitchenEntered) EventBus.Publish(new KitchenEnteredEvent());
        }

        private void OnKitchenEntered(KitchenEnteredEvent _)
        {
            DLog("OnKitchenEntered (state=" + state + ")");
            if (state != StoryState.InvestigateKitchen) return;

            // ข้ามการเก็บจาน
            SequenceController.Instance.SkipCleanPlates();
        }

        private void OnPlatesCleaned(PlatesCleanedEvent e)
        {
            if (state != StoryState.CleanPlates) return;
            platesCleaned = true;
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
            SetState(StoryState.RestorePower);
            ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์", 4f);
        }

        private void OnFridgeScareDone(FridgeScareDoneEvent _)
        {
            if (state != StoryState.FridgeSequence) return;
            SetState(StoryState.CheckOoy);
            if (!useProgressiveHints) ShowHint("ไปดูออย", 4f);
        }

        private void OnOoyChecked(OoyCheckedEvent _)
        {
            if (state != StoryState.CheckOoy && state != StoryState.FindOoy) return;
            if (secondBlackoutStarted)
            {
                DLog("OnOoyChecked ignored (already running)");
                return;
            }
            DLog("OnOoyChecked -> start SecondBlackoutRoutine");
            if (ooyNotFoundVoice) AudioSource.PlayClipAtPoint(ooyNotFoundVoice, player ? player.transform.position : transform.position, ooyNotFoundVoiceVolume);
            else if (!string.IsNullOrEmpty(ooyNotFoundText)) ShowHint(ooyNotFoundText, 3.5f);
            secondBlackoutStarted = true;
            StartCoroutine(SecondBlackoutRoutine());
        }

        [Header("Second Blackout Audio")] [Tooltip("เสียงเตือน/ช็อตไฟก่อนดับรอบสอง (Optional)")] public AudioClip secondBlackoutWarningClip; [Tooltip("ระยะเวลาล่วงหน้าที่จะเล่นเสียงเตือนก่อนดับไฟ (วินาที)")] public float secondBlackoutWarningLead = 0.7f; [Range(0f,1f)] public float secondBlackoutWarningVolume = 1f;
        private IEnumerator SecondBlackoutRoutine()
        {
            // รอแบบเวลาจริง: ยึดตามเสียง แต่จำกัดไม่เกิน maxVoiceWaitSeconds แล้วบวกดีเลย์เพิ่ม
            float voiceLen = (ooyNotFoundVoice != null ? ooyNotFoundVoice.length : 0f);
            float wait = Mathf.Min(maxVoiceWaitSeconds, voiceLen) + Mathf.Max(0f, blackoutAgainDelay);
            if (wait > 0f)
            {
                DLog($"SecondBlackoutRoutine waiting (real) {wait:0.00}s");
                // ถ้ามีเสียงเตือนก่อนดับไฟ ให้แบ่งเวลารอก่อนเล่น
                if (secondBlackoutWarningClip && secondBlackoutWarningLead > 0f && secondBlackoutWarningLead < wait)
                {
                    float preWait = wait - secondBlackoutWarningLead;
                    if (preWait > 0f) yield return new WaitForSecondsRealtime(preWait);
                    AudioSource.PlayClipAtPoint(secondBlackoutWarningClip, player ? player.transform.position : transform.position, secondBlackoutWarningVolume);
                    yield return new WaitForSecondsRealtime(secondBlackoutWarningLead);
                }
                else
                {
                    // ไม่มีเสียงเตือนหรือ lead มากกว่ารอทั้งหมด -> รอเต็มแล้วค่อยเล่นก่อนดับ
                    yield return new WaitForSecondsRealtime(wait);
                    if (secondBlackoutWarningClip && secondBlackoutWarningLead > 0f)
                    {
                        AudioSource.PlayClipAtPoint(secondBlackoutWarningClip, player ? player.transform.position : transform.position, secondBlackoutWarningVolume);
                        // ถ้า lead > wait ให้ดีเลย์เพิ่มเล็กน้อยก่อนดับ (ใช้ lead ที่เหลือ)
                        float extra = secondBlackoutWarningLead - wait;
                        if (extra > 0f) yield return new WaitForSecondsRealtime(extra);
                    }
                }
            }
            DLog("SecondBlackoutRoutine -> power off + blackout event");
            if (tv) tv.PowerOff();
            if (powerManager) powerManager.SetPower(false); else FallbackBlackout();
            EventBus.Publish(new BlackoutStartedEvent());
            // NEW: do not spawn ghost yet, force player to check breaker
            SetState(StoryState.BreakerFail);
            DLog("State set to BreakerFail");
            if (!string.IsNullOrEmpty(checkBreakerHint)) ShowHint(checkBreakerHint, 4f);
        }

        // Handlers that were referenced in subscriptions (restore if missing)
        private void OnBreakerFailed(BreakerFailedEvent _)
        {
            if (state != StoryState.BreakerFail) return;
            if (sceneChasingGhost) sceneChasingGhost.gameObject.SetActive(true); else if (ghostSpawner) ghostSpawner.SpawnRandom(GhostSpawner.GhostKind.Chasing);
            if (hintNotesOnBoxAttempt)
            {
                // รอให้ผู้เล่นลองกดที่กล่องก่อนค่อยบอกว่าให้หาโน้ต
                SetState(StoryState.GhostSpawn);
            }
            else
            {
                // แบบเดิม: ขึ้น Hint โน้ตทันที
                SetState(StoryState.FindNotes);
                if (!string.IsNullOrEmpty(findNotesHint)) ShowHint(findNotesHint, 5f);
            }
        }
        private void OnAllNotesFound(AllNotesFoundEvent _)
        {
            if (state != StoryState.FindNotes) return;
            SetState(StoryState.OpenMysteryBox);
            if (!string.IsNullOrEmpty(openBoxHint)) ShowHint(openBoxHint, 4f);
        }
        private void OnBoxUnlocked(BoxUnlockedEvent _)
        {
            // Stop chase ghost if configured
            if (deactivateChaseGhostOnBoxOpen && sceneChasingGhost) sceneChasingGhost.gameObject.SetActive(false);
            // Lock player and play end animation -> end game
            StartCoroutine(BoxUnlockEndingRoutine());
        }

        private IEnumerator BoxUnlockEndingRoutine()
        {
            // หยุดการควบคุมผู้เล่นทันที
            LockPlayer(true);

            // เตรียมหา Animator ใต้ Player ถ้ายังไม่ได้อ้างอิง
            if (!handCoverAnimator && autoFindHandAnimatorUnderPlayer && player)
            {
                var anims = player.GetComponentsInChildren<Animator>(true);
                for (int i = 0; i < anims.Length; i++)
                {
                    var a = anims[i];
                    if (!a) continue;
                    if (string.IsNullOrEmpty(handAnimatorNameFilter) || a.gameObject.name.IndexOf(handAnimatorNameFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    { handCoverAnimator = a; break; }
                }
            }

            // เปิดและเล่นแอนิเมชันมือปิดหน้า (ไม่ปรับตำแหน่ง/พาเรนต์)
            if (handCoverAnimator)
            {
                var handGO = handCoverAnimator.gameObject;
                if (!handGO.activeSelf) handGO.SetActive(true);
                if (!string.IsNullOrEmpty(handCoverTrigger))
                    handCoverAnimator.SetTrigger(handCoverTrigger);
            }

            // รอแบบเวลาจริง เพื่อไม่ติด timeScale
            if (handCoverDuration > 0f)
                yield return new WaitForSecondsRealtime(handCoverDuration);

            // เข้าสู่ฉากจบ: แสดง To Be Continued (ใช้ระบบ SleepEnd ที่มีอยู่)
            if (state != StoryState.SleepEnd)
            {
                EnterSleepEnd();
            }
        }

        // ===== Fallback (no PowerManager) =====
        private void FallbackBlackout()
        {
            var lights = FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++) if (lights[i]) lights[i].enabled = false;
        }
        private void FallbackRestore()
        {
            var lights = FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++) if (lights[i]) lights[i].enabled = true;
        }

        // ===== Sleep end flow =====
        private void EnterSleepEnd()
        {
            SetState(StoryState.SleepEnd);
            if (!string.IsNullOrEmpty(sleepEndHintText)) ShowHint(sleepEndHintText, 1f);
            if (blackScreenOnSleepEnd)
            {
                var fader = FindObjectOfType<ScreenFader>();
                if (fader) StartCoroutine(fader.FadeOut(Mathf.Max(0.01f, sleepEndFadeTime)));
                LockPlayer(true);
            }
            if (stopAllAudioAtSleepEnd)
            {
                if (globalLoopSource) globalLoopSource.Stop(); if (persistentLoopSource) persistentLoopSource.Stop(); if (!sleepEndBackgroundClip) AudioListener.pause = true;
            }
            if (sleepEndBackgroundClip)
            {
                if (!sleepEndBgSource)
                { var bg = new GameObject("SleepEndBackgroundAudio"); bg.transform.SetParent(transform); sleepEndBgSource = bg.AddComponent<AudioSource>(); sleepEndBgSource.loop = sleepEndBackgroundLoop; sleepEndBgSource.playOnAwake = false; sleepEndBgSource.spatialBlend = 0f; }
                sleepEndBgSource.clip = sleepEndBackgroundClip; sleepEndBgSource.volume = sleepEndBackgroundVolume; sleepEndBgSource.Play();
            }
            StartCoroutine(SleepEndTextRoutine());
            if (loadStartSceneAfterDelay && !string.IsNullOrEmpty(startSceneName)) StartCoroutine(LoadStartSceneRoutine());
        }
        private IEnumerator SleepEndTextRoutine()
        {
            if (!useProgressiveHints) yield break; if (!useHintPresenterForSleepEndText) yield break; if (string.IsNullOrEmpty(sleepEndDisplayText)) yield break; if (sleepEndTextDelay > 0f) yield return new WaitForSeconds(sleepEndTextDelay); ShowHint(sleepEndDisplayText, sleepEndTextDuration);
        }
        private IEnumerator LoadStartSceneRoutine()
        {
            if (loadStartDelay > 0f) yield return new WaitForSecondsRealtime(loadStartDelay);
            // ป้องกันโหลดซ้ำถ้าผู้เล่นออกเกมไปแล้วหรือ state เปลี่ยน
            if (state != StoryState.SleepEnd) yield break;
            // ถ้าอยู่ในซีน Start แล้วไม่ต้องโหลดซ้ำ
            if (SceneManager.GetActiveScene().name == startSceneName) yield break;
            SceneManager.LoadScene(startSceneName);
        }

        // ===== Helpers =====
        private void LockPlayer(bool locked)
        {
            if (!player) return; player.enabled = !locked;
        }
        public void ShowTempHint(string text, float duration = 2f) => ShowHint(text, duration);
        private void ShowHint(string text, float duration) { if (hint) hint.Show(text, duration); }
        private void SetState(StoryState newState)
        {
            if (state == newState) return; var prev = state; state = newState;
            // ปรับ needPliersHint ตามสถานะ
            if (state == StoryState.RestorePower)
            {
                needPliersHint = restorePowerBlockHint; // override to block upstairs progression hint
            }
            else if (needPliersHint == restorePowerBlockHint && originalNeedPliersHint != null && state != StoryState.RestorePower)
            {
                needPliersHint = originalNeedPliersHint; // revert
            }
            DLog($"State -> {newState} (from {prev})"); EventBus.Publish(new StoryStateChangedEvent(prev, newState));
            string voiceId = "state_" + newState.ToString();
            EventBus.Publish(new PlayerVoiceRequestEvent(voiceId));

            // ซ่อนวัตถุเมื่อเข้าสู่ GhostSpawn
            if (newState == StoryState.GhostSpawn && hideOnGhostSpawn)
            {
                hideOnGhostSpawn.SetActive(false);
            }
        }
        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
 if (debugSkipKey != KeyCode.None && UnityEngine.Input.GetKeyDown(debugSkipKey)) { if (state != StoryState.SleepEnd) { EnterSleepEnd(); } }
#endif
#if ENABLE_INPUT_SYSTEM
            if (debugSkipKeyInputSystem != Key.None)
            {
                var kb = Keyboard.current; if (kb != null) { var keyCtrl = kb[debugSkipKeyInputSystem]; if (keyCtrl != null && keyCtrl.wasPressedThisFrame) { if (state != StoryState.SleepEnd) { EnterSleepEnd(); } } }
            }
#endif
        }

        // Call this from Diary interact component when diary is read
        public void NotifyDiaryRead()
        {
            diaryRead = true;
            if (requireDiaryBeforeOoy)
            {
                SetState(StoryState.FindOoy);
                if (!string.IsNullOrEmpty(findOoyHint)) ShowHint(findOoyHint, 4f);
            }
        }

        private void OnUpstairsDoorUnlocked(UpstairsDoorUnlockedEvent _)
        {
            DLog("OnUpstairsDoorUnlocked -> auto start radio if assigned");
            // ถ้ายังอยู่ขั้น RestorePower ให้ย้ำฮินต์ไปใส่ฟิวส์ ไม่ให้ผู้เล่นสับสน
            if (state == StoryState.RestorePower)
            {
                ShowContextualHintIfNeeded();
            }
            else if (!string.IsNullOrEmpty(needUpstairsKeyHint))
            {
                ShowHint(needUpstairsKeyHint, 3.5f);
            }

            if (upstairsRadio && !radioStarted && state != StoryState.RestorePower)
            {
                // เริ่มวิทยุเฉพาะเมื่อไม่ติดอยู่ในขั้น RestorePower (ยังไม่เสร็จฟิวส์)
                TryStartUpstairsRadioDelayed(0f);
            }
        }

        private void OnKeyPickedGeneric(KeyPickedEvent e)
        {
            if (e.KeyId == "UpstairsDoorKey")
            {
                // ถ้ายังไม่ได้ RestorePower → ย้ำให้ไปใส่ฟิวส์
                if (state == StoryState.RestorePower)
                {
                    ShowContextualHintIfNeeded();
                    return;
                }
                if (!string.IsNullOrEmpty(goTurnOffRadioHint))
                    ShowHint(goTurnOffRadioHint, 4f);
            }
        }

        private void OnRadioToggled(RadioToggledEvent e)
        {
            if (!e.On)
            {
                if (!string.IsNullOrEmpty(readDiaryHint)) ShowHint(readDiaryHint, 3.5f);
                if (!requireDiaryBeforeOoy || !diaryInteractCollider)
                {
                    SetState(StoryState.FindOoy);
                    if (!string.IsNullOrEmpty(findOoyHint)) ShowHint(findOoyHint, 4f);
                }
            }
            else
            {
                // เปิดวิทยุ แต่ถ้ายังไม่ได้ RestorePower (เกิดผิดลำดับ) ให้เน้นกลับไปทำฟิวส์
                if (state == StoryState.RestorePower)
                {
                    ShowContextualHintIfNeeded();
                }
            }
        }

        private void OnFuseFound(FuseFoundEvent e)
        {
            if (e.Location == FuseLocation.Upstairs)
            {
                StartCoroutine(PlateCrashSequence());
                return;
            }
            if (e.Location == FuseLocation.StorageRoom)
            {
                if (storageFuseTriggered) return; storageFuseTriggered = true;
                if (tv) { tv.PlayStatic(); }
                if (ghostSpawner) ghostSpawner.SpawnAtIndex(1, GhostSpawner.GhostKind.NonChasing);
                ShowHint("", 2.5f);
                return;
            }
        }

        private void OnPlayerCaughtReset(PlayerCaughtEvent _)
        {
            if (caughtRoutine != null) StopCoroutine(caughtRoutine);
            caughtRoutine = StartCoroutine(CaughtResetFlow());
        }
        private IEnumerator CaughtResetFlow()
        {
            if (catchHoldSeconds > 0f) yield return new WaitForSeconds(catchHoldSeconds);
            var reaction = player ? player.GetComponent<PlayerCatchReaction>() : null; if (reaction) reaction.RestoreToDefault();
            if (player && breakerSpawnPoint)
            {
                player.transform.position = breakerSpawnPoint.position;
                player.transform.rotation = breakerSpawnPoint.rotation;
            }
            if (powerManager) powerManager.SetPower(false); else FallbackBlackout();
            EventBus.Publish(new BlackoutStartedEvent());
            SetState(StoryState.BreakerFail);
            if (!string.IsNullOrEmpty(checkBreakerHint)) ShowHint(checkBreakerHint, 4f);
            if (sceneChasingGhost)
            {
                if (ghostSpawnPoint)
                {
                    sceneChasingGhost.transform.position = ghostSpawnPoint.position;
                    sceneChasingGhost.transform.rotation = ghostSpawnPoint.rotation;
                }
                sceneChasingGhost.ResetCatchPose();
                sceneChasingGhost.gameObject.SetActive(requireBreakerInteractToSpawnGhost == false);
            }
            if (!requireBreakerInteractToSpawnGhost)
            {
                EventBus.Publish(new BreakerFailedEvent());
            }
        }
 


        public void SkipCleanPlates()
        {
            platesCleaned = true;  // ถือว่าเก็บครบแล้ว
            SetState(StoryState.RestorePower);  // ไปขั้นตอนใส่ฟิวส์ต่อ
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);

            // ถ้ามีระบบ hint ให้แสดงข้อความขั้นต่อไป
            if (!useProgressiveHints)
                ShowHint("ไปใส่ฟิวส์ที่คัตเอ้าท์", 4f);

        }

        [Header("Upstairs Radio Options")] [Tooltip("เล่นเสียงวิทยุทันทีหลัง RestorePower (ใส่ฟิวส์) โดยไม่ต้องรอปลดล็อคประตูชั้นบน")] public bool startRadioOnPowerRestore = true; [Tooltip("ดีเลย์ก่อนเริ่มวิทยุหลัง RestorePower (วินาที)")] public float radioStartDelay = 0.2f;
        private bool radioStarted;

        private void TryStartUpstairsRadioDelayed(float? overrideDelay = null)
        {
            if (!upstairsRadio || radioStarted) return;
            float delay = overrideDelay.HasValue ? overrideDelay.Value : Mathf.Max(0f, radioStartDelay);
            StartCoroutine(StartRadioRoutine(delay));
        }
        private IEnumerator StartRadioRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (!upstairsRadio) yield break;
            if (!upstairsRadio.gameObject.activeSelf) upstairsRadio.gameObject.SetActive(true);
            try { upstairsRadio.StartRadio(); radioStarted = true; DLog("Upstairs radio started"); } catch { }
        }

        // NEW: central method to show correct hint based on current state when player triggers unrelated flow
        private void ShowContextualHintIfNeeded()
        {
            // ถ้าอยู่ในขั้น RestorePower ให้ย้ำฮินต์ฟิวส์ แม้ผู้เล่นขึ้นชั้นบนแล้ว
            if (state == StoryState.RestorePower)
            {
                // ใช้ needPliersHint ที่ถูก override แล้วให้เป็นข้อความบล็อค
                ShowHint(needPliersHint, 4f);
                return;
            }
            // หลัง RestorePower แต่ก่อน FindOoy อาจต้องย้ำเป้าหมายเสียง
            if (state == StoryState.InvestigateUpstairs && !string.IsNullOrEmpty(objectiveFindNoiseSource))
            {
                ShowHint(objectiveFindNoiseSource, 4f);
                return;
            }
            // Default no action (progressive system จะจัดการเอง)
        }

        // ===== Mystery Box attempt trigger for note hint =====
        public void NotifyMysteryBoxAttempt()
        {
            if (!hintNotesOnBoxAttempt) return; // using old flow
            if (state == StoryState.FindNotes)
            {
                if (!string.IsNullOrEmpty(findNotesHint)) ShowHint(findNotesHint, 999f);
                return;
            }
            // แก้: อนุญาตให้กดกล่องซ้ำใน GhostSpawn เพื่อโชว์ฮินต์อีกครั้ง แม้เคย attempt แล้ว
            if (mysteryBoxAttempted && (state == StoryState.GhostSpawn || state == StoryState.BreakerFail))
            {
                if (!string.IsNullOrEmpty(findNotesHint)) ShowHint(findNotesHint, 999f);
                return;
            }
            if (mysteryBoxAttempted) return; // other states reuse old guard
            if (state != StoryState.GhostSpawn && state != StoryState.BreakerFail) return; // only during chase phase before notes state
            mysteryBoxAttempted = true;
            SetState(StoryState.FindNotes);
            if (!string.IsNullOrEmpty(findNotesHint)) ShowHint(findNotesHint, 999f);
        }

        public void OnMysteryBoxClosed(bool success)
        {
            if (success) return;
            if (state == StoryState.FindNotes && !string.IsNullOrEmpty(findNotesHint)) ShowHint(findNotesHint, 999f);
        }

        // Force re-show findNotes hint from external (LockedContainerInteractable)
        public void ForceFindNotesHint()
        {
            if (!string.IsNullOrEmpty(findNotesHint)) ShowHint(findNotesHint, 999f);
        }
    }
}
