using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Flashlight Pickup")]
public class FlashlightPickupInteractable : PickupInteractable
{
    [Header("Flashlight Ref")]
    public Flashlight flashlightPrefabOrInstance;
    [Tooltip("�Դ�ѹ����ѧ���������")] public bool turnOnAfterPickup = false;

    protected override void ApplyPickup(PlayerController player)
    {
        if (!flashlightPrefabOrInstance)
        {
            Debug.LogWarning("[FlashlightPickup] No flashlight assigned.");
            return;
        }
        Flashlight toGive = flashlightPrefabOrInstance;
        // ����� prefab (�������� scene) ��� instantiate
        if (!toGive.gameObject.scene.IsValid())
        {
            toGive = Instantiate(toGive);
        }
        player.AcquireFlashlight(toGive, turnOnAfterPickup);
        EventBus.Publish(new FlashlightAcquiredEvent());
    }
}
