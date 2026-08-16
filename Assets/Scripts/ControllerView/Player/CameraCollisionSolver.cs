using System.Collections.Generic;
using UnityEngine;

public readonly struct CameraCollisionCastResult
{
    public CameraCollisionCastResult(float allowedDistance, Collider blockingCollider, float hitDistance)
    {
        AllowedDistance = allowedDistance;
        BlockingCollider = blockingCollider;
        HitDistance = hitDistance;
    }

    public float AllowedDistance { get; }
    public Collider BlockingCollider { get; }
    public float HitDistance { get; }
}

public sealed class CameraCollisionSolver
{
    public CameraCollisionCastResult Cast(
        Vector3 anchor,
        Vector3 direction,
        float desiredDistance,
        Quaternion cameraRotation,
        bool useNearPlaneVolume,
        Vector3 nearPlaneCenterOffset,
        Vector3 nearPlaneHalfExtents,
        float sphereRadius,
        float skinWidth,
        LayerMask collisionMask,
        RaycastHit[] hitBuffer,
        HashSet<Collider> ignoredColliders)
    {
        if (hitBuffer == null || hitBuffer.Length == 0 || desiredDistance <= 0f)
        {
            return new CameraCollisionCastResult(desiredDistance, null, float.PositiveInfinity);
        }

        int hitCount = useNearPlaneVolume
            ? Physics.BoxCastNonAlloc(
                anchor + nearPlaneCenterOffset,
                nearPlaneHalfExtents,
                direction,
                hitBuffer,
                cameraRotation,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore)
            : Physics.SphereCastNonAlloc(
                anchor,
                sphereRadius,
                direction,
                hitBuffer,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

        float allowedDistance = desiredDistance;
        float hitDistance = float.PositiveInfinity;
        Collider blockingCollider = null;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.collider == null || ignoredColliders?.Contains(hit.collider) == true)
            {
                continue;
            }

            float candidateDistance = Mathf.Max(0f, hit.distance - skinWidth);
            if (candidateDistance >= allowedDistance)
            {
                continue;
            }

            allowedDistance = candidateDistance;
            blockingCollider = hit.collider;
            hitDistance = hit.distance;
        }

        return new CameraCollisionCastResult(allowedDistance, blockingCollider, hitDistance);
    }
}
