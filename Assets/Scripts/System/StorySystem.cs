using System;
using System.Collections.Generic;

public sealed class StorySystem : SystemAbstract
{
    private StoryProgressModel progress;

    protected override void OnInit()
    {
        progress = this.GetModel<StoryProgressModel>();
    }

    public bool CommitBeatEntry(StoryChapter chapter, string beatId, bool restored = false)
    {
        if (progress == null || chapter == StoryChapter.None || string.IsNullOrWhiteSpace(beatId))
        {
            return false;
        }

        string stableId = beatId.Trim();
        bool changed = progress.SetCurrent(chapter, stableId);
        if (!changed && !restored)
        {
            return false;
        }

        this.GetEvent().Send(new StoryBeatEnteredEvent(chapter, stableId, restored));
        return true;
    }

    public bool CommitBeatCompletion(string beatId)
    {
        if (progress == null || string.IsNullOrWhiteSpace(beatId))
        {
            return false;
        }

        string stableId = beatId.Trim();
        if (!string.Equals(progress.CurrentBeatId.Value, stableId, StringComparison.Ordinal) ||
            !progress.MarkCompleted(stableId))
        {
            return false;
        }

        this.GetEvent().Send(new StoryBeatCompletedEvent(progress.CurrentChapter.Value, stableId));
        return true;
    }

    public void RestoreProgress(
        StoryChapter chapter,
        string beatId,
        IEnumerable<string> completedBeats,
        string checkpointId)
    {
        if (progress == null)
        {
            return;
        }

        progress.Restore(chapter, beatId, completedBeats, checkpointId);
    }
}
