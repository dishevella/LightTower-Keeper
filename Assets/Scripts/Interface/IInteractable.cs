

public interface IInteractable : 
    IBelongToApp,
    ICanGetSystem,
    ICanGetModel,
    ICanSendCommand
    
{
    string GetInteractionText();
    void Interact();
    bool CanInteract();
}