using System;
using UnityEngine;

public interface ISceneStateApplier
{
    void ApplyCurrentState();
}

public readonly struct SceneStateApplyResult
{
    public SceneStateApplyResult(int appliedCount, int failedCount)
    {
        AppliedCount = appliedCount;
        FailedCount = failedCount;
    }

    public int AppliedCount { get; }
    public int FailedCount { get; }
}

public static class SceneStateRestoration
{
    public static SceneStateApplyResult ApplyAll()
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        int applied = 0;
        int failed = 0;

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not ISceneStateApplier applier)
            {
                continue;
            }

            try
            {
                applier.ApplyCurrentState();
                applied++;
            }
            catch (Exception exception)
            {
                failed++;
                Debug.LogError(
                    $"Scene state applier '{behaviour.GetType().Name}' failed on " +
                    $"'{behaviour.gameObject.name}': {exception.Message}",
                    behaviour);
            }
        }

        return new SceneStateApplyResult(applied, failed);
    }
}
