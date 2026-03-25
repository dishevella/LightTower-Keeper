using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionHintPanel hintPanel;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactLayerMask = ~0;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentInteractable;

    private void Update()
    {
        DetectInteractable();
        HandleInteractionInput();
    }
    private void DetectInteractable()
    {
        currentInteractable = null;
        if(playerCamera == null)
        {
            if (hintPanel != null)
                hintPanel.Hide();
            return;
        }
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactLayerMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }
            if(interactable != null)
                {
                currentInteractable = interactable;
                if (hintPanel != null)
                {
                    string hint = currentInteractable.GetInteractionText();
                    hintPanel.Show($"[E]{hint}");
                }
                return;
            }
        
        }
        if (hintPanel != null)
            hintPanel.Hide();
        }
    private void HandleInteractionInput()
    {
        if (currentInteractable == null) return;
        if(Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact();
        }
    }
}
