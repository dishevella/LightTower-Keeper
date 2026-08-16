using System;
using Unity.VisualScripting;
using UnityEngine;

public class TaskSystem : SystemAbstract
{
    private TaskModel taskModel;

    protected override void OnInit()
    {
        taskModel = this.GetModel<TaskModel>();
    }
    public void SetTask(string taskId,string title, string description)
    {
        if (taskModel == null) return;
        taskModel.TaskId.Value = taskId;
        taskModel.TaskTitle.Value = title;
        taskModel.TaskDescription.Value = description;
        taskModel.IsCompleted.Value = false;
        taskModel.HasTask.Value = true;
        Publish();
        this.GetSystem<ObjectiveSystem>()?.MirrorLegacyTask(taskId, title, description);
    }
    public void CompleteCurrentTask()
    {
        if (!taskModel.HasTask.Value) return;
        taskModel.IsCompleted.Value = true;
        Publish();
        this.GetSystem<ObjectiveSystem>()?.MirrorLegacyCompletion(taskModel.TaskId.Value);
    }
    public void ClearTask()
    {
        taskModel.TaskId.Value = string.Empty;
        taskModel.TaskTitle.Value = string.Empty;
        taskModel.TaskDescription.Value = string.Empty;
        taskModel.IsCompleted.Value = false;
        taskModel.HasTask.Value = false;
        Publish();
        this.GetSystem<ObjectiveSystem>()?.ClearLegacyTask();
    }

    public void SetObjectiveGroupPresentation(
        string taskId,
        string title,
        string description,
        bool completed,
        bool hasTask)
    {
        if (taskModel == null) return;

        taskModel.TaskId.Value = taskId ?? string.Empty;
        taskModel.TaskTitle.Value = title ?? string.Empty;
        taskModel.TaskDescription.Value = description ?? string.Empty;
        taskModel.IsCompleted.Value = completed;
        taskModel.HasTask.Value = hasTask;
        Publish();
    }
    private void Publish()
    {
        this.GetEvent().Send(new TaskChangedEvent
        {
            TaskId = taskModel.TaskId.Value,
            Title = taskModel.TaskTitle.Value,
            Description = taskModel.TaskDescription.Value,
            IsCompleted = taskModel.IsCompleted.Value
        });
    }
   
}
