using UnityEngine;

public class PhaseTest : ControllerAbstract
{
    [Header("Safety")]
    [Tooltip("Only runs in the Unity Editor or a Development Build. Keep disabled for normal play.")]
    [SerializeField] private bool enableAutomaticTest;

    [Header("Stable Story Jump")]
    [SerializeField] private bool useStableBeatJump = true;
    [SerializeField] private StoryChapter targetChapter = StoryChapter.Day0;
    [SerializeField] private string targetBeatId = StoryBeatIds.Day0.FirstNight;

    [Header("Legacy Story Jump")]
    [SerializeField] private StoryPhase targetPhase = StoryPhase.Day0_Rest;

    [Header("Optional Test Task")]
    [SerializeField] private string TaskId;
    [SerializeField] private string TaskTitle;
    [SerializeField] private string TaskDescription;

    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!enableAutomaticTest)
        {
            return;
        }

        RunTestJump();
#else
        if (enableAutomaticTest)
        {
            Debug.LogWarning("PhaseTest is disabled in non-development builds.", this);
        }
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Run Test Jump")]
    private void RunTestJump()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("PhaseTest can only jump while the game is playing.", this);
            return;
        }

        if (useStableBeatJump && !string.IsNullOrWhiteSpace(targetBeatId))
        {
            StoryDirectorSystem director = this.GetSystem<StoryDirectorSystem>();
            StoryBeatDefinition targetDefinition = director?.Catalog?.FindBeat(targetBeatId);
            if (targetDefinition != null && targetDefinition.Chapter != targetChapter)
            {
                Debug.LogError(
                    $"PhaseTest target '{targetBeatId}' belongs to {targetDefinition.Chapter}, " +
                    $"not the configured {targetChapter}.",
                    this);
                return;
            }

            if (director == null || !director.JumpToBeatForDevelopment(targetBeatId))
            {
                Debug.LogError($"PhaseTest could not jump to story beat '{targetBeatId}'.", this);
            }
        }
        else if (targetPhase != StoryPhase.None)
        {
            this.SendCommand(new EnterStoryPhaseCommand(targetPhase));
        }

        if (!string.IsNullOrWhiteSpace(TaskId) ||
            !string.IsNullOrWhiteSpace(TaskTitle) ||
            !string.IsNullOrWhiteSpace(TaskDescription))
        {
            this.SendCommand(new SetTaskCommand(TaskId, TaskTitle, TaskDescription));
        }
    }
#endif
}
