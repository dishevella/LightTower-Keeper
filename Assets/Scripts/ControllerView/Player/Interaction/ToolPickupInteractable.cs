using UnityEngine;
using System.Collections;

public class ToolPickupInteractable : SingleUseInteractableBase
{
    public enum ToolType
    {
        SmallAxe = 0,
        Chainsaw = 1
    }

    [SerializeField] private string interactionText = "Pick up tool";
    [SerializeField] private ToolType toolType = ToolType.SmallAxe;
    [Header("Pickup Message")]
    [SerializeField] private InteractableMessageSubtitle pickupMessageSubtitle;
    [SerializeField] private GameObject[] objectsToDisableOnPickup;
    [SerializeField] private GameObject[] objectsToEnableOnPickup;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    protected override bool IsInteractionBlocked()
    {
        return AlreadyOwnTool() || base.IsInteractionBlocked();
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

        MarkCompleted();

        PlaySubtitle(pickupMessageSubtitle);

        ApplySuccessState(objectsToDisableOnPickup, objectsToEnableOnPickup);
    }

    private bool AlreadyOwnTool()
    {
        var toolInventory = GetToolInventoryModel();
        if (toolInventory == null) return false;

        return toolType == ToolType.SmallAxe
            ? toolInventory.HasSmallAxe.Value
            : toolInventory.HasChainsaw.Value;
    }
}
