using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Seat Interactable")]
public class SeatInteractable : Interactable
{
    [Header("Seat Settings")]
    [Tooltip("จุด anchor ที่จะวางตัวละคร (เช่น จุดกลางเบาะ)")]
    public Transform seatAnchor;

    [Tooltip("จุดตำแหน่ง/หมุนของกล้องตอนนั่ง (ถ้าเว้นว่าง ใช้ค่า cameraRoot เดิม")]
    public Transform cameraPoint;

    public string seatId = "IntroSeat"; // ใช้ตรวจจับใน SequenceController

    [Tooltip("อนุญาตให้นั่งซ้ำได้แม้ oneTime = true ถ้ายังนั่งอยู่")]
    public bool allowReEnterWhileSeated = true;

    private void Reset()
    {
        if (!seatAnchor) seatAnchor = transform;
    }

    public override bool CanInteract(object interactor)
    {
        if (!(interactor is PlayerController pc)) return false;
        // ถ้า oneTime และถูก consume แล้ว -> ห้าม เว้นแต่ว่านั่งอยู่แล้ว (ไม่ค่อยจำเป็น)
        if (!base.CanInteract(interactor))
        {
            if (allowReEnterWhileSeated && pc.IsSeated) return true;
            return false;
        }
        return true;
    }

    protected override void OnInteract(object interactor)
    {
        if (!(interactor is PlayerController pc)) return;
        if (!seatAnchor) seatAnchor = transform;

        if (!pc.IsSeated)
        {
            pc.EnterSeat(seatAnchor, cameraPoint);
            EventBus.Publish(new PlayerSeatedEvent(seatId));
        }
        else
        {
            // ถ้านั่งแล้วไม่ทำอะไร (หรือจะสลับเก้าอี้ค่อยเพิ่ม logic)
        }
    }
}
