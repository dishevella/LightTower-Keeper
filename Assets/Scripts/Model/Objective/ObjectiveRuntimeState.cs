using System;
using System.Collections.Generic;
using System.Linq;

public enum ObjectiveGroupCompletionMode
{
    AllRequired = 0,
    AnyObjective = 1
}

[Serializable]
public sealed class ObjectiveSpec
{
    public string objectiveId;
    public string title;
    public string description;
    public bool optional;
    public bool hidden;
}

[Serializable]
public sealed class ObjectiveGroupSpec
{
    public string groupId;
    public string storyBeatId;
    public string displayName;
    public ObjectiveGroupCompletionMode completionMode = ObjectiveGroupCompletionMode.AllRequired;
    public List<ObjectiveSpec> objectives = new();
}

[Serializable]
public sealed class ObjectiveRuntimeState
{
    public string objectiveId;
    public string title;
    public string description;
    public bool optional;
    public bool hidden;
    public bool completed;

    public ObjectiveRuntimeState Clone()
    {
        return (ObjectiveRuntimeState)MemberwiseClone();
    }
}

[Serializable]
public sealed class ObjectiveGroupRuntimeState
{
    public string groupId;
    public string storyBeatId;
    public string displayName;
    public ObjectiveGroupCompletionMode completionMode;
    public bool completed;
    public List<ObjectiveRuntimeState> objectives = new();

    public ObjectiveGroupRuntimeState Clone()
    {
        return new ObjectiveGroupRuntimeState
        {
            groupId = groupId,
            storyBeatId = storyBeatId,
            displayName = displayName,
            completionMode = completionMode,
            completed = completed,
            objectives = objectives.Select(objective => objective.Clone()).ToList()
        };
    }
}
