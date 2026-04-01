using System;
using UnityEngine;

public class FinishBoatIntroCommand : CommandAbstract
{
    protected  override void OnExecute()
    {
        
            this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day0_Forest);
        
    }
}
