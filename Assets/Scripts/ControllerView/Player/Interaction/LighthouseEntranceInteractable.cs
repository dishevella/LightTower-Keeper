using System;
using System.Collections;
using UnityEngine;

public class LighthouseEntranceInteractable : InteractableAbstract
{
    public enum DoorCompletionCommand
    {
        None = 0,
        FinishApproachLighthouse = 1,
        FinishDay1InspectInside = 2
    }

    [Serializable]
    public class DoorPhaseConfig
    {
        public StoryPhase phase;
        public string interactionText = "Use the door";
        public bool requireSmallAxe;
        public InteractableMessageSubtitle missingAxeSubtitle;
        public DoorCompletionCommand completionCommand = DoorCompletionCommand.None;
        public Animator doorAnimator;
        public string openTriggerName = "Open";
        public float openDuration = 1.2f;
    }

    [Header("Phase Config")]
    [SerializeField] private DoorPhaseConfig[] phaseConfigs;

    [Header("After Open")]
    [SerializeField] private float waitAfterOpen = 0.2f;

    private bool opening;

    public override string GetInteractionText()
    {
        DoorPhaseConfig config = GetCurrentConfig();
        return config != null && !string.IsNullOrEmpty(config.interactionText)
            ? config.interactionText
            : "Use the door";
    }

    public override bool CanInteract()
    {
        return !opening && GetCurrentConfig() != null;
    }

    public override void Interact()
    {
        if (opening) return;

        DoorPhaseConfig config = GetCurrentConfig();
        if (config == null) return;

        if (config.requireSmallAxe)
        {
            if (!HasSmallAxe())
            {
                PlaySubtitle(config.missingAxeSubtitle);

                return;
            }
        }

        StartCoroutine(OpenDoorAndEnterRoutine(config));
    }

    private IEnumerator OpenDoorAndEnterRoutine(DoorPhaseConfig config)
    {
        if (config == null) yield break;

        opening = true;

        if (config.doorAnimator != null && !string.IsNullOrEmpty(config.openTriggerName))
        {
            config.doorAnimator.ResetTrigger(config.openTriggerName);
            config.doorAnimator.SetTrigger(config.openTriggerName);
        }

        if (config.openDuration > 0f)
        {
            yield return new WaitForSeconds(config.openDuration);
        }

        if (waitAfterOpen > 0f)
        {
            yield return new WaitForSeconds(waitAfterOpen);
        }

        opening = false;
        ExecuteCompletionCommand(config.completionCommand);
    }

    private void ExecuteCompletionCommand(DoorCompletionCommand completionCommand)
    {
        switch (completionCommand)
        {
            case DoorCompletionCommand.FinishApproachLighthouse:
                this.SendCommand(new FinishApproachLighthouseCommand());
                break;

            case DoorCompletionCommand.FinishDay1InspectInside:
                this.SendCommand(new FinishDay1InspectInsideCommand());
                break;
        }
    }

    private DoorPhaseConfig GetCurrentConfig()
    {
        if (phaseConfigs == null || phaseConfigs.Length == 0) return null;

        var gameState = this.GetModel<GameStateModel>();
        if (gameState == null) return null;

        StoryPhase currentPhase = gameState.CurrentPhase.Value;
        for (int i = 0; i < phaseConfigs.Length; i++)
        {
            DoorPhaseConfig config = phaseConfigs[i];
            if (config == null) continue;
            if (config.phase == currentPhase) return config;
        }

        return null;
    }
}
