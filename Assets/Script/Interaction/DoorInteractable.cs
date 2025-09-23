using UnityEngine;

public class DoorInteractable : Interactable
{
    [Header("Door Settings")]
    public Transform doorTransform;
    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(0, 90, 0);
    public float openCloseDuration = 0.7f;
    public bool isOpen = false;

    private bool isMoving = false;
    private Quaternion targetRot;
    private Quaternion startRot;
    private float moveTimer = 0f;

    private void Start()
    {
        if (!doorTransform) doorTransform = transform;
        doorTransform.localRotation = Quaternion.Euler(isOpen ? openRotation : closedRotation);
    }

    protected override void OnInteract(object interactor)
    {
        if (isMoving) return;
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        startRot = doorTransform.localRotation;
        targetRot = Quaternion.Euler(isOpen ? openRotation : closedRotation);
        moveTimer = 0f;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;
        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / openCloseDuration);
        doorTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
        if (t >= 1f) isMoving = false;
    }
}