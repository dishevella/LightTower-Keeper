using UnityEngine;

public abstract class ModelBackedSingleUseInteractableBase : InteractableAbstract
{
    [Header("Success State")]
    [SerializeField] private GameObject[] objectsToDisableOnSuccess;
    [SerializeField] private GameObject[] objectsToEnableOnSuccess;
    [SerializeField] private InteractableMessageSubtitle successSubtitle;

    protected override bool IsInteractionBlocked()
    {
        return IsCompletedInModel() || base.IsInteractionBlocked();
    }

    public sealed override void Interact()
    {
        if (!CanInteract()) return;

        if (!CanCompleteInteraction())
        {
            PlaySubtitle(GetBlockedSubtitle());
            return;
        }

        CompleteInModel();
        ApplySuccessState(objectsToDisableOnSuccess, objectsToEnableOnSuccess, successSubtitle);
        OnInteractionCompleted();
    }

    protected virtual bool CanCompleteInteraction()
    {
        return true;
    }

    protected virtual InteractableMessageSubtitle GetBlockedSubtitle()
    {
        return null;
    }

    protected virtual void OnInteractionCompleted()
    {
    }

    protected abstract bool IsCompletedInModel();
    protected abstract void CompleteInModel();
}
