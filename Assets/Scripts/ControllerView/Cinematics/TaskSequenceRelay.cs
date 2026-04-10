using UnityEngine;

public class TaskSequenceRelay : ControllerAbstract
{
    [SerializeField] private string taskId;
    [SerializeField] private string taskTitle;
    [SerializeField] [TextArea(2, 4)] private string taskDescription;
    [SerializeField] private HQTransmissionPanel transmissionPanel;

    public void PublishConfiguredTask()
    {
        this.SendCommand(new SetTaskCommand(taskId, taskTitle, taskDescription));
    }

    public void PublishConfiguredTaskAndOpenTransmission()
    {
        PublishConfiguredTask();

        if (transmissionPanel != null)
        {
            transmissionPanel.OpenCurrentTask();
        }
    }

    public void ClearCurrentTask()
    {
        this.SendCommand<ClearCurrentTaskCommand>();
    }

    public void CompleteCurrentTask()
    {
        this.SendCommand<CompleteCurrentTaskCommand>();
    }
}
