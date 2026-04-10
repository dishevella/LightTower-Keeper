using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class CinematicSequenceTrigger : ControllerAbstract
{
    private const float DefaultFadeIn = 0.12f;
    private const float DefaultHold = 1.75f;
    private const float DefaultFadeOut = 0.22f;

    [System.Serializable]
    public class SubtitleLine
    {
        [TextArea(2, 4)]
        public string text;
        public float fadeIn;
        public float hold;
        public float fadeOut;
    }

    [System.Serializable]
    public class SequenceStep
    {
        [Tooltip("Optional pause before this step begins.")]
        public float delayBefore;
        [Tooltip("Optional focus target override for this step. If empty, the current focus target is kept.")]
        public Transform focusTargetOverride;
        [Tooltip("Optional audio that plays when this step starts.")]
        public AudioSource audioSource;
        [Tooltip("Objects to enable when this step starts.")]
        public GameObject[] objectsToEnable;
        [Tooltip("Objects to disable when this step starts.")]
        public GameObject[] objectsToDisable;
        [Tooltip("Optional event hook for animations, object toggles, task relays, etc.")]
        public UnityEvent onStepStarted;
        [Tooltip("Delay Time before triggering the Subtitles")]
        public float MiddleDelay;
        [Tooltip("Message subtitle lines played in order during this step.")]
        public SubtitleLine[] subtitleLines;
        [Tooltip("Minimum duration of this step. Use this to match long animations.")]
        public float minimumDuration;
        [Tooltip("Optional pause after this step finishes.")]
        public float delayAfter;
    }

    [Header("Activation")]
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private bool triggerOnPlayerEnter = true;
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private bool requireChainsawOwned;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MonoBehaviour[] additionalComponentsToLock;
    [SerializeField] private bool waitUntilGrounded = true;
    [SerializeField] private float groundedSettleDuration = 0.12f;
    [SerializeField] private bool clearMotionOnStart = true;
    [SerializeField] private bool forceStandOnStart = true;
    [SerializeField] private bool lockMovement = true;
    [SerializeField] private bool lockLook = true;

    [Header("Focus")]
    [SerializeField] private Transform defaultFocusTarget;
    [SerializeField] private Vector3 focusOffset;
    [SerializeField] private float focusTurnSpeed = 6f;

    [Header("Letterbox")]
    [SerializeField] private bool useLetterbox = true;
    [SerializeField] private CinematicLetterboxPanel cinematicLetterboxPanel;
    [SerializeField] private float letterboxShowDuration = 0.3f;
    [SerializeField] private float letterboxHideDuration = 0.3f;
    [SerializeField] private AudioSource audioSourceWhileLetter;
    [SerializeField] private float WaitForAudioFinished;

    [Header("Subtitles")]
    [SerializeField] private MessageSubtitlePanel messageSubtitlePanel;
    [SerializeField] private SubtitleLine[] subtitleLinesWhileLetter;

    [Header("Sequence")]
    [SerializeField] private SequenceStep[] steps;
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onSequenceFinished;
    [SerializeField] private float OnlyTriggerSubtitlesTime;
    
    private bool triggered;
    private bool playing;
    private bool lookLocked;
    private Transform currentFocusTarget;

    private bool cachedCanMove;
    private bool cachedCanLook;
    private bool cachedCanRun;
    private bool cachedCanCrouch;
    private bool cachedCanJump;
    private bool cachedCursorLocked;
    private bool[] cachedComponentStates;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void Update()
    {
        if (lookLocked)
        {
            UpdateLockedLookAt();
        }
    } 

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnPlayerEnter) return;
        if (!IsPlayerCollider(other)) return;

        TriggerSequence();
    }

    private void OnDisable()
    {
        lookLocked = false;
        currentFocusTarget = null;

        if (cinematicLetterboxPanel != null)
        {
            cinematicLetterboxPanel.SetHiddenImmediate();
        }

        if (playing)
        {
            RestorePlayerState();
            SetAdditionalComponentsLocked(false);
            playing = false;
        }
    }

    public void TriggerSequence()
    {
        if (playing) return;
        if (triggered && onlyOnce) return;
        if (!IsPhaseAllowed()) return;
        if (!HasRequiredTools()) return;

        if (onlyOnce)
        {
            triggered = true;
        }

        StartCoroutine(PlaySequenceRoutine());
    }

    public void ResetTriggerState()
    {
        triggered = false;
    }

    private IEnumerator PlaySequenceRoutine()
    {
        playing = true;
        currentFocusTarget = defaultFocusTarget;

        yield return PreparePlayerRoutine();
        CachePlayerState();
        onSequenceStarted?.Invoke();
        if (audioSourceWhileLetter != null)
        audioSourceWhileLetter.Play();
        if(WaitForAudioFinished >0)
        yield return new WaitForSeconds(WaitForAudioFinished);
       
        if (useLetterbox && cinematicLetterboxPanel != null)
        {
            yield return cinematicLetterboxPanel.Show(letterboxShowDuration);
        }
        ApplyPlayerSequenceState();
        SetAdditionalComponentsLocked(true);
        StartCoroutine(PlaySubtitleLines(subtitleLinesWhileLetter));
        

        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                yield return PlayStep(steps[i]);
            }
        }

        onSequenceFinished?.Invoke();

        if (useLetterbox && cinematicLetterboxPanel != null)
        {
            yield return cinematicLetterboxPanel.Hide(letterboxHideDuration);
        }
        if (OnlyTriggerSubtitlesTime > 0)
            yield return new WaitForSeconds(OnlyTriggerSubtitlesTime);
        RestorePlayerState();
        SetAdditionalComponentsLocked(false);
        lookLocked = false;
        currentFocusTarget = null;
        playing = false;
    }

    private IEnumerator PreparePlayerRoutine()
    {
        if (playerController == null)
        {
            yield break;
        }

        if (waitUntilGrounded && !playerController.isGrounded)
        {
            yield return new WaitUntil(() => playerController == null || playerController.isGrounded);
        }

        if (playerController == null)
        {
            yield break;
        }

        if (clearMotionOnStart)
        {
            playerController.ClearMotionForCutscene();
        }

        if (forceStandOnStart)
        {
            playerController.ForceStandUp();
        }

        if (groundedSettleDuration > 0f)
        {
            yield return new WaitForSeconds(groundedSettleDuration);
        }
    }

    private void CachePlayerState()
    {
        if (playerController != null)
        {
            cachedCanMove = playerController.CanMove;
            cachedCanLook = playerController.CanLook;
            cachedCanRun = playerController.CanRun;
            cachedCanCrouch = playerController.CanCrouch;
            cachedCanJump = playerController.CanJump;
            cachedCursorLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        if (additionalComponentsToLock == null)
        {
            cachedComponentStates = null;
            return;
        }

        cachedComponentStates = new bool[additionalComponentsToLock.Length];
        for (int i = 0; i < additionalComponentsToLock.Length; i++)
        {
            cachedComponentStates[i] = additionalComponentsToLock[i] != null && additionalComponentsToLock[i].enabled;
        }
    }

    private void ApplyPlayerSequenceState()
    {
        if (playerController == null)
        {
            return;
        }

        if (lockMovement)
        {
            playerController.SetCanMove(false);
            playerController.SetCanRun(false);
            playerController.SetCanCrouch(false);
            playerController.SetCanJump(false);
        }

        if (lockLook)
        {
            lookLocked = true;
            playerController.SetCanLook(false);
        }
        else
        {
            lookLocked = false;
        }

        if (lockMovement || lockLook)
        {
            playerController.SetCursorLocked(true);
        }
    }

    private void RestorePlayerState()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.SetCanMove(cachedCanMove);
        playerController.SetCanLook(cachedCanLook);
        playerController.SetCanRun(cachedCanRun);
        playerController.SetCanCrouch(cachedCanCrouch);
        playerController.SetCanJump(cachedCanJump);
        playerController.SetCursorLocked(cachedCursorLocked);
    }

    private void SetAdditionalComponentsLocked(bool locked)
    {
        if (additionalComponentsToLock == null)
        {
            return;
        }

        for (int i = 0; i < additionalComponentsToLock.Length; i++)
        {
            MonoBehaviour component = additionalComponentsToLock[i];
            if (component == null)
            {
                continue;
            }

            if (!locked && cachedComponentStates != null && i < cachedComponentStates.Length)
            {
                component.enabled = cachedComponentStates[i];
            }
            else if (locked)
            {
                component.enabled = false;
            }
        }
    }

    private IEnumerator PlayStep(SequenceStep step)
    {
        if (step == null)
        {
            yield break;
        }
        if (step.delayBefore > 0f)
        {
            yield return new WaitForSeconds(step.delayBefore);
        }
        if (step.focusTargetOverride != null)
        {
            currentFocusTarget = step.focusTargetOverride;
        }

        if (step.audioSource != null)
        {
            step.audioSource.Play();
        }

        
        SetObjectsActive(step.objectsToDisable, false);
        SetObjectsActive(step.objectsToEnable, true);
        step.onStepStarted?.Invoke();
        if(step.MiddleDelay >0f)
        yield return new WaitForSeconds(step.MiddleDelay);
        float startedAt = Time.time;

        if (step.subtitleLines != null && step.subtitleLines.Length > 0)
        {
            yield return PlaySubtitleLines(step.subtitleLines);
        }

        float elapsed = Time.time - startedAt;
        float minimumDuration = Mathf.Max(0f, step.minimumDuration);
        if (elapsed < minimumDuration)
        {
            yield return new WaitForSeconds(minimumDuration - elapsed);
        }

        if (step.delayAfter > 0f)
        {
            yield return new WaitForSeconds(step.delayAfter);
        }
    }

    private IEnumerator PlaySubtitleLines(SubtitleLine[] subtitleLines)
    {
        if (messageSubtitlePanel == null || subtitleLines == null)
        {
            yield break;
        }

        for (int i = 0; i < subtitleLines.Length; i++)
        {
            SubtitleLine line = subtitleLines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.text))
            {
                continue;
            }

            GetResolvedLineTiming(line, out float fadeIn, out float hold, out float fadeOut);
            yield return messageSubtitlePanel.PlayLine(line.text, fadeIn, hold, fadeOut);
        }
    }

    private void UpdateLockedLookAt()
    {
        if (playerController == null)
        {
            return;
        }

        Vector3 focusPosition = ResolveFocusPosition();
        if (focusPosition == Vector3.zero) return;
        Transform playerTransform = playerController.transform;
        Transform cameraRoot = playerController.CameraRootTransform;
        if (cameraRoot == null)
        {
            return;
        }

        Vector3 flatDirection = focusPosition - playerTransform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation,
                targetYaw,
                focusTurnSpeed * Time.deltaTime);
        }

        Vector3 cameraDirection = focusPosition - cameraRoot.position;
        if (cameraDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 localDirection = playerTransform.InverseTransformDirection(cameraDirection.normalized);
        float targetPitch = -Mathf.Atan2(
            localDirection.y,
            Mathf.Max(0.001f, new Vector2(localDirection.x, localDirection.z).magnitude)) * Mathf.Rad2Deg;

        Quaternion targetLocalRotation = Quaternion.Euler(targetPitch, 0f, 0f);
        cameraRoot.localRotation = Quaternion.Slerp(
            cameraRoot.localRotation,
            targetLocalRotation,
            focusTurnSpeed * Time.deltaTime);
    }

    private Vector3 ResolveFocusPosition()
    {
        if (currentFocusTarget != null)
        {
            return currentFocusTarget.position + focusOffset;
        }
       
        return playerController != null
            ? playerController.transform.position + playerController.transform.forward * 3f
            : Vector3.zero;
    }

    private void GetResolvedLineTiming(SubtitleLine line, out float fadeIn, out float hold, out float fadeOut)
    {
        if (line == null)
        {
            fadeIn = DefaultFadeIn;
            hold = DefaultHold;
            fadeOut = DefaultFadeOut;
            return;
        }

        SubtitleTimingUtility.ResolveTimings(
            line.text,
            line.fadeIn,
            line.hold,
            line.fadeOut,
            DefaultFadeIn,
            DefaultHold,
            DefaultFadeOut,
            out fadeIn,
            out hold,
            out fadeOut);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        PlayerController otherPlayer = other.GetComponentInParent<PlayerController>();
        if (otherPlayer == null)
        {
            return false;
        }

        return playerController == null || otherPlayer == playerController;
    }

    private bool IsPhaseAllowed()
    {
        if (availablePhases == null || availablePhases.Length == 0)
        {
            return true;
        }

        GameStateModel gameState = this.GetModel<GameStateModel>();
        if (gameState == null)
        {
            return false;
        }

        StoryPhase currentPhase = gameState.CurrentPhase.Value;
        for (int i = 0; i < availablePhases.Length; i++)
        {
            if (availablePhases[i] == currentPhase)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasRequiredTools()
    {
        if (!requireChainsawOwned)
        {
            return true;
        }

        ToolInventoryModel toolInventory = this.GetModel<ToolInventoryModel>();
        return toolInventory != null && toolInventory.HasChainsaw.Value;
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }
}
