using System;

[Serializable]
public sealed class GirlStoryStateModel : ModelAbstract
{
    public BindableProperty<GirlStoryState> CurrentState { get; private set; }

    protected override void OnInit()
    {
        CurrentState = new BindableProperty<GirlStoryState>(GirlStoryState.Hidden);
    }

    public void Restore(GirlStoryState state)
    {
        CurrentState.SetValueWithoutNotify(state);
    }
}
