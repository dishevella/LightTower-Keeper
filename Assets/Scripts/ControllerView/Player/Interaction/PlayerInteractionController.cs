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

    [Header("RunTime")]
    [SerializeField] private bool interactionEnabled = true;

    private IInteractable currentInteractable;

    private void Update()
    {
        if(!interactionEnabled || MessageSubtitlePanel.HasVisibleMessageSubtitle)
        {
            ClearCurrentInteractable();
            return;
        }
        DetectInteractable();
        HandleInteractionInput();
    }
    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        if(!interactionEnabled)
        {
            ClearCurrentInteractable();
        }
    }
    private void DetectInteractable()
    {
        currentInteractable = null;
        if(playerCamera == null)
        {
            HideHint();
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
            if(interactable != null && interactable.CanInteract())
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
        HideHint();
    }
    private void HandleInteractionInput()
    {
        if (currentInteractable == null) return;
        if(Input.GetKeyDown(interactKey)&& currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
            HideHint();
        }
    }
    private void ClearCurrentInteractable()
    {
        currentInteractable = null;
        HideHint();
    }

    private void HideHint()
    {
        if (hintPanel != null)
        {
            hintPanel.Hide();
        }
    }
}
