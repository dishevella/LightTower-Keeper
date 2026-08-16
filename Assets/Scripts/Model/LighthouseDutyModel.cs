using System;

[Serializable]
public class LighthouseDutyModel : ModelAbstract
{
    public BindableProperty<bool> GeneratorChecked { get; private set; }
    public BindableProperty<bool> LampRoomChecked { get; private set; }
    public BindableProperty<bool> LensChecked { get; private set; }
    public BindableProperty<bool> LightActivated { get; private set; }
    public BindableProperty<bool> BeamSweepCompleted { get; private set; }

    protected override void OnInit()
    {
        GeneratorChecked = new BindableProperty<bool>(false);
        LampRoomChecked = new BindableProperty<bool>(false);
        LensChecked = new BindableProperty<bool>(false);
        LightActivated = new BindableProperty<bool>(false);
        BeamSweepCompleted = new BindableProperty<bool>(false);
    }

    public void Restore(
        bool generatorChecked,
        bool lampRoomChecked,
        bool lensChecked,
        bool lightActivated,
        bool beamSweepCompleted)
    {
        GeneratorChecked.SetValueWithoutNotify(generatorChecked);
        LampRoomChecked.SetValueWithoutNotify(lampRoomChecked);
        LensChecked.SetValueWithoutNotify(lensChecked);
        LightActivated.SetValueWithoutNotify(lightActivated);
        BeamSweepCompleted.SetValueWithoutNotify(beamSweepCompleted);
    }
}
