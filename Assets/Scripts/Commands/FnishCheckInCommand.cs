using UnityEngine;


public class FinishCheckInCommand : CommandAbstract
{
    protected override void OnExecute()
    {

        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day0_Rest);

    }
}