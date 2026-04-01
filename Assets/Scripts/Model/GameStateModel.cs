using UnityEngine;
using System;
[Serializable]
public class GameStateModel: ModelAbstract
{
    public BindableProperty<StoryPhase> CurrentPhase { get; private set; }

    protected override void OnInit()
    {
        CurrentPhase = new BindableProperty<StoryPhase>(StoryPhase.None);
    }
}