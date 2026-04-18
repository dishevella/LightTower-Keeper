using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GeneratorStartupController : ControllerAbstract
{
    public enum StartupStep
    {
        OpenHatch = 0,
        PullFirstLever = 1,
        PullSecondLever = 2,
        PressButton = 3,
        Completed = 4
    }

    [Header("Sequence")]
    [SerializeField] private float completeDelay = 0.6f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSequenceReset;
    [SerializeField] private UnityEvent onHatchOpened;
    [SerializeField] private UnityEvent onFirstLeverPulled;
    [SerializeField] private UnityEvent onSecondLeverPulled;
    [SerializeField] private UnityEvent onButtonPressed;
    [SerializeField] private UnityEvent onGeneratorStarted;

    private LighthouseDutyModel dutyModel;
    private StartupStep currentStep = StartupStep.OpenHatch;
    private bool activePhase;

    private void Start()
    {
        dutyModel = this.GetModel<LighthouseDutyModel>();

        this.GetEvent().Register<Day1NightDutyStartedEvent>(OnDay1NightDutyStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    public bool IsStepAvailable(StartupStep step)
    {
        if (!activePhase) return false;
        if (dutyModel != null && dutyModel.GeneratorChecked.Value) return false;
        return currentStep == step;
    }

    public void PerformStep(StartupStep step)
    {
        if (!IsStepAvailable(step))
        {
            return;
        }

        switch (step)
        {
            case StartupStep.OpenHatch:
                onHatchOpened?.Invoke();
                currentStep = StartupStep.PullFirstLever;
                break;

            case StartupStep.PullFirstLever:
                onFirstLeverPulled?.Invoke();
                currentStep = StartupStep.PullSecondLever;
                break;

            case StartupStep.PullSecondLever:
                onSecondLeverPulled?.Invoke();
                currentStep = StartupStep.PressButton;
                break;

            case StartupStep.PressButton:
                currentStep = StartupStep.Completed;
                onButtonPressed?.Invoke();
                StartCoroutine(FinishStartupRoutine());
                break;
        }
    }

    private void OnDay1NightDutyStarted(Day1NightDutyStartedEvent evt)
    {
        activePhase = true;
        currentStep = StartupStep.OpenHatch;
        StopAllCoroutines();
        onSequenceReset?.Invoke();
    }

    private IEnumerator FinishStartupRoutine()
    {
        if (completeDelay > 0f)
        {
            yield return new WaitForSeconds(completeDelay);
        }

        if (dutyModel != null)
        {
            dutyModel.GeneratorChecked.Value = true;
        }

        onGeneratorStarted?.Invoke();
    }
}
