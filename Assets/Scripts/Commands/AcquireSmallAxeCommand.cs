public class AcquireSmallAxeCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        var toolInventory = this.GetModel<ToolInventoryModel>();
        if (toolInventory == null) return;
        if (toolInventory.HasSmallAxe.Value) return;

        toolInventory.HasSmallAxe.Value = true;
        this.GetEvent().Send(new SmallAxeAcquiredEvent());
    }
}
