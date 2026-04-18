public class FinishBoatIntroCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day0_Forest;
}
