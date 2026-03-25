using UnityEngine;
using System;
using System.Collections;
using System.Xml.Serialization;

public class TimeSystem : IGameSystem
{
    private GameApp app;
    private GameStateModel statemodel;

    public void Initialize(GameApp app)
    {
        this.app = app;
        statemodel = app.GetModel<GameStateModel>();
    }
    public void SetDay(int day)
    {
        statemodel.CurrentDay = day;
        Publish();
    }
    public void GoToNextDay()
    {
        statemodel.CurrentDay++;
        Publish();
    }
    private void Publish()
    {
        app.Events.Publish(new DayChangedEvent
        {
            Day = statemodel.CurrentDay
        });
    }
}
