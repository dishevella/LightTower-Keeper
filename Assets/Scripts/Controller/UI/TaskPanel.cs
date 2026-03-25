using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
public class TaskPanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statusText;

    private GameApp app;
    private TaskSystem taskSystem;

    private bool isOpen;
    private void Start()
    {
        taskSystem = app.GetSystem<TaskSystem>();
      
    }
    private void OnDestroy()
    {
        if(taskSystem != null)
        {
           
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
    public void OpenPanel()
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        isOpen = true;
    }

    public void ClosePanel()
    {
        if (rootObject != null)
            rootObject.SetActive(false);

        isOpen = false;
    }

    public void TogglePanel()
    {
        if (isOpen) ClosePanel();
        else OpenPanel();
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
