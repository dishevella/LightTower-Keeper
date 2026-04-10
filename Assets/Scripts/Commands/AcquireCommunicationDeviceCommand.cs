public class AcquireCommunicationDeviceCommand : CommandAbstract
{
    protected override void OnExecute()
    {
        var inventorySystem = this.GetSystem<InventorySystem>();
        if (inventorySystem == null) return;

        if (!inventorySystem.AddItem(InventoryItemId.CommunicationDevice, true)) return;

        this.GetEvent().Send(new CommunicationDeviceAcquiredEvent());
    }
}
