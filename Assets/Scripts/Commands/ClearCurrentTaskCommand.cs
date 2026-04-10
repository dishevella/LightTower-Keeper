public class ClearCurrentTaskCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<TaskSystem>()?.ClearTask();
    }
}
