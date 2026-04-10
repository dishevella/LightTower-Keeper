using System.Collections;
using UnityEngine;

public class Day1DockEncounterController : ControllerAbstract
{
    private const float DefaultBeatFadeIn = 0.12f;
    private const float DefaultBeatHold = 1.75f;
    private const float DefaultBeatFadeOut = 0.22f;

    public enum DialogueSpeaker
    {
        Owen = 0,
        FishingNpc = 1,
        SwimmingNpc = 2
    }

    public enum BeatFacingMode
    {
        KeepCurrent = 0,
        FaceOwen = 1,
        FaceOtherNpc = 2,
        FaceFishingNpc = 3,
        FaceSwimmingNpc = 4,
        FaceCustomTarget = 5
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;
        public float fadeIn;
        public float hold;
        public float fadeOut;
    }

    [System.Serializable]
    public class NpcActor
    {
        public Transform actorRoot;
        public Animator animator;
        public string reactionTriggerName;
        public float reactionDuration = 1f;
        public Transform reactionSwimTarget;
        public float reactionSwimSpeed = 1.5f;
        public Transform reactionMoveTarget;
        public float reactionMoveSpeed = 1.5f;
        public string walkingBoolName = "IsWalking";
        public Transform[] returnToBoatWaypoints;
        public float walkSpeed = 2f;
        public GameObject[] objectsToDisableOnBoard;
        public GameObject[] objectsToEnableOnBoard;
    }

    [System.Serializable]
    public class BoatDeparture
    {
        public Transform boatRoot;
        public Transform[] departureWaypoints;
        public float moveSpeed = 3f;
        public GameObject[] objectsToDisableOnFinish;
        public GameObject[] objectsToEnableOnFinish;
    }

    [System.Serializable]
    public class DialogueBeat
    {
        public DialogueSpeaker speaker = DialogueSpeaker.Owen;
        public DialogueLine line = new DialogueLine();
        [Header("NPC Gesture")]
        public string animationTriggerName;
        public float animationDuration = 1f;
        public BeatFacingMode facingMode = BeatFacingMode.KeepCurrent;
        public Transform customFacingTarget;
        public float facingTurnSpeed = 8f;
    }

    [System.Serializable]
    public class ToneDialogueOption
    {
        public DialogueBeat[] beats = new DialogueBeat[0];
    }

    [System.Serializable]
    public class ToneDialogueChoice
    {
        public ToneDialogueOption harsh = new ToneDialogueOption
        {
            beats = new[]
            {
                new DialogueBeat
                {
                    speaker = DialogueSpeaker.Owen,
                    line = new DialogueLine
                    {
                        text = "You shouldn't be here. Get back to your boat."
                    }
                }
            }
        };
        public ToneDialogueOption neutral = new ToneDialogueOption
        {
            beats = new[]
            {
                new DialogueBeat
                {
                    speaker = DialogueSpeaker.Owen,
                    line = new DialogueLine
                    {
                        text = "This shore is restricted. You need to move on."
                    }
                }
            }
        };
        public ToneDialogueOption gentle = new ToneDialogueOption
        {
            beats = new[]
            {
                new DialogueBeat
                {
                    speaker = DialogueSpeaker.Owen,
                    line = new DialogueLine
                    {
                        text = "You should head back out. This place isn't for visitors."
                    }
                }
            }
        };
    }

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform encounterPoint;
    [SerializeField] private float triggerDistance = 4f;
    [SerializeField] private MonoBehaviour[] additionalComponentsToLock;

    [Header("View Lock")]
    [SerializeField] private bool requireBridgeCollapsed = true;
    [SerializeField] private Transform cameraFocusTarget;
    [SerializeField] private Vector3 focusOffset;
    [SerializeField] private float focusTurnSpeed = 6f;

    [Header("Cinematic")]
    [SerializeField] private CinematicLetterboxPanel cinematicLetterboxPanel;
    [SerializeField] private float letterboxShowDuration = 0.3f;
    [SerializeField] private float letterboxHideDuration = 0.3f;

