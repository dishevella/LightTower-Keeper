using UnityEngine;

public class GeneratorStartupStepInteractable : InteractableAbstract
{
    [SerializeField] private GeneratorStartupController generatorController;
    [SerializeField] private GeneratorStartupController.StartupStep step = GeneratorStartupController.StartupStep.OpenHatch;
    [SerializeField] private string interactionText = "Interact";
    [SerializeField] private InteractableMessageSubtitle blockedSubtitle;
    [SerializeField] private InteractableMessageSubtitle successSubtitle;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return base.CanInteract()
            && generatorController != null
            && generatorController.IsStepAvailable(step);
    }

    public override void Interact()
    {
        if (!base.CanInteract()) return;
        if (generatorController == null) return;

        if (!generatorController.IsStepAvailable(step))
        {
            PlaySubtitle(blockedSubtitle);
            return;
        }

        generatorController.PerformStep(step);
        PlaySubtitle(successSubtitle);
    }
}
