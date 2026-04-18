using UnityEngine;

public class FinishDay0RestCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day1_Start;
}
