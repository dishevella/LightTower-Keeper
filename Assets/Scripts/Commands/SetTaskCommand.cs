public class SetTaskCommand : CommandAbstract
{
    public string TaskId { get; }
    public string Title { get; }
    public string Description { get; }

    public SetTaskCommand(string taskId, string title, string description)
    {
        TaskId = taskId;
        Title = title;
        Description = description;
    }

    protected override void OnExecute()
    {
        this.GetSystem<TaskSystem>()?.SetTask(TaskId, Title, Description);
    }
}
