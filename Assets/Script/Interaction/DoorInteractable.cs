using UnityEngine;

public class DoorInteractable : Interactable
{
    [Header("Door Settings")]
    public Transform doorTransform;
    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(0, 90, 0);
    public float openCloseDuration = 0.7f;
    public bool isOpen = false;

    [Header("Audio")]
    [Tooltip("เสียงตอนเปิดประตู")] public AudioClip openSfx;
    [Tooltip("เสียงตอนปิดประตู")] public AudioClip closeSfx;
    [Range(0f,1f)] public float sfxVolume = 1f;

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
        bool newState = !isOpen;
        // Play sound first (based on new state)
        if (newState && openSfx)
            AudioSource.PlayClipAtPoint(openSfx, doorTransform ? doorTransform.position : transform.position, sfxVolume);
        else if (!newState && closeSfx)
            AudioSource.PlayClipAtPoint(closeSfx, doorTransform ? doorTransform.position : transform.position, sfxVolume);

        isOpen = newState;
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