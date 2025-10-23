using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

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

    private int insertedCount =0; // จำนวนฟิวส์ที่ใส่ไปแล้วในรอบนี้
    private int Required => requiredFusesOverride >=0 ? requiredFusesOverride : FuseInventory.Required;

    public override bool CanInteract(object interactor)
    {
        var seq = SequenceController.Instance;
        // Allow always in BreakerFail so player can "check" it even if already used earlier
        if (seq && seq.CurrentState == SequenceController.StoryState.BreakerFail)
            return base.CanInteract(interactor);

        if (singleUse && powerOn) return false;
        if (seq && seq.RequireCleanPlatesBeforeFuse && !seq.PlatesCleaned)
            return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        var seq = SequenceController.Instance;
        if (seq && seq.RequireCleanPlatesBeforeFuse && !seq.PlatesCleaned)
        {
            if (!string.IsNullOrEmpty(seq.needCleanPlatesHint))
                seq.ShowTempHint(seq.needCleanPlatesHint,2f);
            return;
        }

        // NEW: If we are in BreakerFail state (post-second-blackout), breaker cannot restore power
        if (seq && seq.CurrentState == SequenceController.StoryState.BreakerFail)
        {
            if (failClickSfx) AudioSource.PlayClipAtPoint(failClickSfx, transform.position, sfxVolume);
            else if (notEnoughFuseSfx) AudioSource.PlayClipAtPoint(notEnoughFuseSfx, transform.position, sfxVolume);
            seq?.ShowTempHint("คัตเอ้าท์ไม่ทำงาน...",2.5f);
            EventBus.Publish(new BreakerFailedEvent());
            return;
        }

        if (!singleUse && powerOn)
        {
            powerOn = false;
            if (powerOffSfx) AudioSource.PlayClipAtPoint(powerOffSfx, transform.position, sfxVolume);
            EventBus.Publish(new BlackoutStartedEvent());
            return;
        }

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

            // แจ้ง UI/ระบบนับฟิวส์
            EventBus.Publish(new FusesInsertedEvent(insertedCount, Required));

            // เอฟเฟ็กต์พิเศษ: ถ้าฟิวส์จาก upstairs ให้ spawn ผี non-chasing point0
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

        if (!powerOn)
        {
            powerOn = true;
            if (powerOnSfx) AudioSource.PlayClipAtPoint(powerOnSfx, transform.position, sfxVolume);
            EventBus.Publish(new PowerRestoredEvent());
            return;
        }

        if (!singleUse && powerOn)
        {
            powerOn = false;
            if (powerOffSfx) AudioSource.PlayClipAtPoint(powerOffSfx, transform.position, sfxVolume);
            EventBus.Publish(new BlackoutStartedEvent());
        }
    }
}
