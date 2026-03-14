using UnityEngine;

public class GameFlowSystem : MonoBehaviour
{
    public static GameFlowSystem Instance { get; private set; }

    [Header("Optional")]
    [SerializeField] private PlayerController playerController;

    public StoryPhase CurrentPhase { get; private set; } = StoryPhase.None;

    private const string TASK_SEND_MESSAGE = "send_checkin_message";
    private const string TASK_SLEEP = "sleep_day0";
    private const string TASK_DAY1_INSPECT = "day1_inspect_inside";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.SetDay(0);
        }

        EnterPhase(StoryPhase.Day0_Arrive);
    }

    public void EnterPhase(StoryPhase newPhase)
    {
        CurrentPhase = newPhase;
        Debug.Log($"Enter Phase: {CurrentPhase}");

        switch (CurrentPhase)
        {
            case StoryPhase.Day0_Arrive:
                EnterPhase(StoryPhase.Day0_SendCheckIn);
                break;

            case StoryPhase.Day0_SendCheckIn:
                TaskSystem.Instance.SetTask(
                    TASK_SEND_MESSAGE,
                    "Send Arrival Message",
                    "Send a message to HQ to confirm you have arrived."
                );
                break;

            case StoryPhase.Day0_GoSleep:
                TaskSystem.Instance.SetTask(
                    TASK_SLEEP,
                    "Go to Bed",
                    "Sleep to begin the next day."
                );
                break;

            case StoryPhase.Day1_Start:
                EnterPhase(StoryPhase.Day1_InspectInside);
                break;

            case StoryPhase.Day1_InspectInside:
                TaskSystem.Instance.SetTask(
                    TASK_DAY1_INSPECT,
                    "Inspect the Lighthouse",
                    "Check the interior of the lighthouse."
                );
                break;
        }
    }

    public void OnCheckInMessageSent()
    {
        if (CurrentPhase != StoryPhase.Day0_SendCheckIn) return;

        TaskSystem.Instance.CompleteCurrentTask();
        EnterPhase(StoryPhase.Day0_GoSleep);
    }

    public void OnSleepInteracted()
    {
        if (CurrentPhase != StoryPhase.Day0_GoSleep) return;

        TaskSystem.Instance.CompleteCurrentTask();

        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetCanLook(false);
        }

        TimeSystem.Instance.GoToNextDay(() =>
        {
            if (playerController != null)
            {
                playerController.SetCanMove(true);
                playerController.SetCanLook(true);
            }

            EnterPhase(StoryPhase.Day1_Start);
        });
    }
}