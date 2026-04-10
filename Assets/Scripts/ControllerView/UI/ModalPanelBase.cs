using UnityEngine;
using UnityEngine.EventSystems;

public abstract class ModalPanelBase : ControllerAbstract
{
    private bool isOpen;

    protected bool IsPanelOpen => isOpen;

    protected abstract GameObject RootObject { get; }
    protected abstract PlayerController ControlledPlayer { get; }
    protected abstract PlayerInteractionController ControlledInteraction { get; }

    protected virtual bool AllowEscapeToClose => false;
    protected virtual bool HasOpenShortcut => false;
    protected virtual KeyCode OpenShortcutKey => KeyCode.None;

    protected virtual void ResolvePanelReferences() { }
    protected virtual void OnPanelAwake() { }
    protected virtual void OnPanelOpened() { }
    protected virtual void OnPanelClosed() { }
    protected virtual bool TryOpenFromShortcut() { return false; }

    protected virtual void Awake()
    {
        ResolvePanelReferences();
        SetRootActive(false);
        OnPanelAwake();
    }

    protected virtual void Update()
    {
        if (!isOpen)
        {
            if (HasOpenShortcut && Input.GetKeyDown(OpenShortcutKey))
            {
                TryOpenFromShortcut();
            }

            return;
        }

        if (AllowEscapeToClose && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    protected virtual void OnDestroy()
    {
        ReleaseModalState();
    }

    protected bool OpenModalPanel()
    {
        if (isOpen) return true;
        if (!GameplayModalState.TryOpen()) return false;

        SetRootActive(true);
        isOpen = true;
        SetGameplayLocked(true);
        EventSystem.current?.SetSelectedGameObject(null);
        OnPanelOpened();
        return true;
    }

    public virtual void ClosePanel()
    {
        if (!isOpen) return;

        SetRootActive(false);
        SetGameplayLocked(false);
        ReleaseModalState();
        EventSystem.current?.SetSelectedGameObject(null);
        OnPanelClosed();
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    private void SetRootActive(bool active)
    {
        if (RootObject != null)
        {
            RootObject.SetActive(active);
        }
    }

    private void SetGameplayLocked(bool locked)
    {
        if (ControlledPlayer != null)
        {
            ControlledPlayer.SetCanMove(!locked);
            ControlledPlayer.SetCanLook(!locked);
            ControlledPlayer.SetCanRun(!locked);
            ControlledPlayer.SetCanCrouch(!locked);
            ControlledPlayer.SetCanJump(!locked);
            ControlledPlayer.SetCursorLocked(!locked);
        }

        if (ControlledInteraction != null)
        {
            ControlledInteraction.SetInteractionEnabled(!locked);
        }
    }

    private void ReleaseModalState()
    {
        if (!isOpen) return;

        GameplayModalState.CloseOne();
        isOpen = false;
    }
}
