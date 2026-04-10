using UnityEngine;

public class ObservationInteractable : SingleUseInteractableBase
{
    [SerializeField] private string interactionText = "Inspect";
    [SerializeField] private InteractableMessageSubtitle observationSubtitle;
    [SerializeField] private bool onlyOnce;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        PlaySubtitle(observationSubtitle);

        if (onlyOnce)
        {
            MarkCompleted();
        }
    }

}
