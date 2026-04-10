using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class InteractableAbstract : MonoBehaviour, IInteractable
{
    [Header("Phase")]
    [SerializeField] private StoryPhase requiredPhase;
    [SerializeField] private StoryPhase[] availablePhases;

    protected bool IsInRequiredPhase()
    {
        return IsInPhase(requiredPhase);
    }

    protected bool IsInPhase(StoryPhase phase)
    {
        var state = this.GetModel<GameStateModel>();
        return state != null && state.CurrentPhase.Value == phase;
    }

    protected bool IsInAnyPhase(StoryPhase[] phases)
    {
        if (phases == null || phases.Length == 0)
        {
            return false;
        }

        var state = this.GetModel<GameStateModel>();
        if (state == null) return false;

        StoryPhase currentPhase = state.CurrentPhase.Value;
        for (int i = 0; i < phases.Length; i++)
        {
            if (phases[i] == currentPhase)
            {
                return true;
            }
        }

        return false;
    }

    protected bool IsPhaseAllowed(StoryPhase[] phases)
    {
        if (phases == null || phases.Length == 0)
        {
            return IsInRequiredPhase();
        }

        return IsInAnyPhase(phases);
    }

    protected void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    protected void PlaySubtitle(InteractableMessageSubtitle subtitle)
    {
        if (subtitle == null) return;

        subtitle.Play();
    }

    protected void ApplySuccessState(GameObject[] objectsToDisable, GameObject[] objectsToEnable, InteractableMessageSubtitle successSubtitle = null)
    {
        SetObjectsActive(objectsToEnable, true);
        PlaySubtitle(successSubtitle);
        SetObjectsActive(objectsToDisable, false);
        
    }

    protected bool RequireCondition(bool condition, InteractableMessageSubtitle failedSubtitle)
    {
        if (condition) return true;

        PlaySubtitle(failedSubtitle);
        return false;
    }

    protected ToolInventoryModel GetToolInventoryModel()
    {
        return this.GetModel<ToolInventoryModel>();
    }

    protected InventoryModel GetInventoryModel()
    {
        return this.GetModel<InventoryModel>();
    }

    protected LighthouseDutyModel GetDutyModel()
    {
        return this.GetModel<LighthouseDutyModel>();
    }

    protected bool HasSmallAxe()
    {
        ToolInventoryModel toolInventory = GetToolInventoryModel();
        return toolInventory != null && toolInventory.HasSmallAxe.Value;
    }

    protected bool HasChainsaw()
    {
        ToolInventoryModel toolInventory = GetToolInventoryModel();
        return toolInventory != null && toolInventory.HasChainsaw.Value;
    }

    protected bool HasAnyClearingTool()
    {
        return HasSmallAxe() || HasChainsaw();
    }

    protected bool HasSelectedItem(InventoryItemId itemId)
    {
        InventoryModel inventory = GetInventoryModel();
        return inventory != null && inventory.SelectedItem.Value == itemId;
    }

    protected bool HasSelectedSmallAxe()
    {
        return HasSelectedItem(InventoryItemId.SmallAxe);
    }

    protected bool HasSelectedChainsaw()
    {
        return HasSelectedItem(InventoryItemId.Chainsaw);
    }

    protected virtual bool IsInteractionBlocked()
    {
        return false;
    }

    IApp IBelongToApp.GetApp()
    {
        return GameApp.Interface;
    }

    public abstract string GetInteractionText();
    public abstract void Interact();

    public virtual bool CanInteract()
    {
        return !IsInteractionBlocked() && IsPhaseAllowed(availablePhases);
    }
}
