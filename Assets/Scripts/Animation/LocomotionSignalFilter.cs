using UnityEngine;

public sealed class LocomotionSignalFilter
{
    public Vector3 SmoothVelocity(
        Vector3 currentVelocity,
        Vector3 targetVelocity,
        ref Vector3 smoothDampVelocity,
        float accelerationSmoothTime,
        float decelerationSmoothTime,
        float directionSmoothTime,
        float maximumAcceleration,
        float deltaTime)
    {
        float smoothTime = SelectSmoothTime(
            currentVelocity,
            targetVelocity,
            accelerationSmoothTime,
            decelerationSmoothTime,
            directionSmoothTime);
        float maxSpeed = maximumAcceleration > 0f ? maximumAcceleration : Mathf.Infinity;
        return Vector3.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref smoothDampVelocity,
            smoothTime,
            maxSpeed,
            deltaTime);
    }

    public bool ResolveMovementIntent(
        bool wasMoving,
        float targetSpeed,
        float startThreshold,
        float stopThreshold)
    {
        if (!wasMoving && targetSpeed >= startThreshold)
        {
            return true;
        }

        if (wasMoving && targetSpeed <= stopThreshold)
        {
            return false;
        }

        return wasMoving;
    }

    public float SelectSmoothTime(
        Vector3 currentVelocity,
        Vector3 targetVelocity,
        float accelerationSmoothTime,
        float decelerationSmoothTime,
        float directionSmoothTime)
    {
        float targetSpeed = new Vector2(targetVelocity.x, targetVelocity.z).magnitude;
        float currentSpeed = new Vector2(currentVelocity.x, currentVelocity.z).magnitude;
        if (targetSpeed < currentSpeed)
        {
            return decelerationSmoothTime;
        }

        Vector2 currentDirection = new(currentVelocity.x, currentVelocity.z);
        Vector2 targetDirection = new(targetVelocity.x, targetVelocity.z);
        if (currentDirection.sqrMagnitude > 0.001f &&
            targetDirection.sqrMagnitude > 0.001f &&
            Vector2.Angle(currentDirection, targetDirection) > 25f)
        {
            return directionSmoothTime;
        }

        return accelerationSmoothTime;
    }
}
