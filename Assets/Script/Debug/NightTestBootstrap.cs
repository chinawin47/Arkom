using UnityEngine;
using ARKOM.Game;
using ARKOM.Core;
using UnityEngine.InputSystem; // ใช้ระบบ Input System ใหม่

public class NightTestBootstrap : MonoBehaviour
{
    public GameManager gameManager;

    void Start()
    {
        if (!gameManager)
            gameManager = FindObjectOfType<GameManager>();

        Debug.Log("[Test] Press N (หรือปุ่ม N) เพื่อเริ่มกลางคืน. กด E ใส่ anomaly ที่เปลี่ยน.");
        EventBus.Subscribe<AnomalyResolvedEvent>(e => Debug.Log($"[Anomaly] Resolved {e.Id}"));
        EventBus.Subscribe<NightCompletedEvent>(_ => Debug.Log("[Night] Completed!"));
    }

    void Update()
    {
        // ใช้ระบบใหม่แทน Input.GetKeyDown (ซึ่งใช้ไม่ได้เมื่อเลือก 'Input System Package Only')
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            gameManager.BeginNight();
            Debug.Log("[Night] Started");
        }
    }
}