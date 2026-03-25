using UnityEngine;

public class PhoneInteractable : InteractableBase
{
    [SerializeField] private MessagePanel messagePanel;

    public override void Interact()
    {
        if (messagePanel != null)
        {
            messagePanel.OpenPanel();
        }
        else
        {
            Debug.LogWarning("MessagePanel is not assigned.");
        }
    }
}