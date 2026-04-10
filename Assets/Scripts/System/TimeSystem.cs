using UnityEngine;
public class TimeSystem : SystemAbstract
{ 
    private const float DawnStartHour = 5f;
    private const float MorningStartHour = 8f;
    private const float AfternoonStartHour = 12f;
    private const float DuskStartHour = 17f;
    private const float NightStartHour = 20f;

    private TimeOfDayModel timeModel;

    protected override void OnInit()
    {
        timeModel = this.GetModel<TimeOfDayModel>();
        if (timeModel == null) return;

        SetTime(6f);
    }

    public void AdvanceHours(float deltaHours)
    {
        if (timeModel == null) return;

        SetTime(timeModel.CurrentHour.Value + deltaHours);
    }

    public void SetTime(float hour)
    {
        if (timeModel == null) return;

        float wrappedHour = WrapHour(hour);
        TimeOfDayPeriod previousPeriod = timeModel.CurrentPeriod.Value;
        TimeOfDayPeriod newPeriod = EvaluatePeriod(wrappedHour);

        timeModel.CurrentHour.Value = wrappedHour;
        timeModel.NormalizedDayProgress.Value = wrappedHour / 24f;
        timeModel.CurrentPeriod.Value = newPeriod;

        if (previousPeriod != newPeriod)
        {
            this.GetEvent().Send(new TimeOfDayPeriodChangedEvent
            {
                OldPeriod = previousPeriod,
                NewPeriod = newPeriod
            });
        }

        this.GetEvent().Send(new TimeOfDayChangedEvent
        {
            Hour = wrappedHour,
            NormalizedDayProgress = wrappedHour / 24f,
            Period = newPeriod
        });
    }

    public void SetPeriod(TimeOfDayPeriod period)
    {
        SetTime(GetRepresentativeHour(period));
    }

    public void AdvanceToNextPeriod(int stepCount = 1)
    {
        if (timeModel == null) return;
        if (stepCount == 0) return;

        int direction = stepCount > 0 ? 1 : -1;
        int remaining = Mathf.Abs(stepCount);
        TimeOfDayPeriod period = timeModel.CurrentPeriod.Value;

        while (remaining > 0)
        {
            period = GetAdjacentPeriod(period, direction);
            remaining--;
        }

        SetPeriod(period);
    }

    public float GetCurrentHour()
    {
        return timeModel != null ? timeModel.CurrentHour.Value : 0f;
    }

    public static float WrapHour(float hour)
    {
        float wrapped = hour % 24f;
        if (wrapped < 0f)
        {
            wrapped += 24f;
        }

        return wrapped;
    }

    public static TimeOfDayPeriod EvaluatePeriod(float hour)
    {
        float wrapped = WrapHour(hour);

        if (wrapped >= DawnStartHour && wrapped < MorningStartHour)
        {
            return TimeOfDayPeriod.Dawn;
        }

        if (wrapped >= MorningStartHour && wrapped < AfternoonStartHour)
        {
            return TimeOfDayPeriod.Morning;
        }

        if (wrapped >= AfternoonStartHour && wrapped < DuskStartHour)
        {
            return TimeOfDayPeriod.Afternoon;
        }

        if (wrapped >= DuskStartHour && wrapped < NightStartHour)
        {
            return TimeOfDayPeriod.Dusk;
        }

        return TimeOfDayPeriod.Night;
    }

    public static float GetRepresentativeHour(TimeOfDayPeriod period)
    {
        switch (period)
        {
            case TimeOfDayPeriod.Dawn:
                return 6f;

            case TimeOfDayPeriod.Morning:
                return 9f;

            case TimeOfDayPeriod.Afternoon:
                return 14f;

            case TimeOfDayPeriod.Dusk:
                return 18.5f;

            case TimeOfDayPeriod.Night:
            default:
                return 22f;
        }
    }

    public static TimeOfDayPeriod GetAdjacentPeriod(TimeOfDayPeriod period, int direction)
    {
        if (direction >= 0)
        {
            switch (period)
            {
                case TimeOfDayPeriod.Dawn:
                    return TimeOfDayPeriod.Morning;
                case TimeOfDayPeriod.Morning:
                    return TimeOfDayPeriod.Afternoon;
                case TimeOfDayPeriod.Afternoon:
                    return TimeOfDayPeriod.Dusk;
                case TimeOfDayPeriod.Dusk:
                    return TimeOfDayPeriod.Night;
                case TimeOfDayPeriod.Night:
                default:
                    return TimeOfDayPeriod.Dawn;
            }
        }

        switch (period)
        {
            case TimeOfDayPeriod.Dawn:
                return TimeOfDayPeriod.Night;
            case TimeOfDayPeriod.Morning:
                return TimeOfDayPeriod.Dawn;
            case TimeOfDayPeriod.Afternoon:
                return TimeOfDayPeriod.Morning;
            case TimeOfDayPeriod.Dusk:
                return TimeOfDayPeriod.Afternoon;
            case TimeOfDayPeriod.Night:
            default:
                return TimeOfDayPeriod.Dusk;
        }
    }

}
