using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class CoconutInteractable : InteractableBase
{
    [TextArea]
    [SerializeField] private string Message = "Coconut!";

    public override void Interact()
    {
        Debug.Log(Message);
    }
}
