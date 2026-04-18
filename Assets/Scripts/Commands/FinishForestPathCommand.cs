using UnityEngine;

public class FinishForestPathCommand : StoryPhaseTransitionCommand
{
    protected override StoryPhase TargetPhase => StoryPhase.Day0_ApproachLighthouse;
}

