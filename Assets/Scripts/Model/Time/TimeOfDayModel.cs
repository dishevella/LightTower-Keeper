using System;

[Serializable]
public class TimeOfDayModel : ModelAbstract
{
    public BindableProperty<float> CurrentHour { get; private set; }
    public BindableProperty<float> NormalizedDayProgress { get; private set; }
    public BindableProperty<TimeOfDayPeriod> CurrentPeriod { get; private set; }

    protected override void OnInit()
    {
        CurrentHour = new BindableProperty<float>(6f);
        NormalizedDayProgress = new BindableProperty<float>(6f / 24f);
        CurrentPeriod = new BindableProperty<TimeOfDayPeriod>(TimeOfDayPeriod.Dawn);
    }
}
