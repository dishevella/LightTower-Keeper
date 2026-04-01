using UnityEngine;

public class FinishForestPathCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        
            this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day0_ApproachLighthouse);
        
    }
}

