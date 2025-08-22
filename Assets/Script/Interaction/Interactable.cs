using UnityEngine;

public abstract class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Interact";
    [SerializeField] protected bool oneTime = false;
    private bool consumed;

    public string DisplayName => displayName;

    public virtual bool CanInteract(object interactor) => !consumed;

    public void Interact(object interactor)
    {
        if (!CanInteract(interactor)) return;
        OnInteract(interactor);
        if (oneTime) consumed = true;
    }

    protected abstract void OnInteract(object interactor);
}