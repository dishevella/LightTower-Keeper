using UnityEngine;
using TMPro;
using System.Threading.Tasks;
public class TaskPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statusText;
    private void Start()
    {
        if(TaskSystem.Instance!= null)
        {
            TaskSystem.Instance.OnTaskUpdated += RefreshTaskUI;
            RefreshTaskUI(
                TaskSystem.Instance.CurrentTaskTitle,
                TaskSystem.Instance.CurrentTaskDescription,
                TaskSystem.Instance.IsTaskComplete
                );
        }
    }
    private void OnDestroy()
    {
        if(TaskSystem.Instance != null)
        {
            TaskSystem.Instance.OnTaskUpdated -= RefreshTaskUI;
        }
    }
    private void RefreshTaskUI(string title,string description, bool completed)
    {
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(title) ? "No Task" : title;
        if (descriptionText != null)
            descriptionText.text = string.IsNullOrEmpty(description) ? "" : description;
        if (statusText != null)
            statusText.text = completed ? "Completed" : "In Progress";
    }
}
