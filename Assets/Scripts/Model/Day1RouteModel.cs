using System;

[Serializable]
public class Day1RouteModel : ModelAbstract
{
    public BindableProperty<bool> BridgeCollapsed { get; private set; }

    protected override void OnInit()
    {
        BridgeCollapsed = new BindableProperty<bool>(false);
    }
}
