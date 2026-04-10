using System.Globalization;
using UnityEngine;

public class GameFlowSystem : SystemAbstract
{
    
    private GameStateModel gameState;

    protected override void OnInit()
    {
        gameState = this.GetModel<GameStateModel>();
    }
    public void StartGame()
    {
        EnterPhase(StoryPhase.Day0_BoatIntro);
    }
    public void EnterPhase(StoryPhase newphase)
    {
        if (gameState == null) return;
        if (gameState.CurrentPhase.Value == newphase) return;
        var oldphase = gameState.CurrentPhase.Value;
        gameState.CurrentPhase.Value = newphase;
        this.GetEvent().Send(new StoryPhaseChangedEvent(oldphase, newphase));

        switch (newphase)
        {
            case StoryPhase.Day0_BoatIntro:
                this.GetEvent().Send(new BoatIntroStartedEvent());
                break;

            case StoryPhase.Day0_Forest:
                this.GetEvent().Send(new ForestPathStartedEvent());
                break;

            case StoryPhase.Day0_ApproachLighthouse:
                this.GetEvent().Send(new ApproachLighthouseStartedEvent());
                break;

            case StoryPhase.Day0_Rest:
                this.GetEvent().Send(new RestStartedEvent());
                break;

            case StoryPhase.Day1_Start:
                this.GetEvent().Send(new Day1StartedEvent());
                break;

            case StoryPhase.Day1_GoDock:
                this.GetEvent().Send(new Day1GoDockStartedEvent());
                break;

            case StoryPhase.Day1_ReturnRoute:
                this.GetEvent().Send(new Day1ReturnRouteStartedEvent());
                break;

            case StoryPhase.Day1_NightDuty:
                this.GetEvent().Send(new Day1NightDutyStartedEvent());
                break;

            case StoryPhase.Day1_Complete:
                this.GetEvent().Send(new Day1CompletedEvent());
                break;
        }
    }
  
}
