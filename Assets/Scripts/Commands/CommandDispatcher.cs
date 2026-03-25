public class CommandDispatcher
{
    private readonly GameApp app;
    public CommandDispatcher(GameApp app)
    {
        this.app = app;
    }
    public void Dispatch(IGameCommand command)
    {
        command.Execute(app);
    }
}
