using UnityEngine;

public abstract class SingleUseInteractableBase : InteractableAbstract
{
    [Header("Single Use")]
    [SerializeField] private bool startCompleted;

    protected bool IsCompleted { get; private set; }

    protected virtual void Awake()
    {
        IsCompleted = startCompleted;
    }

    protected override bool IsInteractionBlocked()
    {
        return IsCompleted || base.IsInteractionBlocked();
    }

    protected void MarkCompleted()
    {
        IsCompleted = true;
    }

    protected void CompleteInteraction(GameObject[] objectsToDisable, GameObject[] objectsToEnable, InteractableMessageSubtitle successSubtitle = null)
    {
        MarkCompleted();
        ApplySuccessState(objectsToDisable, objectsToEnable, successSubtitle);
    }
}
