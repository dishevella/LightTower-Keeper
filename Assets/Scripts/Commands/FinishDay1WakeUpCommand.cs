public class FinishDay1WakeUpCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day1_InspectInside;
}
