using UnityEngine;
using System.Collections;

public class LighthouseEntranceInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Enter the lighthouse";

    [Header("Door Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private float openDuration = 1.2f;

    [Header("After Open")]
    [SerializeField] private float waitAfterOpen = 0.2f;

    private bool entered;
    private bool opening;
    public override string GetInteractionText()
    {
        return interactionText;
    }
    public override bool CanInteract()
    {
        return !entered && !opening && base.CanInteract();
    }
    public override void Interact()
    {
        if (entered || opening) return;
        StartCoroutine(OpenDoorAndEnterRoutine());
    }
    private IEnumerator OpenDoorAndEnterRoutine()
    {
        opening = true;
        
        if(doorAnimator !=null)
        {
            doorAnimator.ResetTrigger(openTriggerName);
            doorAnimator.SetTrigger(openTriggerName);
        }
        
        yield return new WaitForSeconds(openDuration);

        if (waitAfterOpen > 0f)
        {
            yield return new WaitForSeconds(waitAfterOpen);
        }
        entered = true;
        opening = false;
        this.SendCommand(new FinishApproachLighthouseCommand());
    }
}
