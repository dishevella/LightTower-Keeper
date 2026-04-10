using UnityEngine;

public class Day1NightDutyController : ControllerAbstract
{
    private const string Task_InspectDuty = "task_night_duty_inspect";
    private const string Task_ActivateLight = "task_activate_lighthouse";

    
    [SerializeField] private GameObject[] objectsToEnableOnStart;
    [SerializeField] private GameObject[] objectsToDisableOnStart;
    [SerializeField] private GameObject[] objectsToEnableWhenReadyToActivate;

    private LighthouseDutyModel dutyModel;
    private bool activePhase;

    private void Start()
    {
        dutyModel = this.GetModel<LighthouseDutyModel>();

        if (dutyModel != null)
        {
            dutyModel.GeneratorChecked.OnValueChanged += OnDutyCheckpointChanged;
            dutyModel.LampRoomChecked.OnValueChanged += OnDutyCheckpointChanged;
            dutyModel.LensChecked.OnValueChanged += OnDutyCheckpointChanged;
        }

        this.GetEvent().Register<Day1NightDutyStartedEvent>(OnDay1NightDutyStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDestroy()
    {
        if (dutyModel == null) return;

        dutyModel.GeneratorChecked.OnValueChanged -= OnDutyCheckpointChanged;
        dutyModel.LampRoomChecked.OnValueChanged -= OnDutyCheckpointChanged;
        dutyModel.LensChecked.OnValueChanged -= OnDutyCheckpointChanged;
    }

    private void OnDay1NightDutyStarted(Day1NightDutyStartedEvent evt)
    {
        activePhase = true;

        if (dutyModel != null)
        {
            dutyModel.GeneratorChecked.Value = false;
            dutyModel.LampRoomChecked.Value = false;
            dutyModel.LensChecked.Value = false;
            dutyModel.LightActivated.Value = false;
        }

        SetObjectsActive(objectsToEnableOnStart, true);
        SetObjectsActive(objectsToDisableOnStart, false);
        SetObjectsActive(objectsToEnableWhenReadyToActivate, false);

        PublishInspectionTask();
    }

    private void OnDutyCheckpointChanged(bool _)
    {
        if (!activePhase || dutyModel == null) return;

        bool readyToActivate =
            dutyModel.GeneratorChecked.Value
            && dutyModel.LampRoomChecked.Value
            && dutyModel.LensChecked.Value;

        SetObjectsActive(objectsToEnableWhenReadyToActivate, readyToActivate);

        if (readyToActivate)
        {
            this.SendCommand(new SetTaskCommand(
                Task_ActivateLight,
                "Activate the Lighthouse Light",
                "Turn on the lighthouse light and confirm it is operating normally."));
        }
        else
        {
            PublishInspectionTask();
        }
    }

    private void PublishInspectionTask()
    {
        this.SendCommand(new SetTaskCommand(
            Task_InspectDuty,
            "Inspect the Generator and Lamp Room",
            "Check the generator, lamp room, and lens before turning on the lighthouse light."));
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
