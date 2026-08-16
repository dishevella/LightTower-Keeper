#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LighthouseArchitectureTests
{
    [Test]
    public void StoryCatalog_ContainsEveryRequiredBeatAndValidLinks()
    {
        StoryCatalog catalog = Resources.Load<StoryCatalog>(StoryCatalog.ResourcesPath);
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.Validate(out string error), Is.True, error);
        Assert.That(catalog.Beats.Select(beat => beat.StableBeatId).Distinct().Count(),
            Is.EqualTo(StoryBeatIds.All.Count));

        HashSet<string> visited = new(StringComparer.Ordinal);
        StoryBeatDefinition current = catalog.FindBeat(StoryBeatIds.Day0.ArriveIsland);
        string terminalBeatId = string.Empty;
        while (current != null)
        {
            Assert.That(visited.Add(current.StableBeatId), Is.True,
                $"Canonical chain contains a cycle at {current.StableBeatId}.");
            terminalBeatId = current.StableBeatId;
            current = string.IsNullOrWhiteSpace(current.NextBeatId)
                ? null
                : catalog.FindBeat(current.NextBeatId);
        }

        Assert.That(visited, Is.EquivalentTo(StoryBeatIds.All));
        Assert.That(terminalBeatId, Is.EqualTo(StoryBeatIds.Day4.Ending));
    }

    [Test]
    public void LegacyDay1Flow_BypassesOnlyWhitelistedUnimplementedBeats()
    {
        TestApp app = CreateStoryApp();
        StoryDirectorSystem director = app.GetSystem<StoryDirectorSystem>();
        StoryProgressModel progress = app.GetModel<StoryProgressModel>();
        WorldStateModel world = app.GetModel<WorldStateModel>();

        Assert.That(director.JumpToBeatForDevelopment(StoryBeatIds.Day1.FindFootprints), Is.True);
        Assert.That(director.RequestLegacyTransition(StoryPhase.Day1_ReturnRoute), Is.True);
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day1.ReturnToLighthouse));
        Assert.That(progress.IsCompleted(StoryBeatIds.Day1.FindFootprints), Is.True);
        Assert.That(progress.IsCompleted(StoryBeatIds.Day1.FindBottleDrawing), Is.False);
        Assert.That(world.HasFact(WorldFactIds.FirstDrawingFound), Is.False);

        Assert.That(director.RequestLegacyTransition(StoryPhase.Day1_NightDuty), Is.True);
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day1.NightDuty));
        Assert.That(progress.IsCompleted(StoryBeatIds.Day1.FoodMissing), Is.False);
    }

    [Test]
    public void LegacyDay0ToDay1Flow_ReachesDay2WithoutBreakingAStageMapping()
    {
        TestApp app = CreateStoryApp();
        StoryDirectorSystem director = app.GetSystem<StoryDirectorSystem>();
        StoryProgressModel progress = app.GetModel<StoryProgressModel>();

        Assert.That(director.StartNewGame(), Is.True);
        StoryPhase[] legacyProgression =
        {
            StoryPhase.Day0_Forest,
            StoryPhase.Day0_ApproachLighthouse,
            StoryPhase.Day0_Rest,
            StoryPhase.Day1_Start,
            StoryPhase.Day1_InspectInside,
            StoryPhase.Day1_GoDock,
            StoryPhase.Day1_ReturnRoute,
            StoryPhase.Day1_NightDuty,
            StoryPhase.Day1_Complete
        };

        foreach (StoryPhase phase in legacyProgression)
        {
            Assert.That(director.RequestLegacyTransition(phase), Is.True,
                $"Legacy progression stopped before {phase}.");
        }

        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day1.SeeDistantFigure));
        Assert.That(app.GetModel<GameStateModel>().CurrentPhase.Value,
            Is.EqualTo(StoryPhase.Day1_Complete));
        Assert.That(director.CompleteCurrentBeat(
            StoryBeatIds.Day1.SeeDistantFigure,
            "Legacy flow regression"), Is.True);
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day2.FoodMissingAgain));
    }

    [Test]
    public void StoryDirector_AdvancesLegallyAndDuplicateCompletionCannotSkip()
    {
        TestApp app = CreateStoryApp();
        StoryDirectorSystem director = app.GetSystem<StoryDirectorSystem>();
        StoryProgressModel progress = app.GetModel<StoryProgressModel>();

        Assert.That(director.StartNewGame(), Is.True);
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day0.ArriveIsland));
        Assert.That(director.RequestLegacyTransition(StoryPhase.Day0_Forest), Is.True);
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day0.ReachLighthouse));
        Assert.That(director.CompleteCurrentBeat(StoryBeatIds.Day0.ReachLighthouse, "Test"), Is.True);

        int completedCount = progress.CompletedBeats.Count;
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day0.RestoreLighthouse));
        Assert.That(director.CompleteCurrentBeat(StoryBeatIds.Day0.ReachLighthouse, "Duplicate"), Is.False);
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day0.RestoreLighthouse));
        Assert.That(progress.CompletedBeats.Count, Is.EqualTo(completedCount));
    }

    [Test]
    public void WorldFacts_AreStableAndIdempotent()
    {
        WorldStateModel model = InitializeModel(new WorldStateModel());
        Assert.That(model.SetFact(WorldFactIds.BridgeCollapsed), Is.True);
        Assert.That(model.SetFact(WorldFactIds.BridgeCollapsed), Is.False);
        Assert.That(model.HasFact(WorldFactIds.BridgeCollapsed), Is.True);
        Assert.That(model.Facts.Count, Is.EqualTo(1));
        Assert.That(model.SetFact(WorldFactIds.BridgeCollapsed, false), Is.True);
        Assert.That(model.SetFact(WorldFactIds.BridgeCollapsed, false), Is.False);
    }

    [Test]
    public void ObjectiveGroup_CompletesAllRequiredAndIgnoresOptionalObjective()
    {
        ObjectiveGroupModel model = InitializeModel(new ObjectiveGroupModel());
        ObjectiveGroupRuntimeState state = model.StartOrReplace(new ObjectiveGroupSpec
        {
            groupId = "test.storm_preparation",
            completionMode = ObjectiveGroupCompletionMode.AllRequired,
            objectives = new List<ObjectiveSpec>
            {
                new() { objectiveId = "windows", title = "Secure windows" },
                new() { objectiveId = "lamp", title = "Check lamp" },
                new() { objectiveId = "optional", title = "Make coffee", optional = true }
            }
        });

        Assert.That(state.completed, Is.False);
        Assert.That(model.CompleteObjective(state.groupId, "windows", out bool firstCompletion), Is.True);
        Assert.That(firstCompletion, Is.False);
        Assert.That(model.CompleteObjective(state.groupId, "lamp", out bool groupCompleted), Is.True);
        Assert.That(groupCompleted, Is.True);
        Assert.That(state.completed, Is.True);
    }

    [Test]
    public void SaveDto_RoundTripRestoresStoryProgressAndVersion()
    {
        StoryProgressModel source = InitializeModel(new StoryProgressModel());
        source.Restore(
            StoryChapter.Day3,
            StoryBeatIds.Day3.PrepareForStorm,
            new[] { StoryBeatIds.Day3.StormWarning, StoryBeatIds.Day2.SilentExchangeMontage },
            "checkpoint.day3.storm_warning");

        GameSaveData original = new()
        {
            currentChapter = source.CurrentChapter.Value.ToString(),
            currentBeatId = source.CurrentBeatId.Value,
            completedBeatIds = source.CompletedBeats.ToList(),
            lastCheckpointId = source.LastCheckpointId.Value,
            worldFactIds = new List<string> { WorldFactIds.GirlSeen },
            girlState = GirlStoryState.Observing.ToString()
        };
        original.inventory.itemIds.Add(InventoryItemId.CommunicationDevice.ToString());
        original.inventory.selectedItemId = InventoryItemId.CommunicationDevice.ToString();

        string json = GameSaveSerializer.Serialize(original);
        Assert.That(json, Does.Contain("\"saveVersion\": 1"));
        Assert.That(GameSaveSerializer.TryDeserialize(json, out GameSaveData restored, out string error),
            Is.True, error);

        StoryProgressModel target = InitializeModel(new StoryProgressModel());
        Assert.That(Enum.TryParse(restored.currentChapter, out StoryChapter restoredChapter), Is.True);
        target.Restore(
            restoredChapter,
            restored.currentBeatId,
            restored.completedBeatIds,
            restored.lastCheckpointId);

        Assert.That(restored.saveVersion, Is.EqualTo(SaveDataVersion.Current));
        Assert.That(target.CurrentChapter.Value, Is.EqualTo(source.CurrentChapter.Value));
        Assert.That(target.CurrentBeatId.Value, Is.EqualTo(source.CurrentBeatId.Value));
        Assert.That(target.LastCheckpointId.Value, Is.EqualTo(source.LastCheckpointId.Value));
        Assert.That(target.CompletedBeats, Is.EquivalentTo(source.CompletedBeats));
        Assert.That(restored.worldFactIds, Does.Contain(WorldFactIds.GirlSeen));
    }

    [Test]
    public void SaveFile_UsesAtomicReplacementAndFallsBackToBackup()
    {
        string directory = Path.GetFullPath("Temp/ArchitectureSaveTests");
        string path = Path.Combine(directory, "slot.json");
        Directory.CreateDirectory(directory);
        DeleteTestSaveFiles(path);

        try
        {
            GameSaveData first = new()
            {
                currentChapter = StoryChapter.Day0.ToString(),
                currentBeatId = StoryBeatIds.Day0.ArriveIsland,
                lastCheckpointId = "checkpoint.test.first"
            };
            Assert.That(SaveSystem.TryWriteSnapshot(path, first, out string firstError),
                Is.True, firstError);

            GameSaveData second = new()
            {
                currentChapter = StoryChapter.Day0.ToString(),
                currentBeatId = StoryBeatIds.Day0.ReachLighthouse,
                lastCheckpointId = "checkpoint.test.second"
            };
            Assert.That(SaveSystem.TryWriteSnapshot(path, second, out string secondError),
                Is.True, secondError);
            Assert.That(SaveSystem.TryReadSnapshotWithBackup(
                path,
                out GameSaveData current,
                out string readError,
                out bool loadedBackup), Is.True, readError);
            Assert.That(loadedBackup, Is.False);
            Assert.That(current.currentBeatId, Is.EqualTo(StoryBeatIds.Day0.ReachLighthouse));

            File.WriteAllText(path, "{ intentionally_corrupt");
            Assert.That(SaveSystem.TryReadSnapshotWithBackup(
                path,
                out GameSaveData recovered,
                out string recoveryError,
                out loadedBackup), Is.True, recoveryError);
            Assert.That(loadedBackup, Is.True);
            Assert.That(recovered.currentBeatId, Is.EqualTo(StoryBeatIds.Day0.ArriveIsland));
        }
        finally
        {
            DeleteTestSaveFiles(path);
            if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0)
            {
                Directory.Delete(directory);
            }
        }
    }

    [Test]
    public void SaveSystem_RestoresAllRequiredRuntimeModelsWithoutHistoricalEvents()
    {
        TestApp app = CreateFullStoryApp();
        GameSaveData data = new()
        {
            currentChapter = StoryChapter.Day1.ToString(),
            currentBeatId = StoryBeatIds.Day1.NightDuty,
            completedBeatIds = new List<string> { StoryBeatIds.Day1.ReturnToLighthouse },
            lastCheckpointId = "checkpoint.test.night_duty",
            worldFactIds = new List<string> { WorldFactIds.BridgeCollapsed },
            activeObjectiveGroupId = "test.night_duty",
            collectibleIds = new List<string> { "collectible.test.note" },
            girlState = GirlStoryState.Observing.ToString()
        };
        data.inventory.itemIds.Add(InventoryItemId.CommunicationDevice.ToString());
        data.inventory.selectedItemId = InventoryItemId.CommunicationDevice.ToString();
        data.toolInventory.hasSmallAxe = true;
        data.toolInventory.hasChainsaw = true;
        data.lighthouseDuty.generatorChecked = true;
        data.lighthouseDuty.lampRoomChecked = true;
        data.lighthouseDuty.lensChecked = true;
        data.lighthouseDuty.lightActivated = true;
        data.lighthouseDuty.beamSweepCompleted = false;
        data.timeOfDay.currentHour = 19.25f;
        data.objectiveGroups.Add(new ObjectiveGroupSaveData
        {
            groupId = "test.night_duty",
            storyBeatId = StoryBeatIds.Day1.NightDuty,
            displayName = "Night Duty",
            objectives = new List<ObjectiveSaveData>
            {
                new() { objectiveId = "generator", title = "Generator", completed = true },
                new() { objectiveId = "beam", title = "Beam", completed = false }
            }
        });

        Assert.That(app.GetSystem<SaveSystem>().RestoreSnapshot(
            data,
            applySceneState: false,
            placePlayer: false), Is.True);

        StoryProgressModel progress = app.GetModel<StoryProgressModel>();
        Assert.That(progress.CurrentBeatId.Value, Is.EqualTo(StoryBeatIds.Day1.NightDuty));
        Assert.That(progress.IsCompleted(StoryBeatIds.Day1.ReturnToLighthouse), Is.True);
        Assert.That(app.GetModel<WorldStateModel>().HasFact(WorldFactIds.BridgeCollapsed), Is.True);
        Assert.That(app.GetModel<Day1RouteModel>().BridgeCollapsed.Value, Is.True);
        Assert.That(app.GetModel<InventoryModel>().SelectedItem.Value,
            Is.EqualTo(InventoryItemId.CommunicationDevice));
        Assert.That(app.GetModel<ToolInventoryModel>().HasChainsaw.Value, Is.True);
        Assert.That(app.GetModel<LighthouseDutyModel>().LightActivated.Value, Is.True);
        Assert.That(app.GetModel<TimeOfDayModel>().CurrentHour.Value, Is.EqualTo(19.25f).Within(0.001f));
        Assert.That(app.GetModel<CollectibleStateModel>().IsCollected("collectible.test.note"), Is.True);
        Assert.That(app.GetModel<GirlStoryStateModel>().CurrentState.Value,
            Is.EqualTo(GirlStoryState.Observing));
        Assert.That(app.GetModel<ObjectiveGroupModel>().TryGetGroup(
            "test.night_duty",
            out ObjectiveGroupRuntimeState objectiveGroup), Is.True);
        Assert.That(objectiveGroup.objectives.Single(item => item.objectiveId == "generator").completed,
            Is.True);
        Assert.That(app.GetModel<GameModeModel>().CurrentMode.Value, Is.EqualTo(GameMode.Gameplay));
    }

    [Test]
    public void RestoringCompletedBeat_DoesNotReplayItsLegacyOneShotPresentation()
    {
        TestApp app = CreateStoryApp();
        StoryProgressModel progress = app.GetModel<StoryProgressModel>();
        progress.Restore(
            StoryChapter.Day1,
            StoryBeatIds.Day1.SeeDistantFigure,
            new[] { StoryBeatIds.Day1.SeeDistantFigure },
            "checkpoint.day1.see_distant_figure");
        int replayCount = 0;
        app.Events.Register<Day1CompletedEvent>(_ => replayCount++);

        Assert.That(app.GetSystem<StoryDirectorSystem>().ResumeRestoredBeat(), Is.True);
        Assert.That(replayCount, Is.Zero);
        Assert.That(app.GetModel<GameStateModel>().CurrentPhase.Value, Is.EqualTo(StoryPhase.Day1_Complete));
    }

    [TestCase(StoryPhase.Day0_BoatIntro, StoryChapter.Day0, StoryBeatIds.Day0.ArriveIsland)]
    [TestCase(StoryPhase.Day0_Forest, StoryChapter.Day0, StoryBeatIds.Day0.ReachLighthouse)]
    [TestCase(StoryPhase.Day1_Start, StoryChapter.Day1, StoryBeatIds.Day1.MorningRoutine)]
    [TestCase(StoryPhase.Day1_Complete, StoryChapter.Day1, StoryBeatIds.Day1.SeeDistantFigure)]
    public void LegacyStoryPhaseMap_MapsToStableBeat(
        StoryPhase phase,
        StoryChapter expectedChapter,
        string expectedBeat)
    {
        Assert.That(LegacyStoryPhaseMap.TryGetBeat(phase, out StoryChapter chapter, out string beat), Is.True);
        Assert.That(chapter, Is.EqualTo(expectedChapter));
        Assert.That(beat, Is.EqualTo(expectedBeat));
    }

    [Test]
    public void AnimationCoordinator_PreservesPriorityCommitAndHysteresisRules()
    {
        CharacterAnimationActionCoordinator actions = new();
        Assert.That(actions.CanInterrupt(
            CharacterAnimationLogicalState.Action,
            CharacterAnimationPriority.Interaction,
            CharacterAnimationPriority.Locomotion,
            false,
            true), Is.False);
        Assert.That(actions.CanInterrupt(
            CharacterAnimationLogicalState.Action,
            CharacterAnimationPriority.Interaction,
            CharacterAnimationPriority.Death,
            false,
            true), Is.True);
        CharacterActionProgressDecision decision = actions.EvaluateProgress(
            0.76f,
            0.55f,
            0.75f,
            true,
            false,
            false);
        Assert.That(decision.CommitReached, Is.True);
        Assert.That(decision.Interruptible, Is.True);

        LocomotionSignalFilter locomotion = new();
        Assert.That(locomotion.ResolveMovementIntent(false, 0.21f, 0.2f, 0.1f), Is.True);
        Assert.That(locomotion.ResolveMovementIntent(true, 0.15f, 0.2f, 0.1f), Is.True);
        Assert.That(locomotion.ResolveMovementIntent(true, 0.09f, 0.2f, 0.1f), Is.False);
    }

    [Test]
    public void FormalSceneAndSafetySwitches_AreConfiguredWithoutMissingScripts()
    {
        EditorBuildSettingsScene firstEnabled = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
        Assert.That(firstEnabled, Is.Not.Null);
        Assert.That(firstEnabled.path, Is.EqualTo(LighthouseProjectSetupUtility.MainScenePath));

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = EditorSceneManager.OpenScene(
            LighthouseProjectSetupUtility.MainScenePath,
            OpenSceneMode.Single);
        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            string[] expectedRoots =
            {
                "Environment", "Lighthouse", "Gameplay", "StoryContent", "UI",
                "LightingAndAtmosphere", "DevelopmentOnly"
            };
            Assert.That(expectedRoots.All(name => roots.Any(root => root.name == name)), Is.True);

            Transform storyRoot = roots.First(root => root.name == "StoryContent").transform;
            for (int day = 0; day <= 4; day++)
            {
                Assert.That(storyRoot.Find($"Day{day}"), Is.Not.Null);
            }

            int missingScripts = 0;
            foreach (GameObject root in roots)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                }
            }
            Assert.That(missingScripts, Is.Zero);

            foreach (PhaseTest phaseTest in FindComponents<PhaseTest>(roots))
            {
                SerializedProperty enabledProperty = new SerializedObject(phaseTest)
                    .FindProperty("enableAutomaticTest");
                Assert.That(enabledProperty, Is.Not.Null);
                Assert.That(enabledProperty.boolValue, Is.False);
            }

            foreach (ApproachLighthouseController approach in FindComponents<ApproachLighthouseController>(roots))
            {
                SerializedProperty autoAdvance = new SerializedObject(approach)
                    .FindProperty("autoAdvanceForestForDevelopment");
                Assert.That(autoAdvance, Is.Not.Null);
                Assert.That(autoAdvance.boolValue, Is.False);
            }
        }
        finally
        {
            if (previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }

    private static IEnumerable<T> FindComponents<T>(IEnumerable<GameObject> roots)
        where T : Component
    {
        return roots.SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    private static T InitializeModel<T>(T model) where T : class, IGameModel
    {
        model.Initialize();
        return model;
    }

    private static TestApp CreateStoryApp()
    {
        TestApp app = new();
        app.RegisterModel(new GameStateModel());
        app.RegisterModel(new GameModeModel());
        app.RegisterModel(new StoryProgressModel());
        app.RegisterModel(new WorldStateModel());
        app.RegisterModel(new ObjectiveGroupModel());
        app.RegisterModel(new GirlStoryStateModel());
        app.RegisterModel(new CollectibleStateModel());
        app.RegisterModel(new Day1RouteModel());
        app.RegisterModel(new TimeOfDayModel());

        app.RegisterSystem(new GameModeSystem());
        app.RegisterSystem(new StorySystem());
        app.RegisterSystem(new WorldStateSystem());
        app.RegisterSystem(new ObjectiveSystem());
        app.RegisterSystem(new GirlStorySystem());
        app.RegisterSystem(new TimeSystem());
        app.RegisterSystem(new GameFlowSystem());
        app.RegisterSystem(new StoryDirectorSystem());
        app.Initialize();
        return app;
    }

    private static TestApp CreateFullStoryApp()
    {
        TestApp app = new();
        app.RegisterModel(new GameStateModel());
        app.RegisterModel(new GameModeModel());
        app.RegisterModel(new StoryProgressModel());
        app.RegisterModel(new WorldStateModel());
        app.RegisterModel(new ObjectiveGroupModel());
        app.RegisterModel(new GirlStoryStateModel());
        app.RegisterModel(new CollectibleStateModel());
        app.RegisterModel(new TaskModel());
        app.RegisterModel(new ToolInventoryModel());
        app.RegisterModel(new LighthouseDutyModel());
        app.RegisterModel(new InventoryModel());
        app.RegisterModel(new Day1RouteModel());
        app.RegisterModel(new TimeOfDayModel());

        app.RegisterSystem(new GameModeSystem());
        app.RegisterSystem(new StorySystem());
        app.RegisterSystem(new WorldStateSystem());
        app.RegisterSystem(new ObjectiveSystem());
        app.RegisterSystem(new GirlStorySystem());
        app.RegisterSystem(new CollectibleStateSystem());
        app.RegisterSystem(new TaskSystem());
        app.RegisterSystem(new TimeSystem());
        app.RegisterSystem(new GameFlowSystem());
        app.RegisterSystem(new InventorySystem());
        app.RegisterSystem(new SaveSystem());
        app.RegisterSystem(new StoryDirectorSystem());
        app.Initialize();
        return app;
    }

    private static void DeleteTestSaveFiles(string path)
    {
        foreach (string candidate in new[] { path, path + ".bak", path + ".tmp" })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }

    private sealed class TestApp : IApp
    {
        private readonly Dictionary<Type, IGameModel> models = new();
        private readonly Dictionary<Type, IGameSystem> systems = new();
        private bool initialized;

        public ITypeEventSystem Events { get; } = new TypeEventSystem();

        public void Initialize()
        {
            if (initialized) return;
            foreach (IGameModel model in models.Values) model.Initialize();
            foreach (IGameSystem system in systems.Values) system.Initialize();
            initialized = true;
        }

        public void RegisterSystem<T>(T system) where T : class, IGameSystem
        {
            ((ICanSetApp)system).SetApp(this);
            systems[typeof(T)] = system;
            if (initialized) system.Initialize();
        }

        public void RegisterModel<T>(T model) where T : class, IGameModel
        {
            ((ICanSetApp)model).SetApp(this);
            models[typeof(T)] = model;
            if (initialized) model.Initialize();
        }

        public T GetSystem<T>() where T : class, IGameSystem
        {
            return systems.TryGetValue(typeof(T), out IGameSystem system) ? system as T : null;
        }

        public T GetModel<T>() where T : class, IGameModel
        {
            return models.TryGetValue(typeof(T), out IGameModel model) ? model as T : null;
        }

        public void SendCommand<T>() where T : class, IGameCommand, new()
        {
            SendCommand(new T());
        }

        public void SendCommand<T>(T command) where T : class, IGameCommand
        {
            ((ICanSetApp)command).SetApp(this);
            command.Execute();
        }
    }
}
#endif
