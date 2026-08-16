using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveDataVersion
{
    public const int Current = 1;
}

[Serializable]
public sealed class GameSaveData
{
    public int saveVersion = SaveDataVersion.Current;
    public string savedAtUtc = string.Empty;
    public string currentChapter = string.Empty;
    public string currentBeatId = string.Empty;
    public List<string> completedBeatIds = new();
    public string lastCheckpointId = string.Empty;
    public List<string> worldFactIds = new();
    public List<ObjectiveGroupSaveData> objectiveGroups = new();
    public string activeObjectiveGroupId = string.Empty;
    public InventorySaveData inventory = new();
    public ToolInventorySaveData toolInventory = new();
    public LighthouseDutySaveData lighthouseDuty = new();
    public TimeOfDaySaveData timeOfDay = new();
    public List<string> collectibleIds = new();
    public PlayerCheckpointSaveData playerCheckpoint = new();
    public string girlState = nameof(GirlStoryState.Hidden);
}

[Serializable]
public sealed class ObjectiveGroupSaveData
{
    public string groupId = string.Empty;
    public string storyBeatId = string.Empty;
    public string displayName = string.Empty;
    public string completionMode = nameof(ObjectiveGroupCompletionMode.AllRequired);
    public bool completed;
    public List<ObjectiveSaveData> objectives = new();

    public static ObjectiveGroupSaveData FromRuntime(ObjectiveGroupRuntimeState state)
    {
        ObjectiveGroupSaveData result = new()
        {
            groupId = state?.groupId ?? string.Empty,
            storyBeatId = state?.storyBeatId ?? string.Empty,
            displayName = state?.displayName ?? string.Empty,
            completionMode = (state?.completionMode ?? ObjectiveGroupCompletionMode.AllRequired).ToString(),
            completed = state != null && state.completed
        };

        if (state?.objectives != null)
        {
            foreach (ObjectiveRuntimeState objective in state.objectives)
            {
                if (objective != null)
                {
                    result.objectives.Add(ObjectiveSaveData.FromRuntime(objective));
                }
            }
        }

        return result;
    }

    public ObjectiveGroupRuntimeState ToRuntime()
    {
        Enum.TryParse(completionMode, true, out ObjectiveGroupCompletionMode parsedMode);
        ObjectiveGroupRuntimeState result = new()
        {
            groupId = Normalize(groupId),
            storyBeatId = Normalize(storyBeatId),
            displayName = displayName ?? string.Empty,
            completionMode = parsedMode,
            completed = completed
        };

        if (objectives != null)
        {
            foreach (ObjectiveSaveData objective in objectives)
            {
                if (objective != null && !string.IsNullOrWhiteSpace(objective.objectiveId))
                {
                    result.objectives.Add(objective.ToRuntime());
                }
            }
        }

        result.completed = ObjectiveGroupModel.EvaluateCompletion(result);
        return result;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

[Serializable]
public sealed class ObjectiveSaveData
{
    public string objectiveId = string.Empty;
    public string title = string.Empty;
    public string description = string.Empty;
    public bool optional;
    public bool hidden;
    public bool completed;

    public static ObjectiveSaveData FromRuntime(ObjectiveRuntimeState state)
    {
        return new ObjectiveSaveData
        {
            objectiveId = state.objectiveId ?? string.Empty,
            title = state.title ?? string.Empty,
            description = state.description ?? string.Empty,
            optional = state.optional,
            hidden = state.hidden,
            completed = state.completed
        };
    }

    public ObjectiveRuntimeState ToRuntime()
    {
        return new ObjectiveRuntimeState
        {
            objectiveId = objectiveId.Trim(),
            title = title ?? string.Empty,
            description = description ?? string.Empty,
            optional = optional,
            hidden = hidden,
            completed = completed
        };
    }
}

[Serializable]
public sealed class InventorySaveData
{
    public List<string> itemIds = new();
    public string selectedItemId = nameof(InventoryItemId.None);
}

[Serializable]
public sealed class ToolInventorySaveData
{
    public bool hasSmallAxe;
    public bool hasChainsaw;
}

[Serializable]
public sealed class LighthouseDutySaveData
{
    public bool generatorChecked;
    public bool lampRoomChecked;
    public bool lensChecked;
    public bool lightActivated;
    public bool beamSweepCompleted;
}

[Serializable]
public sealed class TimeOfDaySaveData
{
    public float currentHour = 6f;
}

[Serializable]
public sealed class PlayerCheckpointSaveData
{
    public bool hasTransform;
    public string spawnId = string.Empty;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW = 1f;

    public Vector3 Position => new(positionX, positionY, positionZ);
    public Quaternion Rotation => new(rotationX, rotationY, rotationZ, rotationW);

    public void SetTransform(Transform source)
    {
        if (source == null)
        {
            hasTransform = false;
            return;
        }

        hasTransform = true;
        Vector3 position = source.position;
        Quaternion rotation = source.rotation;
        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;
        rotationX = rotation.x;
        rotationY = rotation.y;
        rotationZ = rotation.z;
        rotationW = rotation.w;
    }
}

public static class GameSaveSerializer
{
    public static string Serialize(GameSaveData data, bool prettyPrint = true)
    {
        if (!TryNormalize(data, out string error))
        {
            throw new ArgumentException(error, nameof(data));
        }

        return JsonUtility.ToJson(data, prettyPrint);
    }

    public static bool TryDeserialize(string json, out GameSaveData data, out string error)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Save file is empty.";
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception exception)
        {
            error = $"Save JSON is invalid: {exception.Message}";
            return false;
        }

        return SaveDataMigration.TryMigrateToCurrent(data, out error) &&
               TryNormalize(data, out error);
    }

