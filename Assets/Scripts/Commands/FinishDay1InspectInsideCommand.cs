public class FinishDay1InspectInsideCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day1_GoDock;
}
