using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class LargeTreeObstacleInteractable : SingleUseInteractableBase
{
    [SerializeField] private string interactionText = "Clear the fallen tree";
    [SerializeField] private InteractableMessageSubtitle blockedSubtitle;
    [SerializeField] private InteractableMessageSubtitle clearedSubtitle;
    [SerializeField] private GameObject[] objectsToDisableOnClear;
    [SerializeField] private GameObject[] objectsToEnableOnClear;
    [Header("Story Progression")]
    [SerializeField] private bool advanceToNightDutyOnClear = true;
    [Header("Optional Clear Hook")]
    [SerializeField] private float clearDelay;
    [SerializeField] private UnityEvent onClearStarted;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        if (!HasSelectedChainsaw())
        {
            PlaySubtitle(blockedSubtitle);
            return;
        }

        StartCoroutine(ClearRoutine());
    }

    private IEnumerator ClearRoutine()
    {
        MarkCompleted();
        PlaySubtitle(clearedSubtitle);
        onClearStarted?.Invoke();

        if (clearDelay > 0f)
        {
            yield return new WaitForSeconds(clearDelay);
        }

        ApplySuccessState(objectsToDisableOnClear, objectsToEnableOnClear);

        if (advanceToNightDutyOnClear)
        {
            this.SendCommand(new FinishDay1ReturnRouteCommand());
        }
    }
}
