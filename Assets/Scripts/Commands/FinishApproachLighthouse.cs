
public class FinishApproachLighthouseCommand : CommandAbstract
{
    protected override void OnExecute()
    {

        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day0_Rest);

    }
}