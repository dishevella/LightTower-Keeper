using System;
using System.Globalization;
[Serializable]
public class TaskModel : ModelAbstract
{
    protected override void OnInit()
    {
        TaskId = new BindableProperty<string>(string.Empty);
        TaskTitle = new BindableProperty<string>(string.Empty);
        TaskDescription = new BindableProperty<string>(string.Empty);
        IsCompleted = new BindableProperty<bool>(false);
        HasTask = new BindableProperty<bool>(false);
    }
    public BindableProperty<string> TaskId { get; set; }
    public BindableProperty<string> TaskTitle { get; set; }
    public BindableProperty<string> TaskDescription { get; set; }
    public BindableProperty<bool> IsCompleted { get; set; }
    public BindableProperty<bool> HasTask { get; set; }
}
