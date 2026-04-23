using UnityEngine;


public class PhaseTest : ControllerAbstract
{
    [SerializeField] private string TaskId;
    [SerializeField] private string TaskTitle;
    [SerializeField] private string TaskDescription;
   
    private void Start()
    {
        this.SendCommand<FinishApproachLighthouseCommand>();
        //this.SendCommand<AcquireSmallAxeCommand>();
       // this.SendCommand<AcquireCommunicationDeviceCommand>();
        //this.SendCommand<AcquireChainsawCommand>();
        this.SendCommand(new SetTaskCommand(TaskId, TaskTitle, TaskDescription));
    }
}
