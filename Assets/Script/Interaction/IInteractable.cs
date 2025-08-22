using UnityEngine;

public interface IInteractable
{
    string DisplayName { get; }
    void Interact(object interactor);
    bool CanInteract(object interactor);
}