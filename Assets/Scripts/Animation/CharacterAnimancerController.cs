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
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentNormalizedTime { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float CurrentLocomotionSpeed { get; private set; }
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly] public float TargetLocomotionSpeed { get; private set; }
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

    private readonly MixerTransition2D locomotionMixer = new MixerTransition2D();
    private readonly MixerTransition2D crouchMixer = new MixerTransition2D();
    private readonly Dictionary<AnimationClip, CharacterAnimationConfig.MobilityClipTuning> clipTunings =
        new Dictionary<AnimationClip, CharacterAnimationConfig.MobilityClipTuning>();
    private AnimancerLayer baseLayer;
    private AnimancerLayer upperBodyLayer;
    private AnimancerLayer additiveLayer;
    private AnimancerLayer reactionLayer;
    private AnimancerState currentState;
    private CharacterAnimationConfig.ActionSettings activeActionSettings;
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
    private int transientSerial;

    public CharacterAnimationConfig Config => config;
    public Animator Animator => animator;
    public bool IsInitialized => initialized;
    public bool HasUsableConfiguration => config != null && config.ValidateConfiguration().IsValid;

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
        if (Airborne) AirborneTime += Time.deltaTime;
        UpdateActionCompletion();
        UpdateTransientCompletion();
        UpdateIdleVariation();
        RefreshRuntimeDebug();
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

    public void Initialize()
    {
        if (initialized) return;

        if (animator == null) animator = GetComponent<Animator>();
        if (animancer == null) animancer = GetComponent<AnimancerComponent>();
        if (animancer == null) animancer = gameObject.AddComponent<AnimancerComponent>();
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
        Initialize();
        if (!initialized || config == null) return;

        Vector3 targetLocal = transform.InverseTransformDirection(worldVelocity);
        targetLocal.y = 0f;
        TargetVelocityX = targetLocal.x;
        TargetVelocityZ = targetLocal.z;
        TargetLocomotionSpeed = new Vector2(targetLocal.x, targetLocal.z).magnitude;

        float previousSpeed = CurrentLocomotionSpeed;
        float smoothTime = SelectVelocitySmoothTime(targetLocal);
        float maxSmoothSpeed = config.Locomotion.MaximumVisualAcceleration > 0f
            ? config.Locomotion.MaximumVisualAcceleration
            : Mathf.Infinity;
        smoothedLocalVelocity = Vector3.SmoothDamp(
            smoothedLocalVelocity,
            targetLocal,
            ref localVelocitySmoothDamp,
            smoothTime,
            maxSmoothSpeed,
            Time.deltaTime);

        LocalVelocityX = smoothedLocalVelocity.x;
        LocalVelocityZ = smoothedLocalVelocity.z;
        CurrentLocomotionSpeed = new Vector2(LocalVelocityX, LocalVelocityZ).magnitude;
        CurrentVisualAcceleration = Time.deltaTime > 0f
            ? (CurrentLocomotionSpeed - previousSpeed) / Time.deltaTime
            : 0f;

        Vector2 targetVelocity = new Vector2(TargetVelocityX, TargetVelocityZ);
        bool targetMoving = TargetLocomotionSpeed >= config.Locomotion.StartInputThreshold;
        bool wasTargetMoving = previousTargetVelocity.magnitude >= config.Locomotion.StopInputThreshold;

        UpdateTurnInput(desiredForward, targetMoving);

        if (targetMoving)
        {
            lastMovingDirection = targetVelocity.normalized;
            lastMovingGait = config.ResolveGait(TargetLocomotionSpeed, Crouching);
            usingIdleVariation = false;
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
            usingLocomotionMixer = false;
            usingCrouchMixer = false;
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

        if (!CanInterruptWith(settings.Priority))
        {
            QueuedAction = action;
            return false;
        }

        EndTransient("Action started", false);
        CleanupActionState($"Action {action} started");
        CurrentAction = action;
        activeActionSettings = settings;
        CurrentLogicalState = action == CharacterAnimationAction.Death
            ? CharacterAnimationLogicalState.Dead
            : CharacterAnimationLogicalState.Action;
        IsInterruptible = settings.Interruptible && settings.InterruptibleNormalizedTime <= 0f;
        RootMotionActive = settings.RootMotion != CharacterAnimationRootMotionStrategy.Disabled;
        animator.applyRootMotion = RootMotionActive;

        ClipTransition transition = CreateClipTransition(settings.Clip, settings.Loop ? null : OnActionEnd);
        currentState = baseLayer.Play(transition, Mathf.Max(settings.FadeIn, activeFallbackFadeOut));
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

        EndTransient("Parkour started", false);
        CleanupActionState($"Parkour {request.Type} started");
        CurrentLogicalState = CharacterAnimationLogicalState.Parkour;
        RootMotionActive = request.AllowRootMotion && settings.RootMotion != CharacterAnimationRootMotionStrategy.Disabled;
        animator.applyRootMotion = RootMotionActive;

        currentState = baseLayer.Play(CreateClipTransition(settings.Clip, OnActionEnd), Mathf.Max(settings.EnterFade, activeFallbackFadeOut));
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
        if (CurrentLogicalState == CharacterAnimationLogicalState.Landing && CurrentClip == clip) return;

        jumpActive = false;
        CleanupActionState("Landing");
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

        float fade = isHolding ? config.Holding.FadeIn : config.Holding.FadeOut;
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
            upperBodyLayer.Play(CreateClipTransition(clip, null), fade);
        upperBodyLayer.StartFade(Mathf.Clamp01(targetWeight), fade);
    }

    public void SetUpperBodyAction(CharacterAnimationAction action, float targetWeight)
    {
        Initialize();
        CharacterAnimationConfig.ActionSettings settings = config != null ? config.FindAction(action) : null;
        if (settings == null || settings.Clip == null)
        {
            upperBodyLayer.StartFade(Mathf.Clamp01(targetWeight), config != null ? config.Layers.UpperBodyFadeDuration : 0.12f);
            return;
        }

        upperBodyLayer.Play(CreateClipTransition(settings.Clip, null), settings.FadeIn);
        upperBodyLayer.StartFade(Mathf.Clamp01(targetWeight), settings.FadeIn);
    }

    public void SetAimWeight(float weight)
    {
        Initialize();
        additiveLayer.StartFade(Mathf.Clamp01(weight), config != null ? config.Layers.AdditiveFadeDuration : 0.12f);
    }

    public void SetLookDirection(Vector3 direction)
    {
        // Public extension point for a future additive MOBILITY PRO look mixer.
    }

    public void ReturnToLocomotion(string reason = "Return to locomotion")
    {
        if (!initialized) return;
        EndTransient(reason, false);
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
        currentState = baseLayer.Play(CreateClipTransition(config.Locomotion.Idle, null), config.Locomotion.IdleVariationCrossFade);
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

        AddMixerClip(animations, thresholds, synchronize, speeds, config.Locomotion.Idle, Vector2.zero, false);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, config.Locomotion.Walk, config.Locomotion.WalkSpeed);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, config.Locomotion.Jog, config.Locomotion.JogSpeed);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, config.Locomotion.Run, config.Locomotion.RunSpeed);
        ApplyMixerData(mixer, animations, thresholds, synchronize, speeds);
        return animations.Count;
    }

    private int ConfigureCrouchMixer(MixerTransition2D mixer)
    {
        List<Object> animations = new List<Object>(9);
        List<Vector2> thresholds = new List<Vector2>(9);
        List<bool> synchronize = new List<bool>(9);
        List<float> speeds = new List<float>(9);

        AddMixerClip(animations, thresholds, synchronize, speeds, config.Locomotion.CrouchIdle, Vector2.zero, false);
        AddDirectionalSet(animations, thresholds, synchronize, speeds, config.Locomotion.Crouch, config.Locomotion.CrouchSpeed);
        ApplyMixerData(mixer, animations, thresholds, synchronize, speeds);
        return animations.Count;
    }

    private void AddDirectionalSet(
        List<Object> animations,
        List<Vector2> thresholds,
        List<bool> synchronize,
        List<float> speeds,
        CharacterAnimationConfig.DirectionalClipSet set,
        float speed)
    {
        if (set == null) return;
        bool sync = config.Locomotion.SynchronizeLocomotionCycles;
        AddMixerClip(animations, thresholds, synchronize, speeds, set.Forward, new Vector2(0f, speed), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.Backward, new Vector2(0f, -speed), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.Left, new Vector2(-speed, 0f), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.Right, new Vector2(speed, 0f), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.ForwardLeft, new Vector2(-Diagonal * speed, Diagonal * speed), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.ForwardRight, new Vector2(Diagonal * speed, Diagonal * speed), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.BackwardLeft, new Vector2(-Diagonal * speed, -Diagonal * speed), sync);
        AddMixerClip(animations, thresholds, synchronize, speeds, set.BackwardRight, new Vector2(Diagonal * speed, -Diagonal * speed), sync);
    }

    private void AddMixerClip(
        List<Object> animations,
        List<Vector2> thresholds,
        List<bool> synchronize,
        List<float> speeds,
        AnimationClip clip,
        Vector2 threshold,
        bool synchronizeCycle)
    {
        if (clip == null) return;
        animations.Add(clip);
        thresholds.Add(threshold);
        synchronize.Add(synchronizeCycle);
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
            currentState = baseLayer.Play(targetMixer, ResolveFadeDuration(null, config.Locomotion.LocomotionCrossFade));
            activeClipTuning = null;
            activeFallbackFadeOut = config.Locomotion.LocomotionFadeOut;
            usingCrouchMixer = Crouching;
            usingLocomotionMixer = !Crouching;
            usingIdleVariation = false;
            CurrentClip = null;
            CurrentAnimationState = Crouching ? "MOBILITY PRO Crouch Mixer" : "MOBILITY PRO Locomotion Mixer";
            LastStateChangeReason = Crouching ? "Crouch locomotion" : "Locomotion";
        }

        Vector2 parameter = new Vector2(LocalVelocityX, LocalVelocityZ);
        targetMixer.State.Parameter = parameter;
        targetMixer.State.Speed = Mathf.Clamp(
            config.Locomotion.GlobalPlaybackSpeed,
            config.Locomotion.MinimumPlaybackSpeed,
            config.Locomotion.MaximumPlaybackSpeed);
        CurrentMixerParameter = parameter;
        CurrentGait = config.ResolveGait(CurrentLocomotionSpeed, Crouching);
        CurrentLogicalState = CharacterAnimationLogicalState.Locomotion;
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
        LocomotionGait gait = config.ResolveGait(targetVelocity.magnitude, Crouching);
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
        LocomotionGait gait = config.ResolveGait(targetVelocity.magnitude, false);
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

            if (turns.EnableMovementCurves &&
                Mathf.Abs(CurrentYawRate) >= turns.CurveStartYawRate)
            {
                TryPlayMovementCurve(CurrentYawRate);
            }
            else if (CurrentTransient == LocomotionTransient.MovementCurve &&
                     yawIdleTimer >= turns.CurveExitDelay)
            {
                EndTransient("Movement curve complete", true);
            }
        }
        else if (turns.EnableTurnInPlace)
        {
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
        usingLocomotionMixer = false;
        usingCrouchMixer = false;
        usingIdleVariation = false;
        currentState = PlayConfiguredClip(clip, fadeDuration, null);
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
        usingLocomotionMixer = false;
        usingCrouchMixer = false;
        usingIdleVariation = false;
        currentState = PlayConfiguredClip(clip, fadeDuration, () => OnTransientEnd(serial));
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
        if (resumeLocomotion && Grounded && !Airborne) PlayLocomotion();
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
        if (clip == null || (CurrentLogicalState == CharacterAnimationLogicalState.Airborne && CurrentClip == clip)) return;

        EndTransient("Enter airborne loop", false);
        currentState = PlayConfiguredClip(clip, config.Airborne.AirFadeDuration, null);
        activeFallbackFadeOut = jumpActive ? config.Airborne.AirFadeOut : config.Airborne.FallFadeOut;
        CurrentClip = clip;
        CurrentAnimationState = clip.name;
        CurrentLogicalState = CharacterAnimationLogicalState.Airborne;
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
            currentState = PlayConfiguredClip(clip, config.Locomotion.IdleVariationCrossFade, null);
            activeFallbackFadeOut = config.Locomotion.IdleVariationCrossFade;
            CurrentClip = clip;
            CurrentAnimationState = clip.name;
            usingLocomotionMixer = false;
            usingIdleVariation = true;
            LastStateChangeReason = "Idle variation";
            break;
        }
    }

    private float SelectVelocitySmoothTime(Vector3 targetLocal)
    {
        float targetSpeed = new Vector2(targetLocal.x, targetLocal.z).magnitude;
        float currentSpeed = new Vector2(smoothedLocalVelocity.x, smoothedLocalVelocity.z).magnitude;
        if (targetSpeed < currentSpeed) return config.Locomotion.DecelerationSmoothTime;

        Vector2 currentDirection = new Vector2(smoothedLocalVelocity.x, smoothedLocalVelocity.z);
        Vector2 targetDirection = new Vector2(targetLocal.x, targetLocal.z);
        if (currentDirection.sqrMagnitude > 0.001f && targetDirection.sqrMagnitude > 0.001f &&
            Vector2.Angle(currentDirection, targetDirection) > 25f)
            return config.Locomotion.DirectionSmoothTime;
        return config.Locomotion.AccelerationSmoothTime;
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

    private AnimancerState PlayConfiguredClip(AnimationClip clip, float defaultFadeIn, Action onEnd)
    {
        CharacterAnimationConfig.MobilityClipTuning tuning = GetClipTuning(clip);
        float fadeDuration = ResolveFadeDuration(tuning, defaultFadeIn);
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
        return state;
    }

    private float ResolveFadeDuration(CharacterAnimationConfig.MobilityClipTuning incoming, float fallback)
    {
        float fadeIn = incoming != null && incoming.OverrideRuntime && incoming.Transition != null
            ? incoming.Transition.FadeDuration
            : fallback;
        float fadeOut = activeClipTuning != null && activeClipTuning.OverrideRuntime
            ? activeClipTuning.FadeOut
            : activeFallbackFadeOut;
        return Mathf.Max(0f, fadeIn, fadeOut);
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
        if (activeActionSettings == null) return true;
        if (CurrentLogicalState == CharacterAnimationLogicalState.Dead) return false;
        if ((int)priority > (int)activeActionSettings.Priority) return true;
        return IsInterruptible && (int)priority >= (int)activeActionSettings.Priority;
    }

    private void UpdateActionCompletion()
    {
        if (currentState == null || activeActionSettings == null) return;
        CurrentNormalizedTime = currentState.NormalizedTime;
        if (!IsInterruptible && activeActionSettings.Interruptible &&
            CurrentNormalizedTime >= activeActionSettings.InterruptibleNormalizedTime)
            IsInterruptible = true;
    }

    private void OnActionEnd()
    {
        LastAnimationEvent = "ActionComplete";
        ReturnToLocomotion("Action complete");
    }

    private void CleanupActionState(string reason)
    {
        activeActionSettings = null;
        RootMotionActive = false;
        IsInterruptible = true;
        if (animator != null) animator.applyRootMotion = false;
        LastStateChangeReason = reason;
    }

    private void RefreshRuntimeDebug()
    {
        if (currentState == null) return;
        CurrentNormalizedTime = currentState.NormalizedTime;
        AnimationClip clip = currentState.Clip;
        if (clip != null) CurrentClip = clip;
    }

    private float GetLayerWeight(int layer)
    {
        if (animancer == null || !animancer.IsGraphInitialized) return 0f;
        return animancer.Layers[layer].Weight;
    }
}
