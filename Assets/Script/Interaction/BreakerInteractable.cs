using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

namespace ARKOM
{
    [AddComponentMenu("Interactable/Circuit Breaker")]
    public class BreakerInteractable : Interactable
    {
        [Header("Breaker")]
        public bool powerOn = false;
        public bool singleUse = true; // ถ้า true เปิดครั้งเดียว (เปิดแล้ว)

        [Header("Fuse Requirement")] // NEW
        [Tooltip("จำนวนฟิวส์ที่ต้องใช้ (ค่า default =3 อิงจากระบบฟิวส์ปัจจุบัน)")] public int requiredFusesOverride = -1;
        [Tooltip("เสียงเมื่อพยายามเปิดแต่ฟิวส์ไม่พอ")] public AudioClip notEnoughFuseSfx;
        [Tooltip("เสียงใส่ฟิวส์สำเร็จ")] public AudioClip insertFuseSfx;

        [Header("Audio")] 
        [Tooltip("เสียงเปิดระบบ")] public AudioClip powerOnSfx;
        [Tooltip("เสียงปิด (กรณี singleUse = false)")] public AudioClip powerOffSfx;
        [Tooltip("เสียงกดแล้วไม่ติด (โหมด BreakerFail)")] public AudioClip failClickSfx;
        [Range(0f,1f)] public float sfxVolume =1f;

        [Header("Story Gate")]
        [Tooltip("เปิดใช้การล็อกด้วยเนื้อเรื่อง (ถึงสถานะนี้ก่อน จึงจะเริ่มใส่ฟิวส์/เปิดไฟได้)")] public bool requireStoryGate = false;
        [Tooltip("สถานะเนื้อเรื่องขั้นต่ำที่ต้องถึงก่อน จึงจะใช้งานตู้ไฟได้")]
        public SequenceController.StoryState requiredStoryState = SequenceController.StoryState.RestorePower;
        [Tooltip("ข้อความเตือนเมื่อยังไม่ถึงขั้นเนื้อเรื่อง")]
        public string notReadyHint = "ยังไม่ถึงเวลาซ่อมไฟ";


        [Header("Fuse Objects")]
        [Tooltip("OBJ ที่ซ่อนในตู้ ใส่ฟิวส์บางส่วนก็เปิดได้")]
        public GameObject hiddenFuseObj1;
        public GameObject hiddenFuseObj2;
        public GameObject hiddenFuseObj3;
        public static SequenceController Instance { get; private set; }

        private int insertedCount =0; // จำนวนฟิวส์ที่ใส่ไปแล้วในรอบนี้
        private int Required => requiredFusesOverride >=0 ? requiredFusesOverride : FuseInventory.Required;

        private bool firedOnFail;

        private void OnEnable()
        {
            EventBus.Subscribe<StoryStateChangedEvent>(OnStoryState);
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryState);
        }
        private void OnStoryState(StoryStateChangedEvent e)
        {
            if (e.Current == SequenceController.StoryState.BreakerFail)
            {
                firedOnFail = false; // รีให้กดครั้งแรกแล้วปล่อยผีได้ทุกครั้งที่เข้า checkpoint นี้
            }
        }

        public override bool CanInteract(object interactor)
        {
            var seq = SequenceController.Instance;
            // อนุญาตให้กดเสมอในโหมด BreakerFail เพื่อทริกเกอร์ผี
            if (seq && seq.CurrentState == SequenceController.StoryState.BreakerFail)
                return base.CanInteract(interactor);

            if (singleUse && powerOn) return false;
            // อนุญาตให้กดเพื่อโชว์ฮินต์แม้ยังไม่ถึงสเตตัสเนื้อเรื่อง
            return base.CanInteract(interactor);
        }

