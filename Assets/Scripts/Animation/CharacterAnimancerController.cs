using System;
using System.Collections.Generic;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class CharacterAnimancerController : MonoBehaviour
{
    private const int BaseLayerIndex = 0;
    private const int UpperBodyLayerIndex = 1;
    private const int AdditiveLayerIndex = 2;
    private const int ReactionLayerIndex = 3;
    private static readonly float Diagonal = 1f / Mathf.Sqrt(2f);

    private RuntimeAnimatorController originalRuntimeAnimatorController;

    [TabGroup("Setup"), Required, SerializeField] private AnimancerComponent animancer;
    [TabGroup("Setup"), Required, SerializeField] private Animator animator;
    [TabGroup("Setup"), Required, InlineEditor, SerializeField] private CharacterAnimationConfig config;
    [TabGroup("Setup"), SerializeField] private bool initializeOnAwake = true;

    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterAnimationLogicalState CurrentLogicalState { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterMovementMode CurrentMovementMode { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public LocomotionGait CurrentGait { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public LocomotionTransient CurrentTransient { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterAnimationAction CurrentAction { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterAnimationAction QueuedAction { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string CurrentAnimationState { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public AnimationClip CurrentClip { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public AnimationClip CurrentCameraClip { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentNormalizedTime { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentLocomotionSpeed { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float TargetLocomotionSpeed { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float LocomotionMotorScale => GetLocomotionMotorScale();
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float LastResolvedFadeDuration { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentVisualAcceleration { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float LocalVelocityX { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float LocalVelocityZ { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float TargetVelocityX { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float TargetVelocityZ { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentYawRate { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float PendingTurnAngle { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public Vector2 CurrentMixerParameter { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float VerticalSpeed { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float AirborneTime { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool Grounded { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool Crouching { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool Airborne { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool RootMotionActive { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool IsInterruptible { get; private set; } = true;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool HoldingItem { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int HoldingType { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float BaseLayerWeight => GetLayerWeight(BaseLayerIndex);
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float UpperBodyLayerWeight => GetLayerWeight(UpperBodyLayerIndex);
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float AdditiveLayerWeight => GetLayerWeight(AdditiveLayerIndex);
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float ReactionLayerWeight => GetLayerWeight(ReactionLayerIndex);
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int ConfiguredLocomotionClipCount { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string LastAnimationEvent { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string LastStateChangeReason { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string RequestedAnimationState { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string PreviousAnimationState { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string LastPlayCaller { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string DiagnosticsWarning { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string UpdatePhase => "Input/State: Update | Pose: Animancer PlayerLoop | Camera: LateUpdate";
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterAnimationMotionOwner MotionOwner { get; private set; } = CharacterAnimationMotionOwner.CharacterMotor;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterAnimationRootMotionStrategy RootMotionStrategy { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float StateTime { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentStateWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentStateTargetWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentStateFadeSpeed { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentPlaybackSpeed { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool CurrentStateIsPlaying { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool CurrentStateIsFading { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public CharacterAnimationPriority CurrentPriority { get; private set; } = CharacterAnimationPriority.Locomotion;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool HasCommitted { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool LocomotionIntentMoving { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float RawMotorVelocityX { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float RawMotorVelocityZ { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float IdleWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float WalkWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float JogWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float RunWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int PlayCallsThisFrame { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int AnimationRequestsThisFrame { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int SameStateRequestsThisFrame { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int StateChangesLastSecond { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int SameStateRequestsLastSecond { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int SameStateReplaysLastSecond { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public int InterruptsLastSecond { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string LastInterruptReason { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool LastPlayReusedState { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool LastPlayResetTime { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool LastPlayRestartedFade { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool LastPlayResetWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool LastPlayReappliedTransition { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public string LastRequestedLayer { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float RawLocomotionSpeed =>
        new Vector2(RawMotorVelocityX, RawMotorVelocityZ).magnitude;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CrouchWeight { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool CameraHeadTrackingActive => false;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CameraHeadPositionInfluence => 0f;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CameraHeadRotationInfluence => 0f;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public bool CameraHeadBoneSamplingActive =>
        playerController != null && playerController.HeadBoneSamplingActive;

    private readonly MixerTransition2D locomotionMixer = new MixerTransition2D();
    private readonly MixerTransition2D crouchMixer = new MixerTransition2D();
    private readonly Dictionary<AnimationClip, CharacterAnimationConfig.MobilityClipTuning> clipTunings =
        new Dictionary<AnimationClip, CharacterAnimationConfig.MobilityClipTuning>();
    private readonly List<LocomotionGait> locomotionChildGaits = new List<LocomotionGait>(25);
    private readonly List<LocomotionGait> crouchChildGaits = new List<LocomotionGait>(9);
    private readonly LocomotionSignalFilter locomotionSignalFilter = new();
    private readonly CharacterAnimationActionCoordinator actionCoordinator = new();
    private AnimancerLayer baseLayer;
    private AnimancerLayer upperBodyLayer;
    private AnimancerLayer additiveLayer;
    private AnimancerLayer reactionLayer;
    private AnimancerState currentState;
    private CharacterController rootMotionCharacterController;
    private PlayerController playerController;
    private CharacterAnimationConfig.ActionSettings activeActionSettings;
    private CharacterAnimationConfig.ParkourSettings activeParkourSettings;
    private Vector3 smoothedLocalVelocity;
    private Vector3 localVelocitySmoothDamp;
    private Vector2 previousTargetVelocity;
    private Vector2 lastMovingDirection = Vector2.up;
    private Vector3 previousDesiredForward;
    private CharacterAnimationConfig.MobilityClipTuning activeClipTuning;
    private float activeFallbackFadeOut;
    private LocomotionGait lastMovingGait = LocomotionGait.Walk;
    private LocomotionGait jumpGait = LocomotionGait.Idle;
    private Vector2 jumpDirection = Vector2.up;
    private bool jumpUsesLeftFoot;
    private bool jumpActive;
    private bool initialized;
    private bool usingLocomotionMixer;
    private bool usingCrouchMixer;
    private bool usingIdleVariation;
    private int idleVariationIndex;
    private float idleVariationTimer;
    private float yawIdleTimer;
    private float movementCurveExitTimer;
    private float preservedLocomotionNormalizedTime;
    private bool hasPreservedLocomotionPhase;
    private int transientSerial;
    private int idleVariationSerial;
    private int diagnosticsFrame = -1;
    private int stateChangesInWindow;
    private int sameStateRequestsInWindow;
    private int sameStateReplaysInWindow;
    private int interruptsInWindow;
    private float diagnosticsWindowStart;

    public event Action<CharacterAnimationAction> ActionCommitted;

    public CharacterAnimationConfig Config => config;
    public Animator Animator => animator;
    public bool IsInitialized => initialized;
    public bool HasUsableConfiguration => config != null && config.ValidateConfiguration().IsValid;
    public bool ShouldMotorDriveTranslation => !RootMotionActive;

    private readonly struct PlaySnapshot
    {
        public readonly AnimancerState State;
        public readonly float NormalizedTime;
        public readonly float Weight;
        public readonly float TargetWeight;
        public readonly float FadeSpeed;
        public readonly bool WasSameStateRequest;
        public readonly bool ChangesBaseState;

        public PlaySnapshot(AnimancerState state, bool wasSameStateRequest, bool changesBaseState)
        {
            State = state;
            NormalizedTime = state != null ? state.NormalizedTime : 0f;
            Weight = state != null ? state.Weight : 0f;
            TargetWeight = state != null ? state.TargetWeight : 0f;
            FadeSpeed = state != null ? state.FadeSpeed : 0f;
            WasSameStateRequest = wasSameStateRequest;
            ChangesBaseState = changesBaseState;
        }
    }

    private void Reset()
    {
        animator = GetComponent<Animator>();
        animancer = GetComponent<AnimancerComponent>();
    }

    private void Awake()
    {
        if (initializeOnAwake) Initialize();
    }

    private void OnEnable()
    {
        if (initialized) CacheAndClearRuntimeAnimatorController();
    }

    private void Update()
    {
        StateTime += Time.deltaTime;
        if (Airborne) AirborneTime += Time.deltaTime;
        UpdateActionCompletion();
        UpdateTransientCompletion();
        UpdateIdleVariation();
        RefreshRuntimeDebug();
        RefreshDiagnosticsWindow();
    }

    private void OnDisable()
    {
        CleanupActionState("Component disabled");
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (originalRuntimeAnimatorController != null && animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = originalRuntimeAnimatorController;
        }
    }

    private void OnAnimatorMove()
    {
        if (!RootMotionActive || animator == null) return;

        Vector3 deltaPosition = animator.deltaPosition;
        Quaternion deltaRotation = animator.deltaRotation;
        switch (RootMotionStrategy)
        {
            case CharacterAnimationRootMotionStrategy.ForwardToCharacterMotor:
                if (rootMotionCharacterController != null && rootMotionCharacterController.enabled)
                    rootMotionCharacterController.Move(deltaPosition);
                else
                    transform.position += deltaPosition;
                transform.rotation *= deltaRotation;
                break;
            case CharacterAnimationRootMotionStrategy.UseAnimationDelta:
                transform.position += deltaPosition;
                transform.rotation *= deltaRotation;
                break;
        }
    }

    public void Initialize()
    {
        if (initialized) return;

        if (animator == null) animator = GetComponent<Animator>();
        if (animancer == null) animancer = GetComponent<AnimancerComponent>();
        if (animancer == null) animancer = gameObject.AddComponent<AnimancerComponent>();
        if (rootMotionCharacterController == null) rootMotionCharacterController = GetComponent<CharacterController>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (animator == null) return;

        animancer.Animator = animator;
        if (config != null && config.Quality != null)
        {
            animator.stabilizeFeet = config.Quality.StabilizeFeet;
            animator.feetPivotActive = config.Quality.FeetPivotActive;
            CacheAndClearRuntimeAnimatorController();
        }

        ConfigureLayers();
        BuildClipTuningLookup();
        RebuildMixers();
        initialized = true;
        diagnosticsWindowStart = Time.unscaledTime;
        Grounded = true;
        ReturnToLocomotion("Initialize");
    }

    private void CacheAndClearRuntimeAnimatorController()
    {
        if (!Application.isPlaying || config == null || config.Quality == null ||
            !config.Quality.ClearRuntimeAnimatorControllerOnInitialize || animator == null)
            return;

        if (animator.runtimeAnimatorController != null)
            originalRuntimeAnimatorController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null;
    }

    /// <summary>Feeds motor-owned world velocity into the visual animation smoothing layer.</summary>
    public void SetMovement(Vector3 worldVelocity, Vector3 desiredForward)
    {
        SetMovement(worldVelocity, desiredForward, worldVelocity);
    }

    /// <summary>
    /// Uses requested velocity for locomotion intent and measured motor velocity for the visual mixer.
    /// Keeping those signals separate prevents collisions and ground correction from restarting states.
    /// </summary>
    public void SetMovement(
        Vector3 requestedWorldVelocity,
        Vector3 desiredForward,
        Vector3 measuredWorldVelocity)
    {
        Initialize();
        if (!initialized || config == null) return;

        Vector3 targetLocal = transform.InverseTransformDirection(requestedWorldVelocity);
        targetLocal.y = 0f;
        TargetVelocityX = targetLocal.x;
        TargetVelocityZ = targetLocal.z;
        TargetLocomotionSpeed = new Vector2(targetLocal.x, targetLocal.z).magnitude;

        Vector3 motorLocal = transform.InverseTransformDirection(measuredWorldVelocity);
        motorLocal.y = 0f;
        RawMotorVelocityX = motorLocal.x;
        RawMotorVelocityZ = motorLocal.z;
        Vector3 visualTarget = config.Locomotion.UseMotorVelocityForMixer
            ? motorLocal
            : targetLocal;

        float previousSpeed = CurrentLocomotionSpeed;
        smoothedLocalVelocity = locomotionSignalFilter.SmoothVelocity(
            smoothedLocalVelocity,
            visualTarget,
            ref localVelocitySmoothDamp,
            config.Locomotion.AccelerationSmoothTime,
            config.Locomotion.DecelerationSmoothTime,
            config.Locomotion.DirectionSmoothTime,
            config.Locomotion.MaximumVisualAcceleration,
            Time.deltaTime);

        LocalVelocityX = smoothedLocalVelocity.x;
        LocalVelocityZ = smoothedLocalVelocity.z;
        CurrentLocomotionSpeed = new Vector2(LocalVelocityX, LocalVelocityZ).magnitude;
        CurrentVisualAcceleration = Time.deltaTime > 0f
            ? (CurrentLocomotionSpeed - previousSpeed) / Time.deltaTime
            : 0f;

        Vector2 targetVelocity = new Vector2(TargetVelocityX, TargetVelocityZ);
        bool wasTargetMoving = LocomotionIntentMoving;
        LocomotionIntentMoving = locomotionSignalFilter.ResolveMovementIntent(
            LocomotionIntentMoving,
            TargetLocomotionSpeed,
            config.Locomotion.StartInputThreshold,
            config.Locomotion.StopInputThreshold);
        bool targetMoving = LocomotionIntentMoving;

        UpdateTurnInput(desiredForward, targetMoving);

        if (targetMoving)
        {
            lastMovingDirection = targetVelocity.normalized;
            lastMovingGait = ResolveStableGait(TargetLocomotionSpeed, Crouching, lastMovingGait);
            CancelIdleVariation();
            idleVariationTimer = 0f;
        }

        if (CurrentLogicalState == CharacterAnimationLogicalState.Action ||
            CurrentLogicalState == CharacterAnimationLogicalState.Parkour ||
            CurrentLogicalState == CharacterAnimationLogicalState.Dead)
        {
            previousTargetVelocity = targetVelocity;
            return;
        }

        if (Airborne)
        {
            PlayAirborne();
            previousTargetVelocity = targetVelocity;
            return;
        }

        if (CurrentTransient == LocomotionTransient.Stopping && targetMoving)
            EndTransient("Stop interrupted by movement", false);
        else if (CurrentTransient == LocomotionTransient.Starting && !targetMoving)
            EndTransient("Start interrupted by stop", false);

        if (CurrentTransient == LocomotionTransient.None)
        {
            if (targetMoving && !wasTargetMoving && TryPlayStart(targetVelocity))
            {
                previousTargetVelocity = targetVelocity;
                return;
            }

            if (!targetMoving && wasTargetMoving && TryPlayStop())
            {
                previousTargetVelocity = targetVelocity;
                return;
            }

            if (targetMoving && wasTargetMoving && TryPlayMovementPivot(previousTargetVelocity, targetVelocity))
            {
                previousTargetVelocity = targetVelocity;
                return;
            }
        }

        if (CurrentTransient == LocomotionTransient.None)
            PlayLocomotion();

        previousTargetVelocity = targetVelocity;
    }

    public void SetGrounded(bool isGrounded)
    {
        if (Grounded && !isGrounded) AirborneTime = 0f;
        Grounded = isGrounded;
        Airborne = !isGrounded;
        if (isGrounded && CurrentLogicalState != CharacterAnimationLogicalState.Landing)
            AirborneTime = 0f;
    }

    public void SetVerticalSpeed(float verticalSpeed)
    {
        VerticalSpeed = verticalSpeed;
    }

    public void PlayJump(Vector3 worldHorizontalVelocity)
    {
        Initialize();
        if (!initialized || config == null || config.Airborne == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(worldHorizontalVelocity);
        jumpDirection = new Vector2(localVelocity.x, localVelocity.z);
        float horizontalSpeed = jumpDirection.magnitude;
        if (horizontalSpeed > 0.001f) jumpDirection /= horizontalSpeed;
        else jumpDirection = Vector2.up;

        jumpGait = config.ResolveGait(horizontalSpeed, false);
        jumpUsesLeftFoot = ResolveTakeoffFoot();
        jumpActive = true;
        Grounded = false;
        Airborne = true;
        AirborneTime = 0f;
        CurrentPriority = CharacterAnimationPriority.Airborne;
        IsInterruptible = false;

        AnimationClip clip = config.Airborne.JumpStarts.GetBest(jumpGait, jumpDirection, jumpUsesLeftFoot);
        if (clip == null)
        {
            PlayAirborneLoop();
            return;
        }

        PlayTransient(
            clip,
            LocomotionTransient.JumpStart,
            config.Airborne.JumpFadeDuration,
            CharacterAnimationLogicalState.Airborne,
            "Jump start");
    }

    public void SetCrouching(bool isCrouching)
    {
        bool changed = Crouching != isCrouching;
        Crouching = isCrouching;
        CurrentMovementMode = isCrouching ? CharacterMovementMode.Crouch : CharacterMovementMode.Free;

        if (!changed || !initialized || config == null || !Grounded) return;
        if (!config.Locomotion.EnableCrouchTransitions || TargetLocomotionSpeed > config.Locomotion.StartInputThreshold)
        {
            EndTransient("Crouch mode changed while moving", false);
            MarkBaseLayerStandalone();
            return;
        }

        AnimationClip clip = isCrouching ? config.Locomotion.StandToCrouch : config.Locomotion.CrouchToStand;
        if (clip == null) return;
        PlayTransient(
            clip,
            isCrouching ? LocomotionTransient.CrouchEnter : LocomotionTransient.CrouchExit,
            config.Locomotion.CrouchCrossFade,
            CharacterAnimationLogicalState.LocomotionTransition,
            isCrouching ? "Enter crouch" : "Exit crouch");
    }

    public void SetMovementMode(CharacterMovementMode mode)
    {
        CurrentMovementMode = mode;
        SetCrouching(mode == CharacterMovementMode.Crouch);
    }

    public bool RequestTurn(float signedAngle)
    {
        Initialize();
        if (!initialized || config == null || !config.Locomotion.Turns.EnableTurnInPlace) return false;
        if (CurrentLogicalState == CharacterAnimationLogicalState.Action ||
            CurrentLogicalState == CharacterAnimationLogicalState.Parkour ||
            CurrentLogicalState == CharacterAnimationLogicalState.Dead)
            return false;
        if (TargetLocomotionSpeed > config.Locomotion.StopInputThreshold || CurrentTransient != LocomotionTransient.None) return false;
        if (Mathf.Abs(signedAngle) < config.Locomotion.Turns.MinimumTurnAngle) return false;

        AnimationClip clip = config.Locomotion.Turns.GetDiscreteTurn(Crouching, signedAngle);
        if (clip == null) return false;
        PlayTransient(
            clip,
            LocomotionTransient.Turn,
            config.Locomotion.Turns.TurnCrossFade,
            CharacterAnimationLogicalState.LocomotionTransition,
            signedAngle < 0f ? "Turn left" : "Turn right");
        return true;
    }

    public bool TryPlayAction(CharacterAnimationAction action)
    {
        Initialize();
        if (config == null) return false;

        CharacterAnimationConfig.ActionSettings settings = config.FindAction(action);
        if (settings == null || settings.Clip == null)
        {
            LastStateChangeReason = $"Missing action clip: {action}";
            return false;
        }

        if ((CurrentLogicalState == CharacterAnimationLogicalState.Action ||
             CurrentLogicalState == CharacterAnimationLogicalState.Dead) &&
            CurrentAction == action && currentState != null && currentState.IsPlaying)
        {
            RecordSuppressedSameState(settings.Clip.name, $"TryPlayAction({action})");
            return true;
        }

        if (!CanInterruptWith(settings.Priority))
        {
            QueuedAction = settings.QueueWhenBlocked ? action : CharacterAnimationAction.None;
            string requestResult = settings.QueueWhenBlocked
                ? $"TryPlayAction({action}) queued"
                : $"TryPlayAction({action}) rejected";
            RecordRequestWithoutPlay(settings.Clip.name, requestResult, true);
            if (config.Diagnostics != null && config.Diagnostics.EnableConsoleLogging)
                Debug.Log($"Animation request {action} {requestResult} at frame {Time.frameCount}.", this);
            return false;
        }

        if (CurrentLogicalState == CharacterAnimationLogicalState.Action ||
            CurrentLogicalState == CharacterAnimationLogicalState.Parkour)
        {
            RecordInterrupt($"{CurrentAnimationState} interrupted by {action}");
        }
        EndTransient("Action started", false);
        CleanupActionState($"Action {action} started");
        CancelIdleVariation();
        CurrentAction = action;
        activeActionSettings = settings;
        CurrentPriority = settings.Priority;
        HasCommitted = false;
        CurrentLogicalState = action == CharacterAnimationAction.Death
            ? CharacterAnimationLogicalState.Dead
            : CharacterAnimationLogicalState.Action;
        IsInterruptible = settings.Interruptible && settings.InterruptibleNormalizedTime <= 0f;
        SetRootMotionOwnership(settings.RootMotion);

        ClipTransition transition = CreateClipTransition(settings.Clip, null);
        MarkBaseLayerStandalone();
        PlaySnapshot playSnapshot = RecordPlayRequest(
            settings.Clip.name,
            $"TryPlayAction({action})",
            true);
        currentState = baseLayer.Play(transition, ResolveTransitionFade(settings.FadeIn, activeFallbackFadeOut));
        if (!settings.Loop)
            currentState.Events(this).OnEnd = OnActionEnd;
        RecordPlayOutcome(playSnapshot, currentState);
        activeClipTuning = null;
        activeFallbackFadeOut = settings.FadeOut;
        CurrentClip = settings.Clip;
        CurrentAnimationState = settings.Action.ToString();
        LastStateChangeReason = $"Action {action}";
        return true;
    }

    public bool TryPlayParkour(ParkourAnimationRequest request)
    {
        Initialize();
        if (config == null) return false;

        CharacterAnimationConfig.ParkourSettings settings = config.FindParkour(request);
        if (settings == null || settings.Clip == null)
        {
            LastStateChangeReason = $"Missing parkour clip: {request.Type}";
            return false;
        }


        if (CurrentLogicalState == CharacterAnimationLogicalState.Parkour &&
            CurrentClip == settings.Clip && currentState != null && currentState.IsPlaying)
        {
            RecordSuppressedSameState(settings.Clip.name, $"TryPlayParkour({request.Type})");
            return true;
        }

        EndTransient("Parkour started", false);
        if (CurrentLogicalState == CharacterAnimationLogicalState.Action ||
            CurrentLogicalState == CharacterAnimationLogicalState.Parkour)
        {
            RecordInterrupt($"{CurrentAnimationState} interrupted by parkour {request.Type}");
        }
        CleanupActionState($"Parkour {request.Type} started");
        CancelIdleVariation();
        CurrentAction = CharacterAnimationAction.None;
        activeParkourSettings = settings;
        CurrentPriority = CharacterAnimationPriority.Parkour;
        HasCommitted = false;
        IsInterruptible = settings.InterruptibleStart <= 0f;
        CurrentLogicalState = CharacterAnimationLogicalState.Parkour;
        SetRootMotionOwnership(
            request.AllowRootMotion
                ? settings.RootMotion
                : CharacterAnimationRootMotionStrategy.Disabled);

        MarkBaseLayerStandalone();
        PlaySnapshot playSnapshot = RecordPlayRequest(
            settings.Clip.name,
            $"TryPlayParkour({request.Type})",
            true);
        currentState = baseLayer.Play(
            CreateClipTransition(settings.Clip, null),
            ResolveTransitionFade(settings.EnterFade, activeFallbackFadeOut));
        currentState.Events(this).OnEnd = OnActionEnd;
        RecordPlayOutcome(playSnapshot, currentState);
        activeClipTuning = null;
        activeFallbackFadeOut = settings.ExitFade;
        CurrentClip = settings.Clip;
        CurrentAnimationState = request.Type.ToString();
        LastStateChangeReason = $"Parkour {request.Type}";
        return true;
    }

    public void PlayLanding(float lastVerticalSpeed, bool wasMoving)
    {
        Initialize();
        if (config == null || config.Airborne == null) return;

        bool hardLanding = lastVerticalSpeed <= config.Airborne.HardLandingMinFallSpeed;
        AnimationClip clip = null;
        if (!hardLanding && jumpActive)
            clip = config.Airborne.JumpLandings.GetBest(jumpGait, jumpDirection, jumpUsesLeftFoot);
        if (clip == null)
        {
            clip = hardLanding && config.Airborne.HardLanding != null
                ? config.Airborne.HardLanding
                : wasMoving && config.Airborne.MovingLanding != null
                    ? config.Airborne.MovingLanding
                    : config.Airborne.SoftLanding;
        }

        if (clip == null)
        {
            jumpActive = false;
            ReturnToLocomotion("Landing clip missing");
            return;
        }
        if (CurrentLogicalState == CharacterAnimationLogicalState.Landing && CurrentClip == clip)
        {
            RecordSuppressedSameState(clip.name, "PlayLanding");
            return;
        }

        jumpActive = false;
        CleanupActionState("Landing");
        CurrentPriority = CharacterAnimationPriority.Airborne;
        IsInterruptible = false;
        PlayTransient(
            clip,
            LocomotionTransient.Landing,
            config.Airborne.LandingFadeDuration,
            CharacterAnimationLogicalState.Landing,
            hardLanding ? "Hard landing" : "Landing");
    }

    public void StopAction(CharacterAnimationAction action)
    {
        if (CurrentAction == action) ReturnToLocomotion($"StopAction {action}");
    }

    public void SetHoldingState(bool isHolding, int holdType, float targetWeight = 1f)
    {
        Initialize();
        HoldingItem = isHolding;
        HoldingType = holdType;
        if (!initialized || config == null || config.Holding == null) return;

        float fade = isHolding
            ? ResolveGlobalFadeIn(config.Holding.FadeIn)
            : ResolveGlobalFadeOut(config.Holding.FadeOut);
        if (!isHolding)
        {
            upperBodyLayer.StartFade(0f, fade);
            return;
        }

        AnimationClip clip = config.Holding.Clip;
        if (clip == null)
        {
            LastStateChangeReason = "Holding requested but no holding clip is configured";
            return;
        }

        if (upperBodyLayer.CurrentState == null || upperBodyLayer.CurrentState.Clip != clip)
        {
            PlaySnapshot playSnapshot = RecordPlayRequest(clip.name, "SetHoldingState", false);
            AnimancerState state = upperBodyLayer.Play(CreateClipTransition(clip, null), fade);
            RecordPlayOutcome(playSnapshot, state);
        }
        upperBodyLayer.StartFade(Mathf.Clamp01(targetWeight), fade);
    }

    public void SetUpperBodyAction(CharacterAnimationAction action, float targetWeight)
    {
        Initialize();
        CharacterAnimationConfig.ActionSettings settings = config != null ? config.FindAction(action) : null;
        if (settings == null || settings.Clip == null)
        {
            float fallback = config != null ? config.Layers.UpperBodyFadeDuration : 0.12f;
            float fade = targetWeight >= upperBodyLayer.Weight
                ? ResolveGlobalFadeIn(fallback)
                : ResolveGlobalFadeOut(fallback);
            upperBodyLayer.StartFade(Mathf.Clamp01(targetWeight), fade);
            return;
        }

        float actionFade = targetWeight >= upperBodyLayer.Weight
            ? ResolveGlobalFadeIn(settings.FadeIn)
            : ResolveGlobalFadeOut(settings.FadeOut);
        if (upperBodyLayer.CurrentState == null ||
            upperBodyLayer.CurrentState.Clip != settings.Clip ||
            !upperBodyLayer.CurrentState.IsPlaying)
        {
            PlaySnapshot playSnapshot = RecordPlayRequest(
                settings.Clip.name,
                $"SetUpperBodyAction({action})",
                false);
            AnimancerState state = upperBodyLayer.Play(CreateClipTransition(settings.Clip, null), actionFade);
            RecordPlayOutcome(playSnapshot, state);
        }
        else
        {
            RecordSuppressedSameState(settings.Clip.name, $"SetUpperBodyAction({action})", false);
        }
        upperBodyLayer.StartFade(Mathf.Clamp01(targetWeight), actionFade);
    }

    public void SetAimWeight(float weight)
    {
        Initialize();
        float fallback = config != null ? config.Layers.AdditiveFadeDuration : 0.12f;
        float fade = weight >= additiveLayer.Weight
            ? ResolveGlobalFadeIn(fallback)
            : ResolveGlobalFadeOut(fallback);
        additiveLayer.StartFade(Mathf.Clamp01(weight), fade);
    }

    public void SetLookDirection(Vector3 direction)
    {
        // Public extension point for a future additive MOBILITY PRO look mixer.
    }

    public void ReturnToLocomotion(string reason = "Return to locomotion")
    {
        if (!initialized) return;
        EndTransient(reason, false);
        CancelIdleVariation();
        CleanupActionState(reason);
        CurrentLogicalState = CharacterAnimationLogicalState.Locomotion;
        CurrentAction = CharacterAnimationAction.None;
        QueuedAction = CharacterAnimationAction.None;
        IsInterruptible = true;
        PlayLocomotion();
    }

    [TabGroup("Runtime Debug"), Button(ButtonSizes.Medium)]
    public void PlayIdle()
    {
        if (!Application.isPlaying || config == null || config.Locomotion.Idle == null) return;
        EndTransient("Debug idle", false);
        MarkBaseLayerStandalone();
        PlaySnapshot playSnapshot = RecordPlayRequest(
            config.Locomotion.Idle.name,
            "PlayIdle debug button",
            true);
        currentState = baseLayer.Play(
            CreateClipTransition(config.Locomotion.Idle, null),
            ResolveGlobalFadeIn(config.Locomotion.IdleVariationCrossFade));
        RecordPlayOutcome(playSnapshot, currentState);
        CurrentClip = config.Locomotion.Idle;
        CurrentAnimationState = "Debug Idle";
    }

    [TabGroup("Runtime Debug"), Button(ButtonSizes.Medium)]
    public void PlayWalk()
    {
        if (!Application.isPlaying || config == null) return;
        SetMovement(transform.forward * Mathf.Max(config.Locomotion.WalkSpeed, 1f), transform.forward);
    }

    [TabGroup("Runtime Debug"), Button(ButtonSizes.Medium)]
    public void PlayRun()
    {
        if (!Application.isPlaying || config == null) return;
        SetMovement(transform.forward * Mathf.Max(config.Locomotion.RunSpeed, 1f), transform.forward);
    }

    [TabGroup("Runtime Debug"), Button(ButtonSizes.Medium)]
    public void PrintCurrentAnimationState()
    {
        Debug.Log(
            $"Animancer State={CurrentLogicalState}, Transient={CurrentTransient}, Gait={CurrentGait}, " +
            $"Clip={CurrentClip}, Time={CurrentNormalizedTime:0.00}, Mixer={CurrentMixerParameter}",
            this);
    }

    [TabGroup("Validation"), Button(ButtonSizes.Medium)]
    public CharacterAnimationValidationResult ValidateConfiguration()
    {
        if (animator == null) return CharacterAnimationValidationResult.Warning("Animator is missing.");
        if (animator.avatar == null || !animator.avatar.isValid) return CharacterAnimationValidationResult.Warning("Animator Avatar is missing or invalid.");
        if (animancer == null) return CharacterAnimationValidationResult.Warning("AnimancerComponent is missing.");
        if (config == null) return CharacterAnimationValidationResult.Warning("CharacterAnimationConfig is missing.");
        return config.ValidateConfiguration();
    }

    public bool TryGetCurrentCameraPosition(out Vector3 localPosition, out float blendSpeed)
    {
        localPosition = default;
        blendSpeed = 0f;
        if (config == null || config.Camera == null || !config.Camera.EnablePerAnimationPositions)
            return false;

        AnimationClip clip = ResolveCurrentCameraClip();
        CurrentCameraClip = clip;
        CharacterAnimationConfig.MobilityClipTuning tuning = GetClipTuning(clip);
        if (tuning == null && clip != null)
            tuning = config.FindClipTuning(clip);
        if (tuning == null || !tuning.OverrideCameraPosition)
            return false;

        localPosition = tuning.CameraLocalPosition;
        blendSpeed = Mathf.Max(0.01f, tuning.CameraBlendSpeed);
        return true;
    }

    private AnimationClip ResolveCurrentCameraClip()
    {
        if (usingLocomotionMixer || usingCrouchMixer)
            return ResolveLocomotionClip(
                CurrentMixerParameter,
                CurrentLocomotionSpeed,
                usingCrouchMixer);

        return CurrentClip;
    }

    public float GetLocomotionMotorScale()
    {
        if (config == null || config.Locomotion == null ||
            CurrentTransient != LocomotionTransient.Starting || currentState == null)
            return 1f;

        float delay = Mathf.Clamp(config.Locomotion.StartMovementDelayNormalizedTime, 0f, 0.99f);
        float fullSpeed = Mathf.Clamp(
            config.Locomotion.StartMovementFullSpeedNormalizedTime,
            delay + 0.01f,
            1f);
        float progress = Mathf.InverseLerp(delay, fullSpeed, currentState.NormalizedTime);
        return Mathf.SmoothStep(0f, 1f, progress);
    }

    private void ConfigureLayers()
    {
        baseLayer = animancer.Layers[BaseLayerIndex];
        upperBodyLayer = animancer.Layers[UpperBodyLayerIndex];
        additiveLayer = animancer.Layers[AdditiveLayerIndex];
        reactionLayer = animancer.Layers[ReactionLayerIndex];

        baseLayer.SetDebugName(config != null ? config.Layers.BaseLayerName : "Base");
        upperBodyLayer.SetDebugName("Upper Body / Holding");
        additiveLayer.SetDebugName("Additive");
        reactionLayer.SetDebugName("Reaction");

        if (config == null) return;
        baseLayer.Weight = config.Layers.BaseDefaultWeight;
        upperBodyLayer.Mask = config.Holding.AvatarMask;
        upperBodyLayer.Weight = config.Layers.UpperBodyDefaultWeight;
        additiveLayer.Mask = config.Layers.AdditiveMask;
        additiveLayer.IsAdditive = true;
        additiveLayer.Weight = config.Layers.AdditiveDefaultWeight;
        reactionLayer.Mask = config.Layers.ReactionMask;
        reactionLayer.Weight = config.Layers.ReactionDefaultWeight;
    }

    private void RebuildMixers()
    {
        if (config == null) return;
        ConfiguredLocomotionClipCount = ConfigureDirectionalMixer(locomotionMixer);
        ConfiguredLocomotionClipCount += ConfigureCrouchMixer(crouchMixer);
    }

    private int ConfigureDirectionalMixer(MixerTransition2D mixer)
    {
        List<Object> animations = new List<Object>(25);
        List<Vector2> thresholds = new List<Vector2>(25);
        List<bool> synchronize = new List<bool>(25);
        List<float> speeds = new List<float>(25);
        locomotionChildGaits.Clear();

        AddMixerClip(animations, thresholds, synchronize, speeds, locomotionChildGaits,
            config.Locomotion.Idle, Vector2.zero, false, LocomotionGait.Idle);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, locomotionChildGaits,
            config.Locomotion.Walk, config.Locomotion.WalkSpeed, LocomotionGait.Walk);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, locomotionChildGaits,
            config.Locomotion.Jog, config.Locomotion.JogSpeed, LocomotionGait.Jog);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, locomotionChildGaits,
            config.Locomotion.Run, config.Locomotion.RunSpeed, LocomotionGait.Run);
        ApplyMixerData(mixer, animations, thresholds, synchronize, speeds);
        return animations.Count;
    }

    private int ConfigureCrouchMixer(MixerTransition2D mixer)
    {
        List<Object> animations = new List<Object>(9);
        List<Vector2> thresholds = new List<Vector2>(9);
        List<bool> synchronize = new List<bool>(9);
        List<float> speeds = new List<float>(9);
        crouchChildGaits.Clear();

        AddMixerClip(animations, thresholds, synchronize, speeds, crouchChildGaits,
            config.Locomotion.CrouchIdle, Vector2.zero, false, LocomotionGait.Idle);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, crouchChildGaits,
            config.Locomotion.Crouch, config.Locomotion.CrouchSpeed, LocomotionGait.Crouch);
        ApplyMixerData(mixer, animations, thresholds, synchronize, speeds);
        return animations.Count;
    }

    private void AddDirectionalSet(
        List<Object> animations,
        List<Vector2> thresholds,
        List<bool> synchronize,
        List<float> speeds,
        List<LocomotionGait> childGaits,
        CharacterAnimationConfig.DirectionalClipSet set,
        float speed,
        LocomotionGait gait)
    {
        if (set == null) return;
        bool sync = config.Locomotion.SynchronizeLocomotionCycles;
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.Forward, new Vector2(0f, speed), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.Backward, new Vector2(0f, -speed), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.Left, new Vector2(-speed, 0f), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.Right, new Vector2(speed, 0f), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.ForwardLeft, new Vector2(-Diagonal * speed, Diagonal * speed), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.ForwardRight, new Vector2(Diagonal * speed, Diagonal * speed), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.BackwardLeft, new Vector2(-Diagonal * speed, -Diagonal * speed), sync, gait);
        AddMixerClip(animations, thresholds, synchronize, speeds, childGaits, set.BackwardRight, new Vector2(Diagonal * speed, -Diagonal * speed), sync, gait);
    }

    private void AddMixerClip(
        List<Object> animations,
        List<Vector2> thresholds,
        List<bool> synchronize,
        List<float> speeds,
        List<LocomotionGait> childGaits,
        AnimationClip clip,
        Vector2 threshold,
        bool synchronizeCycle,
        LocomotionGait gait)
    {
        if (clip == null) return;
        animations.Add(clip);
        thresholds.Add(threshold);
        synchronize.Add(synchronizeCycle);
        childGaits.Add(gait);
        CharacterAnimationConfig.MobilityClipTuning tuning = GetClipTuning(clip);
        float playbackSpeed = tuning != null && tuning.OverrideRuntime && tuning.Transition != null
            ? tuning.Transition.Speed
            : 1f;
        speeds.Add(float.IsNaN(playbackSpeed) ? 1f : playbackSpeed);
    }

    private static void ApplyMixerData(
        MixerTransition2D mixer,
        List<Object> animations,
        List<Vector2> thresholds,
        List<bool> synchronize,
        List<float> speeds)
    {
        mixer.Type = MixerTransition2D.MixerType.Cartesian;
        mixer.Animations = animations.ToArray();
        mixer.Thresholds = thresholds.ToArray();
        mixer.SynchronizeChildren = synchronize.ToArray();
        mixer.Speeds = speeds.ToArray();
    }

    private void PlayLocomotion()
    {
        if (config == null || baseLayer == null || CurrentTransient != LocomotionTransient.None) return;
        if (usingIdleVariation && TargetLocomotionSpeed <= config.Locomotion.StopInputThreshold && !Crouching) return;

        MixerTransition2D targetMixer = Crouching ? crouchMixer : locomotionMixer;
        if (!targetMixer.IsValid)
        {
            PlayFallbackDirectionalClip();
            return;
        }

        bool needsPlay = Crouching ? !usingCrouchMixer : !usingLocomotionMixer;
        if (needsPlay)
        {
            Vector2 targetVelocity = new Vector2(TargetVelocityX, TargetVelocityZ);
            AnimationClip incomingClip = ResolveLocomotionClip(
                targetVelocity,
                TargetLocomotionSpeed,
                Crouching);
            CharacterAnimationConfig.MobilityClipTuning incomingTuning = GetClipTuning(incomingClip);
            string requestedState = Crouching
                ? "MOBILITY PRO Crouch Mixer"
                : "MOBILITY PRO Locomotion Mixer";
            PlaySnapshot playSnapshot = RecordPlayRequest(requestedState, "PlayLocomotion", true);
            currentState = baseLayer.Play(
                targetMixer,
                ResolveFadeDuration(incomingTuning, config.Locomotion.LocomotionCrossFade));
            if (hasPreservedLocomotionPhase)
                targetMixer.State.NormalizedTime = preservedLocomotionNormalizedTime;
            RecordPlayOutcome(playSnapshot, currentState);
            activeClipTuning = null;
            activeFallbackFadeOut = config.Locomotion.LocomotionFadeOut;
            usingCrouchMixer = Crouching;
            usingLocomotionMixer = !Crouching;
            usingIdleVariation = false;
            CurrentClip = null;
            CurrentAnimationState = requestedState;
            LastStateChangeReason = Crouching ? "Crouch locomotion" : "Locomotion";
        }

        Vector2 parameter = new Vector2(LocalVelocityX, LocalVelocityZ);
        targetMixer.State.Parameter = parameter;
        targetMixer.State.Speed = Mathf.Clamp(
            config.Locomotion.GlobalPlaybackSpeed,
            config.Locomotion.MinimumPlaybackSpeed,
            config.Locomotion.MaximumPlaybackSpeed);
        CurrentMixerParameter = parameter;
        CurrentGait = ResolveStableGait(CurrentLocomotionSpeed, Crouching, CurrentGait);
        CurrentLogicalState = CharacterAnimationLogicalState.Locomotion;
        CurrentPriority = CharacterAnimationPriority.Locomotion;
        IsInterruptible = true;
    }

    private void PlayFallbackDirectionalClip()
    {
        CharacterAnimationConfig.DirectionalClipSet set = Crouching
            ? config.Locomotion.Crouch
            : CurrentGait == LocomotionGait.Run
                ? config.Locomotion.Run
                : CurrentGait == LocomotionGait.Jog
                    ? config.Locomotion.Jog
                    : config.Locomotion.Walk;
        AnimationClip clip = CurrentLocomotionSpeed <= config.Locomotion.IdleSpeedThreshold
            ? (Crouching ? config.Locomotion.CrouchIdle : config.Locomotion.Idle)
            : set.GetBest(new Vector2(LocalVelocityX, LocalVelocityZ));
        if (clip == null || (CurrentClip == clip && CurrentLogicalState == CharacterAnimationLogicalState.Locomotion)) return;

        currentState = PlayConfiguredClip(clip, config.Locomotion.LocomotionCrossFade, null);
        CurrentClip = clip;
        CurrentAnimationState = clip.name;
        CurrentLogicalState = CharacterAnimationLogicalState.Locomotion;
        LastStateChangeReason = "Fallback directional locomotion";
    }

    private bool TryPlayStart(Vector2 targetVelocity)
    {
        if (!config.Locomotion.EnableStartTransitions) return false;
        LocomotionGait gait = ResolveStableGait(targetVelocity.magnitude, Crouching, lastMovingGait);
        CharacterAnimationConfig.GaitDirectionalClipSet starts = config.Locomotion.UseForwardTurningStarts
            ? config.Locomotion.ForwardTurningStarts
            : config.Locomotion.Starts;
        AnimationClip clip = starts.Get(gait).GetBest(targetVelocity.normalized);
        if (clip == null) return false;
        PlayTransient(
            clip,
            LocomotionTransient.Starting,
            config.Locomotion.StartCrossFade,
            CharacterAnimationLogicalState.LocomotionTransition,
            $"{gait} start");
        return true;
    }

    private bool TryPlayStop()
    {
        if (!config.Locomotion.EnableStopTransitions) return false;
        bool useLeftFoot = ResolveTakeoffFoot();
        AnimationClip clip = config.Locomotion.EnableFootMatchedStops
            ? config.Locomotion.FootMatchedStops.Get(lastMovingGait).GetBest(lastMovingDirection, useLeftFoot)
            : null;
        if (clip == null)
            clip = config.Locomotion.Stops.Get(lastMovingGait).GetBest(lastMovingDirection);
        if (clip == null) return false;
        PlayTransient(
            clip,
            LocomotionTransient.Stopping,
            config.Locomotion.StopCrossFade,
            CharacterAnimationLogicalState.LocomotionTransition,
            $"{lastMovingGait} stop");
        return true;
    }

    private bool TryPlayMovementPivot(Vector2 previousVelocity, Vector2 targetVelocity)
    {
        CharacterAnimationConfig.TurnSettings turns = config.Locomotion.Turns;
        if (!turns.EnableMovementPivots || targetVelocity.magnitude < turns.MinimumPivotSpeed) return false;
        if (previousVelocity.sqrMagnitude < 0.001f || targetVelocity.sqrMagnitude < 0.001f) return false;

        float signedAngle = -Vector2.SignedAngle(previousVelocity, targetVelocity);
        if (Mathf.Abs(signedAngle) < turns.MinimumPivotAngle) return false;
        LocomotionGait gait = ResolveStableGait(targetVelocity.magnitude, false, lastMovingGait);
        AnimationClip clip = turns.GetPivot(gait, signedAngle);
        if (clip == null) return false;
        PlayTransient(
            clip,
            LocomotionTransient.Pivot,
            turns.PivotFadeIn,
            CharacterAnimationLogicalState.LocomotionTransition,
            $"{gait} pivot");
        return true;
    }

    private void UpdateTurnInput(Vector3 desiredForward, bool targetMoving)
    {
        if (desiredForward.sqrMagnitude < 0.001f) return;
        desiredForward.y = 0f;
        desiredForward.Normalize();

        if (previousDesiredForward.sqrMagnitude < 0.001f)
        {
            previousDesiredForward = desiredForward;
            return;
        }

        CharacterAnimationConfig.TurnSettings turns = config.Locomotion.Turns;
        float deltaAngle = Vector3.SignedAngle(previousDesiredForward, desiredForward, Vector3.up);
        CurrentYawRate = Time.deltaTime > 0f ? deltaAngle / Time.deltaTime : 0f;
        bool yawActive = Mathf.Abs(CurrentYawRate) >= 0.5f;
        yawIdleTimer = yawActive ? 0f : yawIdleTimer + Time.deltaTime;

        bool locomotionCanTurn = CurrentLogicalState != CharacterAnimationLogicalState.Action &&
                                 CurrentLogicalState != CharacterAnimationLogicalState.Parkour &&
                                 CurrentLogicalState != CharacterAnimationLogicalState.Dead &&
                                 !Airborne;
        if (!locomotionCanTurn)
        {
            ResetPendingTurn();
            previousDesiredForward = desiredForward;
            return;
        }

        if (targetMoving)
        {
            ResetPendingTurn();
            if (CurrentTransient == LocomotionTransient.TurnLoop)
                EndTransient("Movement interrupted turn loop", false);

            float absoluteYawRate = Mathf.Abs(CurrentYawRate);
            movementCurveExitTimer = CurrentTransient == LocomotionTransient.MovementCurve &&
                                     absoluteYawRate <= turns.CurveExitYawRate
                ? movementCurveExitTimer + Time.deltaTime
                : 0f;

            if (turns.EnableMovementCurves &&
                absoluteYawRate >= turns.CurveStartYawRate)
            {
                TryPlayMovementCurve(CurrentYawRate);
            }
            else if (CurrentTransient == LocomotionTransient.MovementCurve &&
                     movementCurveExitTimer >= turns.CurveExitDelay)
            {
                EndTransient("Movement curve complete", true);
            }
        }
        else if (turns.EnableTurnInPlace)
        {
            movementCurveExitTimer = 0f;
            if (CurrentTransient == LocomotionTransient.MovementCurve)
                EndTransient("Stopped during movement curve", false);

            if (CurrentTransient == LocomotionTransient.TurnLoop)
            {
                if (turns.EnableContinuousTurnLoops &&
                    Mathf.Abs(CurrentYawRate) >= turns.TurnLoopStartYawRate)
                    TryPlayTurnLoop(CurrentYawRate);
                else if (yawIdleTimer >= turns.TurnLoopExitDelay)
                    EndTransient("Turn loop complete", true);
            }
            else if (CurrentTransient == LocomotionTransient.None)
            {
                PendingTurnAngle = Mathf.Clamp(PendingTurnAngle + deltaAngle, -180f, 180f);
                if (turns.EnableContinuousTurnLoops &&
                    Mathf.Abs(CurrentYawRate) >= turns.TurnLoopStartYawRate &&
                    TryPlayTurnLoop(CurrentYawRate))
                {
                    PendingTurnAngle = 0f;
                }
                else if (yawIdleTimer >= turns.TurnInputSettleTime &&
                         Mathf.Abs(PendingTurnAngle) >= turns.MinimumTurnAngle &&
                         RequestTurn(PendingTurnAngle))
                {
                    PendingTurnAngle = 0f;
                }
            }
        }
        else
        {
            movementCurveExitTimer = 0f;
            ResetPendingTurn();
        }

        previousDesiredForward = desiredForward;
    }

    private bool TryPlayTurnLoop(float signedYawRate)
    {
        CharacterAnimationConfig.TurnSettings turns = config.Locomotion.Turns;
        if (CurrentTransient != LocomotionTransient.None && CurrentTransient != LocomotionTransient.TurnLoop)
            return false;

        AnimationClip clip = turns.GetTurnLoop(Crouching, signedYawRate);
        if (clip == null) return false;
        if (CurrentTransient == LocomotionTransient.TurnLoop && CurrentClip == clip) return true;

        PlayLoopTransient(
            clip,
            LocomotionTransient.TurnLoop,
            turns.TurnLoopFadeIn,
            turns.TurnLoopFadeOut,
            signedYawRate < 0f ? "Continuous turn left" : "Continuous turn right");
        return true;
    }

    private bool TryPlayMovementCurve(float signedYawRate)
    {
        CharacterAnimationConfig.TurnSettings turns = config.Locomotion.Turns;
        if (CurrentTransient != LocomotionTransient.None && CurrentTransient != LocomotionTransient.MovementCurve)
            return false;
        if (TargetLocomotionSpeed < turns.MinimumCurveSpeed) return false;

        LocomotionGait gait = config.ResolveGait(TargetLocomotionSpeed, Crouching);
        bool movingBackward = TargetVelocityZ < -config.Locomotion.StopInputThreshold;
        AnimationClip clip = turns.GetMovementCurve(gait, movingBackward, signedYawRate);
        if (clip == null) return false;
        if (CurrentTransient == LocomotionTransient.MovementCurve && CurrentClip == clip) return true;

        PlayLoopTransient(
            clip,
            LocomotionTransient.MovementCurve,
            turns.CurveFadeIn,
            turns.CurveFadeOut,
            signedYawRate < 0f ? $"{gait} curve left" : $"{gait} curve right");
        return true;
    }

    private void PlayLoopTransient(
        AnimationClip clip,
        LocomotionTransient transient,
        float fadeDuration,
        float fadeOut,
        string reason)
    {
        if (clip == null || baseLayer == null) return;
        transientSerial++;
        CurrentTransient = transient;
        CancelIdleVariation();
        currentState = PlayConfiguredClip(clip, fadeDuration, null, reason);
        activeFallbackFadeOut = fadeOut;
        CurrentClip = clip;
        CurrentAnimationState = clip.name;
        CurrentLogicalState = CharacterAnimationLogicalState.LocomotionTransition;
        LastStateChangeReason = reason;
    }

    private void PlayTransient(
        AnimationClip clip,
        LocomotionTransient transient,
        float fadeDuration,
        CharacterAnimationLogicalState logicalState,
        string reason)
    {
        if (clip == null || baseLayer == null) return;
        int serial = ++transientSerial;
        CurrentTransient = transient;
        CancelIdleVariation();
        currentState = PlayConfiguredClip(
            clip,
            fadeDuration,
            () => OnTransientEnd(serial),
            reason);
        activeFallbackFadeOut = GetDefaultFadeOut(transient);
        CurrentClip = clip;
        CurrentAnimationState = clip.name;
        CurrentLogicalState = logicalState;
        LastStateChangeReason = reason;
    }

    private void UpdateTransientCompletion()
    {
        if (CurrentTransient == LocomotionTransient.None || currentState == null || config == null) return;
        CurrentNormalizedTime = currentState.NormalizedTime;
        if (CurrentTransient == LocomotionTransient.TurnLoop ||
            CurrentTransient == LocomotionTransient.MovementCurve)
            return;
        float blendOut = GetTransientBlendOutTime(CurrentTransient);
        if (CurrentNormalizedTime < blendOut) return;

        if (CurrentTransient == LocomotionTransient.JumpStart)
        {
            EndTransient("Jump start complete", false);
            PlayAirborneLoop();
        }
        else if (CurrentTransient == LocomotionTransient.Landing)
        {
            EndTransient("Landing complete", false);
            LastAnimationEvent = "LandingComplete";
            ReturnToLocomotion("Landing complete");
        }
        else
        {
            EndTransient("Locomotion transition complete", true);
        }
    }

    private float GetTransientBlendOutTime(LocomotionTransient transient)
    {
        if (activeClipTuning != null && activeClipTuning.OverrideRuntime && activeClipTuning.Clip == CurrentClip)
            return Mathf.Min(activeClipTuning.BlendOutNormalizedTime, activeClipTuning.EndNormalizedTime);

        switch (transient)
        {
            case LocomotionTransient.Starting: return config.Locomotion.StartBlendOutNormalizedTime;
            case LocomotionTransient.Stopping: return config.Locomotion.StopBlendOutNormalizedTime;
            case LocomotionTransient.CrouchEnter:
            case LocomotionTransient.CrouchExit: return config.Locomotion.CrouchBlendOutNormalizedTime;
            case LocomotionTransient.Turn: return config.Locomotion.Turns.TurnBlendOutNormalizedTime;
            case LocomotionTransient.Pivot: return config.Locomotion.Turns.PivotBlendOutNormalizedTime;
            case LocomotionTransient.JumpStart: return config.Airborne.JumpStartBlendOutNormalizedTime;
            case LocomotionTransient.Landing: return config.Airborne.LandingBlendOutNormalizedTime;
            default: return 1f;
        }
    }

    private void OnTransientEnd(int serial)
    {
        if (serial != transientSerial || CurrentTransient == LocomotionTransient.None) return;
        LocomotionTransient completed = CurrentTransient;
        if (completed == LocomotionTransient.JumpStart)
        {
            EndTransient("Jump start event", false);
            PlayAirborneLoop();
        }
        else if (completed == LocomotionTransient.Landing)
        {
            LastAnimationEvent = "LandingComplete";
            ReturnToLocomotion("Landing event");
        }
        else
        {
            EndTransient("Transition event", true);
        }
    }

    private void EndTransient(string reason, bool resumeLocomotion)
    {
        if (CurrentTransient == LocomotionTransient.None) return;
        transientSerial++;
        CurrentTransient = LocomotionTransient.None;
        LastStateChangeReason = reason;
        if (!resumeLocomotion) return;

        if (Airborne && !Grounded)
            PlayAirborneLoop();
        else
            PlayLocomotion();
    }

    private void PlayAirborne()
    {
        if (CurrentTransient == LocomotionTransient.JumpStart) return;
        PlayAirborneLoop();
    }

    private void PlayAirborneLoop()
    {
        if (config == null || config.Airborne == null || baseLayer == null) return;
        AnimationClip clip;
        if (jumpActive)
        {
            clip = config.Airborne.JumpAir.GetBest(jumpGait, jumpDirection, jumpUsesLeftFoot);
        }
        else if (AirborneTime >= config.Airborne.LongFallTime && config.Airborne.LongFall != null)
        {
            clip = config.Airborne.LongFall;
        }
        else if (AirborneTime <= config.Airborne.ShortFallTime && config.Airborne.ShortFall != null)
        {
            clip = config.Airborne.ShortFall;
        }
        else
        {
            clip = config.Airborne.Fall;
        }

        if (clip == null) clip = config.Airborne.JumpAir.Standing;
        if (clip == null) return;
        if (CurrentLogicalState == CharacterAnimationLogicalState.Airborne && CurrentClip == clip)
        {
            RecordSuppressedSameState(clip.name, "PlayAirborneLoop");
            return;
        }

        EndTransient("Enter airborne loop", false);
        currentState = PlayConfiguredClip(
            clip,
            config.Airborne.AirFadeDuration,
            null,
            jumpActive ? "PlayAirborneLoop jump" : "PlayAirborneLoop fall");
        activeFallbackFadeOut = jumpActive ? config.Airborne.AirFadeOut : config.Airborne.FallFadeOut;
        CurrentClip = clip;
        CurrentAnimationState = clip.name;
        CurrentLogicalState = CharacterAnimationLogicalState.Airborne;
        CurrentPriority = CharacterAnimationPriority.Airborne;
        IsInterruptible = true;
        LastStateChangeReason = jumpActive ? "Jump air" : "Fall";
    }

    private bool ResolveTakeoffFoot()
    {
        if (!config.Airborne.AlternateTakeoffFoot) return true;
        if (currentState == null) return !jumpUsesLeftFoot;
        float phase = Mathf.Repeat(currentState.NormalizedTime, 1f);
        return phase < 0.5f;
    }

    private void UpdateIdleVariation()
    {
        if (!initialized || config == null || !config.Locomotion.EnableIdleVariations || Crouching || Airborne) return;
        if (CurrentTransient != LocomotionTransient.None || CurrentLogicalState != CharacterAnimationLogicalState.Locomotion) return;
        if (TargetLocomotionSpeed > config.Locomotion.StopInputThreshold)
        {
            idleVariationTimer = 0f;
            return;
        }

        AnimationClip[] variations = config.Locomotion.IdleVariations;
        if (variations == null || variations.Length == 0 || usingIdleVariation) return;
        idleVariationTimer += Time.deltaTime;
        if (idleVariationTimer < config.Locomotion.IdleVariationInterval) return;

        idleVariationTimer = 0f;
        for (int i = 0; i < variations.Length; i++)
        {
            AnimationClip clip = variations[idleVariationIndex % variations.Length];
            idleVariationIndex++;
            if (clip == null) continue;
            int serial = ++idleVariationSerial;
            currentState = PlayConfiguredClip(
                clip,
                config.Locomotion.IdleVariationCrossFade,
                () => OnIdleVariationEnd(serial),
                "UpdateIdleVariation");
            activeFallbackFadeOut = config.Locomotion.IdleVariationCrossFade;
            CurrentClip = clip;
            CurrentAnimationState = clip.name;
            usingIdleVariation = true;
            LastStateChangeReason = "Idle variation";
            break;
        }
    }

    private void OnIdleVariationEnd(int serial)
    {
        if (serial != idleVariationSerial || !usingIdleVariation) return;

        usingIdleVariation = false;
        LastAnimationEvent = "IdleVariationComplete";
        if (!Crouching && !Airborne &&
            TargetLocomotionSpeed <= config.Locomotion.StopInputThreshold)
        {
            PlayLocomotion();
        }
    }

    private void CancelIdleVariation()
    {
        if (!usingIdleVariation) return;
        idleVariationSerial++;
        usingIdleVariation = false;
    }

    private static ClipTransition CreateClipTransition(AnimationClip clip, Action onEnd)
    {
        ClipTransition transition = new ClipTransition { Clip = clip };
        if (onEnd != null) transition.Events.OnEnd = onEnd;
        return transition;
    }

    private void BuildClipTuningLookup()
    {
        clipTunings.Clear();
        if (config == null || config.ClipTunings == null) return;
        for (int i = 0; i < config.ClipTunings.Length; i++)
        {
            CharacterAnimationConfig.MobilityClipTuning tuning = config.ClipTunings[i];
            if (tuning == null || tuning.Clip == null || clipTunings.ContainsKey(tuning.Clip)) continue;
            clipTunings.Add(tuning.Clip, tuning);
        }
    }

    private CharacterAnimationConfig.MobilityClipTuning GetClipTuning(AnimationClip clip)
    {
        if (clip == null) return null;
        clipTunings.TryGetValue(clip, out CharacterAnimationConfig.MobilityClipTuning tuning);
        return tuning;
    }

    private AnimationClip ResolveLocomotionClip(Vector2 velocity, float speed, bool crouching)
    {
        if (config == null || config.Locomotion == null) return null;
        if (speed <= config.Locomotion.IdleSpeedThreshold)
            return crouching ? config.Locomotion.CrouchIdle : config.Locomotion.Idle;

        CharacterAnimationConfig.DirectionalClipSet clips;
        switch (config.ResolveGait(speed, crouching))
        {
            case LocomotionGait.Run:
                clips = config.Locomotion.Run;
                break;
            case LocomotionGait.Jog:
                clips = config.Locomotion.Jog;
                break;
            case LocomotionGait.Crouch:
                clips = config.Locomotion.Crouch;
                break;
            default:
                clips = config.Locomotion.Walk;
                break;
        }

        return clips.GetBest(velocity);
    }

    private AnimancerState PlayConfiguredClip(
        AnimationClip clip,
        float defaultFadeIn,
        Action onEnd,
        string caller = "PlayConfiguredClip")
    {
        CharacterAnimationConfig.MobilityClipTuning tuning = GetClipTuning(clip);
        float fadeDuration = ResolveFadeDuration(tuning, defaultFadeIn);
        MarkBaseLayerStandalone();
        PlaySnapshot playSnapshot = RecordPlayRequest(clip.name, caller, true);
        AnimancerState state;
        if (tuning != null && tuning.OverrideRuntime && tuning.Transition != null)
            state = baseLayer.Play(tuning.Transition, fadeDuration);
        else
            state = baseLayer.Play(CreateClipTransition(clip, null), fadeDuration);

        AnimancerEvent.Sequence events = state.Events(this);
        if (tuning != null && tuning.OverrideRuntime)
            events.NormalizedEndTime = Mathf.Max(0.01f, tuning.EndNormalizedTime);
        events.OnEnd = onEnd;
        if (state is ClipState clipState)
            clipState.ApplyFootIK = tuning == null || tuning.ApplyFootIK;
        activeClipTuning = tuning != null && tuning.OverrideRuntime ? tuning : null;
        activeFallbackFadeOut = defaultFadeIn;
        RecordPlayOutcome(playSnapshot, state);
        return state;
    }

    private void MarkBaseLayerStandalone()
    {
        if ((usingLocomotionMixer || usingCrouchMixer) && currentState != null)
        {
            preservedLocomotionNormalizedTime = currentState.NormalizedTime;
            hasPreservedLocomotionPhase = true;
        }

        usingLocomotionMixer = false;
        usingCrouchMixer = false;
    }

    private LocomotionGait ResolveStableGait(
        float speed,
        bool crouching,
        LocomotionGait previous)
    {
        if (crouching) return LocomotionGait.Crouch;

        float hysteresis = Mathf.Max(0f, config.Locomotion.GaitHysteresis);
        float jogBoundary = (config.Locomotion.WalkSpeed + config.Locomotion.JogSpeed) * 0.5f;
        float runBoundary = (config.Locomotion.JogSpeed + config.Locomotion.RunSpeed) * 0.5f;

        if (speed <= config.Locomotion.IdleSpeedThreshold)
            return LocomotionGait.Idle;

        switch (previous)
        {
            case LocomotionGait.Run:
                return speed < runBoundary - hysteresis
                    ? LocomotionGait.Jog
                    : LocomotionGait.Run;
            case LocomotionGait.Jog:
                if (speed > runBoundary + hysteresis) return LocomotionGait.Run;
                return speed < jogBoundary - hysteresis
                    ? LocomotionGait.Walk
                    : LocomotionGait.Jog;
            case LocomotionGait.Walk:
                return speed > jogBoundary + hysteresis
                    ? LocomotionGait.Jog
                    : LocomotionGait.Walk;
            default:
                if (speed > runBoundary + hysteresis) return LocomotionGait.Run;
                if (speed > jogBoundary + hysteresis) return LocomotionGait.Jog;
                return LocomotionGait.Walk;
        }
    }

    private float ResolveFadeDuration(CharacterAnimationConfig.MobilityClipTuning incoming, float fallback)
    {
        float fadeIn = incoming != null && incoming.OverrideRuntime && incoming.Transition != null
            ? incoming.Transition.FadeDuration
            : fallback;
        CharacterAnimationConfig.MobilityClipTuning outgoing = activeClipTuning;
        if (outgoing == null && (usingLocomotionMixer || usingCrouchMixer))
        {
            AnimationClip outgoingClip = ResolveLocomotionClip(
                CurrentMixerParameter,
                CurrentLocomotionSpeed,
                usingCrouchMixer);
            outgoing = GetClipTuning(outgoingClip);
        }

        float fadeOut = outgoing != null && outgoing.OverrideRuntime
            ? outgoing.FadeOut
            : activeFallbackFadeOut;
        return ResolveTransitionFade(fadeIn, fadeOut);
    }

    private float ResolveTransitionFade(float fadeIn, float fadeOut)
    {
        LastResolvedFadeDuration = Mathf.Max(
            ResolveGlobalFadeIn(fadeIn),
            ResolveGlobalFadeOut(fadeOut));
        return LastResolvedFadeDuration;
    }

    private float ResolveGlobalFadeIn(float duration)
    {
        return config != null && config.GlobalFades != null
            ? config.GlobalFades.ResolveFadeIn(duration)
            : Mathf.Max(0f, duration);
    }

    private float ResolveGlobalFadeOut(float duration)
    {
        return config != null && config.GlobalFades != null
            ? config.GlobalFades.ResolveFadeOut(duration)
            : Mathf.Max(0f, duration);
    }

    private float GetDefaultFadeOut(LocomotionTransient transient)
    {
        switch (transient)
        {
            case LocomotionTransient.Starting: return config.Locomotion.StartFadeOut;
            case LocomotionTransient.Stopping: return config.Locomotion.StopFadeOut;
            case LocomotionTransient.CrouchEnter:
            case LocomotionTransient.CrouchExit: return config.Locomotion.CrouchFadeOut;
            case LocomotionTransient.Turn: return config.Locomotion.Turns.TurnCrossFade;
            case LocomotionTransient.TurnLoop: return config.Locomotion.Turns.TurnLoopFadeOut;
            case LocomotionTransient.Pivot: return config.Locomotion.Turns.PivotFadeOut;
            case LocomotionTransient.MovementCurve: return config.Locomotion.Turns.CurveFadeOut;
            case LocomotionTransient.JumpStart: return config.Airborne.JumpFadeOut;
            case LocomotionTransient.Landing: return config.Airborne.LandingFadeOut;
            default: return config.Locomotion.LocomotionFadeOut;
        }
    }

    private void ResetPendingTurn()
    {
        PendingTurnAngle = 0f;
    }

    private bool CanInterruptWith(CharacterAnimationPriority priority)
    {
        return actionCoordinator.CanInterrupt(
            CurrentLogicalState,
            CurrentPriority,
            priority,
            IsInterruptible,
            activeActionSettings != null);
    }

    private void UpdateActionCompletion()
    {
        if (currentState == null) return;
        CurrentNormalizedTime = currentState.NormalizedTime;
        if (activeActionSettings != null)
        {
            CharacterActionProgressDecision decision = actionCoordinator.EvaluateProgress(
                CurrentNormalizedTime,
                activeActionSettings.CommitNormalizedTime,
                activeActionSettings.InterruptibleNormalizedTime,
                activeActionSettings.Interruptible,
                HasCommitted,
                IsInterruptible);
            if (decision.CommitReached)
            {
                HasCommitted = true;
                LastAnimationEvent = $"{CurrentAction}Commit";
                ActionCommitted?.Invoke(CurrentAction);
            }

            IsInterruptible = decision.Interruptible;
        }
        else if (activeParkourSettings != null && !IsInterruptible &&
                 CurrentNormalizedTime >= activeParkourSettings.InterruptibleStart)
        {
            IsInterruptible = true;
        }
        else if (CurrentLogicalState == CharacterAnimationLogicalState.Landing &&
                 !IsInterruptible && config != null && config.Airborne != null &&
                 StateTime >= Mathf.Max(
                     config.Airborne.LandingLockTime,
                     config.Airborne.InterruptibleAfter))
        {
            IsInterruptible = true;
        }
    }

    private PlaySnapshot RecordPlayRequest(
        string requestedState,
        string caller,
        bool changesBaseState)
    {
        EnsureDiagnosticsFrame();
        AnimancerState previousState = changesBaseState
            ? currentState
            : upperBodyLayer != null ? upperBodyLayer.CurrentState : null;
        bool sameStateRequest =
            (changesBaseState &&
             string.Equals(requestedState, CurrentAnimationState, StringComparison.Ordinal)) ||
            (previousState != null && previousState.Clip != null &&
             string.Equals(requestedState, previousState.Clip.name, StringComparison.Ordinal));

        AnimationRequestsThisFrame++;
        PlayCallsThisFrame++;
        RequestedAnimationState = requestedState;
        LastPlayCaller = caller;
        LastRequestedLayer = changesBaseState ? "Base" : "Upper Body";
        LastPlayReusedState = false;
        LastPlayResetTime = false;
        LastPlayRestartedFade = false;
        LastPlayResetWeight = false;
        LastPlayReappliedTransition = false;

        if (sameStateRequest)
        {
            SameStateRequestsThisFrame++;
            sameStateRequestsInWindow++;
        }
        else if (changesBaseState)
        {
            PreviousAnimationState = CurrentAnimationState;
            StateTime = 0f;
        }

        return new PlaySnapshot(previousState, sameStateRequest, changesBaseState);
    }

    private void RecordSuppressedSameState(
        string requestedState,
        string caller,
        bool changesBaseState = true)
    {
        EnsureDiagnosticsFrame();
        AnimationRequestsThisFrame++;
        SameStateRequestsThisFrame++;
        sameStateRequestsInWindow++;
        RequestedAnimationState = requestedState;
        LastPlayCaller = caller;
        LastRequestedLayer = changesBaseState ? "Base" : "Upper Body";
        LastPlayReusedState = false;
        LastPlayResetTime = false;
        LastPlayRestartedFade = false;
        LastPlayResetWeight = false;
        LastPlayReappliedTransition = false;
    }

    private void RecordRequestWithoutPlay(
        string requestedState,
        string caller,
        bool changesBaseState)
    {
        EnsureDiagnosticsFrame();
        AnimationRequestsThisFrame++;
        RequestedAnimationState = requestedState;
        LastPlayCaller = caller;
        LastRequestedLayer = changesBaseState ? "Base" : "Upper Body";
        LastPlayReusedState = false;
        LastPlayResetTime = false;
        LastPlayRestartedFade = false;
        LastPlayResetWeight = false;
        LastPlayReappliedTransition = false;
    }

    private void RecordPlayOutcome(PlaySnapshot snapshot, AnimancerState playedState)
    {
        if (playedState == null) return;

        LastPlayReusedState = ReferenceEquals(snapshot.State, playedState);
        LastPlayReappliedTransition = snapshot.WasSameStateRequest;
        if (snapshot.WasSameStateRequest && snapshot.State != null)
        {
            float normalizedTime = playedState.NormalizedTime;
            LastPlayResetTime =
                normalizedTime + 0.01f < snapshot.NormalizedTime ||
                (snapshot.NormalizedTime > 0.05f && normalizedTime < 0.02f);
            LastPlayRestartedFade =
                Mathf.Abs(playedState.TargetWeight - snapshot.TargetWeight) > 0.001f ||
                Mathf.Abs(playedState.FadeSpeed - snapshot.FadeSpeed) > 0.001f;
            LastPlayResetWeight = playedState.Weight + 0.001f < snapshot.Weight;
            sameStateReplaysInWindow++;
        }
        else if (snapshot.ChangesBaseState)
        {
            stateChangesInWindow++;
        }

        if (config != null && config.Diagnostics != null && config.Diagnostics.EnableConsoleLogging)
        {
            Debug.Log(
                $"[Animation] Frame={Time.frameCount} Caller={LastPlayCaller} " +
                $"Requested={RequestedAnimationState} Previous={PreviousAnimationState} " +
                $"Current={playedState} Layer={LastRequestedLayer} Priority={CurrentPriority} " +
                $"CanInterrupt={IsInterruptible} Time={playedState.NormalizedTime:0.000} " +
                $"RawSpeed={RawLocomotionSpeed:0.00} SmoothedSpeed={CurrentLocomotionSpeed:0.00} " +
                $"Mixer={CurrentMixerParameter} Same={snapshot.WasSameStateRequest} " +
                $"Reused={LastPlayReusedState} TimeReset={LastPlayResetTime} " +
                $"FadeRestart={LastPlayRestartedFade} WeightReset={LastPlayResetWeight}",
                this);
        }
    }

    private void RecordInterrupt(string reason)
    {
        interruptsInWindow++;
        LastInterruptReason = reason;
        if (config != null && config.Diagnostics != null && config.Diagnostics.EnableConsoleLogging)
            Debug.Log($"[Animation Interrupt] Frame={Time.frameCount} {reason}", this);
    }

    private void EnsureDiagnosticsFrame()
    {
        if (diagnosticsFrame == Time.frameCount) return;
        diagnosticsFrame = Time.frameCount;
        PlayCallsThisFrame = 0;
        AnimationRequestsThisFrame = 0;
        SameStateRequestsThisFrame = 0;
    }

    private void RefreshDiagnosticsWindow()
    {
        EnsureDiagnosticsFrame();
        if (diagnosticsWindowStart <= 0f)
            diagnosticsWindowStart = Time.unscaledTime;
        if (Time.unscaledTime - diagnosticsWindowStart < 1f) return;

        StateChangesLastSecond = stateChangesInWindow;
        SameStateRequestsLastSecond = sameStateRequestsInWindow;
        SameStateReplaysLastSecond = sameStateReplaysInWindow;
        InterruptsLastSecond = interruptsInWindow;

        CharacterAnimationConfig.DiagnosticsSettings settings = config != null ? config.Diagnostics : null;
        if (settings != null && sameStateRequestsInWindow >= settings.SameStateReplayWarningThreshold)
            DiagnosticsWarning = "Excessive repeated animation requests";
        else if (settings != null && stateChangesInWindow >= settings.StateChangeWarningThreshold)
            DiagnosticsWarning = "Animation state thrashing detected";
        else if (settings != null && interruptsInWindow >= settings.InterruptWarningThreshold)
            DiagnosticsWarning = "Animation is being interrupted repeatedly";
        else
            DiagnosticsWarning = string.Empty;

        stateChangesInWindow = 0;
        sameStateRequestsInWindow = 0;
        sameStateReplaysInWindow = 0;
        interruptsInWindow = 0;
        diagnosticsWindowStart = Time.unscaledTime;
    }

    private void OnActionEnd()
    {
        if (!ReferenceEquals(AnimancerEvent.Current.State, currentState) ||
            (CurrentLogicalState != CharacterAnimationLogicalState.Action &&
             CurrentLogicalState != CharacterAnimationLogicalState.Parkour))
            return;

        CharacterAnimationAction queued = QueuedAction;
        LastAnimationEvent = "ActionComplete";
        ReturnToLocomotion("Action complete");
        if (queued != CharacterAnimationAction.None)
            TryPlayAction(queued);
    }

    private void CleanupActionState(string reason)
    {
        activeActionSettings = null;
        activeParkourSettings = null;
        SetRootMotionOwnership(CharacterAnimationRootMotionStrategy.Disabled);
        CurrentPriority = CharacterAnimationPriority.Locomotion;
        HasCommitted = false;
        IsInterruptible = true;
        LastStateChangeReason = reason;
    }

    private void SetRootMotionOwnership(CharacterAnimationRootMotionStrategy strategy)
    {
        if (RootMotionStrategy == strategy &&
            RootMotionActive == (strategy != CharacterAnimationRootMotionStrategy.Disabled))
            return;

        RootMotionStrategy = strategy;
        RootMotionActive = strategy != CharacterAnimationRootMotionStrategy.Disabled;
        switch (strategy)
        {
            case CharacterAnimationRootMotionStrategy.UseAnimationDelta:
                MotionOwner = CharacterAnimationMotionOwner.AnimatorRootMotion;
                break;
            case CharacterAnimationRootMotionStrategy.ForwardToCharacterMotor:
                MotionOwner = CharacterAnimationMotionOwner.CharacterMotorRootMotion;
                break;
            case CharacterAnimationRootMotionStrategy.CustomHandler:
                MotionOwner = CharacterAnimationMotionOwner.Scripted;
                break;
            default:
                MotionOwner = CharacterAnimationMotionOwner.CharacterMotor;
                break;
        }

        if (animator != null)
        {
            animator.applyRootMotion =
                strategy == CharacterAnimationRootMotionStrategy.UseAnimationDelta ||
                strategy == CharacterAnimationRootMotionStrategy.ForwardToCharacterMotor;
        }

        if (config != null && config.Diagnostics != null && config.Diagnostics.EnableConsoleLogging)
            Debug.Log($"[Animation Motion] Owner={MotionOwner}, Strategy={RootMotionStrategy}", this);
    }

    private void RefreshRuntimeDebug()
    {
        if (currentState != null)
        {
            CurrentNormalizedTime = currentState.NormalizedTime;
            CurrentStateWeight = currentState.Weight;
            CurrentStateTargetWeight = currentState.TargetWeight;
            CurrentStateFadeSpeed = currentState.FadeSpeed;
            CurrentPlaybackSpeed = currentState.EffectiveSpeed;
            CurrentStateIsPlaying = currentState.IsPlaying;
            CurrentStateIsFading = currentState.FadeGroup != null;
            AnimationClip clip = currentState.Clip;
            if (clip != null) CurrentClip = clip;
        }

        RefreshMixerWeights();
    }

    private void RefreshMixerWeights()
    {
        IdleWeight = 0f;
        WalkWeight = 0f;
        JogWeight = 0f;
        RunWeight = 0f;
        CrouchWeight = 0f;

        ManualMixerState mixerState;
        List<LocomotionGait> childGaits;
        if (usingCrouchMixer)
        {
            mixerState = crouchMixer.State;
            childGaits = crouchChildGaits;
        }
        else if (usingLocomotionMixer)
        {
            mixerState = locomotionMixer.State;
            childGaits = locomotionChildGaits;
        }
        else
        {
            return;
        }

        int count = Mathf.Min(mixerState.ChildCount, childGaits.Count);
        for (int i = 0; i < count; i++)
        {
            float weight = mixerState.GetChild(i).Weight;
            switch (childGaits[i])
            {
                case LocomotionGait.Idle: IdleWeight += weight; break;
                case LocomotionGait.Walk: WalkWeight += weight; break;
                case LocomotionGait.Jog: JogWeight += weight; break;
                case LocomotionGait.Run: RunWeight += weight; break;
                case LocomotionGait.Crouch: CrouchWeight += weight; break;
            }
        }
    }

    private float GetLayerWeight(int layer)
    {
        if (animancer == null || !animancer.IsGraphInitialized) return 0f;
        return animancer.Layers[layer].Weight;
    }
}
