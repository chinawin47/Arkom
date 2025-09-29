using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Seat Interactable")]
public class SeatInteractable : Interactable
{
    [Header("Seat Settings")]
    [Tooltip("�ش anchor �����ҧ����Ф� (�� �ش��ҧ���)")]
    public Transform seatAnchor;

    [Tooltip("�ش���˹�/��ع�ͧ���ͧ�͹��� (��������ҧ ���� cameraRoot ���")]
    public Transform cameraPoint;

    public string seatId = "IntroSeat"; // ���Ǩ�Ѻ� SequenceController

    [Tooltip("͹حҵ����觫������� oneTime = true ����ѧ�������")]
    public bool allowReEnterWhileSeated = true;

    private void Reset()
    {
        if (!seatAnchor) seatAnchor = transform;
    }

    public override bool CanInteract(object interactor)
    {
        if (!(interactor is PlayerController pc)) return false;
        // ��� oneTime ��ж١ consume ���� -> ���� �������ҹ���������� (�����¨���)
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
            // ��ҹ�������������� (���ͨ���Ѻ������������� logic)
        }
    }
}
