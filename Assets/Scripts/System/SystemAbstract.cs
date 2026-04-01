using UnityEngine;

public abstract class SystemAbstract : IGameSystem
    
{
    private IApp App;
    IApp IBelongToApp.GetApp()
    {
        return App;
    }

    void ICanSetApp.SetApp(IApp app)
    {
        this.App = app;
    }

    void IGameSystem.Initialize()
    {
        OnInit();
    }

    public void Initialize()
    {
        OnInit();
    }
    protected abstract void OnInit();
}