        protected override void OnInteract(object interactor)
        {
            var seq = SequenceController.Instance;

            //1) โหมดรีเซ็ตครั้งที่สอง: ตู้ไฟพัง -> ให้ทริกเกอร์ผีเมื่อกดครั้งแรกเท่านั้น
            if (seq && seq.CurrentState == SequenceController.StoryState.BreakerFail)
            {
                if (seq.requireBreakerInteractToSpawnGhost && !firedOnFail)
                {
                    if (failClickSfx) AudioSource.PlayClipAtPoint(failClickSfx, transform.position, sfxVolume);
                    else if (notEnoughFuseSfx) AudioSource.PlayClipAtPoint(notEnoughFuseSfx, transform.position, sfxVolume);
                    seq?.ShowTempHint("คัตเอ้าท์ไม่ทำงาน...",2.5f);
                    firedOnFail = true;
                    EventBus.Publish(new BreakerFailedEvent());
                }
                return;
            }

            //2) Gate ด้วยเนื้อเรื่องก่อน (สำหรับการใส่ฟิวส์/เปิดไฟ)
            if (requireStoryGate && !IsStoryAllowed(seq))
            {
                SequenceController.Instance?.ShowTempHint(notReadyHint,2.5f);
                return;
            }

            //3) สลับปิดได้เมื่อไม่ singleUse และเปิดอยู่
            if (!singleUse && powerOn)
            {
                powerOn = false;
                if (powerOffSfx) AudioSource.PlayClipAtPoint(powerOffSfx, transform.position, sfxVolume);
                EventBus.Publish(new BlackoutStartedEvent());
                return;
            }

            //4) ขั้นตอนใส่ฟิวส์
            if (insertedCount < Required)
            {
                // consume one fuse with origin
                if (!FuseInventory.RemoveOne(out var origin))
                {
                    if (notEnoughFuseSfx) AudioSource.PlayClipAtPoint(notEnoughFuseSfx, transform.position, sfxVolume);
                    return;
                }
                insertedCount++;

                StoryDebug.Log($"Insert fuse {insertedCount}/{Required} (origin={origin})", this);
                if (insertFuseSfx) AudioSource.PlayClipAtPoint(insertFuseSfx, transform.position, sfxVolume);
                EventBus.Publish(new FusesInsertedEvent(insertedCount, Required));

                // --- Progressive objects ---
                if (insertedCount == 1 && hiddenFuseObj1) hiddenFuseObj1.SetActive(true);
                if (insertedCount == 2 && hiddenFuseObj2) hiddenFuseObj2.SetActive(true);
                if (insertedCount >= 3)
                {
                    if (hiddenFuseObj3) hiddenFuseObj3.SetActive(true); // ตัวนี้อาจเป็นไฟจริง
                    powerOn = true;
                    if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
                    EventBus.Publish(new PowerRestoredEvent());
                }

                // เอฟเฟ็กต์พิเศษเดิม: ฟิวส์จากชั้นบน -> spawn non-chasing
                if (origin == FuseLocation.Upstairs && seq && seq.ghostSpawner)
                {
                    seq.ghostSpawner.SpawnAtIndex(0);
                }

                if (insertedCount >= Required)
                {
                    powerOn = true;
                    if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
                    EventBus.Publish(new PowerRestoredEvent());
                }
                return;
            }

            //5) ถ้าฟิวส์ครบแล้วแต่ยังปิดอยู่ -> เปิดไฟ
            if (!powerOn)
            {
                powerOn = true;
                if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
                EventBus.Publish(new PowerRestoredEvent());
                return;
            }

            //6) ปิดได้ถ้าไม่ singleUse
            if (!singleUse && powerOn)
            {
                powerOn = false;
                if (powerOffSfx) AudioSource.PlayClipAtPoint(powerOffSfx, transform.position, sfxVolume);
                EventBus.Publish(new BlackoutStartedEvent());
            }
        }

        private bool IsStoryAllowed(SequenceController seq)
        {
            if (!seq) return true;
            return (int)seq.CurrentState >= (int)requiredStoryState;
        }
    }
}
