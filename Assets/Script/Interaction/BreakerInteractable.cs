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
    [Tooltip("จำนวนฟิวส์ที่ต้องการ (ค่า default = 3 ถ้าไม่ตั้งจะใช้ระบบกลาง)")] public int requiredFusesOverride = -1;
    [Tooltip("เสียงเตือนเมื่อยังไม่มีฟิวส์ในตัว")] public AudioClip notEnoughFuseSfx;
    [Tooltip("เสียงตอนใส่ฟิวส์สำเร็จแต่ยังไม่ครบ")] public AudioClip insertFuseSfx;

    [Header("Audio")] 
    [Tooltip("เสียงตอนเปิดใช้งาน")] public AudioClip powerOnSfx;
    [Tooltip("เสียงตอนปิด (กรณี singleUse = false)")] public AudioClip powerOffSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

    private int insertedCount = 0; // จำนวนฟิวส์ที่ใส่เข้าไปแล้วในตู้
    private int Required => requiredFusesOverride >= 0 ? requiredFusesOverride : FuseInventory.Required;

    public override bool CanInteract(object interactor)
    {
        if (singleUse && powerOn) return false;
        var seq = SequenceController.Instance;
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
                seq.ShowTempHint(seq.needCleanPlatesHint, 2f);
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

            // แจ้งระบบว่าใส่ฟิวส์แล้วกี่อัน
            EventBus.Publish(new FusesInsertedEvent(insertedCount, Required));

            // เงื่อนไขพิเศษ: ถ้าฟิวส์ที่ใส่มาจากชั้นบน -> ผีโผล่ทันที (stairs index 0)
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
