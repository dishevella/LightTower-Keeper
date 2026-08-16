public readonly struct CharacterActionProgressDecision
{
    public CharacterActionProgressDecision(bool commitReached, bool interruptible)
    {
        CommitReached = commitReached;
        Interruptible = interruptible;
    }

    public bool CommitReached { get; }
    public bool Interruptible { get; }
}

public sealed class CharacterAnimationActionCoordinator
{
    public bool CanInterrupt(
        CharacterAnimationLogicalState currentState,
        CharacterAnimationPriority currentPriority,
        CharacterAnimationPriority requestedPriority,
        bool isInterruptible,
        bool hasActiveAction)
    {
        if (currentState == CharacterAnimationLogicalState.Dead)
        {
            return false;
        }

        if (requestedPriority == CharacterAnimationPriority.Death)
        {
            return true;
        }

        if (currentState == CharacterAnimationLogicalState.Parkour ||
            currentState == CharacterAnimationLogicalState.Airborne ||
            currentState == CharacterAnimationLogicalState.Landing)
        {
            return isInterruptible && (int)requestedPriority >= (int)currentPriority;
        }

        if (!hasActiveAction)
        {
            return true;
        }

        if ((int)requestedPriority > (int)currentPriority)
        {
            return true;
        }

        return isInterruptible && (int)requestedPriority >= (int)currentPriority;
    }

    public CharacterActionProgressDecision EvaluateProgress(
        float normalizedTime,
        float commitNormalizedTime,
        float interruptibleNormalizedTime,
        bool actionAllowsInterrupt,
        bool hasCommitted,
        bool isInterruptible)
    {
        bool commitReached = !hasCommitted && normalizedTime >= commitNormalizedTime;
        bool resolvedInterruptible = isInterruptible ||
                                     (actionAllowsInterrupt &&
                                      normalizedTime >= interruptibleNormalizedTime);
        return new CharacterActionProgressDecision(commitReached, resolvedInterruptible);
    }
}
