
using UnityEngine;

public interface IGameController :
    IBelongToApp,
    ICanGetSystem,
    ICanGetModel,
    ICanSendCommand,
    ICanGetEvent
{
   
}
