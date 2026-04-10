public class AdvanceTimeByHoursCommand : CommandAbstract
{
    public float Hours { get; }

    public AdvanceTimeByHoursCommand(float hours)
    {
        Hours = hours;
    }

    protected override void OnExecute()
    {
        this.GetSystem<TimeSystem>()?.AdvanceHours(Hours);
    }
}

public class SetTimeOfDayCommand : CommandAbstract
{
    public float Hour { get; }

    public SetTimeOfDayCommand(float hour)
    {
        Hour = hour;
    }

    protected override void OnExecute()
    {
        this.GetSystem<TimeSystem>()?.SetTime(Hour);
    }
}
