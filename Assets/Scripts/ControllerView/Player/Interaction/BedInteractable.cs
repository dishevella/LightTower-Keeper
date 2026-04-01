using System.Collections;
using UnityEngine;

public class BedInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Go to sleep";
    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private float fadeToBlackDuration = 2f;
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    private bool sleeping;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !sleeping && base.CanInteract();
    }

    public override void Interact()
    {
        if (sleeping) return;
        StartCoroutine(SleepRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        sleeping = true;
        SetPlayerLocked(true);

        if (fadePanel != null)
        {
            yield return fadePanel.FadeToBlack(fadeToBlackDuration);
        }

        this.SendCommand(new FinishDay0RestCommand());
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