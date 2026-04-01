using UnityEngine;

public abstract class ModelAbstract: IGameModel
    
{
    private IApp app;
    void ICanSetApp.SetApp(IApp app)
    {
        this.app = app;
    }
    IApp IBelongToApp.GetApp()
    {
        return app;
    }
    void IGameModel.Initialize()
    {
        OnInit();
    }
    protected abstract void OnInit();
}
