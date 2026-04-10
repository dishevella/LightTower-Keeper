public class Day1BridgeCollapseRelay : ControllerAbstract
{
    public void CollapseBridge()
    {
        this.SendCommand(new CollapseDay1BridgeCommand());
    }
}
