using UnityEngine;

public class HQTransmissionInteractable : InteractableAbstract
{
    [SerializeField] private string interactionText = "Check the communication device";
    [SerializeField] private HQTransmissionPanel transmissionPanel;

    [Header("Transmission Content")]
    [SerializeField] private string transmissionTitle = "HQ Task Notice";
    [TextArea(3, 8)]
    [SerializeField] private string transmissionMessage;

    [Header("Task Update")]
    [SerializeField] private bool updateTaskOnRead = true;
    [SerializeField] private string taskId = string.Empty;
    [SerializeField] private string taskTitle = string.Empty;
    [TextArea(2, 4)]
    [SerializeField] private string taskDescription = string.Empty;

    [Header("Flow")]
    [SerializeField] private bool advanceToGoDockOnRead = false;
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private StoryPhase[] availablePhases;

    private bool completed;
    private bool waitingForClose;

    public override string GetInteractionText()
    {
        return interactionText;
    }

    public override bool CanInteract()
    {
        return !completed && !waitingForClose && IsPhaseAllowed();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        if (transmissionPanel == null)
        {
            FinishRead();
            return;
        }

        waitingForClose = true;
        transmissionPanel.Closed -= HandlePanelClosed;
        transmissionPanel.Closed += HandlePanelClosed;
        transmissionPanel.OpenMessage(transmissionTitle, transmissionMessage);
    }

    private void HandlePanelClosed()
    {
        if (transmissionPanel != null)
            transmissionPanel.Closed -= HandlePanelClosed;

        waitingForClose = false;
        FinishRead();
    }

    private void FinishRead()
    {
        if (updateTaskOnRead)
        {
            var taskSystem = this.GetSystem<TaskSystem>();
            if (taskSystem != null && !string.IsNullOrEmpty(taskTitle))
            {
                taskSystem.SetTask(taskId, taskTitle, taskDescription);
            }
        }

        if (advanceToGoDockOnRead)
        {
            this.SendCommand(new FinishDay1WakeUpCommand());
        }

        if (onlyOnce)
        {
            completed = true;
        }
    }

    private bool IsPhaseAllowed()
    {
        if (availablePhases == null || availablePhases.Length == 0)
        {
            return base.CanInteract();
        }

        var gameState = this.GetModel<GameStateModel>();
        if (gameState == null) return false;

        var currentPhase = gameState.CurrentPhase.Value;
        for (int i = 0; i < availablePhases.Length; i++)
        {
            if (availablePhases[i] == currentPhase)
                return true;
        }

        return false;
    }
}
