using UnityEngine;

public class Day1NightDutyController : ControllerAbstract
{
    private const string Task_StartGenerator = "task_start_generator";
    private const string Task_PrepareLampRoom = "task_prepare_lamp_room";
    private const string Task_ActivateLight = "task_activate_lighthouse";
    private const string Task_SweepShoreline = "task_sweep_shoreline";

    [SerializeField] private GameObject[] objectsToEnableOnStart;
    [SerializeField] private GameObject[] objectsToDisableOnStart;
    [SerializeField] private GameObject[] objectsToEnableWhenReadyToActivate;

    private LighthouseDutyModel dutyModel;
    private bool activePhase;
    private bool completed;

    private void Start()
    {
        dutyModel = this.GetModel<LighthouseDutyModel>();

        if (dutyModel != null)
        {
            dutyModel.GeneratorChecked.OnValueChanged += OnDutyStateChanged;
            dutyModel.LampRoomChecked.OnValueChanged += OnDutyStateChanged;
            dutyModel.LightActivated.OnValueChanged += OnDutyStateChanged;
            dutyModel.BeamSweepCompleted.OnValueChanged += OnDutyStateChanged;
        }

        this.GetEvent().Register<Day1NightDutyStartedEvent>(OnDay1NightDutyStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDestroy()
    {
        if (dutyModel == null) return;

        dutyModel.GeneratorChecked.OnValueChanged -= OnDutyStateChanged;
        dutyModel.LampRoomChecked.OnValueChanged -= OnDutyStateChanged;
        dutyModel.LightActivated.OnValueChanged -= OnDutyStateChanged;
        dutyModel.BeamSweepCompleted.OnValueChanged -= OnDutyStateChanged;
    }

    private void OnDay1NightDutyStarted(Day1NightDutyStartedEvent evt)
    {
        activePhase = false;
        completed = false;

        if (dutyModel != null)
        {
            dutyModel.GeneratorChecked.Value = false;
            dutyModel.LampRoomChecked.Value = false;
            dutyModel.LensChecked.Value = false;
            dutyModel.LightActivated.Value = false;
            dutyModel.BeamSweepCompleted.Value = false;
        }

        SetObjectsActive(objectsToEnableOnStart, true);
        SetObjectsActive(objectsToDisableOnStart, false);
        SetObjectsActive(objectsToEnableWhenReadyToActivate, false);

        activePhase = true;
        RefreshDutyState();
    }

    private void OnDutyStateChanged(bool _)
    {
        if (!activePhase || dutyModel == null) return;

        RefreshDutyState();
    }

    private void RefreshDutyState()
    {
        if (dutyModel == null) return;

        bool generatorStarted = dutyModel.GeneratorChecked.Value;
        bool lampRoomReady = dutyModel.LampRoomChecked.Value;
        bool lightActivated = dutyModel.LightActivated.Value;
        bool beamSweepCompleted = dutyModel.BeamSweepCompleted.Value;

        SetObjectsActive(objectsToEnableWhenReadyToActivate, lampRoomReady);

        if (beamSweepCompleted)
        {
            if (!completed)
            {
                completed = true;
                activePhase = false;
                this.SendCommand(new FinishDay1NightDutyCommand());
            }
            return;
        }

        if (lightActivated)
        {
            this.SendCommand(new SetTaskCommand(
                Task_SweepShoreline,
                "Sweep the Shoreline",
                "Rotate the lighthouse beam for a moment, then let it settle once the sweep feels steady."));
            return;
        }

        if (lampRoomReady)
        {
            this.SendCommand(new SetTaskCommand(
                Task_ActivateLight,
                "Light the Beacon",
                "Ignite the lighthouse beacon and bring the main beam online."));
            return;
        }

        if (generatorStarted)
        {
            this.SendCommand(new SetTaskCommand(
                Task_PrepareLampRoom,
                "Prepare the Lamp Room",
                "Head up to the lamp room and bring the lighting system into working order."));
            return;
        }

        this.SendCommand(new SetTaskCommand(
            Task_StartGenerator,
            "Start the Generator",
            "Get the generator running before the night watch can begin."));
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
