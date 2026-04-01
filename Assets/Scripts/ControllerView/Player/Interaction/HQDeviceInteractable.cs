using UnityEngine;
 
public class HQDeviceInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Check the communication device";
    [TextArea(2, 5)]
    [SerializeField] private string receiveMessage = "Settle in and rest";
    private bool completed;
    public override string GetInteractionText()
    {
        return interactionText;
    }
    public override bool CanInteract()
    {
        return !completed && base.CanInteract();
    }
    public override void Interact()
    {
        if (completed) return;
        completed = true;
        Debug.Log($"HQ MESSAGE:{receiveMessage}");
        this.SendCommand(new FinishCheckInCommand());
    }
}