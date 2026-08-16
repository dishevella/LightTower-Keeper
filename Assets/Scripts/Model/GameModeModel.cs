using System;

[Serializable]
public sealed class GameModeModel : ModelAbstract
{
    public BindableProperty<GameMode> CurrentMode { get; private set; }
    public BindableProperty<GameMode> PreviousMode { get; private set; }

    protected override void OnInit()
    {
        CurrentMode = new BindableProperty<GameMode>(GameMode.Boot);
        PreviousMode = new BindableProperty<GameMode>(GameMode.Boot);
    }
}
