using UnityEngine;

public class DutyCheckpointInteractable : InteractableAbstract
{
    public enum DutyCheckpointType
    {
        Generator = 0,
        LampRoom = 1,
        Lens = 2
    }

    [SerializeField] private string interactionText = "Inspect";
    [SerializeField] private DutyCheckpointType checkpointType = DutyCheckpointType.Generator;
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private WorldSubtitleView successSubtitleView;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !IsAlreadyChecked() && IsPhaseAllowed();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        var dutyModel = this.GetModel<LighthouseDutyModel>();
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

        if (successSubtitleView != null)
        {
            successSubtitleView.ResetView();
            successSubtitleView.Play();
        }
    }

    private bool IsAlreadyChecked()
    {
        var dutyModel = this.GetModel<LighthouseDutyModel>();
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

    private bool IsPhaseAllowed()
    {
        if (availablePhases == null || availablePhases.Length == 0)
        {
            return base.CanInteract();
        }

        var gameState = this.GetModel<GameStateModel>();
        if (gameState == null) return false;

        var currentPhase = gameState.CurrentPhase.Value;
        for (int i = 0; i < availablePhases.Length; i++)
        {
            if (availablePhases[i] == currentPhase)
                return true;
        }

        return false;
    }
}
