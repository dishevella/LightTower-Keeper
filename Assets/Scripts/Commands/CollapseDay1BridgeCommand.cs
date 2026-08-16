public class CollapseDay1BridgeCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        this.GetSystem<WorldStateSystem>()?.SetFact(WorldFactIds.BridgeCollapsed);
    }
}
