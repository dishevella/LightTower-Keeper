using System.Collections.Generic;

public sealed class WorldStateSystem : SystemAbstract
{
    private WorldStateModel worldState;
    private Day1RouteModel legacyRoute;

    protected override void OnInit()
    {
        worldState = this.GetModel<WorldStateModel>();
        legacyRoute = this.GetModel<Day1RouteModel>();
        SyncLegacyModels();
    }

    public bool HasFact(string factId)
    {
        return worldState != null && worldState.HasFact(factId);
    }

    public bool SetFact(string factId, bool enabled = true)
    {
        if (worldState == null || !worldState.SetFact(factId, enabled))
        {
            return false;
        }

        SyncLegacyFact(factId, enabled);
        this.GetEvent().Send(new WorldFactChangedEvent(factId.Trim(), enabled));
        return true;
    }

    public void Restore(IEnumerable<string> facts)
    {
        if (worldState == null)
        {
            return;
        }

        worldState.Restore(facts);
        SyncLegacyModels();
    }

    public void Clear()
    {
        worldState?.Clear();
        SyncLegacyModels();
    }

    private void SyncLegacyModels()
    {
        if (legacyRoute != null)
        {
            legacyRoute.BridgeCollapsed.Value = HasFact(WorldFactIds.BridgeCollapsed);
        }
    }

    private void SyncLegacyFact(string factId, bool enabled)
    {
        if (factId == WorldFactIds.BridgeCollapsed && legacyRoute != null)
        {
            legacyRoute.BridgeCollapsed.Value = enabled;
            if (enabled)
            {
                this.GetEvent().Send(new Day1BridgeCollapsedEvent());
            }
        }
    }
}
