public sealed class GirlStorySystem : SystemAbstract
{
    private GirlStoryStateModel model;

    protected override void OnInit()
    {
        model = this.GetModel<GirlStoryStateModel>();
    }

    public bool SetState(GirlStoryState state)
    {
        if (model == null || model.CurrentState.Value == state)
        {
            return false;
        }

        GirlStoryState previous = model.CurrentState.Value;
        model.CurrentState.Value = state;
        this.GetEvent().Send(new GirlStoryStateChangedEvent(previous, state));
        return true;
    }
}
