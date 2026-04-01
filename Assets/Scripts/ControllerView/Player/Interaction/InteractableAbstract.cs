using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public abstract class InteractableAbstract : MonoBehaviour, IInteractable
{
    [SerializeField] private StoryPhase requiredPhase;
    protected bool IsInRequiredPhase()
    {
        var state = this.GetModel<GameStateModel>();
        return state != null && state.CurrentPhase.Value == requiredPhase;
    }
    IApp IBelongToApp.GetApp()
    {
        return GameApp.Interface;
    }
    public abstract string GetInteractionText();
    public abstract void Interact();
    public virtual bool CanInteract() => IsInRequiredPhase();
}
