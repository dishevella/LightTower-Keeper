using UnityEngine;

public class DutyCheckpointInteractable : ModelBackedSingleUseInteractableBase
{
    public enum DutyCheckpointType
    {
        Generator = 0,
        LampRoom = 1,
        Lens = 2
    }

    [SerializeField] private string interactionText = "Inspect";
    [SerializeField] private DutyCheckpointType checkpointType = DutyCheckpointType.Generator;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    protected override bool IsCompletedInModel()
    {
        var dutyModel = GetDutyModel();
        if (dutyModel == null) return false;

        switch (checkpointType)
        {
            case DutyCheckpointType.Generator:
                return dutyModel.GeneratorChecked.Value;

            case DutyCheckpointType.LampRoom:
                return dutyModel.LampRoomChecked.Value;

            case DutyCheckpointType.Lens:
                return dutyModel.LensChecked.Value;
        }

        return false;
    }

    protected override void CompleteInModel()
    {
        var dutyModel = GetDutyModel();
        if (dutyModel == null) return;

        switch (checkpointType)
        {
            case DutyCheckpointType.Generator:
                dutyModel.GeneratorChecked.Value = true;
                break;

            case DutyCheckpointType.LampRoom:
                dutyModel.LampRoomChecked.Value = true;
                break;

            case DutyCheckpointType.Lens:
                dutyModel.LensChecked.Value = true;
                break;
        }
    }
}
