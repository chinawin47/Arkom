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
        public SeatInteractable introSeat; // ����������
        public SeatInteractable reuseSeat; // �������������Ѻ�ҹ����ѧ�Դ� (���������� introSeat)
        public FlashlightPickupInteractable flashlightPickup;
        public Interactable breakerInteractable; // �����繤ѵ��ҷ�/�á����
        public TVController tv; // OPTIONAL: ��ǤǺ������� (�ѧ������ҧ ��������)
        public PowerManager powerManager; // OPTIONAL

        [Header("Config Timings")]
        public float introNewsDuration = 4f; // ��蹢��ǡ�͹�Ѻ�
        public float blackoutDelay = 0.5f;   // ˹�ǧ��͹�Ѵ���ѧ intro
        public float timeSkipFadeOut = 1.2f;
        public float timeSkipBlackHold = 1.5f;
        public float timeSkipFadeIn = 1.2f;

        [Header("IDs / Seat")]
        public string introSeatId = "IntroSeat";

        [Header("UI / Hints (Optional external manager)")]
        public HintPresenter hint; // Optional helper ���س�Ҩ���ҧ

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
        public bool useProgressiveHints = true; // ��� true �������� hint ������� (����к� ProgressiveHintController �Ѵ���)

        // internal flags
        private bool sweepDone;
        private bool anomalySeen;
        private bool ghostSpawned;

        private StoryState state;
        private bool started;

        private Coroutine autoAnomalyCo;

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
            SleepEnd
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
            EventBus.Subscribe<FridgeScareDoneEvent>(OnFridgeScareDone);
            EventBus.Subscribe<OoyCheckedEvent>(OnOoyChecked);
            EventBus.Subscribe<SweepCompleteEvent>(OnSweepComplete);
            EventBus.Subscribe<AnomalyFirstSeenEvent>(OnAnomalyFirstSeen);
            EventBus.Subscribe<GhostSpawnedEvent>(OnGhostSpawned);
            EventBus.Subscribe<PlayerInBedEvent>(OnPlayerInBed);
            EventBus.Subscribe<PrayerFinishedEvent>(OnPrayerFinished);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<FlashlightAcquiredEvent>(OnFlashlight);
            EventBus.Unsubscribe<PowerRestoredEvent>(OnPowerRestored);
            EventBus.Unsubscribe<PlayerSeatedEvent>(OnPlayerSeated);
            EventBus.Unsubscribe<TimeSkipFinishedEvent>(OnTimeSkipFinished);
            EventBus.Unsubscribe<KitchenEnteredEvent>(OnKitchenEntered);
            EventBus.Unsubscribe<PlatesCleanedEvent>(OnPlatesCleaned);
            EventBus.Unsubscribe<FridgeScareDoneEvent>(OnFridgeScareDone);
            EventBus.Unsubscribe<OoyCheckedEvent>(OnOoyChecked);
            EventBus.Unsubscribe<SweepCompleteEvent>(OnSweepComplete);
            EventBus.Unsubscribe<AnomalyFirstSeenEvent>(OnAnomalyFirstSeen);
            EventBus.Unsubscribe<GhostSpawnedEvent>(OnGhostSpawned);
            EventBus.Unsubscribe<PlayerInBedEvent>(OnPlayerInBed);
            EventBus.Unsubscribe<PrayerFinishedEvent>(OnPrayerFinished);
        }

        private void SetupInitial()
        {
            if (started) return;
            started = true;
            state = StoryState.IntroSeated;
            StoryDebug.Log("State -> IntroSeated", this);

            // �ѧ�Ѻ����� (��� seatInteractable ���¡ EnterSeat)
            if (introSeat && player && !player.IsSeated)
                player.EnterSeat(introSeat.seatAnchor, introSeat.cameraPoint);

            // �Դ interact ��� �
            if (flashlightPickup) flashlightPickup.gameObject.SetActive(false); // ���Դ�͹��俩��
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(false);

            // �������蹢���
            if (tv) tv.PlayIntro();
            ShowHint("������...", introNewsDuration);
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
            // ����¼������ء�ͧ���ѵ��ѵ�
            if (player && player.IsSeated) player.ExitSeat();
            // �Դ俩�������
            if (flashlightPickup) flashlightPickup.gameObject.SetActive(true);
            state = StoryState.FindFlashlight;
            StoryDebug.Log("State -> FindFlashlight", this);
            if(!useProgressiveHints) ShowHint("俴Ѻ... ��俩�¡�͹", 4f);
        }

        private void OnFlashlight(FlashlightAcquiredEvent _)
        {
            if (state != StoryState.FindFlashlight) return;
            StoryDebug.Log("Flashlight acquired", this);
            state = StoryState.RestorePower;
            StoryDebug.Log("State -> RestorePower", this);
            if (breakerInteractable) breakerInteractable.gameObject.SetActive(true);
            if(!useProgressiveHints) ShowHint("��Դ�ѵ��ҷ�", 4f);
        }

        private void OnPowerRestored(PowerRestoredEvent _)
        {
            if (state != StoryState.RestorePower) return;
            StoryDebug.Log("Power restored", this);
            state = StoryState.ReturnToSeat;
            StoryDebug.Log("State -> ReturnToSeat", this);
            if (powerManager) powerManager.SetPower(true);
            if (tv) tv.PreparePostRestoreNews();
            ShowHint("��Ѻ仹�觴٢���", 4f);
            if (reuseSeat) reuseSeat.gameObject.SetActive(true);
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
            state = StoryState.TimeSkipCutscene;
            StoryDebug.Log("State -> TimeSkipCutscene", this);
            LockPlayer(true);
            if (hint) hint.HideImmediate();
            var fader = FindObjectOfType<ScreenFader>();
            if (fader) yield return fader.FadeOut(timeSkipFadeOut);
            if (tv) tv.PlayTimeSkipNews();
            yield return new WaitForSeconds(timeSkipBlackHold);
            if (fader) yield return fader.FadeIn(timeSkipFadeIn);
            LockPlayer(false);
            state = StoryState.Finished;
            StoryDebug.Log("State -> Finished", this);
            EventBus.Publish(new TimeSkipFinishedEvent());
            ShowHint("�� F �����ء", 3f);
        }

        private void LockPlayer(bool locked)
        {
            if (!player) return;
            if (locked)
            {
                // �Դ input map
                player.enabled = false; // ������ flag ���� �����ҡ��� Update �ҧ���ҧ�ѧ�ӧҹ
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
            state = StoryState.PlateCrashStart;
            StoryDebug.Log("State -> PlateCrashStart", this);
            // TODO: ������§��� + spawn ��ɨҹ
            ShowHint("�Դ���§�ѧ�ҡ����...", 3f);
            yield return new WaitForSeconds(2f);
            // �������: 仵�Ǩ������
            state = StoryState.InvestigateKitchen;
            StoryDebug.Log("State -> InvestigateKitchen", this);
            ShowHint("仴ٷ�����", 4f);
        }

        private void OnKitchenEntered(KitchenEnteredEvent _)
        {
            if (state != StoryState.InvestigateKitchen) return;
            StoryDebug.Log("KitchenEnteredEvent", this);
            state = StoryState.CleanPlates;
            StoryDebug.Log("State -> CleanPlates", this);
            var shards = FindObjectsOfType<PlateShardPickup>();
            PlateShardPickup.ResetCounter(shards.Length);
            foreach (var s in shards) s.gameObject.SetActive(true);
            if(!useProgressiveHints) ShowHint("����ɨҹ������", 4f);
        }

        private void OnPlatesCleaned(PlatesCleanedEvent e)
        {
            if (state != StoryState.CleanPlates) return;
            StoryDebug.Log("PlatesCleanedEvent (Total=" + e.Total + ")", this);
            state = StoryState.FridgeSequence;
            StoryDebug.Log("State -> FridgeSequence", this);
            if(!useProgressiveHints) ShowHint("�Դ������", 4f);
        }

        private void OnFridgeScareDone(FridgeScareDoneEvent _)
        {
            if (state != StoryState.FridgeSequence) return;
            StoryDebug.Log("FridgeScareDoneEvent", this);
            state = StoryState.CheckOoy;
            StoryDebug.Log("State -> CheckOoy", this);
            if(!useProgressiveHints) ShowHint("仴����", 4f);
        }

        private void OnOoyChecked(OoyCheckedEvent _)
        {
            if (state != StoryState.CheckOoy) return;
            StoryDebug.Log("OoyCheckedEvent", this);
            state = StoryState.HouseSweep;
            StoryDebug.Log("State -> HouseSweep", this);
            if (sweepManager) sweepManager.BeginSweep();
            if(!useProgressiveHints) ShowHint("��Ǩ�ͺ��ҹ", 4f);
        }

        private void OnSweepComplete(SweepCompleteEvent _)
        {
            if (state != StoryState.HouseSweep) return;
            StoryDebug.Log("SweepCompleteEvent", this);
            sweepDone = true;
            if(!useProgressiveHints) ShowHint("�������š�...", 3f);
            if (autoAnomalyAfterSweep && !anomalySeen && autoAnomalyCo == null)
                autoAnomalyCo = StartCoroutine(AutoAnomalyRoutine());
        }

        private IEnumerator AutoAnomalyRoutine()
        {
            float t = anomalyAutoDelay;
            StoryDebug.Log("������Ѻ���� Auto Anomaly (" + t + "s)", this);
            while (t > 0f)
            {
                if (anomalySeen || state != StoryState.HouseSweep) yield break; // �١��ԡ�������� �����͡�ҡ�൨
                t -= Time.deltaTime;
                yield return null;
            }
            if (anomalySeen || state != StoryState.HouseSweep) yield break;
            StoryDebug.Log("Auto ��ԡ���� AnomalyFirstSeenEvent", this);
            // publish anomaly event
            EventBus.Publish(new AnomalyFirstSeenEvent("Auto"));
            if (spawnGhostDirectOnAuto && !ghostSpawned)
            {
                // SpawnGhost() �ж١���¡��ҹ OnAnomalyFirstSeen ��������
            }
        }

        private void OnAnomalyFirstSeen(AnomalyFirstSeenEvent e)
        {
            if (state != StoryState.HouseSweep && state != StoryState.AnomalyFound) return;
            if (anomalySeen) return;
            if (autoAnomalyCo != null) { StopCoroutine(autoAnomalyCo); autoAnomalyCo = null; }
            StoryDebug.Log("AnomalyFirstSeenEvent id=" + e.AnomalyId, this);
            anomalySeen = true;
            state = StoryState.AnomalyFound;
            StoryDebug.Log("State -> AnomalyFound", this);
            SpawnGhost();
        }

        private void SpawnGhost()
        {
            if (ghostSpawned) return;
            StoryDebug.Log("SpawnGhost()", this);
            state = StoryState.GhostSpawn;
            StoryDebug.Log("State -> GhostSpawn", this);
            if (ghostSpawner)
            {
                ghostSpawner.SpawnRandom();
            }
            else
            {
                // �������� spawner ��������ͧ
                EventBus.Publish(new GhostSpawnedEvent(-1));
            }
        }

        private void OnGhostSpawned(GhostSpawnedEvent e)
        {
            if (state != StoryState.GhostSpawn) return;
            StoryDebug.Log("GhostSpawnedEvent index=" + e.Index, this);
            ghostSpawned = true;
            state = StoryState.RunToBed;
            StoryDebug.Log("State -> RunToBed", this);
            ShowHint("��Ѻ仹͹!", 4f);
            if (finalBedTrigger) finalBedTrigger.EnableBed();
        }

        private void OnPlayerInBed(PlayerInBedEvent _)
        {
            if (state != StoryState.RunToBed) return;
            StoryDebug.Log("PlayerInBedEvent", this);
            state = StoryState.PraySequence;
            StoryDebug.Log("State -> PraySequence", this);
            ShowHint("�Ǵ 3 �ͺ...", 2f);
            if (prayController) prayController.BeginPrayer(3);
            else EventBus.Publish(new PrayerFinishedEvent()); // fallback
        }

        private void OnPrayerFinished(PrayerFinishedEvent _)
        {
            if (state != StoryState.PraySequence) return;
            StoryDebug.Log("PrayerFinishedEvent", this);
            state = StoryState.SleepEnd;
            StoryDebug.Log("State -> SleepEnd", this);
            ShowHint("...",1f);
            // Optionally fade out or change GameState here.
        }

        public StoryState CurrentState => state; // public read-only accessor
    }

    // Placeholder TV & Power Manager interfaces (�س�����ҧ��ԧ�¡������)
    public class TVController : MonoBehaviour
    {
        public void PlayIntro() { /* ��蹢����á */ }
        public void PowerOff() { /* �Դ˹�Ҩ� */ }
        public void PreparePostRestoreNews() { /* �������Ի���� */ }
        public void PlayTimeSkipNews() { /* ��蹢�����ѧ time skip */ }
    }

    public class PowerManager : MonoBehaviour
    {
        public Light[] normalLights;
        public Light[] emergencyLights;
        public void SetPower(bool on)
        {
            foreach (var l in normalLights) if (l) l.enabled = on;
            foreach (var l in emergencyLights) if (l) l.enabled = !on; // 俩ء�Թ��Ѻ��ҹ
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
