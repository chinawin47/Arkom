using UnityEngine;
using ARKOM.Core;
using ARKOM.Player;

[AddComponentMenu("Interactable/Key Pickup")]
public class KeyPickup : PickupInteractable
{
    public string keyId = "UpstairsBoxKey";

    protected override void ApplyPickup(PlayerController player)
    {
        Keyring.Add(keyId);
    }
}
