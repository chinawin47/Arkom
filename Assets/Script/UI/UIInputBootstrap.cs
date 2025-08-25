using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// ʤ�Ի������ ����������� EventSystem + InputSystemUIInputModule
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

        // �� action asset �������Ѻ UI (�������� map UI ���ѧ���� default pointer/submit/cancel ��)
        if (module.actionsAsset == null)
            module.actionsAsset = inputActions.asset;
    }
}