using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;
using ARKOM.Story;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[AddComponentMenu("Interactable/Hide Spot (Simple)")]
public class HideSpotInteractable : Interactable
{
    [Header("Anchors")] public Transform seatAnchor; public Transform cameraPoint; [Tooltip("????????????? InvestigatePointEvent ???????")] public Transform searchPoint;

    [Header("E Key Interaction")] [Tooltip("?????????????????????? E ????/??????????")] public float interactDistance = 2f; [Tooltip("????? E ?????????/?????????? Interactable ????")] public bool useEKeyDirect = true;
    [Header("Facing Constraint")] [Tooltip("????????? (????) ?????????? Player.forward ????????????????? ??????????????????")][Range(0f,180f)] public float requiredFacingAngle = 70f; [Tooltip("??????????????? (Raycast) ???? ??? true ??????????? Collider ????")] public bool requireLineOfSight = false; [Tooltip("LayerMask ?????? LineOfSight (????????????????)")] public LayerMask losMask;

    [Header("Lock While Hidden")] [Tooltip("?????????? PlayerController ??????????? (?????????????)")] public bool disablePlayerControllerWhileHidden = true; [Tooltip("??? Interactable ???? ? ??????????????????? (?????????????????)")] public bool disableOtherInteractablesWhileHidden = true;

    private bool occupied; private PlayerController currentPlayer;
    private Interactable[] cachedOthers; private System.Collections.Generic.List<Interactable> disabledList = new System.Collections.Generic.List<Interactable>();

    public override bool CanInteract(object interactor)
    {
        if (oneTime && occupied) return false;
        return base.CanInteract(interactor);
    }

    protected override void OnInteract(object interactor)
    {
        if (useEKeyDirect) return; // ??????? E ?????? ??????? Interactable ????
        Toggle(interactor as PlayerController);
    }

    void Update()
    {
        if (!useEKeyDirect) return;
        if (!currentPlayer) currentPlayer = FindObjectOfType<PlayerController>();
        if (!currentPlayer) return;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; bool ePressed = kb != null && kb.eKey.wasPressedThisFrame;
#else
        bool ePressed = Input.GetKeyDown(KeyCode.E);
#endif
        if (!ePressed) return;

        // ?????????????????? (??????????????????????, ?????????????????????????)
        if (!occupied && Vector3.Distance(currentPlayer.transform.position, transform.position) > interactDistance) return;

        if (!occupied)
        {
            // ??????????????????? + LOS
            if (!IsFacingSpot(currentPlayer.transform)) return;
            if (requireLineOfSight && !HasLineOfSight(currentPlayer.transform)) return;
        }

        Toggle(currentPlayer);
    }

    private bool IsFacingSpot(Transform playerTf)
    {
        Vector3 toSpot = (transform.position - playerTf.position); toSpot.y = 0f;
        Vector3 forward = playerTf.forward; forward.y = 0f;
        if (toSpot.sqrMagnitude < 0.0001f) return true;
        float angle = Vector3.Angle(forward.normalized, toSpot.normalized);
        return angle <= requiredFacingAngle;
    }

    private bool HasLineOfSight(Transform playerTf)
    {
        Vector3 origin = playerTf.position + Vector3.up * 0.5f;
        Vector3 dir = (transform.position - origin);
        float dist = dir.magnitude;
        if (dist < 0.05f) return true;
        dir /= dist;
        if (Physics.Raycast(origin, dir, dist, losMask, QueryTriggerInteraction.Ignore)) return false;
        return true;
    }

    private void Toggle(PlayerController pc)
    {
        if (!pc) return;
        if (!occupied)
        {
            if (!IsFacingSpot(pc.transform)) return;
            if (requireLineOfSight && !HasLineOfSight(pc.transform)) return;
            occupied = true; currentPlayer = pc;
            PlayerStealth.SetHidden(transform);
            pc.EnterSeat(seatAnchor, cameraPoint);
            if (disablePlayerControllerWhileHidden && pc.enabled) pc.enabled = false;
            if (disableOtherInteractablesWhileHidden) DisableOtherInteractables();
            Vector3 pt = (searchPoint ? searchPoint.position : transform.position);
            EventBus.Publish(new InvestigatePointEvent(pt));
        }
        else
        {
            occupied = false;
            PlayerStealth.Clear();
            pc.ExitSeat();
            if (disablePlayerControllerWhileHidden && !pc.enabled) pc.enabled = true;
            if (disableOtherInteractablesWhileHidden) RestoreOtherInteractables();
        }
    }

    private void DisableOtherInteractables()
    {
        disabledList.Clear();
        if (cachedOthers == null) cachedOthers = FindObjectsOfType<Interactable>(true);
        for (int i = 0; i < cachedOthers.Length; i++)
        {
            var it = cachedOthers[i]; if (!it) continue; if (it == this) continue; if (!it.enabled) continue; // already off
            it.enabled = false; disabledList.Add(it);
        }
    }
    private void RestoreOtherInteractables()
    {
        for (int i = 0; i < disabledList.Count; i++)
        {
            if (disabledList[i]) disabledList[i].enabled = true;
        }
        disabledList.Clear();
    }
}
