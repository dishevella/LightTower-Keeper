public class CompleteCurrentTaskCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<TaskSystem>()?.CompleteCurrentTask();
    }
}
