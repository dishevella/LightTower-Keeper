public class FinishCheckInCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day0_Rest;
}
