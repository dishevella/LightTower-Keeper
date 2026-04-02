public class FinishDay1NightDutyCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day1_Complete);
    }
}
