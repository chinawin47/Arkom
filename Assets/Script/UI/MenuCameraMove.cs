using UnityEngine;
using UnityEngine.InputSystem; 

public class MenuCameraMove : MonoBehaviour
{
    [Header("การตั้งค่าความเคลื่อนไหว")]
    public float moveAmount = 0.5f;
    public float smoothSpeed = 3f;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        if (Mouse.current == null) return; // กัน error ถ้าไม่มีเมาส์

        // อ่านตำแหน่งเมาส์จาก New Input System
        Vector2 mousePos = Mouse.current.position.ReadValue();
        float mouseX = (mousePos.x / Screen.width) - 0.5f;
        float mouseY = (mousePos.y / Screen.height) - 0.5f;

        Vector3 targetPos = initialPosition + new Vector3(mouseX * moveAmount, mouseY * moveAmount, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
    }
}
