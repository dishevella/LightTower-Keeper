using System;
using System.Globalization;
[Serializable]
public class TaskModel : IGameModel
{
    public string TaskId;
    public string TaskTitle;
    public string TaskDescription;
    public bool IsCompleted;
    public bool HasTask;
}
