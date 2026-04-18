public class FinishDay1DockEncounterCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day1_ReturnRoute;
}
