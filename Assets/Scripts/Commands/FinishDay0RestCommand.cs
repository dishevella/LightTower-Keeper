using UnityEngine;

public class FinishDay0RestCommand :CommandAbstract
{
    protected override void OnExecute()
    {
        
           this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day1_Start);
        
    }
}
