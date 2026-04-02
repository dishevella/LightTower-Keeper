public class FinishDay1ReturnRouteCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day1_NightDuty);
    }
}
