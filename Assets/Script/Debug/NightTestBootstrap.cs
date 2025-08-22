using UnityEngine;
using ARKOM.Game;
using ARKOM.Core;
using UnityEngine.InputSystem; // ���к� Input System ����

public class NightTestBootstrap : MonoBehaviour
{
    public GameManager gameManager;

    void Start()
    {
        if (!gameManager)
            gameManager = FindObjectOfType<GameManager>();

        Debug.Log("[Test] Press N (���ͻ��� N) �����������ҧ�׹. �� E ��� anomaly �������¹.");
        EventBus.Subscribe<AnomalyResolvedEvent>(e => Debug.Log($"[Anomaly] Resolved {e.Id}"));
        EventBus.Subscribe<NightCompletedEvent>(_ => Debug.Log("[Night] Completed!"));
    }

    void Update()
    {
        // ���к�����᷹ Input.GetKeyDown (������������������͡ 'Input System Package Only')
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            gameManager.BeginNight();
            Debug.Log("[Night] Started");
        }
    }
}