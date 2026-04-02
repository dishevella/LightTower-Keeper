public class AcquireChainsawCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        var toolInventory = this.GetModel<ToolInventoryModel>();
        if (toolInventory == null) return;
        if (toolInventory.HasChainsaw.Value) return;

        toolInventory.HasChainsaw.Value = true;
        this.GetEvent().Send(new ChainsawAcquiredEvent());
    }
}
