using JetBrains.Annotations;
using System;
using UnityEngine;

public class TaskSystem : IGameSystem
{
    private GameApp app;
    private TaskModel taskModel;

    public void Initialize(GameApp app)
    {
        this.app = app;
        taskModel = app.GetModel<TaskModel>();
    }
    public void SetTask(string taskId,string title, string description)
    {
        taskModel.TaskId = taskId;
        taskModel.TaskTitle = title;
        taskModel.TaskDescription = description;
        taskModel.IsCompleted = false;
        taskModel.HasTask = true;
        Publish();
    }
    public void CompleteCurrentTask()
    {
        if (!taskModel.HasTask) return;
        taskModel.IsCompleted = true;
        Publish();
    }
    public void ClearTask()
    {
        taskModel.TaskId = string.Empty;
        taskModel.TaskTitle = string.Empty;
        taskModel.TaskDescription = string.Empty;
        taskModel.IsCompleted = false;
        taskModel.HasTask = false;
        Publish();
    }
    private void Publish()
    {
        app.Events.Publish(new TaskChangedEvent
        {
            TaskId = taskModel.TaskId,
            Title = taskModel.TaskTitle,
            Description = taskModel.TaskDescription,
            IsCompleted = taskModel.IsCompleted
        });
    }
   
}
