using UnityEngine;

public class LightActivationInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Activate lighthouse light";
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private WorldSubtitleView missingRequirementSubtitleView;
    [SerializeField] private WorldSubtitleView activatedSubtitleView;
    [SerializeField] private GameObject[] objectsToEnableOnActivate;
    [SerializeField] private GameObject[] objectsToDisableOnActivate;

    private bool activated;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !activated && IsPhaseAllowed();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        var dutyModel = this.GetModel<LighthouseDutyModel>();
        if (dutyModel == null) return;

        bool canActivate =
            dutyModel.GeneratorChecked.Value
            && dutyModel.LampRoomChecked.Value
            && dutyModel.LensChecked.Value;

        if (!canActivate)
        {
            if (missingRequirementSubtitleView != null)
            {
                missingRequirementSubtitleView.ResetView();
                missingRequirementSubtitleView.Play();
            }
            return;
        }

        dutyModel.LightActivated.Value = true;
        activated = true;

        SetObjectsActive(objectsToEnableOnActivate, true);
        SetObjectsActive(objectsToDisableOnActivate, false);

        if (activatedSubtitleView != null)
        {
            activatedSubtitleView.ResetView();
            activatedSubtitleView.Play();
        }

        this.SendCommand(new FinishDay1NightDutyCommand());
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

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
