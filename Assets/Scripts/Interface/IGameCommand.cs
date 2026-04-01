public interface IGameCommand: 
    IBelongToApp, 
    ICanSetApp,
    ICanGetSystem,
    ICanGetModel,
    ICanSendCommand
    
{
    void Execute();
}
