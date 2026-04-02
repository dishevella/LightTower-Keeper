 using UnityEngine;

public class RestController : ControllerAbstract
{
    private const string TASK_GoToSleep = "Task_Go_To_Sleep";
    private void Start()
    {
        this.GetEvent().Register<RestStartedEvent>(OnRestStarted)
    .UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    private void OnRestStarted(RestStartedEvent evt)
    {
        var taskSystem = this.GetSystem<TaskSystem>();
        if(taskSystem != null)
        {
            taskSystem.SetTask(
                TASK_GoToSleep,
                "Go to Bed",
                "Rest fot the night");
        }
    }
}