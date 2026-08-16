using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StoryDirectorSystem : SystemAbstract
{
    private StoryCatalog catalog;
    private StoryProgressModel progress;
    private StorySystem storySystem;
    private WorldStateSystem worldState;
    private ObjectiveSystem objectives;
    private TimeSystem timeSystem;
    private GameModeSystem gameMode;
    private GameFlowSystem legacyFlow;
    private GirlStorySystem girlStory;

    public StoryCatalog Catalog => catalog;

    protected override void OnInit()
    {
        progress = this.GetModel<StoryProgressModel>();
        storySystem = this.GetSystem<StorySystem>();
        worldState = this.GetSystem<WorldStateSystem>();
        objectives = this.GetSystem<ObjectiveSystem>();
        timeSystem = this.GetSystem<TimeSystem>();
        gameMode = this.GetSystem<GameModeSystem>();
        legacyFlow = this.GetSystem<GameFlowSystem>();
        girlStory = this.GetSystem<GirlStorySystem>();
        catalog = Resources.Load<StoryCatalog>(StoryCatalog.ResourcesPath);
    }

    public bool StartNewGame()
    {
        if (!EnsureCatalog())
        {
            return false;
        }

        this.GetSystem<SaveSystem>()?.DeleteDefaultSave();
        progress?.ResetProgress();
        worldState?.Clear();
        this.GetModel<ObjectiveGroupModel>()?.ClearAll();
        this.GetModel<CollectibleStateModel>()?.Restore(null);
        this.GetSystem<InventorySystem>()?.Clear();
        this.GetModel<ToolInventoryModel>()?.Restore(false, false);
        this.GetModel<LighthouseDutyModel>()?.Restore(false, false, false, false, false);
        this.GetModel<GirlStoryStateModel>()?.Restore(GirlStoryState.Hidden);
        this.GetSystem<TaskSystem>()?.ClearTask();
        gameMode?.SetMode(GameMode.Loading);
        return EnterBeat(StoryBeatIds.Day0.ArriveIsland, false, true);
    }

    public bool RequestLegacyTransition(StoryPhase targetPhase)
    {
        if (!LegacyStoryPhaseMap.TryGetBeat(targetPhase, out StoryChapter targetChapter, out string targetBeatId))
        {
            return Fail($"Legacy phase '{targetPhase}' has no Chapter/Beat mapping.");
        }

        if (progress == null || string.IsNullOrWhiteSpace(progress.CurrentBeatId.Value))
        {
            return EnterBeat(targetBeatId, false, true);
        }

        if (string.Equals(progress.CurrentBeatId.Value, targetBeatId, StringComparison.Ordinal))
        {
            return false;
        }

        StoryBeatDefinition current = FindBeat(progress.CurrentBeatId.Value);
        if (current == null)
        {
            return false;
        }

        if (!IsAllowedLegacyTarget(current, targetBeatId))
        {
            return Fail(
                $"Rejected legacy transition '{current.StableBeatId}' -> '{targetBeatId}'. " +
                $"Canonical next beat is '{current.NextBeatId}' and the target is not in its migration whitelist.");
        }

        CompleteLegacyObjectiveAdapter(current);
        return CompleteAndEnter(current, targetChapter, targetBeatId, "Legacy StoryPhase adapter");
    }

    public bool CompleteCurrentBeat(string expectedBeatId, string completionSource = "Gameplay")
    {
        if (progress == null || string.IsNullOrWhiteSpace(expectedBeatId) ||
            !string.Equals(progress.CurrentBeatId.Value, expectedBeatId.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        StoryBeatDefinition current = FindBeat(progress.CurrentBeatId.Value);
        if (current == null)
        {
            return false;
        }

        if (current.ObjectiveGroup != null &&
            !objectives.IsGroupComplete(current.ObjectiveGroup.StableGroupId))
        {
            return Fail(
                $"Beat '{current.StableBeatId}' cannot complete because objective group " +
                $"'{current.ObjectiveGroup.StableGroupId}' is incomplete.");
        }

        if (string.IsNullOrWhiteSpace(current.NextBeatId))
        {
            if (!CommitCompletion(current, completionSource))
            {
                return false;
            }

            gameMode?.SetMode(GameMode.Gameplay);
            CreateCheckpointIfRequested(current);
            return true;
        }

        StoryBeatDefinition next = FindBeat(current.NextBeatId);
        return next != null && CompleteAndEnter(current, next.Chapter, next.StableBeatId, completionSource);
    }

    public bool ReportSequenceCompleted(string beatId, string sequenceKey)
    {
        StoryBeatDefinition current = progress == null ? null : FindBeat(progress.CurrentBeatId.Value);
        if (current == null || !string.Equals(current.StableBeatId, beatId, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(current.SequenceKey) &&
            !string.Equals(current.SequenceKey, sequenceKey, StringComparison.Ordinal))
        {
            return Fail(
                $"Sequence completion key '{sequenceKey}' does not match beat " +
                $"'{beatId}' sequence '{current.SequenceKey}'.");
        }

        return CompleteCurrentBeat(beatId, $"Sequence '{sequenceKey}'");
    }

    public bool JumpToBeatForDevelopment(string beatId)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!EnsureCatalog())
        {
            return false;
        }

        gameMode?.SetMode(GameMode.Loading);
        return EnterBeat(beatId, false, true);
#else
        return false;
#endif
    }

    public bool ResumeRestoredBeat()
    {
        if (progress == null || string.IsNullOrWhiteSpace(progress.CurrentBeatId.Value))
        {
            return false;
        }

        bool replayCurrentPresentation = !progress.IsCompleted(progress.CurrentBeatId.Value);
        return EnterBeat(progress.CurrentBeatId.Value, true, replayCurrentPresentation);
    }

    private bool CompleteAndEnter(
        StoryBeatDefinition current,
        StoryChapter targetChapter,
        string targetBeatId,
        string source)
    {
        if (!CommitCompletion(current, source))
        {
            return false;
        }

        if (!EnterBeat(targetBeatId, false, true, targetChapter))
        {
            return false;
        }

        CreateCheckpointIfRequested(current);
        return true;
    }

    private bool CommitCompletion(StoryBeatDefinition current, string source)
    {
        if (!storySystem.CommitBeatCompletion(current.StableBeatId))
        {
            return false;
        }

        SetFacts(current.FactsOnComplete);
        this.GetEvent().Send(new StoryBeatCommittedEvent(
            current.Chapter,
            current.StableBeatId,
            source,
            current.CreatesCheckpoint,
            current.CheckpointId));
        return true;
    }

    private bool EnterBeat(
        string beatId,
        bool restored,
        bool dispatchLegacyPresentation,
        StoryChapter expectedChapter = StoryChapter.None)
    {
        if (!EnsureCatalog())
        {
            return false;
        }

        StoryBeatDefinition definition = FindBeat(beatId);
        if (definition == null)
        {
            return false;
        }

        if (expectedChapter != StoryChapter.None && definition.Chapter != expectedChapter)
        {
            return Fail(
                $"Beat '{definition.StableBeatId}' belongs to {definition.Chapter}, not {expectedChapter}.");
        }

        if (!restored && !AreEntryRequirementsMet(definition))
        {
            return false;
        }

        if (!restored && progress != null &&
            string.Equals(progress.CurrentBeatId.Value, definition.StableBeatId, StringComparison.Ordinal))
        {
            return false;
        }

        gameMode?.SetMode(restored ? GameMode.Loading : definition.EntryGameMode);

        if (!storySystem.CommitBeatEntry(definition.Chapter, definition.StableBeatId, restored))
        {
            return false;
        }

        if (!restored)
        {
            SetFacts(definition.FactsOnEnter);
            if (definition.OverrideTime)
            {
                timeSystem?.SetTime(definition.TargetHour);
            }
        }

        if (!restored && definition.ObjectiveGroup != null)
        {
            objectives?.StartGroup(definition.ObjectiveGroup, definition.StableBeatId);
        }

        if (!restored && definition.OverrideGirlState)
        {
            girlStory?.SetState(definition.GirlState);
        }

        if (LegacyStoryPhaseMap.TryGetPhase(definition.StableBeatId, out StoryPhase legacyPhase))
        {
            legacyFlow?.ApplyLegacyPhaseFromStory(
                legacyPhase,
                dispatchLegacyPresentation,
                restored && dispatchLegacyPresentation);
        }
        else
        {
            legacyFlow?.ApplyLegacyPhaseFromStory(StoryPhase.None, false);
        }

        this.GetEvent().Send(new StoryBeatReadyEvent(
            definition.Chapter,
            definition.StableBeatId,
            definition.SequenceKey,
            definition.WeatherProfileKey,
            restored));

        if (restored)
        {
            gameMode?.SetMode(GameMode.Gameplay);
        }

        return true;
    }

    private void CreateCheckpointIfRequested(StoryBeatDefinition completedDefinition)
    {
        if (completedDefinition == null || !completedDefinition.CreatesCheckpoint)
        {
            return;
        }

        string checkpointId = string.IsNullOrWhiteSpace(completedDefinition.CheckpointId)
            ? $"checkpoint.{completedDefinition.StableBeatId}"
            : completedDefinition.CheckpointId;
        this.GetSystem<SaveSystem>()?.SaveCheckpoint(checkpointId);
    }

    private bool AreEntryRequirementsMet(StoryBeatDefinition definition)
    {
        foreach (string requiredFact in definition.RequiredWorldFacts)
        {
            if (!worldState.HasFact(requiredFact))
            {
                return Fail(
                    $"Beat '{definition.StableBeatId}' requires missing world fact '{requiredFact}'.");
            }
        }

        return true;
    }

    private void CompleteLegacyObjectiveAdapter(StoryBeatDefinition definition)
    {
        if (definition.ObjectiveGroup != null &&
            !objectives.IsGroupComplete(definition.ObjectiveGroup.StableGroupId))
        {
            objectives.CompleteAllRequired(definition.ObjectiveGroup.StableGroupId);
        }
    }

    private static bool IsAllowedLegacyTarget(StoryBeatDefinition current, string targetBeatId)
    {
        if (string.Equals(current.NextBeatId, targetBeatId, StringComparison.Ordinal))
        {
            return true;
        }

        IReadOnlyList<string> compatibleTargets = current.LegacyCompatibleNextBeatIds;
        for (int i = 0; i < compatibleTargets.Count; i++)
        {
            if (string.Equals(compatibleTargets[i], targetBeatId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void SetFacts(System.Collections.Generic.IReadOnlyList<string> facts)
    {
        if (facts == null)
        {
            return;
        }

        foreach (string fact in facts)
        {
            worldState?.SetFact(fact);
        }
    }

    private StoryBeatDefinition FindBeat(string beatId)
    {
        StoryBeatDefinition definition = catalog?.FindBeat(beatId);
        if (definition == null)
        {
            Fail($"Story beat definition '{beatId}' was not found.");
        }

        return definition;
    }

    private bool EnsureCatalog()
    {
        if (catalog == null)
        {
            catalog = Resources.Load<StoryCatalog>(StoryCatalog.ResourcesPath);
        }

        if (catalog == null)
        {
            return Fail(
                $"Story catalog is missing. Generate Resources/{StoryCatalog.ResourcesPath}.asset " +
                "from Light Tower/Architecture/Generate Story Content.");
        }

        return catalog.Validate(out string error) || Fail(error);
    }

    private bool Fail(string message)
    {
        Debug.LogError($"[StoryDirector] {message}");
        this.GetEvent().Send(new StoryDirectorErrorEvent(message));
        return false;
    }
}
