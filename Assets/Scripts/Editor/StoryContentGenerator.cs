#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StoryContentGenerator
{
    private const string Root = "Assets/Resources/Story";
    private const string BeatRoot = Root + "/Beats";
    private const string ChapterRoot = Root + "/Chapters";
    private const string ObjectiveRoot = Root + "/Objectives";
    private const string CatalogPath = Root + "/StoryCatalog.asset";
    private const string GeneratorVersion = "2";

    private static readonly string GeneratorKey =
        $"LightTower.StoryContentGenerator.{Application.dataPath.GetHashCode()}.{GeneratorVersion}";

    private sealed class BeatSeed
    {
        public string Id;
        public StoryChapter Chapter;
        public string DisplayName;
        public string Next;
        public bool Checkpoint;
        public float? Hour;
        public string ObjectiveGroupId;
        public string[] EnterFacts;
        public string[] CompleteFacts;
        public string SequenceKey;
        public bool SetGirlState;
        public GirlStoryState GirlState;
        public string[] LegacyCompatibleNextBeatIds;
    }

    [InitializeOnLoadMethod]
    private static void ScheduleGeneration()
    {
        if (!EditorPrefs.GetBool(GeneratorKey, false))
        {
            EditorApplication.delayCall += TryAutoGenerate;
        }
    }

    [MenuItem("Light Tower/Architecture/Generate Story Content")]
    public static void GenerateStoryContent()
    {
        EnsureFolders();
        Dictionary<string, ObjectiveGroupDefinition> objectiveGroups = GenerateObjectiveGroups();
        List<StoryBeatDefinition> beats = GenerateBeats(objectiveGroups);
        List<StoryChapterDefinition> chapters = GenerateChapters(beats);

        StoryCatalog catalog = LoadOrCreate<StoryCatalog>(CatalogPath);
        catalog.ConfigureForEditor(chapters, beats);
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!catalog.Validate(out string error))
        {
            throw new InvalidOperationException(error);
        }

        EditorPrefs.SetBool(GeneratorKey, true);
        Debug.Log(
            $"Story content generated: {chapters.Count} chapters, {beats.Count} beats, " +
            $"{objectiveGroups.Count} objective groups. Catalog={CatalogPath}");
    }

    [MenuItem("Light Tower/Architecture/Validate Story Content")]
    public static void ValidateStoryContent()
    {
        StoryCatalog catalog = AssetDatabase.LoadAssetAtPath<StoryCatalog>(CatalogPath);
        if (catalog == null)
        {
            throw new InvalidOperationException($"Missing story catalog at {CatalogPath}.");
        }

        if (!catalog.Validate(out string error))
        {
            throw new InvalidOperationException(error);
        }

        Debug.Log(
            $"Story catalog valid: {catalog.Chapters.Count} chapters and {catalog.Beats.Count} beats.");
    }

    private static void TryAutoGenerate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryAutoGenerate;
            return;
        }

        try
        {
            GenerateStoryContent();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Dictionary<string, ObjectiveGroupDefinition> GenerateObjectiveGroups()
    {
        Dictionary<string, ObjectiveGroupDefinition> result = new(StringComparer.Ordinal);

        CreateObjectiveGroup(
            result,
            "objectives.day1.morning_routine",
            "Day 1 Morning Routine",
            new ObjectiveSpec
            {
                objectiveId = "day1.inspect_lighthouse",
                title = "Inspect the lighthouse interior",
                description = "Check that the station survived the night."
            },
            new ObjectiveSpec
            {
                objectiveId = "day1.collect_small_axe",
                title = "Collect the small axe",
                description = "Prepare a tool for the northern route."
            },
            new ObjectiveSpec
            {
                objectiveId = "day1.check_communications",
                title = "Check communications",
                description = "Listen for the morning supply instructions."
            });

        CreateObjectiveGroup(
            result,
            "objectives.day1.night_duty",
            "Lighthouse Night Duty",
            new ObjectiveSpec
            {
                objectiveId = "day1.check_generator",
                title = "Check the generator"
            },
            new ObjectiveSpec
            {
                objectiveId = "day1.inspect_lamp_room",
                title = "Inspect the lamp room"
            },
            new ObjectiveSpec
            {
                objectiveId = "day1.inspect_lens",
                title = "Inspect the lens"
            },
            new ObjectiveSpec
            {
                objectiveId = "day1.complete_beam_sweep",
                title = "Complete a beacon sweep"
            });

        CreateObjectiveGroup(
            result,
            "objectives.day3.storm_preparation",
            "Prepare for the Storm",
            new ObjectiveSpec
            {
                objectiveId = "day3.reinforce_windows",
                title = "Reinforce the windows"
            },
            new ObjectiveSpec
            {
                objectiveId = "day3.check_main_light",
                title = "Check the main light"
            },
            new ObjectiveSpec
            {
                objectiveId = "day3.retrieve_outdoor_tools",
                title = "Bring in the outdoor tools"
            },
            new ObjectiveSpec
            {
                objectiveId = "day3.enable_automatic_light",
                title = "Switch the beacon to automatic operation"
            });

        return result;
    }

    private static List<StoryBeatDefinition> GenerateBeats(
        IReadOnlyDictionary<string, ObjectiveGroupDefinition> objectiveGroups)
    {
        List<BeatSeed> seeds = BuildBeatSeeds();
        List<StoryBeatDefinition> result = new(seeds.Count);

        foreach (BeatSeed seed in seeds)
        {
            string assetName = seed.Id.Replace('.', '_');
            string path = $"{BeatRoot}/{assetName}.asset";
            StoryBeatDefinition beat = LoadOrCreate<StoryBeatDefinition>(path);
            objectiveGroups.TryGetValue(seed.ObjectiveGroupId ?? string.Empty, out ObjectiveGroupDefinition group);
            beat.ConfigureForEditor(
                seed.Id,
                seed.Chapter,
                seed.DisplayName,
                seed.Next,
                seed.Checkpoint,
                seed.Hour,
                group,
                seed.EnterFacts,
                seed.CompleteFacts,
                seed.SequenceKey,
                GameMode.Gameplay,
                seed.SetGirlState,
                seed.GirlState,
                seed.LegacyCompatibleNextBeatIds);
            EditorUtility.SetDirty(beat);
            result.Add(beat);
        }

        return result;
    }

    private static List<StoryChapterDefinition> GenerateChapters(
        IReadOnlyList<StoryBeatDefinition> beats)
    {
        List<StoryChapterDefinition> result = new();
        AddChapter(result, beats, StoryChapter.Day0, "Day 0", StoryBeatIds.Day0.ArriveIsland);
        AddChapter(result, beats, StoryChapter.Day1, "Day 1", StoryBeatIds.Day1.MorningRoutine);
        AddChapter(result, beats, StoryChapter.Day2, "Day 2", StoryBeatIds.Day2.FoodMissingAgain);
        AddChapter(result, beats, StoryChapter.Day3, "Day 3", StoryBeatIds.Day3.StormWarning);
        AddChapter(result, beats, StoryChapter.Day4, "Day 4", StoryBeatIds.Day4.SecondCupMorning);
        return result;
    }

    private static void AddChapter(
        ICollection<StoryChapterDefinition> result,
        IEnumerable<StoryBeatDefinition> allBeats,
        StoryChapter chapter,
        string displayName,
        string initialBeatId)
    {
        string path = $"{ChapterRoot}/{chapter}.asset";
        StoryChapterDefinition definition = LoadOrCreate<StoryChapterDefinition>(path);
        definition.ConfigureForEditor(
            chapter,
            displayName,
            initialBeatId,
            allBeats.Where(beat => beat.Chapter == chapter));
        EditorUtility.SetDirty(definition);
        result.Add(definition);
    }

    private static void CreateObjectiveGroup(
        IDictionary<string, ObjectiveGroupDefinition> result,
        string groupId,
        string displayName,
        params ObjectiveSpec[] objectives)
    {
        string assetName = groupId.Replace('.', '_');
        ObjectiveGroupDefinition definition =
            LoadOrCreate<ObjectiveGroupDefinition>($"{ObjectiveRoot}/{assetName}.asset");
        definition.ConfigureForEditor(
            groupId,
            displayName,
            ObjectiveGroupCompletionMode.AllRequired,
            objectives);
        EditorUtility.SetDirty(definition);
        result[groupId] = definition;
    }

    private static List<BeatSeed> BuildBeatSeeds()
    {
        return new List<BeatSeed>
        {
            Beat(StoryBeatIds.Day0.ArriveIsland, StoryChapter.Day0, "Arrive on the Island", StoryBeatIds.Day0.ReachLighthouse, 17.5f, sequence: "legacy.day0.boat_intro"),
            Beat(StoryBeatIds.Day0.ReachLighthouse, StoryChapter.Day0, "Reach the Lighthouse", StoryBeatIds.Day0.RestoreLighthouse, 17.75f, sequence: "legacy.day0.forest_path"),
            Beat(StoryBeatIds.Day0.RestoreLighthouse, StoryChapter.Day0, "Restore the Lighthouse", StoryBeatIds.Day0.FirstNight, 18.25f, sequence: "legacy.day0.approach_lighthouse"),
            Beat(StoryBeatIds.Day0.FirstNight, StoryChapter.Day0, "First Night", StoryBeatIds.Day1.MorningRoutine, 22f, true, sequence: "legacy.day0.first_night"),

            Beat(StoryBeatIds.Day1.MorningRoutine, StoryChapter.Day1, "Morning Routine", StoryBeatIds.Day1.GoToSupplyPoint, 8f, true, "objectives.day1.morning_routine", sequence: "legacy.day1.wakeup"),
            Beat(StoryBeatIds.Day1.GoToSupplyPoint, StoryChapter.Day1, "Go to the Supply Point", StoryBeatIds.Day1.FindFootprints, 10f),
            Beat(StoryBeatIds.Day1.FindFootprints, StoryChapter.Day1, "Find the Footprints", StoryBeatIds.Day1.FindBottleDrawing, 11.5f, sequence: "legacy.day1.dock_encounter", legacyNext: new[] { StoryBeatIds.Day1.ReturnToLighthouse }),
            Beat(StoryBeatIds.Day1.FindBottleDrawing, StoryChapter.Day1, "Find the Bottle Drawing", StoryBeatIds.Day1.ReturnToLighthouse, 13f, completeFacts: new[] { WorldFactIds.FirstDrawingFound }),
            Beat(StoryBeatIds.Day1.ReturnToLighthouse, StoryChapter.Day1, "Return to the Lighthouse", StoryBeatIds.Day1.FoodMissing, 17f, legacyNext: new[] { StoryBeatIds.Day1.NightDuty }),
            Beat(StoryBeatIds.Day1.FoodMissing, StoryChapter.Day1, "Food Missing", StoryBeatIds.Day1.NightDuty, 18f),
            Beat(StoryBeatIds.Day1.NightDuty, StoryChapter.Day1, "Night Duty", StoryBeatIds.Day1.SeeDistantFigure, 20f, objectives: "objectives.day1.night_duty"),
            Beat(StoryBeatIds.Day1.SeeDistantFigure, StoryChapter.Day1, "See a Distant Figure", StoryBeatIds.Day2.FoodMissingAgain, 21f, true, completeFacts: new[] { WorldFactIds.GirlSeen }, setGirl: true, girl: GirlStoryState.Observing),

            Beat(StoryBeatIds.Day2.FoodMissingAgain, StoryChapter.Day2, "Food Missing Again", StoryBeatIds.Day2.FirstClearSightOfGirl, 8f, true),
            Beat(StoryBeatIds.Day2.FirstClearSightOfGirl, StoryChapter.Day2, "First Clear Sight of the Girl", StoryBeatIds.Day2.LeaveFood, 12f, completeFacts: new[] { WorldFactIds.GirlSeen }, setGirl: true, girl: GirlStoryState.Fleeing),
            Beat(StoryBeatIds.Day2.LeaveFood, StoryChapter.Day2, "Leave Food", StoryBeatIds.Day2.ReceiveDrawingAndStone, 17f, completeFacts: new[] { WorldFactIds.FoodLeftForGirl }),
            Beat(StoryBeatIds.Day2.ReceiveDrawingAndStone, StoryChapter.Day2, "Receive a Drawing and Stone", StoryBeatIds.Day2.SilentExchangeMontage, 8f, completeFacts: new[] { WorldFactIds.GirlReplyReceived }),
            Beat(StoryBeatIds.Day2.SilentExchangeMontage, StoryChapter.Day2, "Silent Exchange", StoryBeatIds.Day3.StormWarning, 18f, true, setGirl: true, girl: GirlStoryState.Observing),

            Beat(StoryBeatIds.Day3.StormWarning, StoryChapter.Day3, "Storm Warning", StoryBeatIds.Day3.PrepareForStorm, 14f, true),
            Beat(StoryBeatIds.Day3.PrepareForStorm, StoryChapter.Day3, "Prepare for the Storm", StoryBeatIds.Day3.SeeGirlInStorm, 16f, objectives: "objectives.day3.storm_preparation"),
            Beat(StoryBeatIds.Day3.SeeGirlInStorm, StoryChapter.Day3, "See the Girl in the Storm", StoryBeatIds.Day3.FollowGirl, 18f, setGirl: true, girl: GirlStoryState.Fleeing),
            Beat(StoryBeatIds.Day3.FollowGirl, StoryChapter.Day3, "Follow the Girl", StoryBeatIds.Day3.EnterCave, 18.5f),
            Beat(StoryBeatIds.Day3.EnterCave, StoryChapter.Day3, "Enter the Cave", StoryBeatIds.Day3.FindBriggs, 19f, completeFacts: new[] { WorldFactIds.CaveOpened }, setGirl: true, girl: GirlStoryState.WaitingInCave),
            Beat(StoryBeatIds.Day3.FindBriggs, StoryChapter.Day3, "Find Briggs", StoryBeatIds.Day3.ReachSafeRoom, 19.5f, completeFacts: new[] { WorldFactIds.BriggsFound }),
            Beat(StoryBeatIds.Day3.ReachSafeRoom, StoryChapter.Day3, "Reach the Safe Room", StoryBeatIds.Day3.RescueGirl, 20f),
            Beat(StoryBeatIds.Day3.RescueGirl, StoryChapter.Day3, "Rescue the Girl", StoryBeatIds.Day4.SecondCupMorning, 21f, true, completeFacts: new[] { WorldFactIds.GirlRescued }, setGirl: true, girl: GirlStoryState.FollowingOwen),

            Beat(StoryBeatIds.Day4.SecondCupMorning, StoryChapter.Day4, "Second Cup Morning", StoryBeatIds.Day4.WatchBriggsRecord, 8f, true, setGirl: true, girl: GirlStoryState.SafeInLighthouse),
            Beat(StoryBeatIds.Day4.WatchBriggsRecord, StoryChapter.Day4, "Watch Briggs' Record", StoryBeatIds.Day4.TellGirlTruth, 10f),
            Beat(StoryBeatIds.Day4.TellGirlTruth, StoryChapter.Day4, "Tell the Girl the Truth", StoryBeatIds.Day4.ActivateEmergencyBeacon, 12f),
            Beat(StoryBeatIds.Day4.ActivateEmergencyBeacon, StoryChapter.Day4, "Activate the Emergency Beacon", StoryBeatIds.Day4.Ending, 17.5f, completeFacts: new[] { WorldFactIds.EmergencyBeaconActivated }),
            Beat(StoryBeatIds.Day4.Ending, StoryChapter.Day4, "Ending", string.Empty, 18f, true)
        };
    }

    private static BeatSeed Beat(
        string id,
        StoryChapter chapter,
        string displayName,
        string next,
        float? hour,
        bool checkpoint = false,
        string objectives = null,
        string[] enterFacts = null,
        string[] completeFacts = null,
        string sequence = "",
        bool setGirl = false,
        GirlStoryState girl = GirlStoryState.Hidden,
        string[] legacyNext = null)
    {
        return new BeatSeed
        {
            Id = id,
            Chapter = chapter,
            DisplayName = displayName,
            Next = next,
            Checkpoint = checkpoint,
            Hour = hour,
            ObjectiveGroupId = objectives,
            EnterFacts = enterFacts,
            CompleteFacts = completeFacts,
            SequenceKey = sequence,
            SetGirlState = setGirl,
            GirlState = girl,
            LegacyCompatibleNextBeatIds = legacyNext
        };
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Story");
        EnsureFolder(Root, "Beats");
        EnsureFolder(Root, "Chapters");
        EnsureFolder(Root, "Objectives");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
