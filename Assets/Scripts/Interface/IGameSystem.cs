
public interface IGameSystem : 
    IBelongToApp,
    ICanSetApp,
    ICanGetSystem,
    ICanGetModel,
    ICanSendCommand,
    ICanGetEvent
{
    void Initialize();
}