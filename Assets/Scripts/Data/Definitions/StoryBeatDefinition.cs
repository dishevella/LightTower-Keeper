using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Light Tower/Story/Story Beat",
    fileName = "StoryBeat")]
public sealed class StoryBeatDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string stableBeatId;
    [SerializeField] private StoryChapter chapter;
    [SerializeField] private string displayName;

    [Header("Requirements")]
    [SerializeField] private List<string> requiredWorldFacts = new();

    [Header("Gameplay")]
    [SerializeField] private ObjectiveGroupDefinition objectiveGroup;
    [SerializeField] private GameMode entryGameMode = GameMode.Gameplay;

    [Header("Environment")]
    [SerializeField] private bool overrideTime;
    [Range(0f, 24f)]
    [SerializeField] private float targetHour = 17.75f;
    [SerializeField] private string weatherProfileKey;

    [Header("Presentation")]
    [SerializeField] private string sequenceKey;

    [Header("World State")]
    [SerializeField] private List<string> factsOnEnter = new();
    [SerializeField] private List<string> factsOnComplete = new();
    [SerializeField] private bool overrideGirlState;
    [SerializeField] private GirlStoryState girlState;

    [Header("Progression")]
    [SerializeField] private string nextBeatId;
    [SerializeField] private bool createsCheckpoint;
    [SerializeField] private string checkpointId;

    [Header("Legacy Migration")]
    [Tooltip("Targets that an existing StoryPhase controller may enter while its content is being migrated. These do not change the canonical story chain.")]
    [SerializeField] private List<string> legacyCompatibleNextBeatIds = new();

    public string StableBeatId => stableBeatId;
    public StoryChapter Chapter => chapter;
    public string DisplayName => displayName;
    public IReadOnlyList<string> RequiredWorldFacts => requiredWorldFacts;
    public ObjectiveGroupDefinition ObjectiveGroup => objectiveGroup;
    public GameMode EntryGameMode => entryGameMode;
    public bool OverrideTime => overrideTime;
    public float TargetHour => targetHour;
    public string WeatherProfileKey => weatherProfileKey;
    public string SequenceKey => sequenceKey;
    public IReadOnlyList<string> FactsOnEnter => factsOnEnter;
    public IReadOnlyList<string> FactsOnComplete => factsOnComplete;
    public bool OverrideGirlState => overrideGirlState;
    public GirlStoryState GirlState => girlState;
    public string NextBeatId => nextBeatId;
    public bool CreatesCheckpoint => createsCheckpoint;
    public string CheckpointId => checkpointId;
    public IReadOnlyList<string> LegacyCompatibleNextBeatIds => legacyCompatibleNextBeatIds;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string beatId,
        StoryChapter storyChapter,
        string beatDisplayName,
        string nextId,
        bool checkpoint,
        float? hour = null,
        ObjectiveGroupDefinition objectives = null,
        IEnumerable<string> enterFacts = null,
        IEnumerable<string> completeFacts = null,
        string presentationSequenceKey = "",
        GameMode mode = GameMode.Gameplay,
        bool setGirlState = false,
        GirlStoryState targetGirlState = GirlStoryState.Hidden,
        IEnumerable<string> legacyNextBeatIds = null)
    {
        stableBeatId = beatId;
        chapter = storyChapter;
        displayName = beatDisplayName;
        nextBeatId = nextId;
        createsCheckpoint = checkpoint;
        checkpointId = checkpoint ? $"checkpoint.{beatId}" : string.Empty;
        overrideTime = hour.HasValue;
        targetHour = hour ?? targetHour;
        objectiveGroup = objectives;
        factsOnEnter = enterFacts == null ? new List<string>() : new List<string>(enterFacts);
        factsOnComplete = completeFacts == null ? new List<string>() : new List<string>(completeFacts);
        sequenceKey = presentationSequenceKey ?? string.Empty;
        entryGameMode = mode;
        overrideGirlState = setGirlState;
        girlState = targetGirlState;
        legacyCompatibleNextBeatIds = legacyNextBeatIds == null
            ? new List<string>()
            : new List<string>(legacyNextBeatIds);
    }
#endif

    private void OnValidate()
    {
        stableBeatId = Normalize(stableBeatId);
        nextBeatId = Normalize(nextBeatId);
        checkpointId = Normalize(checkpointId);
        sequenceKey = Normalize(sequenceKey);
        weatherProfileKey = Normalize(weatherProfileKey);

        if (legacyCompatibleNextBeatIds == null)
        {
            legacyCompatibleNextBeatIds = new List<string>();
        }

        for (int i = legacyCompatibleNextBeatIds.Count - 1; i >= 0; i--)
        {
            legacyCompatibleNextBeatIds[i] = Normalize(legacyCompatibleNextBeatIds[i]);
            if (legacyCompatibleNextBeatIds[i].Length == 0)
            {
                legacyCompatibleNextBeatIds.RemoveAt(i);
            }
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
