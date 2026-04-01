using TMPro;
using UnityEngine;

public class TaskPanelController : ControllerAbstract
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private TaskModel taskModel;

    private void Start()
    {
        taskModel = this.GetModel<TaskModel>();

        if (taskModel == null) return;

        taskModel.TaskTitle.OnValueChanged += OnTitleChanged;
        taskModel.TaskDescription.OnValueChanged += OnDescriptionChanged;

        OnTitleChanged(taskModel.TaskTitle.Value);
        OnDescriptionChanged(taskModel.TaskDescription.Value);
    }

    private void OnDestroy()
    {
        if (taskModel == null) return;

        taskModel.TaskTitle.OnValueChanged -= OnTitleChanged;
        taskModel.TaskDescription.OnValueChanged -= OnDescriptionChanged;
    }

    private void OnTitleChanged(string value)
    {
        if (titleText != null)
            titleText.text = value;
    }

    private void OnDescriptionChanged(string value)
    {
        if (descriptionText != null)
            descriptionText.text = value;
    }
}