using UnityEngine;
[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] protected string interactionText = "Interact";
    public virtual string GetInteractionText()
    {
        return interactionText;
    }
    public abstract void Interact();

}