    public static bool TryNormalize(GameSaveData data, out string error)
    {
        if (data == null)
        {
            error = "Save data is null.";
            return false;
        }

        if (data.saveVersion != SaveDataVersion.Current)
        {
            error = $"Unsupported save version {data.saveVersion}; expected {SaveDataVersion.Current}.";
            return false;
        }

        if (!Enum.TryParse(data.currentChapter, true, out StoryChapter chapter) ||
            chapter == StoryChapter.None || string.IsNullOrWhiteSpace(data.currentBeatId))
        {
            error = "Save data has no valid current Chapter/Beat.";
            return false;
        }

        data.currentChapter = chapter.ToString();
        data.currentBeatId = data.currentBeatId.Trim();
        data.lastCheckpointId = Normalize(data.lastCheckpointId);
        data.completedBeatIds ??= new List<string>();
        data.worldFactIds ??= new List<string>();
        data.objectiveGroups ??= new List<ObjectiveGroupSaveData>();
        data.inventory ??= new InventorySaveData();
        data.inventory.itemIds ??= new List<string>();
        data.toolInventory ??= new ToolInventorySaveData();
        data.lighthouseDuty ??= new LighthouseDutySaveData();
        data.timeOfDay ??= new TimeOfDaySaveData();
        data.collectibleIds ??= new List<string>();
        data.playerCheckpoint ??= new PlayerCheckpointSaveData();
        data.activeObjectiveGroupId = Normalize(data.activeObjectiveGroupId);
        data.girlState = Normalize(data.girlState);
        error = string.Empty;
        return true;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

public static class SaveDataMigration
{
    public static bool TryMigrateToCurrent(GameSaveData data, out string error)
    {
        if (data == null)
        {
            error = "Save data is null.";
            return false;
        }

        if (data.saveVersion > SaveDataVersion.Current)
        {
            error = $"Save version {data.saveVersion} is newer than this game supports.";
            return false;
        }

        if (data.saveVersion < 1)
        {
            error = $"Save version {data.saveVersion} predates the supported migration range.";
            return false;
        }

        // Add explicit version-to-version migration steps here when version 2 is introduced.
        error = string.Empty;
        return true;
    }
}
