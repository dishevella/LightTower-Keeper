public sealed class GameModeSystem : SystemAbstract
{
    private GameModeModel model;

    protected override void OnInit()
    {
        model = this.GetModel<GameModeModel>();
    }

    public bool SetMode(GameMode newMode)
    {
        if (model == null || model.CurrentMode.Value == newMode)
        {
            return false;
        }

        GameMode oldMode = model.CurrentMode.Value;
        model.PreviousMode.Value = oldMode;
        model.CurrentMode.Value = newMode;
        this.GetEvent().Send(new GameModeChangedEvent(oldMode, newMode));
        return true;
    }

    public bool RestorePreviousMode()
    {
        return model != null && SetMode(model.PreviousMode.Value);
    }
}
