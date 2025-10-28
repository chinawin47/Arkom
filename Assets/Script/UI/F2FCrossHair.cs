using UnityEngine;

public class F2FCrossHair : MonoBehaviour
{
    [Header("Crosshair UI")]
    public GameObject CircleFull_Gameobject;

    [Header("Player Settings")]
    public Transform playerCamera; // ถ้า crosshair อยู่บน camera ใช้ transform ของ camera ได้เลย
    public float interactRange = 3f; // ระยะ interact

    private bool canInteract = true;

    void Update()
    {
        if (!canInteract) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        // ใช้ interactRange เป็นระยะ Ray
        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // ถ้าโดน collider ที่มี tag "Door" หรือ "Cube"
            if (hit.collider.CompareTag("Door") || hit.collider.CompareTag("Cube"))
            {
                CircleFull_Gameobject.SetActive(true);
            }
            else
            {
                CircleFull_Gameobject.SetActive(false);
            }
        }
        else
        {
            CircleFull_Gameobject.SetActive(false);
        }
    }
}