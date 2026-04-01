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
