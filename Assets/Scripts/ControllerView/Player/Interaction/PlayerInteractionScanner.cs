using UnityEngine;

public sealed class PlayerInteractionScanner
{
    public IInteractable FindInteractable(
        Camera camera,
        float maximumDistance,
        LayerMask layerMask)
    {
        if (camera == null || maximumDistance <= 0f)
        {
            return null;
        }

        Ray ray = new(camera.transform.position, camera.transform.forward);
        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maximumDistance,
                layerMask,
                QueryTriggerInteraction.UseGlobal))
        {
            return null;
        }

        IInteractable interactable = hit.collider.GetComponent<IInteractable>() ??
                                     hit.collider.GetComponentInParent<IInteractable>();
        return interactable != null && interactable.CanInteract() ? interactable : null;
    }
}
