using System;
using System.Collections.Generic;
using System.Text;

public sealed class ObjectiveSystem : SystemAbstract
{
    public const string LegacyGroupId = "legacy.current_task";

    private ObjectiveGroupModel model;

    protected override void OnInit()
    {
        model = this.GetModel<ObjectiveGroupModel>();
    }

    public ObjectiveGroupRuntimeState StartGroup(ObjectiveGroupSpec spec)
    {
        return StartGroupInternal(spec, true);
    }

    public ObjectiveGroupRuntimeState StartGroup(ObjectiveGroupDefinition definition, string storyBeatId)
    {
        return definition == null ? null : StartGroup(definition.CreateSpec(storyBeatId));
    }

    public bool CompleteObjective(string groupId, string objectiveId)
    {
        if (model == null ||
            !model.CompleteObjective(groupId, objectiveId, out bool groupJustCompleted) ||
            !model.TryGetGroup(groupId, out ObjectiveGroupRuntimeState group))
        {
            return false;
        }

        this.GetEvent().Send(new ObjectiveCompletedEvent(group.groupId, objectiveId.Trim()));
        this.GetEvent().Send(new ObjectiveGroupChangedEvent(group.Clone()));

        if (groupJustCompleted)
        {
            this.GetEvent().Send(new ObjectiveGroupCompletedEvent(group.groupId, group.storyBeatId));
        }

        SyncLegacyTaskPresentation(group);
        return true;
    }

    public bool IsGroupComplete(string groupId)
    {
        return model != null &&
               model.TryGetGroup(groupId, out ObjectiveGroupRuntimeState state) &&
               state.completed;
    }

    public bool CompleteAllRequired(string groupId)
    {
        if (model == null || !model.TryGetGroup(groupId, out ObjectiveGroupRuntimeState group))
        {
            return false;
        }

        bool changed = false;
        List<string> requiredIds = new();
        foreach (ObjectiveRuntimeState objective in group.objectives)
        {
            if (!objective.optional && !objective.completed)
            {
                requiredIds.Add(objective.objectiveId);
            }
        }

        foreach (string objectiveId in requiredIds)
        {
            changed |= CompleteObjective(groupId, objectiveId);
        }

        return changed || group.completed;
    }

    public void MirrorLegacyTask(string taskId, string title, string description)
    {
        string objectiveId = string.IsNullOrWhiteSpace(taskId) ? "legacy.objective" : taskId.Trim();
        ObjectiveGroupSpec spec = new()
        {
            groupId = LegacyGroupId,
            displayName = title,
            completionMode = ObjectiveGroupCompletionMode.AllRequired,
            objectives = new List<ObjectiveSpec>
            {
                new()
                {
                    objectiveId = objectiveId,
                    title = title,
                    description = description
                }
            }
        };

        StartGroupInternal(spec, false);
    }

    public void MirrorLegacyCompletion(string taskId)
    {
        if (model == null || !model.TryGetGroup(LegacyGroupId, out ObjectiveGroupRuntimeState group))
        {
            return;
        }

        string objectiveId = string.IsNullOrWhiteSpace(taskId)
            ? group.objectives.Count > 0 ? group.objectives[0].objectiveId : string.Empty
            : taskId.Trim();
        CompleteObjective(LegacyGroupId, objectiveId);
    }

    public void ClearLegacyTask()
    {
        if (model != null && model.ActiveGroupId.Value == LegacyGroupId)
        {
            model.ClearActiveGroup();
        }
    }

    public ObjectiveGroupRuntimeState GetActiveGroup()
    {
        if (model == null || string.IsNullOrWhiteSpace(model.ActiveGroupId.Value))
        {
            return null;
        }

        return model.TryGetGroup(model.ActiveGroupId.Value, out ObjectiveGroupRuntimeState state)
            ? state
            : null;
    }

    public void Restore(IEnumerable<ObjectiveGroupRuntimeState> groups, string activeGroupId)
    {
        model?.Restore(groups, activeGroupId);
        ObjectiveGroupRuntimeState active = GetActiveGroup();
        if (active != null)
        {
            SyncLegacyTaskPresentation(active);
        }
    }

    private ObjectiveGroupRuntimeState StartGroupInternal(ObjectiveGroupSpec spec, bool syncLegacyPresentation)
    {
        ObjectiveGroupRuntimeState state = model?.StartOrReplace(spec);
        if (state == null)
        {
            return null;
        }

        this.GetEvent().Send(new ObjectiveGroupChangedEvent(state.Clone()));
        if (syncLegacyPresentation)
        {
            SyncLegacyTaskPresentation(state);
        }

        return state;
    }

    private void SyncLegacyTaskPresentation(ObjectiveGroupRuntimeState group)
    {
        TaskSystem taskSystem = this.GetSystem<TaskSystem>();
        if (taskSystem == null || group == null)
        {
            return;
        }

        StringBuilder description = new();
        foreach (ObjectiveRuntimeState objective in group.objectives)
        {
            if (objective.hidden)
            {
                continue;
            }

            if (description.Length > 0)
            {
                description.AppendLine();
            }

            description.Append(objective.completed ? "[x] " : "[ ] ");
            description.Append(string.IsNullOrWhiteSpace(objective.title)
                ? objective.description
                : objective.title);
            if (objective.optional)
            {
                description.Append(" (Optional)");
            }
        }

        taskSystem.SetObjectiveGroupPresentation(
            group.groupId,
            group.displayName,
            description.ToString(),
            group.completed,
            true);
    }
}
