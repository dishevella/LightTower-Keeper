using System;
using System.Collections.Generic;

[Serializable]
public sealed class StoryProgressModel : ModelAbstract
{
    private readonly HashSet<string> completedBeats = new(StringComparer.Ordinal);

    public BindableProperty<StoryChapter> CurrentChapter { get; private set; }
    public BindableProperty<string> CurrentBeatId { get; private set; }
    public BindableProperty<string> LastCheckpointId { get; private set; }
    public IReadOnlyCollection<string> CompletedBeats => completedBeats;

    protected override void OnInit()
    {
        completedBeats.Clear();
        CurrentChapter = new BindableProperty<StoryChapter>(StoryChapter.None);
        CurrentBeatId = new BindableProperty<string>(string.Empty);
        LastCheckpointId = new BindableProperty<string>(string.Empty);
    }

    public bool SetCurrent(StoryChapter chapter, string beatId)
    {
        string stableId = NormalizeId(beatId);
        if (chapter == StoryChapter.None || stableId.Length == 0)
        {
            return false;
        }

        if (CurrentChapter.Value == chapter &&
            string.Equals(CurrentBeatId.Value, stableId, StringComparison.Ordinal))
        {
            return false;
        }

        CurrentChapter.Value = chapter;
        CurrentBeatId.Value = stableId;
        return true;
    }

    public bool MarkCompleted(string beatId)
    {
        string stableId = NormalizeId(beatId);
        return stableId.Length > 0 && completedBeats.Add(stableId);
    }

    public bool IsCompleted(string beatId)
    {
        return completedBeats.Contains(NormalizeId(beatId));
    }

    public void SetCheckpoint(string checkpointId)
    {
        LastCheckpointId.Value = NormalizeId(checkpointId);
    }

    public void Restore(
        StoryChapter chapter,
        string beatId,
        IEnumerable<string> restoredCompletedBeats,
        string checkpointId)
    {
        completedBeats.Clear();
        if (restoredCompletedBeats != null)
        {
            foreach (string completedBeat in restoredCompletedBeats)
            {
                string stableId = NormalizeId(completedBeat);
                if (stableId.Length > 0)
                {
                    completedBeats.Add(stableId);
                }
            }
        }

        CurrentChapter.SetValueWithoutNotify(chapter);
        CurrentBeatId.SetValueWithoutNotify(NormalizeId(beatId));
        LastCheckpointId.SetValueWithoutNotify(NormalizeId(checkpointId));
    }

    public void ResetProgress()
    {
        completedBeats.Clear();
        CurrentChapter.Value = StoryChapter.None;
        CurrentBeatId.Value = string.Empty;
        LastCheckpointId.Value = string.Empty;
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
