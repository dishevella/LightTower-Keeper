using System.Collections;
using UnityEngine;

public class BedInteractable : SingleUseInteractableBase
{
    [SerializeField] private string interactionText = "Go to sleep";
    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private float fadeToBlackDuration = 2f;
    [SerializeField] private float idleSettleDuration = 0.12f;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override void Interact()
    {
        if (!CanInteract()) return;
        if (!playerController.isGrounded) return;

        StartCoroutine(SleepRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        MarkCompleted();
        PreparePlayerForSleep();

        if (idleSettleDuration > 0f)
        {
            yield return new WaitForSeconds(idleSettleDuration);
        }

        SetPlayerLocked(true);

        if (fadePanel != null)
        {
            yield return fadePanel.FadeToBlack(fadeToBlackDuration);
        }

        this.SendCommand(new FinishDay0RestCommand());
    }

    private void PreparePlayerForSleep()
    {
        if (playerController == null) return;

        playerController.ClearMotionForCutscene();
        playerController.SetCanMove(false);
        playerController.SetCanLook(false);
        playerController.SetCanRun(false);
        playerController.SetCanCrouch(false);
        playerController.SetCanJump(false);
        playerController.ForceStandUp();
    }

    private void SetPlayerLocked(bool locked)
    {
        if (playerControlComponents == null) return;

        foreach (var comp in playerControlComponents)
        {
            if (comp != null)
            {
                comp.enabled = !locked;
            }
        }
    }
}
