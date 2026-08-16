using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Light Tower/Story/Story Chapter",
    fileName = "StoryChapter")]
public sealed class StoryChapterDefinition : ScriptableObject
{
    [SerializeField] private StoryChapter chapter;
    [SerializeField] private string displayName;
    [SerializeField] private string initialBeatId;
    [SerializeField] private List<StoryBeatDefinition> beats = new();

    public StoryChapter Chapter => chapter;
    public string DisplayName => displayName;
    public string InitialBeatId => initialBeatId;
    public IReadOnlyList<StoryBeatDefinition> Beats => beats;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        StoryChapter storyChapter,
        string chapterDisplayName,
        string firstBeatId,
        IEnumerable<StoryBeatDefinition> chapterBeats)
    {
        chapter = storyChapter;
        displayName = chapterDisplayName;
        initialBeatId = firstBeatId;
        beats = chapterBeats == null
            ? new List<StoryBeatDefinition>()
            : new List<StoryBeatDefinition>(chapterBeats);
    }
#endif
}
