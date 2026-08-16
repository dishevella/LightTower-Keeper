using System;

[Serializable]
public class ToolInventoryModel : ModelAbstract
{
    public BindableProperty<bool> HasSmallAxe { get; private set; }
    public BindableProperty<bool> HasChainsaw { get; private set; }

    protected override void OnInit()
    {
        HasSmallAxe = new BindableProperty<bool>(false);
        HasChainsaw = new BindableProperty<bool>(false);
    }

    public void Restore(bool hasSmallAxe, bool hasChainsaw)
    {
        HasSmallAxe.SetValueWithoutNotify(hasSmallAxe);
        HasChainsaw.SetValueWithoutNotify(hasChainsaw);
    }
}
