using UnityEngine;

public class CheckInController :ControllerAbstract
{
    private const string TASK_InspectEquipment = "Task_Inspect_Equipment";
   
    private void Start()
    {
        this.GetEvent().Register<CheckInStartedEvent>(OnCheckInStarted)
    .UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    private void OnCheckInStarted(CheckInStartedEvent evt)
    {
        var taskSystem = this.GetSystem<TaskSystem>();
        if(taskSystem!=null)
        {
            taskSystem.SetTask(
                TASK_InspectEquipment,
                "Inspect the Equipment",
                "Check the communication equipment inside the lighthouse."
                );
        }
    }
}
