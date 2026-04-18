public class FinishDay1ReturnRouteCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day1_NightDuty;
}
