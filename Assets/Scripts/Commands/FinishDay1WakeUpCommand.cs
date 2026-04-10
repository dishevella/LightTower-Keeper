public class FinishDay1WakeUpCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day1_InspectInside);
    }
}
