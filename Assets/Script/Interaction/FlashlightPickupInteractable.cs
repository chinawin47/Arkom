using UnityEngine;
using ARKOM.Player;
using ARKOM.Core;

[AddComponentMenu("Interactable/Flashlight Pickup")]
public class FlashlightPickupInteractable : PickupInteractable
{
    [Header("Flashlight Ref")]
    public Flashlight flashlightPrefabOrInstance;
    [Tooltip("�Դ�ѹ����ѧ���������")] public bool turnOnAfterPickup = false;

    [Header("Flashlight Pickup Audio")] // �������§੾�е͹�Ѻ俩��
    [Tooltip("���§�͹�������Դ俩�� (������� turnOnAfterPickup = true)")] public AudioClip pickupTurnOnSfx;
    [Tooltip("���§�͹�����ѧ����Դ俩�� (turnOnAfterPickup = false)")] public AudioClip pickupTurnOffSfx;
    [Range(0f,1f)] public float flashlightTogglePickupVolume = 1f;

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

        // ������§੾��ʶҹ��Դ/�ѧ����Դ (�͡�˹�ͨҡ pickupSfx �ͧ�ҹ)
        if (turnOnAfterPickup && pickupTurnOnSfx)
        {
            AudioSource.PlayClipAtPoint(pickupTurnOnSfx, transform.position, flashlightTogglePickupVolume);
        }
        else if (!turnOnAfterPickup && pickupTurnOffSfx)
        {
            AudioSource.PlayClipAtPoint(pickupTurnOffSfx, transform.position, flashlightTogglePickupVolume);
        }
    }
}
