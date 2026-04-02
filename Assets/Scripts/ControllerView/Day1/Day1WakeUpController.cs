using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Day1WakeUpController : ControllerAbstract
{
    [Header("Reference")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform deskWorkPoint;
    [SerializeField] private DayTransitionPanel dayTransitionPanel;
    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private int dayNumber = 1;

    [Header("Base Layer Animation")]
    [SerializeField] private string typingStateName = "Typing";
    [SerializeField] private float introAnimationCrossFade = 0.05f;

    [Header("Right Hand Layer")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string startRightHandTriggerName = "StartRightHandAction";
    [SerializeField] private string endRightHandTriggerName = "EndRightHandAction";

    [Header("Player Lock")]
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    [Header("Timing")]
    [SerializeField] private float holdBeforeReveal = 0.2f;
    [SerializeField] private float revealDuration = 1.2f;
    [SerializeField] private float typingHoldDuration = 1.2f;
    [SerializeField] private float notificationDelay = 0.1f;
    [SerializeField] private float reachToCheckingDuration = 1.0f;
    [SerializeField] private float taskUiHoldDuration = 2.5f;
    [SerializeField] private float reverseHoldDuration = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource notificationAudioSource;

    [Header("Task UI Hook")]
    [SerializeField] private UnityEvent onTaskUiRequested;

    [Header("Scene State")]
    [SerializeField] private GameObject[] objectsToEnableOnStart;
    [SerializeField] private GameObject[] objectsToDisableOnStart;

    private bool started;

    private void Awake()
    {
        this.GetEvent().Register<Day1StartedEvent>(OnDay1Started)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDay1Started(Day1StartedEvent evt)
    {
        if (started) return;
        StartCoroutine(PlayDeskIntroRoutine());
    }

    private IEnumerator PlayDeskIntroRoutine()
    {
        started = true;

        var taskSystem = this.GetSystem<TaskSystem>();
        if (taskSystem != null)
        {
            taskSystem.ClearTask();
        }

        ResetModels();
        SetObjectsActive(objectsToEnableOnStart, true);
        SetObjectsActive(objectsToDisableOnStart, false);
        SetPlayerLocked(true);

        if (playerController != null)
        {
            playerController.ClearMotionForCutscene();
            playerController.SetAnimationDriving(false);
            playerController.SetCursorLocked(false);
            playerController.SetCanMove(false);
            playerController.SetCanLook(false);
            playerController.SetCanRun(false);
            playerController.SetCanCrouch(false);
            playerController.SetCanJump(false);
            playerController.ForceStandUp();
        }

        if (fadePanel != null)
        {
            fadePanel.SetBlackImmediate();
        }

        SnapPlayerToDeskPoint();
        ResetRightHandLayer();

        if (dayTransitionPanel != null)
        {
            yield return dayTransitionPanel.PlayTransition(dayNumber);
        }

        if (holdBeforeReveal > 0f)
        {
            yield return new WaitForSeconds(holdBeforeReveal);
        }

        if (fadePanel != null)
        {
            yield return fadePanel.FadeFromBlack(revealDuration);
        }

        
        if (playerController != null && !string.IsNullOrEmpty(typingStateName))
        {
            playerController.PlayExternalAnimationState(typingStateName, introAnimationCrossFade);
        }

        if (typingHoldDuration > 0f)
        {
            yield return new WaitForSeconds(typingHoldDuration);
        }

        if (notificationDelay > 0f)
        {
            yield return new WaitForSeconds(notificationDelay);
        }

        if (notificationAudioSource != null)
        {
            notificationAudioSource.Play();
        }

        
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(endRightHandTriggerName);
            playerAnimator.SetTrigger(startRightHandTriggerName);
        }

        
        if (reachToCheckingDuration > 0f)
        {
            yield return new WaitForSeconds(reachToCheckingDuration);
        }

        onTaskUiRequested?.Invoke();

        if (taskUiHoldDuration > 0f)
        {
            yield return new WaitForSeconds(taskUiHoldDuration);
        }

        
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(startRightHandTriggerName);
            playerAnimator.SetTrigger(endRightHandTriggerName);
        }

        if (reverseHoldDuration > 0f)
        {
            yield return new WaitForSeconds(reverseHoldDuration);
        }

        if (playerController != null)
        {
            playerController.SetAnimationDriving(true);
            playerController.SetCursorLocked(true);
            playerController.SetCanMove(true);
            playerController.SetCanLook(true);
            playerController.SetCanRun(true);
            playerController.SetCanCrouch(true);
            playerController.SetCanJump(true);
        }

        SetPlayerLocked(false);
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

    private void SnapPlayerToDeskPoint()
    {
        if (playerRoot == null || deskWorkPoint == null) return;

        playerRoot.position = deskWorkPoint.position;
        playerRoot.rotation = deskWorkPoint.rotation;
    }

    private void ResetRightHandLayer()
    {
        if (playerAnimator == null) return;

        playerAnimator.ResetTrigger(startRightHandTriggerName);
        playerAnimator.ResetTrigger(endRightHandTriggerName);
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
}