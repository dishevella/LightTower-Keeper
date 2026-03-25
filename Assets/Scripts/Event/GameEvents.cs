public struct BoatIntroStartedEvent
{

}
public struct StoryPhaseChangedEvent
{
   public StoryPhase Phase;
}
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
 public struct StoryPhaseChangedEvents
{
    public StoryPhase OldPhase;
    public StoryPhase NewPhase;
    public StoryPhaseChangedEvents(StoryPhase oldPhase, StoryPhase newPhase)
    {
        OldPhase = oldPhase;
        NewPhase = newPhase;
    }
}

public struct ForestPathStartedEvent { }
