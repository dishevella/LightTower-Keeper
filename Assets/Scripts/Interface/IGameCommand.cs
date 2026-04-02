public interface IGameCommand: 
    IBelongToApp, 
    ICanSetApp,
    ICanGetSystem,
    ICanGetModel,
    ICanSendCommand,
    ICanGetEvent
    
{
    void Execute();
}
