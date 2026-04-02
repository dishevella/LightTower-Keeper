using System;

[Serializable]
public class LighthouseDutyModel : ModelAbstract
{
    public BindableProperty<bool> GeneratorChecked { get; private set; }
    public BindableProperty<bool> LampRoomChecked { get; private set; }
    public BindableProperty<bool> LensChecked { get; private set; }
    public BindableProperty<bool> LightActivated { get; private set; }

    protected override void OnInit()
    {
        GeneratorChecked = new BindableProperty<bool>(false);
        LampRoomChecked = new BindableProperty<bool>(false);
        LensChecked = new BindableProperty<bool>(false);
        LightActivated = new BindableProperty<bool>(false);
    }
}
