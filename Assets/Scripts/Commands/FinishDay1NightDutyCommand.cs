public class FinishDay1NightDutyCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day1_Complete;
}
