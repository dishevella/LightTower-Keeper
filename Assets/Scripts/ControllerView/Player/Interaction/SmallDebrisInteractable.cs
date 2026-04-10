using UnityEngine;
using UnityEngine.Events;

public class SmallDebrisInteractable : SingleUseInteractableBase
{
    [SerializeField] private string interactionText = "Chop the small tree";
    [SerializeField] private InteractableMessageSubtitle missingToolSubtitle;
    [SerializeField] private InteractableMessageSubtitle clearedSubtitle;
    [SerializeField] private GameObject[] objectsToDisableOnClear;
    [SerializeField] private GameObject[] objectsToEnableOnClear;
    [Header("Optional Chop Hook")]
    [SerializeField] private float clearDelay;
    [SerializeField] private UnityEvent onClearStarted;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        if (!HasSelectedSmallAxe())
        {
            PlaySubtitle(missingToolSubtitle);
            return;
        }

        StartCoroutine(ClearRoutine());
    }

    private System.Collections.IEnumerator ClearRoutine()
    {
        MarkCompleted();
        PlaySubtitle(clearedSubtitle);
        onClearStarted?.Invoke();

        if (clearDelay > 0f)
        {
            yield return new WaitForSeconds(clearDelay);
        }

        ApplySuccessState(objectsToDisableOnClear, objectsToEnableOnClear);
    }
}
