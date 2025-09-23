using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// สคริปต์ง่ายๆ ให้แน่ใจว่ามี EventSystem + InputSystemUIInputModule
public class UIInputBootstrap : MonoBehaviour
{
    public PlayerInputActions inputActions;

    void Awake()
    {
        if (!EventSystem.current)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }

        var module = FindObjectOfType<InputSystemUIInputModule>();
        if (!module) return;

        if (inputActions == null)
            inputActions = new PlayerInputActions();

        // ใช้ action asset เดิมสำหรับ UI (ถ้าไม่มี map UI ก็ยังใช้ค่า default pointer/submit/cancel ได้)
        if (module.actionsAsset == null)
            module.actionsAsset = inputActions.asset;
    }
}