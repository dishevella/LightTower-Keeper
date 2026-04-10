using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TransmissionRequestEvent : UnityEvent<string, string>
{
}

public class Day1WakeUpController : ControllerAbstract
{
    [Header("Gameplay Player")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera gameplayCamera;

    [Header("Wake Up Cutscene")]
    [SerializeField] private GameObject wakeUpCutsceneRoot;
    [SerializeField] private Transform wakeUpCutsceneActorRoot;
    [SerializeField] private Animator wakeUpCutsceneAnimator;
    [SerializeField] private Animator wakeUpCameraAnimator;
    [SerializeField] private Camera wakeUpCutsceneCamera;
    [SerializeField] private Transform wakeUpStartPoint;
    [SerializeField] private Transform wakeUpHandoffPoint;
    [SerializeField] private string typingStateName = "Typing";
    [SerializeField] private string cameraTrackStateName = "WakeUp_CameraTrack";
    [SerializeField] private string reachingTriggerName = "StartReaching";
    [SerializeField] private string reachingReverseTriggerName = "StartReachingReverse";
    [SerializeField] private string checkingTriggerName = "StartChecking";
    [SerializeField] private string standUpTriggerName = "StartStandUp";

    [Header("UI")]
    [SerializeField] private DayTransitionPanel dayTransitionPanel;
    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private int dayNumber = 1;
    [SerializeField] private HQTransmissionPanel transmissionPanel; 

    [Header("Player Lock")]
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    [Header("Timing")]
    [SerializeField] private float holdBeforeReveal = 0.2f;
    [SerializeField] private float revealDuration = 1.2f;
    [SerializeField] private float typingDuration = 10f;
    [SerializeField] private float notificationDelay = 0.1f;
    [SerializeField] private float reachingDuration = 2f;
    [SerializeField] private float reachingReverseDuration = 2f;
    [SerializeField] private float checkingDuration = 2f;
    [SerializeField] private float standUpDuration = 1.5f;
    [SerializeField] private float standAfterDuration = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioSource notificationAudioSource;

    [Header("Communication Device")]
    [SerializeField] private GameObject deskCommunicationDeviceObject;
    [SerializeField] private GameObject handCommunicationDeviceObject;

    [Header("Scene State")]
    [SerializeField] private GameObject[] objectsToEnableOnStart;
    [SerializeField] private GameObject[] objectsToDisableOnStart;

    [Header("Cutscene Visibility")]
    [SerializeField] private GameObject[] objectsToEnableDuringCutscene;
    [SerializeField] private GameObject[] objectsToDisableDuringCutscene;

    private bool started;
    private bool checkingFinishedTriggered;

    private void Awake()
    {
        this.GetEvent().Register<Day1StartedEvent>(OnDay1Started)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        if (wakeUpCutsceneRoot != null)
        {
            wakeUpCutsceneRoot.SetActive(false);
        }

        SetCameraEnabled(wakeUpCutsceneCamera, false);

    }
 
    private void OnDay1Started(Day1StartedEvent evt)
    {
        if (started) return;
        StartCoroutine(PlayWakeUpRoutine());
    }

    private IEnumerator PlayWakeUpRoutine()
    {
        started = true;
        checkingFinishedTriggered = false;

        this.SendCommand<ClearCurrentTaskCommand>();

        ResetModels();
        SetObjectsActive(objectsToEnableOnStart, true);
        SetObjectsActive(objectsToDisableOnStart, false);

        PrepareGameplayPlayerForCutscene();
        SetPlayerLocked(true);

        if (fadePanel != null)
        {
            fadePanel.SetBlackImmediate();
        }

        SetCommunicationDeviceInHand(false);
        PositionCutsceneActorAtStart();
        BeginCutsceneView();
        PrepareCutscenePlayback();

        if (dayTransitionPanel != null)
        {
            yield return dayTransitionPanel.PlayTransition(dayNumber);
        }

        if (holdBeforeReveal > 0f)
        {
            yield return new WaitForSeconds(holdBeforeReveal);
        }

        ResumeCutscenePlayback();
        if (fadePanel != null)
        {
            yield return fadePanel.FadeFromBlack(revealDuration);
        }

        
        yield return PlayWakeUpCutsceneSequence();

        AlignGameplayPlayerToCutsceneView();
        EndCutsceneView();

        SetPlayerLocked(false);
        RestoreGameplayPlayerAfterCutscene();
    }

    private void PrepareGameplayPlayerForCutscene()
    {
        if (playerController == null) return;

        playerController.ClearMotionForCutscene();
        playerController.SetCanMove(false);
        playerController.SetCanLook(false);
        playerController.SetCanRun(false);
        playerController.SetCanCrouch(false);
        playerController.SetCanJump(false);
        playerController.ForceStandUp();
    }

    private void BeginCutsceneView()
    {
        SetObjectsActive(objectsToEnableDuringCutscene, true);
        SetObjectsActive(objectsToDisableDuringCutscene, false);

        if (wakeUpCutsceneRoot != null)
        {
            wakeUpCutsceneRoot.SetActive(true);
        }

        SetCameraEnabled(gameplayCamera, false);
        SetCameraEnabled(wakeUpCutsceneCamera, true);
    }

    private void EndCutsceneView()
    {
        SetCameraEnabled(wakeUpCutsceneCamera, false);

        if (wakeUpCutsceneRoot != null)
        {
            wakeUpCutsceneRoot.SetActive(false);
        }

        SetObjectsActive(objectsToEnableDuringCutscene, false);
        SetObjectsActive(objectsToDisableDuringCutscene, true);
        SetCameraEnabled(gameplayCamera, true);
    }

    private IEnumerator PlayWakeUpCutsceneSequence()
    {
        if (typingDuration > 0f)
        {
            yield return new WaitForSeconds(typingDuration);
        }

        if (notificationAudioSource != null)
        {
            notificationAudioSource.Play();
        }

        if (notificationDelay > 0f)
        {
            yield return new WaitForSeconds(notificationDelay);
        }

        TriggerCutsceneState(reachingTriggerName);

        if (reachingDuration > 0f)
        {
            yield return new WaitForSeconds(reachingDuration);
        }

        TriggerCutsceneState(reachingReverseTriggerName);

        if (reachingReverseDuration > 0f)
        {
            yield return new WaitForSeconds(reachingReverseDuration);
        }

        TriggerCutsceneState(checkingTriggerName);

        if (checkingDuration > 0f)
        {
            yield return new WaitForSeconds(checkingDuration);
        }

        HandleCheckingFinished();
        yield return PlayPostCheckingSequence();
    }

    private void ResetModels()
    {
        var toolInventory = this.GetModel<ToolInventoryModel>();
        if (toolInventory != null)
        {
            toolInventory.HasSmallAxe.Value = false;
            toolInventory.HasChainsaw.Value = false;
        }

        var dutyModel = this.GetModel<LighthouseDutyModel>();
        if (dutyModel != null)
        {
            dutyModel.GeneratorChecked.Value = false;
            dutyModel.LampRoomChecked.Value = false;
            dutyModel.LensChecked.Value = false;
            dutyModel.LightActivated.Value = false;
        }
    }

    private void PositionCutsceneActorAtStart()
    {
        if (wakeUpCutsceneActorRoot == null || wakeUpStartPoint == null) return;

        wakeUpCutsceneActorRoot.position = wakeUpStartPoint.position;
        wakeUpCutsceneActorRoot.rotation = wakeUpStartPoint.rotation;
    }

    private void SnapPlayerToHandoffPoint()
    {
        if (playerRoot == null || wakeUpHandoffPoint == null) return;

        playerRoot.position = wakeUpHandoffPoint.position;
        playerRoot.rotation = wakeUpHandoffPoint.rotation;
    }

    private void AlignGameplayPlayerToCutsceneView()
    {
        if (playerController != null && wakeUpCutsceneCamera != null)
        {
            playerController.SnapToView(
                wakeUpCutsceneCamera.transform.position,
                wakeUpCutsceneCamera.transform.rotation);
            return;
        }

        SnapPlayerToHandoffPoint();
    }

    private void SetPlayerLocked(bool locked)
    {
        if (playerControlComponents == null) return;

        for (int i = 0; i < playerControlComponents.Length; i++)
        {
            if (playerControlComponents[i] != null)
            {
                playerControlComponents[i].enabled = !locked;
            }
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    private void SetCameraEnabled(Camera targetCamera, bool enabled)
    {
        if (targetCamera != null)
        {
            targetCamera.enabled = enabled;
        }
    }

    private void RestoreGameplayPlayerAfterCutscene()
    {
        if (playerController == null) return;

        playerController.ClearMotionForCutscene();
        playerController.SetCanMove(true);
        playerController.SetCanLook(true);
        playerController.SetCanRun(true);
        playerController.SetCanCrouch(true);
        playerController.SetCanJump(true);
        playerController.SetCursorLocked(true);
    }

    private void PrepareCutscenePlayback()
    {
        PlayAnimatorStateFromStart(wakeUpCutsceneAnimator, typingStateName);
        PlayAnimatorStateFromStart(wakeUpCameraAnimator, cameraTrackStateName);
        SetAnimatorSpeed(wakeUpCutsceneAnimator, 0f);
        SetAnimatorSpeed(wakeUpCameraAnimator, 0f);
    }

    private void ResumeCutscenePlayback()
    {
        SetAnimatorSpeed(wakeUpCutsceneAnimator, 1f);
        SetAnimatorSpeed(wakeUpCameraAnimator, 1f);
    }

    private void TriggerCutsceneState(string triggerName)
    {
        if (wakeUpCutsceneAnimator == null || string.IsNullOrEmpty(triggerName)) return;

        wakeUpCutsceneAnimator.ResetTrigger(triggerName);
        wakeUpCutsceneAnimator.SetTrigger(triggerName);
    }

    public void OnReachGrabCommunicationDevice()
    {
        SetCommunicationDeviceInHand(true);
    }

    public void OnCheckingAnimationFinished()
    {
        HandleCheckingFinished();
    }

    private void PlayAnimatorStateFromStart(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(stateName)) return;

        targetAnimator.Play(stateName, 0, 0f);
        targetAnimator.Update(0f);
    }

    private void SetAnimatorSpeed(Animator targetAnimator, float speed)
    {
        if (targetAnimator != null)
        {
            targetAnimator.speed = speed;
        }
    }

    private void SetCommunicationDeviceInHand(bool inHand)
    {
        if (handCommunicationDeviceObject != null)
        {
            handCommunicationDeviceObject.SetActive(inHand);
        }

        if (deskCommunicationDeviceObject != null)
        {
            deskCommunicationDeviceObject.SetActive(!inHand);
        }
    }

    private void HandleCheckingFinished()
    {
        if (checkingFinishedTriggered) return;

        checkingFinishedTriggered = true;
        this.SendCommand(new AcquireCommunicationDeviceCommand());
        this.SendCommand(new FinishDay1WakeUpCommand());
        this.SendCommand(new SetTaskCommand(
            "day1_clear_north_route",
            "Clear the North Route",
            "Take the small axe from inside the lighthouse and clear the fallen trees blocking the north route."));
        
        
    }

    private IEnumerator PlayPostCheckingSequence()
    {
        TriggerCutsceneState(standUpTriggerName);

        if (standUpDuration > 0f)
        {
            yield return new WaitForSeconds(standUpDuration);
        }

        if (standAfterDuration > 0f)
        {
            yield return new WaitForSeconds(standAfterDuration);
        }
    }
}
