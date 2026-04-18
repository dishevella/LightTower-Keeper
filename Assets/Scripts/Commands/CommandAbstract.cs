public abstract class CommandAbstract: IGameCommand
{
    protected IApp App;
    IApp IBelongToApp.GetApp()
    {
        return App;
    }
    void ICanSetApp.SetApp(IApp app)
    {
        this.App = app;
    }
    void IGameCommand.Execute()
    {
        OnExecute();
    }
    protected abstract void OnExecute();
}

public class EnterStoryPhaseCommand : CommandAbstract
{
    public StoryPhase TargetPhase { get; }

    public EnterStoryPhaseCommand(StoryPhase targetPhase)
    {
        TargetPhase = targetPhase;
    }

    protected override void OnExecute()
    {
        this.GetSystem<GameFlowSystem>()?.EnterPhase(TargetPhase);
    }
}

public abstract class StoryPhaseTransitionCommand : CommandAbstract
{
    protected abstract StoryPhase TargetPhase { get; }

    protected sealed override void OnExecute()
    {
        this.SendCommand(new EnterStoryPhaseCommand(TargetPhase));
    }
}
