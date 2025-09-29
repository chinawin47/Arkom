using UnityEngine;
using ARKOM.Core;

[AddComponentMenu("Interactable/Circuit Breaker")]
public class BreakerInteractable : Interactable
{
    [Header("Breaker")]
    public bool powerOn = false;
    public bool singleUse = true;

    protected override void OnInteract(object interactor)
    {
        if (powerOn && singleUse) return;
        powerOn = true;
        EventBus.Publish(new PowerRestoredEvent());
    }
}
