using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class SaveSystem : SystemAbstract
{
    public const string DefaultSlotFileName = "slot0.json";

    private StoryProgressModel storyProgress;
    private WorldStateModel worldState;
    private ObjectiveGroupModel objectives;
    private InventoryModel inventory;
    private ToolInventoryModel toolInventory;
    private LighthouseDutyModel lighthouseDuty;
    private TimeOfDayModel timeOfDay;
    private CollectibleStateModel collectibles;
    private GirlStoryStateModel girlStory;

    public string SaveDirectory => Path.Combine(
        Application.persistentDataPath,
        "LightTower",
        "Saves");

    public string DefaultSlotPath => Path.Combine(SaveDirectory, DefaultSlotFileName);
    public bool HasDefaultSave => File.Exists(DefaultSlotPath) || File.Exists(GetBackupPath(DefaultSlotPath));

    protected override void OnInit()
    {
        storyProgress = this.GetModel<StoryProgressModel>();
        worldState = this.GetModel<WorldStateModel>();
        objectives = this.GetModel<ObjectiveGroupModel>();
        inventory = this.GetModel<InventoryModel>();
        toolInventory = this.GetModel<ToolInventoryModel>();
        lighthouseDuty = this.GetModel<LighthouseDutyModel>();
        timeOfDay = this.GetModel<TimeOfDayModel>();
        collectibles = this.GetModel<CollectibleStateModel>();
        girlStory = this.GetModel<GirlStoryStateModel>();
    }

    public GameSaveData CreateSnapshot()
    {
        GameSaveData data = new()
        {
            saveVersion = SaveDataVersion.Current,
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            currentChapter = storyProgress?.CurrentChapter.Value.ToString() ?? string.Empty,
            currentBeatId = storyProgress?.CurrentBeatId.Value ?? string.Empty,
            lastCheckpointId = storyProgress?.LastCheckpointId.Value ?? string.Empty,
            activeObjectiveGroupId = objectives?.ActiveGroupId.Value ?? string.Empty,
            girlState = girlStory?.CurrentState.Value.ToString() ?? nameof(GirlStoryState.Hidden)
        };

        AddStrings(data.completedBeatIds, storyProgress?.CompletedBeats);
        AddStrings(data.worldFactIds, worldState?.Facts);
        AddStrings(data.collectibleIds, collectibles?.CollectedIds);

        if (objectives != null)
        {
            foreach (ObjectiveGroupRuntimeState state in objectives.CreateSnapshot())
            {
                data.objectiveGroups.Add(ObjectiveGroupSaveData.FromRuntime(state));
            }
        }

        if (inventory != null)
        {
            foreach (InventoryItemId itemId in inventory.Items)
            {
                data.inventory.itemIds.Add(itemId.ToString());
            }

            data.inventory.selectedItemId = inventory.SelectedItem.Value.ToString();
        }

        if (toolInventory != null)
        {
            data.toolInventory.hasSmallAxe = toolInventory.HasSmallAxe.Value;
            data.toolInventory.hasChainsaw = toolInventory.HasChainsaw.Value;
        }

        if (lighthouseDuty != null)
        {
            data.lighthouseDuty.generatorChecked = lighthouseDuty.GeneratorChecked.Value;
            data.lighthouseDuty.lampRoomChecked = lighthouseDuty.LampRoomChecked.Value;
            data.lighthouseDuty.lensChecked = lighthouseDuty.LensChecked.Value;
            data.lighthouseDuty.lightActivated = lighthouseDuty.LightActivated.Value;
            data.lighthouseDuty.beamSweepCompleted = lighthouseDuty.BeamSweepCompleted.Value;
        }

        data.timeOfDay.currentHour = timeOfDay?.CurrentHour.Value ?? 6f;
        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        data.playerCheckpoint.SetTransform(player != null ? player.transform : null);
        return data;
    }

    public bool SaveCheckpoint(string checkpointId)
    {
        if (storyProgress == null || string.IsNullOrWhiteSpace(checkpointId))
        {
            return Fail("Cannot save a checkpoint without initialized story progress and a stable checkpoint ID.");
        }

        string previousCheckpoint = storyProgress.LastCheckpointId.Value;
        storyProgress.SetCheckpoint(checkpointId);
        GameSaveData data = CreateSnapshot();
        if (!TryWriteSnapshot(DefaultSlotPath, data, out string error))
        {
            storyProgress.SetCheckpoint(previousCheckpoint);
            return Fail(error);
        }

        this.GetEvent().Send(new SaveGameWrittenEvent(
            DefaultSlotPath,
            data.lastCheckpointId,
            data.currentBeatId));
        return true;
    }

    public bool TryLoadDefaultSlot()
    {
        if (!TryReadSnapshotWithBackup(
                DefaultSlotPath,
                out GameSaveData data,
                out string error,
                out _))
        {
            return Fail(error, false);
        }

        return RestoreSnapshot(data);
    }

    public bool RestoreSnapshot(
        GameSaveData data,
        bool applySceneState = true,
        bool placePlayer = true)
    {
        if (!GameSaveSerializer.TryNormalize(data, out string error))
        {
            return Fail(error);
        }

        if (!Enum.TryParse(data.currentChapter, true, out StoryChapter chapter))
        {
            return Fail($"Save chapter '{data.currentChapter}' is invalid.");
        }

        StoryDirectorSystem director = this.GetSystem<StoryDirectorSystem>();
        StoryBeatDefinition beat = director?.Catalog?.FindBeat(data.currentBeatId);
        if (beat == null || beat.Chapter != chapter)
        {
            return Fail(
                $"Save beat '{data.currentBeatId}' does not exist in chapter '{chapter}'.");
        }

        this.GetSystem<GameModeSystem>()?.SetMode(GameMode.Loading);
        this.GetSystem<StorySystem>()?.RestoreProgress(
            chapter,
            data.currentBeatId,
            data.completedBeatIds,
            data.lastCheckpointId);
        this.GetSystem<WorldStateSystem>()?.Restore(data.worldFactIds);

        List<ObjectiveGroupRuntimeState> restoredGroups = new();
        foreach (ObjectiveGroupSaveData savedGroup in data.objectiveGroups)
        {
            if (savedGroup != null)
            {
                ObjectiveGroupRuntimeState runtimeGroup = savedGroup.ToRuntime();
                if (!string.IsNullOrWhiteSpace(runtimeGroup.groupId))
                {
                    restoredGroups.Add(runtimeGroup);
                }
            }
        }

        this.GetSystem<ObjectiveSystem>()?.Restore(restoredGroups, data.activeObjectiveGroupId);
        this.GetSystem<InventorySystem>()?.Restore(
            ParseInventoryItems(data.inventory.itemIds),
            ParseInventoryItem(data.inventory.selectedItemId));
        toolInventory?.Restore(
            data.toolInventory.hasSmallAxe,
            data.toolInventory.hasChainsaw);
        lighthouseDuty?.Restore(
            data.lighthouseDuty.generatorChecked,
            data.lighthouseDuty.lampRoomChecked,
            data.lighthouseDuty.lensChecked,
            data.lighthouseDuty.lightActivated,
            data.lighthouseDuty.beamSweepCompleted);
        this.GetSystem<CollectibleStateSystem>()?.Restore(data.collectibleIds);

        GirlStoryState restoredGirlState = Enum.TryParse(data.girlState, true, out GirlStoryState parsedGirlState)
            ? parsedGirlState
            : GirlStoryState.Hidden;
        girlStory?.Restore(restoredGirlState);
        this.GetSystem<TimeSystem>()?.SetTime(data.timeOfDay.currentHour);

        SceneStateApplyResult initialApply = applySceneState
            ? SceneStateRestoration.ApplyAll()
            : default;
        if (placePlayer)
        {
            PlacePlayerAtCheckpoint(data.playerCheckpoint);
        }

        if (director == null || !director.ResumeRestoredBeat())
        {
            return Fail($"Story Director could not resume beat '{data.currentBeatId}'.");
        }

        SceneStateApplyResult finalApply = applySceneState
            ? SceneStateRestoration.ApplyAll()
            : default;
        this.GetSystem<GameModeSystem>()?.SetMode(GameMode.Gameplay);
        this.GetEvent().Send(new SaveGameRestoredEvent(
            DefaultSlotPath,
            data.lastCheckpointId,
            data.currentBeatId,
            initialApply.AppliedCount + finalApply.AppliedCount,
            initialApply.FailedCount + finalApply.FailedCount));
        return true;
    }

    public bool DeleteDefaultSave(bool logMissing = false)
    {
        bool deleted = DeleteIfPresent(DefaultSlotPath);
        deleted |= DeleteIfPresent(GetBackupPath(DefaultSlotPath));
        deleted |= DeleteIfPresent(GetTemporaryPath(DefaultSlotPath));
        if (!deleted && logMissing)
        {
            Debug.Log("No default save slot exists to delete.");
        }

        return deleted;
    }

    public static bool TryReadSnapshotWithBackup(
        string path,
        out GameSaveData data,
        out string error,
        out bool loadedBackup)
    {
        loadedBackup = false;
        if (TryRead(path, out data, out error))
        {
            return true;
        }

        string primaryError = error;
        string backupPath = GetBackupPath(path);
        if (TryRead(backupPath, out data, out error))
        {
            loadedBackup = true;
            Debug.LogWarning(
                $"Primary save could not be read ({primaryError}). Loaded backup '{backupPath}'.");
            return true;
        }

        error = $"No valid save is available. Primary: {primaryError} Backup: {error}";
        return false;
    }

    private static bool TryRead(string path, out GameSaveData data, out string error)
    {
        data = null;
        if (!File.Exists(path))
        {
            error = $"Save file '{path}' does not exist.";
            return false;
        }

        try
        {
            return GameSaveSerializer.TryDeserialize(File.ReadAllText(path), out data, out error);
        }
        catch (Exception exception)
        {
            error = $"Could not read save '{path}': {exception.Message}";
            return false;
        }
    }

    public static bool TryWriteSnapshot(string path, GameSaveData data, out string error)
    {
        string temporaryPath = GetTemporaryPath(path);
        string backupPath = GetBackupPath(path);
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = GameSaveSerializer.Serialize(data);
            using (FileStream stream = new(
                       temporaryPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            using (StreamWriter writer = new(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temporaryPath, path, backupPath, true);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(path, backupPath, true);
                    File.Delete(path);
                    File.Move(temporaryPath, path);
                }
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            DeleteIfPresent(temporaryPath);
            error = $"Could not write save '{path}': {exception.Message}";
            return false;
        }
    }

    private static void PlacePlayerAtCheckpoint(PlayerCheckpointSaveData checkpoint)
    {
        if (checkpoint == null || !checkpoint.hasTransform)
        {
            return;
        }

        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        player?.TeleportToCheckpoint(checkpoint.Position, checkpoint.Rotation);
    }

    private static List<InventoryItemId> ParseInventoryItems(IEnumerable<string> itemIds)
    {
        List<InventoryItemId> result = new();
        if (itemIds == null) return result;

        foreach (string itemId in itemIds)
        {
            InventoryItemId parsed = ParseInventoryItem(itemId);
            if (parsed != InventoryItemId.None && !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static InventoryItemId ParseInventoryItem(string itemId)
    {
        return Enum.TryParse(itemId, true, out InventoryItemId parsed) &&
               Enum.IsDefined(typeof(InventoryItemId), parsed)
            ? parsed
            : InventoryItemId.None;
    }

    private static void AddStrings(ICollection<string> destination, IEnumerable<string> source)
    {
        if (source == null) return;
        foreach (string value in source)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                destination.Add(value.Trim());
            }
        }
    }

    private bool Fail(string message, bool logAsError = true)
    {
        if (logAsError)
        {
            Debug.LogError($"[SaveSystem] {message}");
        }
        else
        {
            Debug.LogWarning($"[SaveSystem] {message}");
        }

        this.GetEvent().Send(new SaveGameErrorEvent(message));
        return false;
    }

    private static string GetTemporaryPath(string path) => path + ".tmp";
    private static string GetBackupPath(string path) => path + ".bak";

    private static bool DeleteIfPresent(string path)
    {
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
}
