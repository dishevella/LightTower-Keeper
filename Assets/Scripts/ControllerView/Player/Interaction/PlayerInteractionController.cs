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
    private readonly PlayerInteractionScanner scanner = new();
    private readonly PlayerInputAdapter inputAdapter = new();

    public bool InteractionEnabled => interactionEnabled;

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
        currentInteractable = scanner.FindInteractable(
            playerCamera,
            interactionDistance,
            interactLayerMask);
        if (currentInteractable != null)
        {
            if (hintPanel != null)
            {
                string hint = currentInteractable.GetInteractionText();
                hintPanel.Show($"[E]{hint}");
            }

            return;
        }

        HideHint();
    }
    private void HandleInteractionInput()
    {
        if (currentInteractable == null) return;
        if(inputAdapter.WasPressedThisFrame(interactKey) && currentInteractable.CanInteract())
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
