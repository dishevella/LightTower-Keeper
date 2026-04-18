public class FinishApproachLighthouseCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day0_Rest;
}
