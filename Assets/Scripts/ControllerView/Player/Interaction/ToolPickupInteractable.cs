using UnityEngine;

public class ToolPickupInteractable : InteractableAbstract
{
    public enum ToolType
    {
        SmallAxe = 0,
        Chainsaw = 1
    }

    [SerializeField] private string interactionText = "Pick up tool";
    [SerializeField] private ToolType toolType = ToolType.SmallAxe;
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private WorldSubtitleView pickupSubtitleView;
    [SerializeField] private GameObject[] objectsToDisableOnPickup;
    [SerializeField] private GameObject[] objectsToEnableOnPickup;

    private bool pickedUp;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !pickedUp && IsPhaseAllowed() && !AlreadyOwnTool();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        switch (toolType)
        {
            case ToolType.SmallAxe:
                this.SendCommand(new AcquireSmallAxeCommand());
                break;

            case ToolType.Chainsaw:
                this.SendCommand(new AcquireChainsawCommand());
                break;
        }

        pickedUp = true;

        if (pickupSubtitleView != null)
        {
            pickupSubtitleView.ResetView();
            pickupSubtitleView.Play();
        }

        SetObjectsActive(objectsToDisableOnPickup, false);
        SetObjectsActive(objectsToEnableOnPickup, true);
    }

    private bool AlreadyOwnTool()
    {
        var toolInventory = this.GetModel<ToolInventoryModel>();
        if (toolInventory == null) return false;

        return toolType == ToolType.SmallAxe
            ? toolInventory.HasSmallAxe.Value
            : toolInventory.HasChainsaw.Value;
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
