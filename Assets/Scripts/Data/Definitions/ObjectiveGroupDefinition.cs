using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Light Tower/Story/Objective Group",
    fileName = "ObjectiveGroup")]
public sealed class ObjectiveGroupDefinition : ScriptableObject
{
    [SerializeField] private string stableGroupId;
    [SerializeField] private string displayName;
    [SerializeField] private ObjectiveGroupCompletionMode completionMode =
        ObjectiveGroupCompletionMode.AllRequired;
    [SerializeField] private List<ObjectiveSpec> objectives = new();

    public string StableGroupId => stableGroupId;
    public string DisplayName => displayName;
    public IReadOnlyList<ObjectiveSpec> Objectives => objectives;

    public ObjectiveGroupSpec CreateSpec(string storyBeatId)
    {
        ObjectiveGroupSpec spec = new()
        {
            groupId = stableGroupId,
            storyBeatId = storyBeatId,
            displayName = displayName,
            completionMode = completionMode
        };

        foreach (ObjectiveSpec objective in objectives)
        {
            if (objective == null)
            {
                continue;
            }

            spec.objectives.Add(new ObjectiveSpec
            {
                objectiveId = objective.objectiveId,
                title = objective.title,
                description = objective.description,
                optional = objective.optional,
                hidden = objective.hidden
            });
        }

        return spec;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string groupId,
        string groupDisplayName,
        ObjectiveGroupCompletionMode mode,
        IEnumerable<ObjectiveSpec> objectiveDefinitions)
    {
        stableGroupId = groupId;
        displayName = groupDisplayName;
        completionMode = mode;
        objectives = objectiveDefinitions == null
            ? new List<ObjectiveSpec>()
            : new List<ObjectiveSpec>(objectiveDefinitions);
    }
#endif
}
