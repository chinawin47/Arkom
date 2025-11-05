using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Hide Spot (Simple)")]
public class HideSpotInteractable : Interactable
{
    [Header("Anchors")]
    public Transform seatAnchor;
    public Transform cameraPoint;
    [Tooltip("ตำแหน่งให้ผีมาค้นหา (ถ้าเว้นว่างจะใช้ตำแหน่งของตู้)")] public Transform searchPoint;

    private bool occupied;

    public override bool CanInteract(object interactor)
    {
        if (oneTime && occupied) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        var pc = interactor as PlayerController;
        if (!pc) pc = GameObject.FindObjectOfType<PlayerController>();
        if (!pc) return;

        if (!occupied)
        {
            // เข้าไปหลบ
            occupied = true;
            PlayerStealth.SetHidden(transform);
            pc.EnterSeat(seatAnchor, cameraPoint);

            // แจ้งผีให้มาค้นหน้าตู้ (เฉพาะตอนกำลังไล่เท่านั้นจะสนใจเอง)
            Vector3 pt = (searchPoint ? searchPoint.position : transform.position);
            EventBus.Publish(new InvestigatePointEvent(pt));
        }
        else
        {
            // ออกจากที่หลบ
            occupied = false;
            PlayerStealth.Clear();
            pc.ExitSeat();
        }
    }
}
