using UnityEngine;

public class ObservationInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Inspect";
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private WorldSubtitleView observationSubtitleView;
    [SerializeField] private bool onlyOnce;

    private bool observed;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !observed && IsPhaseAllowed();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        if (observationSubtitleView != null)
        {
            observationSubtitleView.ResetView();
            observationSubtitleView.Play();
        }

        if (onlyOnce)
        {
            observed = true;
        }
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
