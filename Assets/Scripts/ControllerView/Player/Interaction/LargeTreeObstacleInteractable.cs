using UnityEngine;

public class LargeTreeObstacleInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Clear the fallen tree";
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private WorldSubtitleView blockedSubtitleView;
    [SerializeField] private WorldSubtitleView clearedSubtitleView;
    [SerializeField] private GameObject[] objectsToDisableOnClear;
    [SerializeField] private GameObject[] objectsToEnableOnClear;

    private bool cleared;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !cleared && IsPhaseAllowed();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        var toolInventory = this.GetModel<ToolInventoryModel>();
        bool canClear = toolInventory != null && toolInventory.HasChainsaw.Value;

        if (!canClear)
        {
            if (blockedSubtitleView != null)
            {
                blockedSubtitleView.ResetView();
                blockedSubtitleView.Play();
            }
            return;
        }

        cleared = true;
        SetObjectsActive(objectsToDisableOnClear, false);
        SetObjectsActive(objectsToEnableOnClear, true);

        if (clearedSubtitleView != null)
        {
            clearedSubtitleView.ResetView();
            clearedSubtitleView.Play();
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
