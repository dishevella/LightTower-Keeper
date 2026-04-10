using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HQTransmissionPanel : ModalPanelBase
{
    [Header("View")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageContentText;
    [SerializeField] private Button closeButton;

    [Header("Control")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractionController playerInteractionController;
    [SerializeField] private KeyCode openTaskKey = KeyCode.Tab;
    [SerializeField] private bool allowEscapeToClose = false;

    public event Action Closed;

    private string currentTitle = string.Empty;
    private string currentMessage = string.Empty;

    protected override GameObject RootObject => rootObject;
    protected override PlayerController ControlledPlayer => playerController;
    protected override PlayerInteractionController ControlledInteraction => playerInteractionController;
    protected override bool AllowEscapeToClose => allowEscapeToClose;
    protected override bool HasOpenShortcut => true;
    protected override KeyCode OpenShortcutKey => openTaskKey;

    protected override void Awake()
    {
        base.Awake();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public bool HasCurrentTask()
    {
        var taskModel = this.GetModel<TaskModel>();
        return taskModel != null && taskModel.HasTask.Value;
    }

    public bool OpenCurrentTask()
    {
        
        var taskModel = this.GetModel<TaskModel>();
        if (taskModel == null) return false;
        if (!taskModel.HasTask.Value) return false;
     
        OpenMessage(taskModel.TaskTitle.Value, taskModel.TaskDescription.Value);
        return true;
    }

    public void OpenMessage(string title, string message)
    {
        currentTitle = title;
        currentMessage = message;

        if (!OpenModalPanel()) return;
        ApplyCurrentMessage();
    }

    private void ResolveInteractionController()
    {
        if (playerInteractionController != null || playerController == null) return;
        playerInteractionController = playerController.GetComponent<PlayerInteractionController>();
    }

    protected override bool TryOpenFromShortcut()
    {
        return OpenCurrentTask();
    }

    protected override void ResolvePanelReferences()
    {
        ResolveInteractionController();
    }

    protected override void OnPanelOpened()
    {
        ApplyCurrentMessage();
    }

    protected override void OnPanelClosed()
    {
        Closed?.Invoke();
    }

    private void ApplyCurrentMessage()
    {
        if (titleText != null)
        {
            titleText.text = currentTitle;
        }

        if (messageContentText != null)
        {
            messageContentText.text = currentMessage;
        }
    }
}
