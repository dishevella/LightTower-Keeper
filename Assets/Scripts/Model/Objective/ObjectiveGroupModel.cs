using System;
using System.Collections.Generic;

[Serializable]
public sealed class ObjectiveGroupModel : ModelAbstract
{
    private readonly Dictionary<string, ObjectiveGroupRuntimeState> groups =
        new(StringComparer.Ordinal);

    public BindableProperty<string> ActiveGroupId { get; private set; }
    public IReadOnlyDictionary<string, ObjectiveGroupRuntimeState> Groups => groups;

    protected override void OnInit()
    {
        groups.Clear();
        ActiveGroupId = new BindableProperty<string>(string.Empty);
    }

    public ObjectiveGroupRuntimeState StartOrReplace(ObjectiveGroupSpec spec)
    {
        if (spec == null || string.IsNullOrWhiteSpace(spec.groupId))
        {
            return null;
        }

        ObjectiveGroupRuntimeState state = new()
        {
            groupId = spec.groupId.Trim(),
            storyBeatId = Normalize(spec.storyBeatId),
            displayName = Normalize(spec.displayName),
            completionMode = spec.completionMode,
            completed = false
        };

        HashSet<string> seenIds = new(StringComparer.Ordinal);
        if (spec.objectives != null)
        {
            foreach (ObjectiveSpec objective in spec.objectives)
            {
                if (objective == null || string.IsNullOrWhiteSpace(objective.objectiveId))
                {
                    continue;
                }

                string objectiveId = objective.objectiveId.Trim();
                if (!seenIds.Add(objectiveId))
                {
                    continue;
                }

                state.objectives.Add(new ObjectiveRuntimeState
                {
                    objectiveId = objectiveId,
                    title = Normalize(objective.title),
                    description = Normalize(objective.description),
                    optional = objective.optional,
                    hidden = objective.hidden,
                    completed = false
                });
            }
        }

        state.completed = EvaluateCompletion(state);
        groups[state.groupId] = state;
        ActiveGroupId.Value = state.groupId;
        return state;
    }

    public bool TryGetGroup(string groupId, out ObjectiveGroupRuntimeState state)
    {
        return groups.TryGetValue(Normalize(groupId), out state);
    }

    public bool CompleteObjective(string groupId, string objectiveId, out bool groupJustCompleted)
    {
        groupJustCompleted = false;
        if (!TryGetGroup(groupId, out ObjectiveGroupRuntimeState state) ||
            string.IsNullOrWhiteSpace(objectiveId))
        {
            return false;
        }

        string stableObjectiveId = objectiveId.Trim();
        ObjectiveRuntimeState objective = state.objectives.Find(
            candidate => string.Equals(candidate.objectiveId, stableObjectiveId, StringComparison.Ordinal));
        if (objective == null || objective.completed)
        {
            return false;
        }

        objective.completed = true;
        bool wasCompleted = state.completed;
        state.completed = EvaluateCompletion(state);
        groupJustCompleted = !wasCompleted && state.completed;
        return true;
    }

    public void ClearActiveGroup()
    {
        ActiveGroupId.Value = string.Empty;
    }

    public void ClearAll()
    {
        groups.Clear();
        ActiveGroupId.Value = string.Empty;
    }

    public List<ObjectiveGroupRuntimeState> CreateSnapshot()
    {
        List<ObjectiveGroupRuntimeState> result = new(groups.Count);
        foreach (ObjectiveGroupRuntimeState state in groups.Values)
        {
            result.Add(state.Clone());
        }

        return result;
    }

    public void Restore(IEnumerable<ObjectiveGroupRuntimeState> restoredGroups, string activeGroupId)
    {
        groups.Clear();
        if (restoredGroups != null)
        {
            foreach (ObjectiveGroupRuntimeState state in restoredGroups)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.groupId))
                {
                    continue;
                }

                ObjectiveGroupRuntimeState clone = state.Clone();
                clone.groupId = clone.groupId.Trim();
                clone.completed = EvaluateCompletion(clone);
                groups[clone.groupId] = clone;
            }
        }

        string normalizedActiveId = Normalize(activeGroupId);
        ActiveGroupId.SetValueWithoutNotify(
            groups.ContainsKey(normalizedActiveId) ? normalizedActiveId : string.Empty);
    }

    public static bool EvaluateCompletion(ObjectiveGroupRuntimeState state)
    {
        if (state == null || state.objectives == null || state.objectives.Count == 0)
        {
            return false;
        }

        if (state.completionMode == ObjectiveGroupCompletionMode.AnyObjective)
        {
            return state.objectives.Exists(objective => objective.completed);
        }

        bool hasRequired = false;
        foreach (ObjectiveRuntimeState objective in state.objectives)
        {
            if (objective.optional)
            {
                continue;
            }

            hasRequired = true;
            if (!objective.completed)
            {
                return false;
            }
        }

        return hasRequired;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
