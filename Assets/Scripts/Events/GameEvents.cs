

public struct TaskChangedEvent
{
    public string TaskId;
    public string Title;
    public string Description;
    public bool IsCompleted;
}
public struct DayChangedEvent
{
    public int Day;
}
 public struct StoryPhaseChangedEvent
{
    public StoryPhase OldPhase;
    public StoryPhase NewPhase;
    public StoryPhaseChangedEvent(StoryPhase oldPhase, StoryPhase newPhase)
    {
        OldPhase = oldPhase;
        NewPhase = newPhase;
    }
}
public struct BoatIntroStartedEvent { }
public struct ForestPathStartedEvent { }
public struct ApproachLighthouseStartedEvent { }
public struct LastForestSubtitleFinshedEvent { }
public struct CheckInStartedEvent { }
public struct RestStartedEvent { }
public struct Day1StartedEvent { }
public struct Day1GoDockStartedEvent { }
public struct Day1ReturnRouteStartedEvent { }
public struct Day1NightDutyStartedEvent { }
public struct Day1CompletedEvent { }
public struct SmallAxeAcquiredEvent { }
public struct ChainsawAcquiredEvent { }
