public class CollapseDay1BridgeCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        var routeModel = this.GetModel<Day1RouteModel>();
        if (routeModel == null) return;
        if (routeModel.BridgeCollapsed.Value) return;

        routeModel.BridgeCollapsed.Value = true;
        this.GetEvent().Send(new Day1BridgeCollapsedEvent());
    }
}
