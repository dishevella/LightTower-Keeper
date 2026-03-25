using System;
using UnityEngine;

public class FinishBoatIntroCommand : IGameCommand
{
    public void Execute(GameApp app)
    {
     app.GetSystem<GameFlowSystem>().OnBoatIntroFinished();
    }
}
