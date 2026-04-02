public class FinishDay1DockEncounterCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<GameFlowSystem>().EnterPhase(StoryPhase.Day1_ReturnRoute);
    }
}
