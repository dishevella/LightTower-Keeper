public sealed class StartNewGameCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<StoryDirectorSystem>()?.StartNewGame();
    }
}

public sealed class EnterStoryBeatCommand : CommandAbstract
{
    public EnterStoryBeatCommand(string beatId)
    {
        BeatId = beatId;
    }

    public string BeatId { get; }

    protected override void OnExecute()
    {
        this.GetSystem<StoryDirectorSystem>()?.JumpToBeatForDevelopment(BeatId);
    }
}

public sealed class CompleteStoryBeatCommand : CommandAbstract
{
    public CompleteStoryBeatCommand(string beatId, string source = "Gameplay")
    {
        BeatId = beatId;
        Source = source;
    }

    public string BeatId { get; }
    public string Source { get; }

    protected override void OnExecute()
    {
        this.GetSystem<StoryDirectorSystem>()?.CompleteCurrentBeat(BeatId, Source);
    }
}

public sealed class ReportStorySequenceCompletedCommand : CommandAbstract
{
    public ReportStorySequenceCompletedCommand(string beatId, string sequenceKey)
    {
        BeatId = beatId;
        SequenceKey = sequenceKey;
    }

    public string BeatId { get; }
    public string SequenceKey { get; }

    protected override void OnExecute()
    {
        this.GetSystem<StoryDirectorSystem>()?.ReportSequenceCompleted(BeatId, SequenceKey);
    }
}
