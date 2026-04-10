public class AdvanceTimeToNextPeriodCommand : CommandAbstract
{
    public int StepCount { get; }

    public AdvanceTimeToNextPeriodCommand(int stepCount = 1)
    {
        StepCount = stepCount;
    }

    protected override void OnExecute()
    {
        this.GetSystem<TimeSystem>()?.AdvanceToNextPeriod(StepCount);
    }
}

public class SetTimeOfDayPeriodCommand : CommandAbstract
{
    public TimeOfDayPeriod Period { get; }

    public SetTimeOfDayPeriodCommand(TimeOfDayPeriod period)
    {
        Period = period;
    }

    protected override void OnExecute()
    {
        this.GetSystem<TimeSystem>()?.SetPeriod(Period);
    }
}
