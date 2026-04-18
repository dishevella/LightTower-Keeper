using UnityEngine;

public class LightActivationInteractable : ModelBackedSingleUseInteractableBase
{
    [SerializeField] private string interactionText = "Activate lighthouse light";
    [SerializeField] private InteractableMessageSubtitle missingRequirementSubtitle;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    protected override bool IsCompletedInModel()
    {
        var dutyModel = GetDutyModel();
        return dutyModel != null && dutyModel.LightActivated.Value;
    }

    protected override bool CanCompleteInteraction()
    {
        var dutyModel = GetDutyModel();
        if (dutyModel == null) return false;

        return dutyModel.GeneratorChecked.Value
            && dutyModel.LampRoomChecked.Value;
    }

    protected override InteractableMessageSubtitle GetBlockedSubtitle()
    {
        return missingRequirementSubtitle;
    }

    protected override void CompleteInModel()
    {
        var dutyModel = GetDutyModel();
        if (dutyModel == null) return;

        dutyModel.LightActivated.Value = true;
    }

    protected override void OnInteractionCompleted()
    {
    }
}
