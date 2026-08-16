

public struct TaskChangedEvent
{
    public string TaskId;
    public string Title;
    public string Description;
    public bool IsCompleted;
}
public struct DayChangedEvent
{
    public int Day;
}
 public struct StoryPhaseChangedEvent
{
    public StoryPhase OldPhase;
    public StoryPhase NewPhase;
    public StoryPhaseChangedEvent(StoryPhase oldPhase, StoryPhase newPhase)
    {
        OldPhase = oldPhase;
        NewPhase = newPhase;
    }
}
public struct InventoryChangedEvent
{
    public InventoryItemId[] Items;
    public InventoryItemId SelectedItem;
}

public struct InventoryItemAddedEvent
{
    public InventoryItemId ItemId;
}

public struct InventoryItemRemovedEvent
{
    public InventoryItemId ItemId;
}

public struct InventorySelectionChangedEvent
{
    public InventoryItemId ItemId;
}

public struct CommunicationDeviceAcquiredEvent { }
public struct TimeOfDayChangedEvent
{
    public float Hour;
    public float NormalizedDayProgress;
    public TimeOfDayPeriod Period;
}

public struct TimeOfDayPeriodChangedEvent
{
    public TimeOfDayPeriod OldPeriod;
    public TimeOfDayPeriod NewPeriod;
}

public struct BoatIntroStartedEvent { }
public struct ForestPathStartedEvent { }
public struct ApproachLighthouseStartedEvent { }
public struct LastForestSubtitleFinshedEvent { }
public struct CheckInStartedEvent { }
public struct RestStartedEvent { }
public struct Day1StartedEvent { }
public struct Day1GoDockStartedEvent { }
public struct Day1BridgeCollapsedEvent { }
public struct Day1ReturnRouteStartedEvent { }
public struct Day1NightDutyStartedEvent { }
public struct Day1CompletedEvent { }
public struct SmallAxeAcquiredEvent { }
public struct ChainsawAcquiredEvent { }

public readonly struct GameModeChangedEvent
{
    public GameModeChangedEvent(GameMode oldMode, GameMode newMode)
    {
        OldMode = oldMode;
        NewMode = newMode;
    }

    public GameMode OldMode { get; }
    public GameMode NewMode { get; }
}

public readonly struct StoryBeatEnteredEvent
{
    public StoryBeatEnteredEvent(StoryChapter chapter, string beatId, bool restored)
    {
        Chapter = chapter;
        BeatId = beatId;
        Restored = restored;
    }

    public StoryChapter Chapter { get; }
    public string BeatId { get; }
    public bool Restored { get; }
}

public readonly struct StoryBeatCompletedEvent
{
    public StoryBeatCompletedEvent(StoryChapter chapter, string beatId)
    {
        Chapter = chapter;
        BeatId = beatId;
    }

    public StoryChapter Chapter { get; }
    public string BeatId { get; }
}

public readonly struct WorldFactChangedEvent
{
    public WorldFactChangedEvent(string factId, bool enabled)
    {
        FactId = factId;
        Enabled = enabled;
    }

    public string FactId { get; }
    public bool Enabled { get; }
}

public readonly struct ObjectiveCompletedEvent
{
    public ObjectiveCompletedEvent(string groupId, string objectiveId)
    {
        GroupId = groupId;
        ObjectiveId = objectiveId;
    }

    public string GroupId { get; }
    public string ObjectiveId { get; }
}

public readonly struct ObjectiveGroupChangedEvent
{
    public ObjectiveGroupChangedEvent(ObjectiveGroupRuntimeState state)
    {
        State = state;
    }

    public ObjectiveGroupRuntimeState State { get; }
}

public readonly struct ObjectiveGroupCompletedEvent
{
    public ObjectiveGroupCompletedEvent(string groupId, string storyBeatId)
    {
        GroupId = groupId;
        StoryBeatId = storyBeatId;
    }

    public string GroupId { get; }
    public string StoryBeatId { get; }
}

public readonly struct GirlStoryStateChangedEvent
{
    public GirlStoryStateChangedEvent(GirlStoryState previous, GirlStoryState current)
    {
        Previous = previous;
        Current = current;
    }

    public GirlStoryState Previous { get; }
    public GirlStoryState Current { get; }
}

public readonly struct StoryBeatReadyEvent
{
    public StoryBeatReadyEvent(
        StoryChapter chapter,
        string beatId,
        string sequenceKey,
        string weatherProfileKey,
        bool restored)
    {
        Chapter = chapter;
        BeatId = beatId;
        SequenceKey = sequenceKey;
        WeatherProfileKey = weatherProfileKey;
        Restored = restored;
    }

    public StoryChapter Chapter { get; }
    public string BeatId { get; }
    public string SequenceKey { get; }
    public string WeatherProfileKey { get; }
    public bool Restored { get; }
}

public readonly struct StoryBeatCommittedEvent
{
    public StoryBeatCommittedEvent(
        StoryChapter chapter,
        string beatId,
        string source,
        bool createsCheckpoint,
        string checkpointId)
    {
        Chapter = chapter;
        BeatId = beatId;
        Source = source;
        CreatesCheckpoint = createsCheckpoint;
        CheckpointId = checkpointId;
    }

    public StoryChapter Chapter { get; }
    public string BeatId { get; }
    public string Source { get; }
    public bool CreatesCheckpoint { get; }
    public string CheckpointId { get; }
}

public readonly struct StoryDirectorErrorEvent
{
    public StoryDirectorErrorEvent(string message)
    {
        Message = message;
    }

    public string Message { get; }
}

public readonly struct CollectibleStateChangedEvent
{
    public CollectibleStateChangedEvent(string collectibleId, bool collected)
    {
        CollectibleId = collectibleId;
        Collected = collected;
    }

    public string CollectibleId { get; }
    public bool Collected { get; }
}

public readonly struct SaveGameWrittenEvent
{
    public SaveGameWrittenEvent(string path, string checkpointId, string beatId)
    {
        Path = path;
        CheckpointId = checkpointId;
        BeatId = beatId;
    }

    public string Path { get; }
    public string CheckpointId { get; }
    public string BeatId { get; }
}

public readonly struct SaveGameRestoredEvent
{
    public SaveGameRestoredEvent(
        string path,
        string checkpointId,
        string beatId,
        int appliedSceneStates,
        int failedSceneStates)
    {
        Path = path;
        CheckpointId = checkpointId;
        BeatId = beatId;
        AppliedSceneStates = appliedSceneStates;
        FailedSceneStates = failedSceneStates;
    }

    public string Path { get; }
    public string CheckpointId { get; }
    public string BeatId { get; }
    public int AppliedSceneStates { get; }
    public int FailedSceneStates { get; }
}

public readonly struct SaveGameErrorEvent
{
    public SaveGameErrorEvent(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
