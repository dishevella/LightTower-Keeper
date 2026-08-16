using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Light Tower/Story/Story Catalog",
    fileName = "StoryCatalog")]
public sealed class StoryCatalog : ScriptableObject
{
    public const string ResourcesPath = "Story/StoryCatalog";

    [SerializeField] private List<StoryChapterDefinition> chapters = new();
    [SerializeField] private List<StoryBeatDefinition> beats = new();

    private Dictionary<string, StoryBeatDefinition> beatLookup;
    private Dictionary<StoryChapter, StoryChapterDefinition> chapterLookup;

    public IReadOnlyList<StoryChapterDefinition> Chapters => chapters;
    public IReadOnlyList<StoryBeatDefinition> Beats => beats;

    public StoryBeatDefinition FindBeat(string beatId)
    {
        EnsureLookup();
        return !string.IsNullOrWhiteSpace(beatId) &&
               beatLookup.TryGetValue(beatId.Trim(), out StoryBeatDefinition definition)
            ? definition
            : null;
    }

    public StoryChapterDefinition FindChapter(StoryChapter chapter)
    {
        EnsureLookup();
        return chapterLookup.TryGetValue(chapter, out StoryChapterDefinition definition)
            ? definition
            : null;
    }

    public bool Validate(out string error)
    {
        HashSet<string> declaredBeatIds = new(StringComparer.Ordinal);
        foreach (StoryBeatDefinition beat in beats)
        {
            if (beat == null)
            {
                error = "Story catalog contains a null beat reference.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(beat.StableBeatId))
            {
                error = "Story catalog contains a beat without a stable ID.";
                return false;
            }

            if (!declaredBeatIds.Add(beat.StableBeatId))
            {
                error = $"Story catalog contains duplicate beat ID '{beat.StableBeatId}'.";
                return false;
            }
        }

        beatLookup = null;
        chapterLookup = null;
        EnsureLookup();
        foreach (string requiredBeatId in StoryBeatIds.All)
        {
            if (!beatLookup.ContainsKey(requiredBeatId))
            {
                error = $"Story catalog is missing required beat '{requiredBeatId}'.";
                return false;
            }
        }

        foreach (StoryBeatDefinition beat in beats)
        {
            if (!string.IsNullOrWhiteSpace(beat.NextBeatId) && !beatLookup.ContainsKey(beat.NextBeatId))
            {
                error = $"Beat '{beat.StableBeatId}' points to missing next beat '{beat.NextBeatId}'.";
                return false;
            }

            foreach (string compatibleTarget in beat.LegacyCompatibleNextBeatIds)
            {
                if (!beatLookup.ContainsKey(compatibleTarget))
                {
                    error =
                        $"Beat '{beat.StableBeatId}' has missing legacy-compatible target " +
                        $"'{compatibleTarget}'.";
                    return false;
                }
            }
        }

        HashSet<string> reachable = new(StringComparer.Ordinal);
        string currentBeatId = StoryBeatIds.Day0.ArriveIsland;
        string terminalBeatId = string.Empty;
        while (!string.IsNullOrWhiteSpace(currentBeatId))
        {
            if (!reachable.Add(currentBeatId))
            {
                error = $"Canonical story chain contains a cycle at '{currentBeatId}'.";
                return false;
            }

            StoryBeatDefinition current = FindBeat(currentBeatId);
            if (current == null)
            {
                error = $"Canonical story chain points to missing beat '{currentBeatId}'.";
                return false;
            }

            terminalBeatId = current.StableBeatId;
            currentBeatId = current.NextBeatId;
        }

        if (reachable.Count != declaredBeatIds.Count)
        {
            List<string> unreachable = new();
            foreach (string beatId in declaredBeatIds)
            {
                if (!reachable.Contains(beatId))
                {
                    unreachable.Add(beatId);
                }
            }

            unreachable.Sort(StringComparer.Ordinal);
            error = $"Canonical story chain has unreachable beats: {string.Join(", ", unreachable)}.";
            return false;
        }

        if (!string.Equals(terminalBeatId, StoryBeatIds.Day4.Ending, StringComparison.Ordinal))
        {
            error =
                $"Canonical story chain terminates at '{terminalBeatId}', " +
                $"not '{StoryBeatIds.Day4.Ending}'.";
            return false;
        }

        error = string.Empty;
        return true;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        IEnumerable<StoryChapterDefinition> storyChapters,
        IEnumerable<StoryBeatDefinition> storyBeats)
    {
        chapters = storyChapters == null
            ? new List<StoryChapterDefinition>()
            : new List<StoryChapterDefinition>(storyChapters);
        beats = storyBeats == null
            ? new List<StoryBeatDefinition>()
            : new List<StoryBeatDefinition>(storyBeats);
        beatLookup = null;
        chapterLookup = null;
    }
#endif

    private void OnEnable()
    {
        beatLookup = null;
        chapterLookup = null;
    }

    private void EnsureLookup()
    {
        if (beatLookup != null && chapterLookup != null)
        {
            return;
        }

        beatLookup = new Dictionary<string, StoryBeatDefinition>(StringComparer.Ordinal);
        foreach (StoryBeatDefinition beat in beats)
        {
            if (beat == null || string.IsNullOrWhiteSpace(beat.StableBeatId))
            {
                continue;
            }

            beatLookup[beat.StableBeatId] = beat;
        }

        chapterLookup = new Dictionary<StoryChapter, StoryChapterDefinition>();
        foreach (StoryChapterDefinition chapter in chapters)
        {
            if (chapter != null && chapter.Chapter != StoryChapter.None)
            {
                chapterLookup[chapter.Chapter] = chapter;
            }
        }
    }
}
