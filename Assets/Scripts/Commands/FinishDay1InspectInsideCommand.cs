public class FinishDay1InspectInsideCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day1_GoDock);
    }
}