    [Header("Dialogue")]
    [SerializeField] private MessageSubtitlePanel messageSubtitlePanel;
    [SerializeField] private DialogueChoicePanel dialogueChoicePanel;
    [SerializeField] private float openingLineDelay = 0.35f;
    [SerializeField] private float groundedSettleDuration = 0.12f;
    [SerializeField] private DialogueLine[] playerOpeningLines = new[]
    {
        new DialogueLine
        {
            text = "Hey. What are you doing out there?"
        }
    };
    [SerializeField] private ToneDialogueChoice[] toneChoices = new[]
    {
        new ToneDialogueChoice()
    };

    [Header("Actors")]
    [SerializeField] private NpcActor fishingNpc;
    [SerializeField] private NpcActor swimmingNpc;

    [Header("Boat")]
    [SerializeField] private BoatDeparture boatDeparture;

    private bool activePhase;
    private bool playing;
    private bool lookLocked;

    private void Awake()
    {
        this.GetEvent().Register<Day1GoDockStartedEvent>(OnDay1GoDockStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDisable()
    {
        lookLocked = false;

        if (cinematicLetterboxPanel != null)
        {
            cinematicLetterboxPanel.SetHiddenImmediate();
        }
    }

    private void Update()
    {
        if (lookLocked)
        {
            UpdateLockedLookAt();
        }

        if (!activePhase || playing) return;
        if (playerController == null || encounterPoint == null) return;
        if (requireBridgeCollapsed && !IsBridgeCollapsed()) return;

        Vector3 playerPos = playerController.transform.position;
        Vector3 targetPos = encounterPoint.position;
        playerPos.y = 0f;
        targetPos.y = 0f;

        if (Vector3.Distance(playerPos, targetPos) <= triggerDistance)
        {
            StartCoroutine(PlayDockEncounterRoutine());
        }
    }

    private void OnDay1GoDockStarted(Day1GoDockStartedEvent evt)
    {
        activePhase = true;
        playing = false;
    }

    private IEnumerator PlayDockEncounterRoutine()
    {
        activePhase = false;
        playing = true;
        yield return PreparePlayerForEncounterRoutine();
        PreparePlayerForEncounter();

        if (groundedSettleDuration > 0f)
        {
            yield return new WaitForSeconds(groundedSettleDuration);
        }

        SetAdditionalComponentsLocked(true);
        SetPlayerMovementLocked(true);
        SetPlayerLookLocked(true);

        int pendingReactions = 0;
        int completedReactions = 0;
        bool swimReactionCompleted = !HasReaction(swimmingNpc);

        if (cinematicLetterboxPanel != null)
        {
            yield return cinematicLetterboxPanel.Show(letterboxShowDuration);
        }

        if (HasReaction(fishingNpc))
        {
            pendingReactions++;
            StartCoroutine(PlayActorReactionRoutine(fishingNpc, () => completedReactions++));
        }

       
        if (openingLineDelay > 0f)
        {
            yield return new WaitForSeconds(openingLineDelay);
        }

        if (playerOpeningLines != null && playerOpeningLines.Length > 0)
        {
            yield return PlayDialogueLine(playerOpeningLines[0]);
        }

        SetPlayerLookLocked(false);

        if (HasReaction(swimmingNpc))
        {
            pendingReactions++;
            StartCoroutine(PlayActorReactionRoutine(swimmingNpc, () =>
            {
                swimReactionCompleted = true;
                completedReactions++;
            }));
        }

        if (playerOpeningLines != null && playerOpeningLines.Length > 1)
        {
            yield return PlayDialogueLine(playerOpeningLines[1]);
        }

        if (playerOpeningLines != null && playerOpeningLines.Length > 2)
        {
            if (!swimReactionCompleted)
            {
                yield return new WaitUntil(() => swimReactionCompleted);
            }

            yield return PlayDialogueLine(playerOpeningLines[2]);
        }

        if (playerOpeningLines != null && playerOpeningLines.Length > 3)
        {
            yield return PlayDialogueLines(playerOpeningLines, 3);
        }

        if (pendingReactions > 0)
        {
            yield return new WaitUntil(() => completedReactions >= pendingReactions);
        }

        yield return PlayToneDialogueChoices();

        yield return MoveActorsToBoatRoutine();

        if (cinematicLetterboxPanel != null)
        {
            yield return cinematicLetterboxPanel.Hide(letterboxHideDuration);
        }

        SetPlayerMovementLocked(false);
        SetAdditionalComponentsLocked(false);
        yield return MoveBoatRoutine();

        this.SendCommand(new FinishDay1DockEncounterCommand());
        playing = false;
    }

    private IEnumerator PreparePlayerForEncounterRoutine()
    {
        if (playerController == null)
        {
            yield break;
        }

        if (!playerController.isGrounded)
        {
            yield return new WaitUntil(() => playerController == null || playerController.isGrounded);
        }

        if (playerController == null)
        {
            yield break;
        }
    }

    private void PreparePlayerForEncounter()
    {
        if (playerController == null) return;

        playerController.ClearMotionForCutscene();
        playerController.ForceStandUp();
        playerController.SetCursorLocked(true);
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (playerController == null) return;

        playerController.SetCanMove(!locked);
        playerController.SetCanRun(!locked);
        playerController.SetCanCrouch(!locked);
        playerController.SetCanJump(!locked);
    }

    private void SetPlayerLookLocked(bool locked)
    {
        if (playerController == null) return;

        lookLocked = locked;
        playerController.SetCanLook(!locked);
        playerController.SetCursorLocked(true);
    }

    private void SetAdditionalComponentsLocked(bool locked)
    {
        if (additionalComponentsToLock == null) return;

        for (int i = 0; i < additionalComponentsToLock.Length; i++)
        {
            if (additionalComponentsToLock[i] != null)
            {
                additionalComponentsToLock[i].enabled = !locked;
            }
        }
    }

    private bool HasReaction(NpcActor actor)
    {
        return actor != null &&
               (GetReactionDuration(actor) > 0f ||
                (actor.animator != null && !string.IsNullOrEmpty(actor.reactionTriggerName)) ||
                actor.reactionSwimTarget != null ||
                actor.reactionMoveTarget != null);
    }

    private IEnumerator PlayActorReactionRoutine(NpcActor actor, System.Action onFinished)
    {
        if (actor == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        TriggerReaction(actor);

        float duration = GetReactionDuration(actor);
        float elapsed = 0f;

        bool hasReactionSwim = actor.actorRoot != null && actor.reactionSwimTarget != null;
        bool reachedSwimTarget = !hasReactionSwim;

        while (elapsed < duration || !reachedSwimTarget)
        {
            if (!reachedSwimTarget)
            {
                reachedSwimTarget = MoveActorTowardsPoint(
                    actor.actorRoot,
                    actor.reactionSwimTarget,
                    actor.reactionSwimSpeed);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (actor.actorRoot != null && actor.reactionMoveTarget != null)
        {
            SetWalking(actor, true);

            while (!MoveActorTowardsPoint(
                actor.actorRoot,
                actor.reactionMoveTarget,
                actor.reactionMoveSpeed))
            {
                yield return null;
            }

            SetWalking(actor, false);
        }

        onFinished?.Invoke();
    }

    private void TriggerReaction(NpcActor actor)
    {
        if (actor == null || actor.animator == null || string.IsNullOrEmpty(actor.reactionTriggerName))
            return;

        actor.animator.ResetTrigger(actor.reactionTriggerName);
        actor.animator.SetTrigger(actor.reactionTriggerName);
    }

    private float GetReactionDuration(NpcActor actor)
    {
        return actor != null ? Mathf.Max(0f, actor.reactionDuration) : 0f;
    }

    private IEnumerator PlayDialogueLine(DialogueLine line)
    {
        if (messageSubtitlePanel == null || line == null || string.IsNullOrWhiteSpace(line.text))
        {
            yield break;
        }

        GetResolvedLineTiming(line, out float fadeIn, out float hold, out float fadeOut);

        yield return messageSubtitlePanel.PlayLine(
            line.text,
            fadeIn,
            hold,
            fadeOut);
    }

    private IEnumerator PlayDialogueLines(DialogueLine[] lines, int startIndex = 0)
    {
        if (lines == null || lines.Length == 0 || startIndex >= lines.Length)
        {
            yield break;
        }

        for (int i = Mathf.Max(0, startIndex); i < lines.Length; i++)
        {
            yield return PlayDialogueLine(lines[i]);
        }
    }

    private IEnumerator PlayToneDialogueChoices()
    {
        if (toneChoices == null || toneChoices.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < toneChoices.Length; i++)
        {
            ToneDialogueChoice choice = toneChoices[i];
            if (!HasPlayableToneChoice(choice))
            {
                continue;
            }

            yield return PlayToneDialogueChoice(choice);
        }
    }

    private bool HasPlayableToneChoice(ToneDialogueChoice choice)
    {
        return choice != null &&
               (HasToneOptionContent(choice.harsh) ||
                HasToneOptionContent(choice.neutral) ||
                HasToneOptionContent(choice.gentle));
    }

    private IEnumerator PlayToneDialogueChoice(ToneDialogueChoice choice)
    {
        ToneDialogueOption selectedOption = ResolveDefaultToneOption(choice);

        if (!HasToneChoice(choice))
        {
            yield return PlayToneOption(selectedOption);
            yield break;
        }

        bool choiceResolved = false;

        SetPlayerLookLocked(true);
        if (playerController != null)
        {
            playerController.SetCursorLocked(false);
        }

        dialogueChoicePanel.ShowChoices(
            GetToneChoiceText(choice.harsh, "Get back to your boat."),
            GetToneChoiceText(choice.neutral, "You need to move on."),
            GetToneChoiceText(choice.gentle, "Just go. I don't want trouble."),
            tone =>
            {
                selectedOption = ResolveToneOption(choice, tone);
                choiceResolved = true;
            });

        while (!choiceResolved)
        {
            yield return null;
        }

        if (playerController != null)
        {
            playerController.SetCursorLocked(true);
        }
        SetPlayerLookLocked(false);

        yield return PlayToneOption(selectedOption);
    }

    private IEnumerator MoveActorsToBoatRoutine()
    {
        int actorCount = 0;
        int completed = 0;

        if (HasBoardingPath(fishingNpc))
        {
            actorCount++;
            StartCoroutine(MoveActorToBoatRoutine(fishingNpc, () => completed++));
        }

        if (HasBoardingPath(swimmingNpc))
        {
            actorCount++;
            StartCoroutine(MoveActorToBoatRoutine(swimmingNpc, () => completed++));
        }

        if (actorCount > 0)
        {
            yield return new WaitUntil(() => completed >= actorCount);
        }
    }

    private bool HasBoardingPath(NpcActor actor)
    {
        return actor != null &&
               actor.actorRoot != null &&
               actor.returnToBoatWaypoints != null &&
               actor.returnToBoatWaypoints.Length > 0;
    }

    private IEnumerator MoveActorToBoatRoutine(NpcActor actor, System.Action onFinished)
    {
        if (actor == null || actor.actorRoot == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        SetWalking(actor, true);

        for (int i = 0; i < actor.returnToBoatWaypoints.Length; i++)
        {
            Transform waypoint = actor.returnToBoatWaypoints[i];
            if (waypoint == null) continue;

            while (!MoveActorTowardsPoint(actor.actorRoot, waypoint, actor.walkSpeed))
            {
                yield return null;
            }
        }

        SetWalking(actor, false);
        SetObjectsActive(actor.objectsToEnableOnBoard, true);
        SetObjectsActive(actor.objectsToDisableOnBoard, false);
        onFinished?.Invoke();
    }

    private IEnumerator MoveBoatRoutine()
    {
        if (boatDeparture == null || boatDeparture.boatRoot == null)
        {
            yield break;
        }

        if (boatDeparture.departureWaypoints != null)
        {
            for (int i = 0; i < boatDeparture.departureWaypoints.Length; i++)
            {
                Transform waypoint = boatDeparture.departureWaypoints[i];
                if (waypoint == null) continue;

                while (!MoveActorTowardsPoint(boatDeparture.boatRoot, waypoint, boatDeparture.moveSpeed, 5f))
                {
                    yield return null;
                }
            }
        }

        SetObjectsActive(boatDeparture.objectsToEnableOnFinish, true);
        SetObjectsActive(boatDeparture.objectsToDisableOnFinish, false);
    }

    private void SetWalking(NpcActor actor, bool walking)
    {
        if (actor == null || actor.animator == null || string.IsNullOrEmpty(actor.walkingBoolName))
            return;

        actor.animator.SetBool(actor.walkingBoolName, walking);
    }

    private bool MoveActorTowardsPoint(Transform actorRoot, Transform target, float moveSpeed, float turnSpeed = 10f)
    {
        if (actorRoot == null || target == null)
        {
            return true;
        }

        if (Vector3.Distance(actorRoot.position, target.position) <= 0.05f)
        {
            actorRoot.position = target.position;
            actorRoot.rotation = target.rotation;
            return true;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            actorRoot.position,
            target.position,
            moveSpeed * Time.deltaTime);

        Vector3 moveDirection = target.position - actorRoot.position;
        moveDirection.y = 0f;

        actorRoot.position = nextPosition;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            actorRoot.rotation = Quaternion.Slerp(
                actorRoot.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        bool reachedTarget = Vector3.Distance(actorRoot.position, target.position) <= 0.05f;
        if (reachedTarget)
        {
            actorRoot.position = target.position;
            actorRoot.rotation = target.rotation;
        }

        return reachedTarget;
    }

    private bool HasToneChoice(ToneDialogueChoice choice)
    {
        return dialogueChoicePanel != null &&
               choice != null &&
               (HasToneOptionContent(choice.harsh) ||
                HasToneOptionContent(choice.neutral) ||
                HasToneOptionContent(choice.gentle));
    }

    private bool HasToneOptionContent(ToneDialogueOption option)
    {
        return HasDialogueBeats(option?.beats);
    }

    private bool HasDialogueBeats(DialogueBeat[] beats)
    {
        if (beats == null || beats.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < beats.Length; i++)
        {
            DialogueBeat beat = beats[i];
            if (beat == null) continue;

            bool hasLine = beat.line != null && !string.IsNullOrWhiteSpace(beat.line.text);
            bool hasAnimation = !string.IsNullOrWhiteSpace(beat.animationTriggerName);
            bool hasFacing = beat.facingMode != BeatFacingMode.KeepCurrent;

            if (hasLine || hasAnimation || hasFacing)
            {
                return true;
            }
        }

        return false;
    }

    private ToneDialogueOption ResolveDefaultToneOption(ToneDialogueChoice choice)
    {
        if (choice == null) return null;
        if (HasToneOptionContent(choice.neutral)) return choice.neutral;
        if (HasToneOptionContent(choice.harsh)) return choice.harsh;
        if (HasToneOptionContent(choice.gentle)) return choice.gentle;
        return null;
    }

    private ToneDialogueOption ResolveToneOption(ToneDialogueChoice choice, DialogueChoicePanel.DialogueTone tone)
    {
        switch (tone)
        {
            case DialogueChoicePanel.DialogueTone.Harsh:
                return choice != null && HasToneOptionContent(choice.harsh)
                    ? choice.harsh
                    : ResolveDefaultToneOption(choice);

            case DialogueChoicePanel.DialogueTone.Gentle:
                return choice != null && HasToneOptionContent(choice.gentle)
                    ? choice.gentle
                    : ResolveDefaultToneOption(choice);

            default:
                return choice != null && HasToneOptionContent(choice.neutral)
                    ? choice.neutral
                    : ResolveDefaultToneOption(choice);
        }
    }

    private string GetToneChoiceText(ToneDialogueOption option, string fallback)
    {
        if (option == null || option.beats == null)
        {
            return fallback;
        }

        for (int i = 0; i < option.beats.Length; i++)
        {
            DialogueBeat beat = option.beats[i];
            if (beat == null || beat.line == null) continue;

            if (beat.speaker == DialogueSpeaker.Owen &&
                !string.IsNullOrWhiteSpace(beat.line.text))
            {
                return beat.line.text;
            }
        }

        for (int i = 0; i < option.beats.Length; i++)
        {
            DialogueBeat beat = option.beats[i];
            if (beat == null || beat.line == null) continue;

            if (!string.IsNullOrWhiteSpace(beat.line.text))
            {
                return beat.line.text;
            }
        }

        return fallback;
    }

    private IEnumerator PlayToneOption(ToneDialogueOption option)
    {
        if (option == null)
        {
            yield break;
        }

        yield return PlayDialogueBeats(option.beats);
    }

    private IEnumerator PlayDialogueBeats(DialogueBeat[] beats)
    {
        if (beats == null || beats.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < beats.Length; i++)
        {
            yield return PlayDialogueBeat(beats[i]);
        }
    }

    private IEnumerator PlayDialogueBeat(DialogueBeat beat)
    {
        if (beat == null)
        {
            yield break;
        }

        NpcActor actor = ResolveSpeakerActor(beat.speaker);
        float lineDuration = GetLineDuration(beat.line);
        float beatDuration = Mathf.Max(lineDuration, Mathf.Max(0f, beat.animationDuration));

        bool presentationFinished = true;

        if (actor != null &&
            (!string.IsNullOrWhiteSpace(beat.animationTriggerName) || beat.facingMode != BeatFacingMode.KeepCurrent))
        {
            presentationFinished = false;
            StartCoroutine(PlayBeatPresentationRoutine(actor, beat, () => presentationFinished = true));
        }

        if (beat.line != null && !string.IsNullOrWhiteSpace(beat.line.text))
        {
            yield return PlayDialogueLine(beat.line);
        }
        else if (beatDuration > 0f)
        {
            yield return new WaitForSeconds(beatDuration);
        }

        if (!presentationFinished)
        {
            yield return new WaitUntil(() => presentationFinished);
        }
    }

    private IEnumerator PlayBeatPresentationRoutine(NpcActor actor, DialogueBeat beat, System.Action onFinished)
    {
        if (actor == null || actor.actorRoot == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        if (actor.animator != null && !string.IsNullOrWhiteSpace(beat.animationTriggerName))
        {
            actor.animator.ResetTrigger(beat.animationTriggerName);
            actor.animator.SetTrigger(beat.animationTriggerName);
        }

        float duration = Mathf.Max(GetLineDuration(beat.line), Mathf.Max(0f, beat.animationDuration));
        Transform facingTarget = ResolveBeatFacingTarget(beat);

        if (duration <= 0f || facingTarget == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            RotateActorTowardsTarget(actor.actorRoot, facingTarget, beat.facingTurnSpeed);
            yield return null;
        }

        RotateActorTowardsTarget(actor.actorRoot, facingTarget, beat.facingTurnSpeed);
        onFinished?.Invoke();
    }

    private NpcActor ResolveSpeakerActor(DialogueSpeaker speaker)
    {
        switch (speaker)
        {
            case DialogueSpeaker.FishingNpc:
                return fishingNpc;

            case DialogueSpeaker.SwimmingNpc:
                return swimmingNpc;

            default:
                return null;
        }
    }

    private Transform ResolveBeatFacingTarget(DialogueBeat beat)
    {
        if (beat == null)
        {
            return null;
        }

        switch (beat.facingMode)
        {
            case BeatFacingMode.FaceOwen:
                return playerController != null ? playerController.transform : null;

            case BeatFacingMode.FaceFishingNpc:
                return fishingNpc != null ? fishingNpc.actorRoot : null;

            case BeatFacingMode.FaceSwimmingNpc:
                return swimmingNpc != null ? swimmingNpc.actorRoot : null;

            case BeatFacingMode.FaceOtherNpc:
                switch (beat.speaker)
                {
                    case DialogueSpeaker.FishingNpc:
                        return swimmingNpc != null ? swimmingNpc.actorRoot : null;

                    case DialogueSpeaker.SwimmingNpc:
                        return fishingNpc != null ? fishingNpc.actorRoot : null;

                    default:
                        return null;
                }

            case BeatFacingMode.FaceCustomTarget:
                return beat.customFacingTarget;

            default:
                return null;
        }
    }

    private void RotateActorTowardsTarget(Transform actorRoot, Transform target, float turnSpeed)
    {
        if (actorRoot == null || target == null)
        {
            return;
        }

        Vector3 direction = target.position - actorRoot.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        actorRoot.rotation = Quaternion.Slerp(
            actorRoot.rotation,
            targetRotation,
            Mathf.Max(0.01f, turnSpeed) * Time.deltaTime);
    }

    private float GetLineDuration(DialogueLine line)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.text))
        {
            return 0f;
        }

        GetResolvedLineTiming(line, out float fadeIn, out float hold, out float fadeOut);

        return fadeIn + hold + fadeOut;
    }

    private void GetResolvedLineTiming(DialogueLine line, out float fadeIn, out float hold, out float fadeOut)
    {
        if (line == null)
        {
            fadeIn = DefaultBeatFadeIn;
            hold = DefaultBeatHold;
            fadeOut = DefaultBeatFadeOut;
            return;
        }

        SubtitleTimingUtility.ResolveTimings(
            line.text,
            line.fadeIn,
            line.hold,
            line.fadeOut,
            DefaultBeatFadeIn,
            DefaultBeatHold,
            DefaultBeatFadeOut,
            out fadeIn,
            out hold,
            out fadeOut);
    }

    private void UpdateLockedLookAt()
    {
        if (playerController == null) return;

        Vector3 focusPosition = ResolveFocusPosition();
        Transform playerTransform = playerController.transform;
        Transform cameraRoot = playerController.CameraRootTransform;
        if (cameraRoot == null) return;

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
        if (cameraDirection.sqrMagnitude <= 0.001f) return;

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
        if (cameraFocusTarget != null)
        {
            return cameraFocusTarget.position + focusOffset;
        }

        bool hasFishing = fishingNpc != null && fishingNpc.actorRoot != null;
        bool hasSwimming = swimmingNpc != null && swimmingNpc.actorRoot != null;

        if (hasFishing && hasSwimming)
        {
            return (fishingNpc.actorRoot.position + swimmingNpc.actorRoot.position) * 0.5f + focusOffset;
        }

        if (hasFishing)
        {
            return fishingNpc.actorRoot.position + focusOffset;
        }

        if (hasSwimming)
        {
            return swimmingNpc.actorRoot.position + focusOffset;
        }

        return playerController != null
            ? playerController.transform.position + playerController.transform.forward * 3f
            : Vector3.zero;
    }

    private bool IsBridgeCollapsed()
    {
        var routeModel = this.GetModel<Day1RouteModel>();
        return routeModel != null && routeModel.BridgeCollapsed.Value;
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
