using System;
using UnityEngine;

public class TaskSystem : MonoBehaviour
{
    public static TaskSystem Instance { get; private set; }
    public string CurrentTaskId { get; private set; }
    public string CurrentTaskTitle { get; private set; }
    public string CurrentTaskDescription { get; private set; }
    public bool IsTaskComplete { get; private set; }

    public event Action<string, string, bool> OnTaskUpdated;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void SetTask(string taskId, string taskTitle, string taskDescription)
    {
        CurrentTaskId = taskId;
        CurrentTaskTitle = taskTitle;
        CurrentTaskDescription = taskDescription;
        IsTaskComplete = false;
        NotifyTaskUpdated();
    }
    public bool IsCurrentTask(string taskId)
    {
        return CurrentTaskId == taskId;
    }
    public void CompleteCurrentTask()
    {
        if (string.IsNullOrEmpty(CurrentTaskId)) return;
        IsTaskComplete = true;
        NotifyTaskUpdated();
    }
    public void ClearTask()
    {
        CurrentTaskId = string.Empty;
        CurrentTaskTitle = string.Empty;
        CurrentTaskDescription = string.Empty;
        IsTaskComplete = false;
        NotifyTaskUpdated();
    }
    private void NotifyTaskUpdated()
    {
        OnTaskUpdated?.Invoke(CurrentTaskTitle, CurrentTaskDescription, IsTaskComplete);
    }
}
