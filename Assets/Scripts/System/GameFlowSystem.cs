using System.Globalization;
using UnityEngine;

public class GameFlowSystem : IGameSystem
{
    private GameApp app;
    private TaskSystem taskSystem;
    public StoryPhase CurrentPhase { get; private set; } = StoryPhase.None;

    public void Initialize(GameApp app)
    {
        this.app = app;
        taskSystem = app.GetSystem<TaskSystem>();
    }
    public void StartGame()
    {
        EnterPhase(StoryPhase.Day0_BoatIntro);
    }
    public void EnterPhase(StoryPhase phase)
    {
        if (CurrentPhase == phase) return;
        CurrentPhase = phase;
        app.Events.Publish(new StoryPhaseChangedEvent
        {
            Phase = phase
        });

        switch (phase)
        {
            case StoryPhase.Day0_BoatIntro:
                app.Events.Publish(new BoatIntroStartedEvent());
                break;

            case StoryPhase.Day0_BeachPath:
                taskSystem.SetTask(
                   "goto_lighthouse","go to the lighthouse","walk along the path, finding the lighthouse"
                );
                break;

            case StoryPhase.Day0_GoSleep:
                taskSystem.SetTask(
                    "TASK_SLEEP",
                    "Go to Bed",
                    "Sleep to begin the next day."
                );
                break;

            case StoryPhase.Day1_Start:
                EnterPhase(StoryPhase.Day1_InspectInside);
                break;

            case StoryPhase.Day1_InspectInside:
                taskSystem.SetTask(
                    "TASK_DAY1_INSPECT",
                    "Inspect the Lighthouse",
                    "Check the interior of the lighthouse."
                );
                break;
        }
    }
    public void OnBoatIntroFinished()
    {
        EnterPhase(StoryPhase.Day0_BeachPath);
    }
  
}