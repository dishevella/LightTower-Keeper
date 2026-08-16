using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Serializable]
    public class AnimationCameraPose
    {
        public string key;
        public Vector3 localPosition;
        public float blendSpeed = 8f;
    }

    [Header("Reference")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private CharacterAnimancerController animancerController;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundStickForce = -2f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float standingCameraY = 0.75f;
    [SerializeField] private float crouchCameraY = 0.4f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("FOV")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float runFOV = 68f;
    [SerializeField] private float fovSmoothSpeed = 8f;

    [Header("Animation Camera")]
    [SerializeField] private AnimationCameraPose[] animationCameraPoses;
    [SerializeField] private float defaultCameraPoseBlendSpeed = 8f;

    [Header("Input Keys")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Control Flags")]
    public bool CanMove = true;
    public bool CanLook = true;
    public bool CanRun = true;
    public bool CanCrouch = true;
    public bool CanJump = true;

    [Header("Animation Driving")]
    [SerializeField] private bool driveLocomotionAnimation = true;
    [SerializeField] private bool preferAnimancerAnimation = true;
    [SerializeField] private float animationCrossFade = 0.08f;
    [SerializeField] private float landStateHoldTime = 0.12f;
    [SerializeField] private float jumpStartHoldTime = 0.12f;

    [Header("Landing Detection")]
    [SerializeField] private float minAirTimeForLand = 0.12f;
    [SerializeField] private float minFallSpeedForLand = -4f;
    [Header("Animation Grounding Grace")]
    [SerializeField] private float fallAnimationMinAirTime = 0.18f;
    [SerializeField] private float fallAnimationMinSpeed = -3.5f;

    [Header("Animation State Names")]
    [SerializeField] private string idleStandingState = "A_Idle_Standing_Masc";
    [SerializeField] private string idleCrouchingState = "A_Idle_Crouching_Masc";
    [SerializeField] private string crouchWalkState = "A_Crouch_FwdStrafeF_Masc";

    [SerializeField] private string walkForwardState = "A_Walk_F_Masc";
    
    [SerializeField] private string runForwardState = "A_Run_F_Masc";
    
    [SerializeField] private string jumpIdleState = "A_Jump_Idle_Masc";
    [SerializeField] private string jumpWalkingState = "A_Jump_Walking_Masc";
    [SerializeField] private string jumpRunningState = "A_Jump_Running_Masc";

    [SerializeField] private string inAirFallShortState = "A_InAir_FallShort_Masc";

    [SerializeField] private string landIdleSoftState = "A_Land_IdleSoft_Masc";
    [SerializeField] private string landWalkingState = "A_Land_Walking_Masc";
    [SerializeField] private string landRunningState = "A_Land_Running_Masc";

    private CharacterController controller;
    private readonly PlayerInputAdapter inputAdapter = new();
    private readonly CameraCollisionSolver cameraCollisionSolver = new();

    private float pitch;
    private float verticalVelocity;


    private float lastAirborneVerticalVelocity;
    private float landingImpactVelocity;
    private float timeSinceGrounded;
    private bool animationGrounded;
    private float pendingGroundSnapDistance;
    private readonly RaycastHit[] groundProbeHits = new RaycastHit[16];

    private float jumpStartTimer;
    private bool jumpStartedThisFrame;

    public bool isGrounded { get; private set; }
    public bool isRunning { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isJumping { get; private set; }

    public Transform CameraRootTransform => cameraRoot;

    private bool UseAnimancerAnimation =>
        preferAnimancerAnimation &&
        animancerController != null &&
        animancerController.Config != null;

    private float currentSpeed;
    private Vector3 moveDirection;

    private float rawInputX;
    private float rawInputZ;
    private bool hasMoveInput;

    private bool wasGroundedLastFrame;
    private bool lastGroundHadMoveInput;
    private bool lastGroundWasRunning;
    private float landStateTimer;
    private bool landingAnimationRequested;

    private string currentAnimationState;
    private string cameraPoseOverrideKey;
    private Vector3 defaultCameraRootLocalPosition;
    private Vector3 targetCameraRootLocalPosition;
    private float currentCameraPoseBlendSpeed;
    private Vector3 defaultCameraPositionInRoot;
    private float currentCameraCollisionDistance = -1f;
    private readonly RaycastHit[] cameraCollisionHits = new RaycastHit[32];
    private readonly Collider[] cameraOverlapHits = new Collider[32];
    private HashSet<Collider> cameraIgnoredColliders;
    private Transform animatedHeadBone;
    private Transform animatedEyesAnchor;
    private Transform animatedEyebrowsAnchor;
    private Vector3 faceForwardInHeadLocal = Vector3.forward;
    private Transform animatedNeckBone;
    private Transform animatedChestBone;
    private Transform animatedLeftShoulderBone;
    private Transform animatedRightShoulderBone;
    private PlayerCameraHeadHider cameraHeadHider;
    private PlayerCameraProximityClip cameraProximityClip;
    private GameObject cameraCollisionProbeObject;
    private BoxCollider cameraCollisionProbe;
    private Vector3 lastEnvironmentValidCameraPosition;
    private bool hasEnvironmentValidCameraPosition;
    private int lastPreRenderValidationFrame = -1;
    private float nextCameraSafetyWarningTime;
    private Quaternion desiredCameraRotation = Quaternion.identity;

    [Title("Camera Protection Debug", "Runtime read-only constraint state")]
    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public bool CameraCollisionActive { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public bool CameraHeadProtectionActive { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraForwardFromHead { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraDistanceFromHead { get; private set; }
    public float CurrentLocomotionMovementScale { get; private set; } = 1f;
    public bool AnimationGrounded => animationGrounded;
    public bool GroundSnapActive { get; private set; }
    public float CurrentGroundDistance { get; private set; } = float.PositiveInfinity;
    public float LastUngroundedDuration { get; private set; }
    public bool HeadBoneSamplingActive => animatedHeadBone != null;
    public Transform ProtectedHeadBone => animatedHeadBone;
    public bool FaceSurfaceSamplingActive =>
        animatedHeadBone != null &&
        (animatedEyesAnchor != null || animatedEyebrowsAnchor != null);
    public bool HeadRenderSuppressionEnabled => cameraHeadHider != null && cameraHeadHider.enabled;

    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Vector3 DesiredCameraPosition { get; private set; }
    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Vector3 EnvironmentSafeCameraPosition { get; private set; }
    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Vector3 CharacterCorrectedCameraPosition { get; private set; }
    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Vector3 FinalCameraPosition { get; private set; }
    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Quaternion FinalCameraRotation { get; private set; } = Quaternion.identity;
    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Vector3 FinalCameraCorrection { get; private set; }
    [BoxGroup("Camera Debug - Camera"), ShowInInspector, ReadOnly]
    public Vector3 CameraCollisionAnchorPosition { get; private set; }

    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public Collider CurrentCameraBlockingCollider { get; private set; }
    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public float CameraCollisionHitDistance { get; private set; } = float.PositiveInfinity;
    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public float CameraCollisionSkinWidth { get; private set; }
    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public Vector3 NearPlaneCollisionHalfExtents { get; private set; }
    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public bool NearPlaneOverlapActive { get; private set; }
    [BoxGroup("Camera Debug - Environment"), ShowInInspector, ReadOnly]
    public bool FinalEnvironmentValid { get; private set; } = true;

    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public string ClosestCameraBone { get; private set; } = string.Empty;
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraDistanceFromNeck { get; private set; } = float.PositiveInfinity;
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraDistanceFromChest { get; private set; } = float.PositiveInfinity;
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraDistanceFromLeftShoulder { get; private set; } = float.PositiveInfinity;
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraDistanceFromRightShoulder { get; private set; } = float.PositiveInfinity;
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCharacterSafetyRadius { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public bool CharacterInteriorConstraintActive { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public Vector3 CurrentFaceSurfacePosition { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public Vector3 CurrentFaceSurfaceForward { get; private set; }
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float CurrentCameraFaceClearance { get; private set; } = float.PositiveInfinity;
    [BoxGroup("Camera Debug - Character"), ShowInInspector, ReadOnly]
    public float RequiredCameraFaceClearance { get; private set; }

    [BoxGroup("Camera Debug - Visibility"), ShowInInspector, ReadOnly]
    public bool CharacterVisibilityFallbackActive { get; private set; }
    [BoxGroup("Camera Debug - Visibility"), ShowInInspector, ReadOnly]
    public bool HeadHiddenForPlayerCamera => cameraHeadHider != null && cameraHeadHider.WillHideForPlayerCamera;
    [BoxGroup("Camera Debug - Visibility"), ShowInInspector, ReadOnly]
    public bool HairHiddenForPlayerCamera => false;
    [BoxGroup("Camera Debug - Visibility"), ShowInInspector, ReadOnly]
    public bool ProximityClipAvailable => cameraProximityClip != null && cameraProximityClip.Available;
    [BoxGroup("Camera Debug - Visibility"), ShowInInspector, ReadOnly]
    public bool ProximityClipActive => cameraProximityClip != null && cameraProximityClip.WillClipForPlayerCamera;
    [BoxGroup("Camera Debug - Visibility"), ShowInInspector, ReadOnly]
    public bool BoneScaleFallbackActive => cameraHeadHider != null && cameraHeadHider.BoneScaleFallbackActive;

    [BoxGroup("Camera Debug - Animation"), ShowInInspector, ReadOnly]
    public string CameraAnimationState => animancerController != null
        ? animancerController.CurrentAnimationState
        : currentAnimationState;
    [BoxGroup("Camera Debug - Animation"), ShowInInspector, ReadOnly]
    public float CameraAnimationNormalizedTime => animancerController != null
        ? animancerController.CurrentNormalizedTime
        : 0f;
    [BoxGroup("Camera Debug - Animation"), ShowInInspector, ReadOnly]
    public bool CameraTraversalActive => animancerController != null &&
                                         animancerController.CurrentLogicalState == CharacterAnimationLogicalState.Parkour;
    [BoxGroup("Camera Debug - Animation"), ShowInInspector, ReadOnly]
    public bool CameraCrouching => isCrouching;

    [BoxGroup("Camera Debug - Timing"), ShowInInspector, ReadOnly]
    public string AnimationEvaluationPhase => "Animator/Animancer evaluation before LateUpdate";
    [BoxGroup("Camera Debug - Timing"), ShowInInspector, ReadOnly]
    public string CameraSolverPhase => "LateUpdate + pre-render validation";
    [BoxGroup("Camera Debug - Timing"), ShowInInspector, ReadOnly]
    public string LastCameraWriter { get; private set; } = "Not evaluated";

    private CharacterAnimationConfig.CameraSettings CameraSettings =>
        animancerController != null && animancerController.Config != null
            ? animancerController.Config.Camera
            : null;

    private CharacterAnimationConfig.AirborneSettings AirborneConfig =>
        animancerController != null && animancerController.Config != null
            ? animancerController.Config.Airborne
            : null;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight / 2f, 0f);

        if (animancerController == null)
            animancerController = GetComponent<CharacterAnimancerController>();
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
        animatedHeadBone = ResolveHeadBone();
        animatedEyesAnchor = ResolveNamedTransform("eyes", "eye_anchor", "eyeanchor");
        animatedEyebrowsAnchor = ResolveNamedTransform("eyebrows", "brows", "brow_anchor");
        if (animatedHeadBone != null)
            faceForwardInHeadLocal = animatedHeadBone.InverseTransformDirection(transform.forward).normalized;
        animatedNeckBone = ResolveHumanoidBone(HumanBodyBones.Neck, "neck_01", "neck");
        animatedChestBone = ResolveHumanoidBone(
            HumanBodyBones.UpperChest,
            "spine_03",
            "upperchest",
            "upper_chest");
        if (animatedChestBone == null)
            animatedChestBone = ResolveHumanoidBone(HumanBodyBones.Chest, "spine_02", "chest");
        animatedLeftShoulderBone = ResolveHumanoidBone(
            HumanBodyBones.LeftShoulder,
            "clavicle_l",
            "leftshoulder");
        animatedRightShoulderBone = ResolveHumanoidBone(
            HumanBodyBones.RightShoulder,
            "clavicle_r",
            "rightshoulder");

        CharacterAnimationConfig.CameraSettings cameraSettings = CameraSettings;

        if (cameraRoot != null)
        {
            Vector3 camLocalPos = cameraRoot.localPosition;
            camLocalPos.y = standingCameraY;
            if (cameraSettings != null && cameraSettings.EnableCharacterInteriorProtection)
                camLocalPos.z = Mathf.Max(camLocalPos.z, cameraSettings.MinimumCameraLocalForward);
            cameraRoot.localPosition = camLocalPos;
            defaultCameraRootLocalPosition = cameraRoot.localPosition;
            targetCameraRootLocalPosition = defaultCameraRootLocalPosition;
            currentCameraPoseBlendSpeed = defaultCameraPoseBlendSpeed;
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = normalFOV;
            if (cameraRoot != null)
                defaultCameraPositionInRoot = cameraRoot.InverseTransformPoint(playerCamera.transform.position);

            if (cameraSettings != null)
                playerCamera.nearClipPlane = cameraSettings.CameraNearClip;

            ConfigureCameraHeadHider(cameraSettings);
            ConfigureCameraProximityClip(cameraSettings);
        }

        cameraIgnoredColliders = new HashSet<Collider>(GetComponentsInChildren<Collider>(true));
        InitializeCameraCollisionProbe();

       
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        jumpStartedThisFrame = false;
        UpdateGroundedState();
        HandleLook();
        HandleCrouch();
        HandleJump();
        HandleMovement();
        ApplyFOV();
        UpdateAnimationState();
        ApplyCharacterMovement();
        ApplyGravity();
        ApplyAnimationCameraPose();

        wasGroundedLastFrame = isGrounded;

        if (isGrounded)
        {
            lastGroundHadMoveInput = hasMoveInput;
            lastGroundWasRunning = isRunning;
        }
    }

    private void LateUpdate()
    {
        SolveCameraConstraints(true, "PlayerController.LateUpdate");
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        cameraProximityClip?.SetVisibilityFallback(false);
        cameraHeadHider?.SetVisibilityFallback(false);
        if (!Application.isPlaying) return;
        CameraCollisionActive = false;
        GroundSnapActive = false;
        pendingGroundSnapDistance = 0f;
        currentCameraCollisionDistance = -1f;
        if (cameraRoot != null && playerCamera != null)
            playerCamera.transform.position = cameraRoot.TransformPoint(defaultCameraPositionInRoot);
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        if (cameraCollisionProbeObject == null) return;

        if (Application.isPlaying)
            Destroy(cameraCollisionProbeObject);
        else
            DestroyImmediate(cameraCollisionProbeObject);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != playerCamera || lastPreRenderValidationFrame == Time.frameCount) return;

        lastPreRenderValidationFrame = Time.frameCount;
        SolveCameraConstraints(false, "URP beginCameraRendering");
        cameraProximityClip?.PrepareForCameraRender(camera);
        cameraHeadHider?.PrepareForCameraRender(camera);
    }

    private void UpdateGroundedState()
    {
        bool wasGrounded = isGrounded;
        float airborneDurationBeforeContact = timeSinceGrounded;
        bool controllerGrounded = controller.isGrounded;
        CharacterAnimationConfig.AirborneSettings settings = AirborneConfig;

        GroundSnapActive = false;
        CurrentGroundDistance = float.PositiveInfinity;
        pendingGroundSnapDistance = 0f;

        bool canProbeGround =
            settings != null &&
            settings.EnableGroundSnap &&
            !controllerGrounded &&
            verticalVelocity <= 0f &&
            (!isJumping || timeSinceGrounded >= settings.JumpGroundSnapDelay);
        float groundDistance = float.PositiveInfinity;
        bool hasWalkableGround =
            canProbeGround &&
            TryGetWalkableGround(settings, out groundDistance);

        if (controllerGrounded)
        {
            CurrentGroundDistance = 0f;
        }
        else if (hasWalkableGround)
        {
            CurrentGroundDistance = groundDistance;
            pendingGroundSnapDistance = groundDistance;
            GroundSnapActive = true;
        }

        isGrounded = controllerGrounded || hasWalkableGround;

        if (isGrounded)
        {
            if (!wasGrounded)
            {
                LastUngroundedDuration = airborneDurationBeforeContact;
                landingImpactVelocity = lastAirborneVerticalVelocity;
                float minimumAirTime = settings != null
                    ? settings.LandingMinimumAirTime
                    : minAirTimeForLand;
                float minimumFallSpeed = settings != null
                    ? settings.LandingMinimumFallSpeed
                    : minFallSpeedForLand;
                bool shouldPlayLand =
                    airborneDurationBeforeContact >= minimumAirTime ||
                    lastAirborneVerticalVelocity <= minimumFallSpeed;

                if (shouldPlayLand)
                {
                    landStateTimer = landStateHoldTime;
                    landingAnimationRequested = false;
                }

                lastAirborneVerticalVelocity = 0f;
            }

            timeSinceGrounded = 0f;
        }
        else
        {
            timeSinceGrounded += Time.deltaTime;
            lastAirborneVerticalVelocity = verticalVelocity;
        }

        float airborneGraceTime = settings != null
            ? settings.GroundedGraceTime
            : fallAnimationMinAirTime;
        float fallSpeedThreshold = settings != null
            ? settings.FallAnimationMinSpeed
            : fallAnimationMinSpeed;
        bool allowAirAnimation =
            !isGrounded &&
            timeSinceGrounded >= airborneGraceTime &&
            verticalVelocity <= fallSpeedThreshold;

        animationGrounded = isGrounded || !allowAirAnimation;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundStickForce;
    }

    private bool TryGetWalkableGround(
        CharacterAnimationConfig.AirborneSettings settings,
        out float groundDistance)
    {
        groundDistance = float.PositiveInfinity;

        Vector3 up = transform.up;
        float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius);
        Vector3 worldCenter = transform.TransformPoint(controller.center);
        Vector3 bottomSphereCenter =
            worldCenter - up * (halfHeight - controller.radius);
        float probeRadius = Mathf.Max(
            0.05f,
            controller.radius * Mathf.Clamp(settings.GroundProbeRadiusScale, 0.5f, 0.98f));
        float lift = Mathf.Max(0.01f, controller.skinWidth) + 0.02f;
        float radiusInset = Mathf.Max(0f, controller.radius - probeRadius);
        float baselineTravel = lift + radiusInset;
        Vector3 origin = bottomSphereCenter + up * lift;
        float castDistance = baselineTravel + Mathf.Max(0f, settings.GroundSnapDistance);

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            probeRadius,
            -up,
            groundProbeHits,
            castDistance,
            settings.GroundCollisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundProbeHits[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null ||
                (cameraIgnoredColliders != null && cameraIgnoredColliders.Contains(hitCollider)))
            {
                continue;
            }

            if (Vector3.Angle(hit.normal, up) > controller.slopeLimit + 0.5f)
                continue;

            float gap = Mathf.Max(0f, hit.distance - baselineTravel);
            if (gap <= settings.GroundSnapDistance && gap < groundDistance)
                groundDistance = gap;
        }

        return !float.IsPositiveInfinity(groundDistance);
    }


    private void HandleLook()
    {
        if (!CanLook) return;

        CharacterAnimationConfig.CameraSettings settings = CameraSettings;
        float lookSensitivity = settings != null
            ? Mathf.Max(1f, settings.LookSensitivity)
            : mouseSensitivity;
        Vector2 lookInput = inputAdapter.ReadLookAxes();
        float mouseX = lookInput.x * lookSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * lookSensitivity * Time.deltaTime;
       
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        if (!CanMove)
        {
            rawInputX = 0f;
            rawInputZ = 0f;
            hasMoveInput = false;
            moveDirection = Vector3.zero;
            currentSpeed = 0f;
            isRunning = false;
            return;
        }

        Vector2 movementInput = inputAdapter.ReadMovementAxes();
        rawInputX = movementInput.x;
        rawInputZ = movementInput.y;

        Vector3 inputDirection = (transform.right * rawInputX + transform.forward * rawInputZ).normalized;
        hasMoveInput = inputDirection.sqrMagnitude > 0.01f;

        bool wantsToRun = inputAdapter.IsHeld(runKey);

        
        bool forwardRunAllowed = rawInputZ > 0.1f;
        isRunning = hasMoveInput && wantsToRun && CanRun && !isCrouching && forwardRunAllowed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
        }

        moveDirection = inputDirection * currentSpeed;
    }

    private void ApplyCharacterMovement()
    {
        if (UseAnimancerAnimation && !animancerController.ShouldMotorDriveTranslation)
        {
            CurrentLocomotionMovementScale = 0f;
            return;
        }

        CurrentLocomotionMovementScale = UseAnimancerAnimation
            ? animancerController.GetLocomotionMotorScale()
            : 1f;
        Vector3 finalMove = moveDirection;
        finalMove *= CurrentLocomotionMovementScale;
        finalMove.y = verticalVelocity;

        CharacterAnimationConfig.AirborneSettings settings = AirborneConfig;
        if (GroundSnapActive &&
            settings != null &&
            pendingGroundSnapDistance > 0.001f &&
            verticalVelocity <= 0f)
        {
            float requiredSnapSpeed =
                pendingGroundSnapDistance / Mathf.Max(Time.deltaTime, 0.001f);
            finalMove.y = -Mathf.Max(
                Mathf.Abs(groundStickForce),
                Mathf.Min(settings.GroundSnapSpeed, requiredSnapSpeed));
        }

        controller.Move(finalMove * Time.deltaTime);
    }
   
    private void ApplyGravity()
    {
        if (UseAnimancerAnimation && !animancerController.ShouldMotorDriveTranslation)
        {
            verticalVelocity = 0f;
            return;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleCrouch()
    {
        if (!CanCrouch) return;

        if (inputAdapter.WasPressedThisFrame(crouchKey))
        {
            SetCrouching(!isCrouching);
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.height = newHeight;
        controller.center = new Vector3(0f, controller.height / 2f, 0f);

    }

    private void HandleJump()
    {
        if (!CanJump) return;

        if (isGrounded)
        {
            isJumping = false;
        }

        if (isGrounded && inputAdapter.WasPressedThisFrame(jumpKey))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
            jumpStartedThisFrame = true;
            landStateTimer = 0f;
            landingAnimationRequested = false;
            jumpStartTimer = jumpStartHoldTime;
        }
    }
   
    private void ApplyFOV()
    {
        if (playerCamera == null) return;

        float targetFOV = isRunning ? runFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovSmoothSpeed * Time.deltaTime);
    }

    private void UpdateAnimationState()
    {

        if (!driveLocomotionAnimation) return;

        if (UseAnimancerAnimation)
        {
            animancerController.SetVerticalSpeed(verticalVelocity);
            animancerController.SetGrounded(animationGrounded && !isJumping);
            animancerController.SetCrouching(isCrouching);

            if (jumpStartedThisFrame)
                animancerController.PlayJump(moveDirection);

            Vector3 measuredVelocity = controller != null ? controller.velocity : moveDirection;
            measuredVelocity.y = 0f;
            if (landStateTimer > 0f)
            {
                if (!landingAnimationRequested)
                {
                    animancerController.PlayLanding(landingImpactVelocity, lastGroundHadMoveInput);
                    landingAnimationRequested = true;
                }
                animancerController.SetMovement(moveDirection, transform.forward, measuredVelocity);
                landStateTimer -= Time.deltaTime;
                RefreshAnimancerCameraPoseState();
                return;
            }

            landingAnimationRequested = false;
            animancerController.SetMovement(moveDirection, transform.forward, measuredVelocity);
            if (jumpStartTimer > 0f)
                jumpStartTimer -= Time.deltaTime;
            RefreshAnimancerCameraPoseState();
            return;
        }

        if (playerAnimator == null) return;

        string targetState = GetTargetAnimationState();

        if (string.IsNullOrEmpty(targetState)) return;
        if (currentAnimationState == targetState) return;

        playerAnimator.CrossFadeInFixedTime(targetState, animationCrossFade);
        currentAnimationState = targetState;
        RefreshAnimationCameraPose();
    }

    private string GetTargetAnimationState()
    {
        if (!animationGrounded)
        {
            if (jumpStartTimer > 0f)
            {
                jumpStartTimer -= Time.deltaTime;
                return jumpIdleState;
            }

            if (verticalVelocity > 0.1f)
            {
                if (isRunning) return jumpRunningState;
                if (hasMoveInput) return jumpWalkingState;
                return jumpIdleState;
            }

            return inAirFallShortState;
        }
        if (landStateTimer > 0f)
        {
            landStateTimer -= Time.deltaTime;

            if (lastGroundWasRunning) return landRunningState;
            if (lastGroundHadMoveInput) return landWalkingState;
            return landIdleSoftState;
        }

        if (!animationGrounded)
        {
            if (verticalVelocity > 0.1f)
            {
                if (isRunning) return jumpRunningState;
                if (hasMoveInput) return jumpWalkingState;
                return jumpIdleState;
            }

            return inAirFallShortState;
        }

        if (isCrouching)
        {
            if (hasMoveInput) return crouchWalkState;
            return idleCrouchingState;
        }

        if (!hasMoveInput)
        {
            return idleStandingState;
        }

        if (isRunning)
        {
            return runForwardState;
        }

        return walkForwardState;
    }

    public void SetCanMove(bool value)
    {
        CanMove = value;
    }

    public void SetCanLook(bool value)
    {
        CanLook = value;
    }

    public void SetCanRun(bool value)
    {
        CanRun = value;
    }

    public void SetCanCrouch(bool value)
    {
        CanCrouch = value;
    }

    public void SetCanJump(bool value)
    {
        CanJump = value;
    }

    public void ForceStandUp()
    {
        SetCrouching(false);
    }

    public void SetCrouching(bool value)
    {
        isCrouching = value;
        if (value) isRunning = false;

        if (UseAnimancerAnimation)
            animancerController.SetCrouching(value);
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void SetAnimationDriving(bool value)
    {
        driveLocomotionAnimation = value;

        if (!value)
        {
            if (UseAnimancerAnimation)
            {
                animancerController.SetMovement(Vector3.zero, transform.forward);
                animancerController.ReturnToLocomotion("PlayerController animation driving disabled");
            }

            currentAnimationState = string.Empty;
            RefreshAnimationCameraPose();
        }
    }

    public void PlayExternalAnimationState(string stateName, float crossFade = 0.08f)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(stateName)) return;
        if (UseAnimancerAnimation)
        {
            Debug.LogWarning(
                $"Ignored Animator Controller state '{stateName}' because this player is Animancer-first. " +
                "Route the request through CharacterAnimancerController instead.",
                this);
            return;
        }
        if (currentAnimationState == stateName) return;

        playerAnimator.CrossFadeInFixedTime(stateName, crossFade);
        currentAnimationState = stateName;
        RefreshAnimationCameraPose();
    }
    public void ClearMotionForCutscene()
    {
        rawInputX = 0f;
        rawInputZ = 0f;
        hasMoveInput = false;

        moveDirection = Vector3.zero;
        currentSpeed = 0f;

        isRunning = false;
        isJumping = false;

        verticalVelocity = controller != null && controller.isGrounded
            ? groundStickForce
            : 0f;

        if (UseAnimancerAnimation)
        {
            animancerController.SetVerticalSpeed(verticalVelocity);
            animancerController.SetGrounded(controller != null && controller.isGrounded);
            animancerController.SetMovement(Vector3.zero, transform.forward);
        }
    }

    private void RefreshAnimancerCameraPoseState()
    {
        string poseState = GetAnimancerCameraPoseState();
        if (currentAnimationState == poseState) return;

        currentAnimationState = poseState;
        RefreshAnimationCameraPose();
    }

    private string GetAnimancerCameraPoseState()
    {
        if (!animationGrounded || isJumping)
        {
            if (jumpStartTimer > 0f || verticalVelocity > 0.1f)
            {
                if (isRunning) return jumpRunningState;
                if (hasMoveInput) return jumpWalkingState;
                return jumpIdleState;
            }

            return inAirFallShortState;
        }

        if (landStateTimer > 0f)
        {
            if (lastGroundWasRunning) return landRunningState;
            if (lastGroundHadMoveInput) return landWalkingState;
            return landIdleSoftState;
        }

        if (isCrouching)
            return hasMoveInput ? crouchWalkState : idleCrouchingState;

        if (!hasMoveInput) return idleStandingState;
        return isRunning ? runForwardState : walkForwardState;
    }

    public void SetCameraPoseOverride(string poseKey)
    {
        cameraPoseOverrideKey = poseKey;
        RefreshAnimationCameraPose();
    }

    public void SetCameraPose(string poseKey)
    {
        SetCameraPoseOverride(poseKey);
    }

    public void ClearCameraPoseOverride()
    {
        cameraPoseOverrideKey = string.Empty;
        RefreshAnimationCameraPose();
    }

    public void ClearCameraPose()
    {
        ClearCameraPoseOverride();
    }

    public void SnapToView(Vector3 cameraWorldPosition, Quaternion cameraWorldRotation)
    {
        if (cameraRoot == null || playerCamera == null) return;

        Vector3 cameraLocalOffset = transform.InverseTransformPoint(playerCamera.transform.position);
        float targetYaw = cameraWorldRotation.eulerAngles.y;
        float targetPitch = NormalizeSignedAngle(cameraWorldRotation.eulerAngles.x);
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        transform.position = cameraWorldPosition - (transform.rotation * cameraLocalOffset);

        pitch = targetPitch;
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void SyncCurrentLookState()
    {
        if (cameraRoot == null) return;

        pitch = NormalizeSignedAngle(cameraRoot.localEulerAngles.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ApplyAnimationCameraPose()
    {
        if (cameraRoot == null) return;

        Vector3 configuredLocalPosition = default;
        float configuredBlendSpeed = 0f;
        bool configuredPose =
            string.IsNullOrEmpty(cameraPoseOverrideKey) &&
            UseAnimancerAnimation &&
            animancerController.TryGetCurrentCameraPosition(
                out configuredLocalPosition,
                out configuredBlendSpeed);

        Vector3 targetPosition = configuredPose
            ? configuredLocalPosition
            : targetCameraRootLocalPosition;
        float blendSpeed = configuredPose
            ? configuredBlendSpeed
            : currentCameraPoseBlendSpeed > 0f
            ? currentCameraPoseBlendSpeed
            : defaultCameraPoseBlendSpeed;

        if (!configuredPose)
            targetPosition.y += isCrouching ? crouchCameraY - standingCameraY : 0f;

        CharacterAnimationConfig.CameraSettings settings = CameraSettings;
        bool protectFromInterior = settings != null && settings.EnableCharacterInteriorProtection;
        float minimumCameraForward = protectFromInterior
            ? GetMinimumCameraRootForward(settings)
            : float.NegativeInfinity;
        if (protectFromInterior)
            targetPosition.z = Mathf.Max(targetPosition.z, minimumCameraForward);

        Vector3 cameraRootLocalPos = cameraRoot.localPosition;
        float horizontalBlendSpeed = protectFromInterior &&
                                     cameraRootLocalPos.z < minimumCameraForward
            ? Mathf.Max(blendSpeed, settings.InteriorProtectionBlendSpeed)
            : blendSpeed;
        float positionBlend = DampFactor(horizontalBlendSpeed);
        cameraRootLocalPos.x = Mathf.Lerp(cameraRootLocalPos.x, targetPosition.x, positionBlend);
        cameraRootLocalPos.z = Mathf.Lerp(cameraRootLocalPos.z, targetPosition.z, positionBlend);

        float verticalBlendSpeed = blendSpeed;
        if (!configuredPose && CameraSettings != null)
        {
            verticalBlendSpeed = targetPosition.y < cameraRootLocalPos.y
                ? CameraSettings.CrouchDownBlendSpeed
                : CameraSettings.StandUpBlendSpeed;
        }

        cameraRootLocalPos.y = Mathf.Lerp(
            cameraRootLocalPos.y,
            targetPosition.y,
            DampFactor(verticalBlendSpeed));

        cameraRoot.localPosition = cameraRootLocalPos;
    }

    private void SolveCameraConstraints(bool allowReturnSmoothing, string writer)
    {
        if (cameraRoot == null || playerCamera == null) return;

        CharacterAnimationConfig.CameraSettings settings = CameraSettings;
        DesiredCameraPosition = cameraRoot.TransformPoint(defaultCameraPositionInRoot);
        desiredCameraRotation = playerCamera.transform.rotation;
        CameraCollisionAnchorPosition = settings != null
            ? transform.TransformPoint(settings.CollisionAnchorLocalPosition)
            : cameraRoot.position;
        LastCameraWriter = writer;
        CharacterVisibilityFallbackActive = false;
        CharacterInteriorConstraintActive = false;
        CameraHeadProtectionActive = false;
        NearPlaneOverlapActive = false;
        CurrentCameraBlockingCollider = null;
        CameraCollisionHitDistance = float.PositiveInfinity;
        CameraCollisionSkinWidth = settings != null
            ? Mathf.Max(settings.CollisionPadding, settings.NearPlaneSkinWidth)
            : 0f;
        NearPlaneCollisionHalfExtents = GetCameraCollisionHalfExtents(settings);

        EnvironmentSafeCameraPosition = ResolveEnvironmentConstraint(
            DesiredCameraPosition,
            desiredCameraRotation,
            settings,
            allowReturnSmoothing);
        CharacterCorrectedCameraPosition = ResolveCharacterInteriorConstraint(
            EnvironmentSafeCameraPosition,
            desiredCameraRotation,
            settings);

        Vector3 finalPosition = CharacterCorrectedCameraPosition;
        if (settings != null && settings.EnableWallCollision)
        {
            bool corrected = ResolveEnvironmentPenetration(
                ref finalPosition,
                desiredCameraRotation,
                settings,
                out Collider overlapCollider);
            if (corrected)
            {
                NearPlaneOverlapActive = true;
                CameraCollisionActive = true;
                CurrentCameraBlockingCollider = overlapCollider != null
                    ? overlapCollider
                    : CurrentCameraBlockingCollider;

                if (CharacterInteriorConstraintActive)
                {
                    CharacterVisibilityFallbackActive = true;
                    if (IsCameraPoseEnvironmentSafe(
                            EnvironmentSafeCameraPosition,
                            desiredCameraRotation,
                            settings,
                            out _))
                    {
                        finalPosition = EnvironmentSafeCameraPosition;
                    }
                }
            }

            FinalEnvironmentValid = IsCameraPoseEnvironmentSafe(
                finalPosition,
                desiredCameraRotation,
                settings,
                out Collider finalBlockingCollider);
            if (!FinalEnvironmentValid)
            {
                CharacterVisibilityFallbackActive = true;
                CurrentCameraBlockingCollider = finalBlockingCollider != null
                    ? finalBlockingCollider
                    : CurrentCameraBlockingCollider;

                if (IsCameraPoseEnvironmentSafe(
                        EnvironmentSafeCameraPosition,
                        desiredCameraRotation,
                        settings,
                        out _))
                {
                    finalPosition = EnvironmentSafeCameraPosition;
                    FinalEnvironmentValid = true;
                }
                else if (hasEnvironmentValidCameraPosition &&
                         IsCameraPoseEnvironmentSafe(
                             lastEnvironmentValidCameraPosition,
                             desiredCameraRotation,
                             settings,
                             out _))
                {
                    finalPosition = lastEnvironmentValidCameraPosition;
                    FinalEnvironmentValid = true;
                }
            }
        }
        else
        {
            FinalEnvironmentValid = true;
        }

        playerCamera.transform.SetPositionAndRotation(finalPosition, desiredCameraRotation);
        FinalCameraPosition = finalPosition;
        FinalCameraRotation = desiredCameraRotation;
        FinalCameraCorrection = finalPosition - DesiredCameraPosition;
        if (FinalEnvironmentValid)
        {
            lastEnvironmentValidCameraPosition = finalPosition;
            hasEnvironmentValidCameraPosition = true;
        }
        RefreshCharacterProtectionDebug(finalPosition, settings);
        cameraProximityClip?.SetVisibilityFallback(CharacterVisibilityFallbackActive);
        bool proximityClipHandlesFallback =
            CharacterVisibilityFallbackActive &&
            cameraProximityClip != null &&
            cameraProximityClip.Available;
        cameraHeadHider?.SetVisibilityFallback(
            CharacterVisibilityFallbackActive && !proximityClipHandlesFallback);
        WarnIfCameraUnsafe(settings);
    }

    private Vector3 ResolveEnvironmentConstraint(
        Vector3 desiredPosition,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings,
        bool allowReturnSmoothing)
    {
        if (settings == null || !settings.EnableWallCollision)
        {
            CameraCollisionActive = false;
            currentCameraCollisionDistance = -1f;
            return desiredPosition;
        }

        Vector3 anchor = transform.TransformPoint(settings.CollisionAnchorLocalPosition);
        Vector3 cameraOffset = desiredPosition - anchor;
        float desiredDistance = cameraOffset.magnitude;
        if (desiredDistance <= 0.0001f)
        {
            CameraCollisionActive = false;
            currentCameraCollisionDistance = 0f;
            return desiredPosition;
        }

        Vector3 direction = cameraOffset / desiredDistance;
        float allowedDistance = GetAllowedCameraDistance(
            anchor,
            direction,
            desiredDistance,
            cameraRotation,
            settings,
            out Collider blockingCollider,
            out float hitDistance);
        allowedDistance = Mathf.Clamp(allowedDistance, 0f, desiredDistance);
        CurrentCameraBlockingCollider = blockingCollider;
        CameraCollisionHitDistance = hitDistance;

        if (currentCameraCollisionDistance < 0f)
            currentCameraCollisionDistance = allowedDistance;
        else if (allowedDistance < currentCameraCollisionDistance)
            currentCameraCollisionDistance = allowedDistance;
        else if (allowReturnSmoothing)
            currentCameraCollisionDistance = Mathf.Lerp(
                currentCameraCollisionDistance,
                allowedDistance,
                DampFactor(settings.ReturnSpeed));

        currentCameraCollisionDistance = Mathf.Min(currentCameraCollisionDistance, allowedDistance);
        CameraCollisionActive =
            blockingCollider != null ||
            currentCameraCollisionDistance < desiredDistance - 0.001f;

        Vector3 safePosition = anchor + direction * currentCameraCollisionDistance;
        if (ResolveEnvironmentPenetration(
                ref safePosition,
                cameraRotation,
                settings,
                out Collider overlapCollider))
        {
            NearPlaneOverlapActive = true;
            CameraCollisionActive = true;
            CurrentCameraBlockingCollider = overlapCollider != null
                ? overlapCollider
                : CurrentCameraBlockingCollider;
        }

        return safePosition;
    }

    private float GetAllowedCameraDistance(
        Vector3 anchor,
        Vector3 direction,
        float desiredDistance,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings,
        out Collider blockingCollider,
        out float hitDistance)
    {
        CameraCollisionCastResult result = cameraCollisionSolver.Cast(
            anchor,
            direction,
            desiredDistance,
            cameraRotation,
            settings.UseNearPlaneCollisionVolume,
            GetCameraCollisionCenterOffset(cameraRotation),
            NearPlaneCollisionHalfExtents,
            settings.CollisionRadius,
            CameraCollisionSkinWidth,
            settings.CollisionMask,
            cameraCollisionHits,
            cameraIgnoredColliders);
        blockingCollider = result.BlockingCollider;
        hitDistance = result.HitDistance;
        return result.AllowedDistance;
    }

    private Vector3 ResolveCharacterInteriorConstraint(
        Vector3 environmentSafePosition,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings)
    {
        ResetCharacterProtectionDebug();
        if (settings == null || !settings.EnableCharacterInteriorProtection ||
            !settings.TrackAnimatedHead || animatedHeadBone == null)
            return environmentSafePosition;

        float closestDistance = float.PositiveInfinity;
        float correction = 0f;
        float headRadius = isCrouching ? settings.CrouchHeadSafetyRadius : settings.HeadSafetyRadius;
        float headForwardClearance = isCrouching
            ? settings.CrouchHeadForwardClearance
            : settings.HeadForwardClearance;

        correction = Mathf.Max(
            correction,
            EvaluateCharacterBone(
                animatedHeadBone,
                "Head",
                headRadius,
                headForwardClearance,
                environmentSafePosition,
                ref closestDistance));

        if (settings.TrackUpperBodyBones)
        {
            float radiusScale = isCrouching ? settings.CrouchUpperBodyRadiusScale : 1f;
            correction = Mathf.Max(
                correction,
                EvaluateCharacterBone(
                    animatedNeckBone,
                    "Neck",
                    settings.NeckSafetyRadius * radiusScale,
                    0f,
                    environmentSafePosition,
                    ref closestDistance));
            correction = Mathf.Max(
                correction,
                EvaluateCharacterBone(
                    animatedChestBone,
                    "Upper Chest",
                    settings.ChestSafetyRadius * radiusScale,
                    0f,
                    environmentSafePosition,
                    ref closestDistance));
            correction = Mathf.Max(
                correction,
                EvaluateCharacterBone(
                    animatedLeftShoulderBone,
                    "Left Shoulder",
                    settings.ShoulderSafetyRadius * radiusScale,
                    0f,
                    environmentSafePosition,
                    ref closestDistance));
            correction = Mathf.Max(
                correction,
                EvaluateCharacterBone(
                    animatedRightShoulderBone,
                    "Right Shoulder",
                    settings.ShoulderSafetyRadius * radiusScale,
                    0f,
                    environmentSafePosition,
                    ref closestDistance));
        }

        Vector3 correctionVector = transform.forward * correction;
        Vector3 sphereCorrectedPosition = environmentSafePosition + correctionVector;
        float faceCorrection = EvaluateFaceSurfaceConstraint(
            sphereCorrectedPosition,
            cameraRotation,
            settings,
            out Vector3 faceCorrectionDirection);
        correctionVector += faceCorrectionDirection * faceCorrection;

        if (correctionVector.sqrMagnitude <= 0.000001f) return environmentSafePosition;

        CharacterInteriorConstraintActive = true;
        Vector3 candidate = environmentSafePosition + correctionVector;
        Collider candidateBlocker = null;
        bool candidateSafe = !settings.EnableWallCollision ||
                             IsCameraCorrectionPathSafe(
                                 environmentSafePosition,
                                 candidate,
                                 cameraRotation,
                                 settings,
                                 out candidateBlocker);
        if (!candidateSafe)
        {
            CharacterVisibilityFallbackActive = true;
            CurrentCameraBlockingCollider = candidateBlocker != null
                ? candidateBlocker
                : CurrentCameraBlockingCollider;
            return environmentSafePosition;
        }

        CameraHeadProtectionActive = true;
        return candidate;
    }

    private float EvaluateFaceSurfaceConstraint(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings,
        out Vector3 correctionDirection)
    {
        correctionDirection = transform.forward;
        if (settings == null || !settings.EnableFaceSurfaceConstraint ||
            !TryGetAnimatedFaceSurface(settings, out Vector3 facePosition, out Vector3 faceForward))
            return 0f;

        correctionDirection = faceForward;
        float requiredClearance = GetRequiredFaceClearance(settings);
        float currentClearance = GetMinimumFaceClearance(
            cameraPosition,
            cameraRotation,
            facePosition,
            faceForward);
        CurrentFaceSurfacePosition = facePosition;
        CurrentFaceSurfaceForward = faceForward;
        CurrentCameraFaceClearance = currentClearance;
        RequiredCameraFaceClearance = requiredClearance;
        if (currentClearance >= requiredClearance) return 0f;

        ClosestCameraBone = "Face Surface";
        CurrentCharacterSafetyRadius = requiredClearance;
        return requiredClearance - currentClearance;
    }

    private float GetRequiredFaceClearance(CharacterAnimationConfig.CameraSettings settings)
    {
        if (settings == null) return 0f;

        float baseClearance = isCrouching
            ? settings.CrouchFaceSurfaceClearance
            : settings.FaceSurfaceClearance;
        float upwardAngle = Mathf.Max(0f, -pitch);
        float fullAngle = Mathf.Max(
            settings.LookUpFaceClearanceStartAngle + 1f,
            settings.LookUpFaceClearanceFullAngle);
        float upwardBlend = Mathf.InverseLerp(
            settings.LookUpFaceClearanceStartAngle,
            fullAngle,
            upwardAngle);
        upwardBlend = upwardBlend * upwardBlend * (3f - 2f * upwardBlend);
        return baseClearance + settings.LookUpExtraFaceClearance * upwardBlend;
    }

    private float GetMinimumFaceClearance(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        Vector3 facePosition,
        Vector3 faceForward)
    {
        float cameraPointClearance = Vector3.Dot(cameraPosition - facePosition, faceForward);
        if (playerCamera == null) return cameraPointClearance;

        float nearClip = Mathf.Max(0.01f, playerCamera.nearClipPlane);
        float halfHeight = Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * nearClip;
        float halfWidth = halfHeight * Mathf.Max(0.1f, playerCamera.aspect);
        Vector3 nearCenter = cameraPosition + cameraRotation * Vector3.forward * nearClip;
        Vector3 nearRight = cameraRotation * Vector3.right;
        Vector3 nearUp = cameraRotation * Vector3.up;
        float nearCenterClearance = Vector3.Dot(nearCenter - facePosition, faceForward);
        float projectedNearExtent =
            Mathf.Abs(Vector3.Dot(nearRight, faceForward)) * halfWidth +
            Mathf.Abs(Vector3.Dot(nearUp, faceForward)) * halfHeight;
        float nearPlaneClearance = nearCenterClearance - projectedNearExtent;
        return Mathf.Min(cameraPointClearance, nearPlaneClearance);
    }

    private bool TryGetAnimatedFaceSurface(
        CharacterAnimationConfig.CameraSettings settings,
        out Vector3 facePosition,
        out Vector3 faceForward)
    {
        facePosition = animatedHeadBone != null ? animatedHeadBone.position : transform.position;
        faceForward = transform.forward;
        if (animatedHeadBone == null) return false;

        Vector3 animatedForward = animatedHeadBone.TransformDirection(faceForwardInHeadLocal).normalized;
        if (Vector3.Dot(animatedForward, transform.forward) < 0f)
            animatedForward = -animatedForward;
        faceForward = Vector3.Slerp(
            transform.forward,
            animatedForward,
            Mathf.Clamp01(settings.FaceDirectionFollow)).normalized;

        float faceDepth = float.NegativeInfinity;
        int markerCount = 0;
        AccumulateFaceMarkerDepth(
            animatedEyesAnchor,
            faceForward,
            ref faceDepth,
            ref markerCount);
        AccumulateFaceMarkerDepth(
            animatedEyebrowsAnchor,
            faceForward,
            ref faceDepth,
            ref markerCount);
        if (markerCount == 0 || faceDepth < 0.05f)
            faceDepth = Mathf.Max(0.01f, settings.FaceAnchorFallbackDepth);
        facePosition = animatedHeadBone.position + faceForward * faceDepth;
        return true;
    }

    private void AccumulateFaceMarkerDepth(
        Transform marker,
        Vector3 faceForward,
        ref float faceDepth,
        ref int markerCount)
    {
        if (marker == null || animatedHeadBone == null) return;

        faceDepth = Mathf.Max(
            faceDepth,
            Vector3.Dot(marker.position - animatedHeadBone.position, faceForward));
        markerCount++;
    }

    private float EvaluateCharacterBone(
        Transform bone,
        string label,
        float safetyRadius,
        float minimumForwardClearance,
        Vector3 cameraPosition,
        ref float closestDistance)
    {
        if (bone == null || safetyRadius <= 0f)
            return 0f;

        Vector3 boneToCamera = cameraPosition - bone.position;
        float recordedDistance = boneToCamera.magnitude;
        SetBoneDistance(label, recordedDistance);
        if (recordedDistance < closestDistance)
        {
            closestDistance = recordedDistance;
            ClosestCameraBone = label;
            CurrentCharacterSafetyRadius = safetyRadius;
        }
        if (recordedDistance >= safetyRadius) return 0f;

        float forwardDistance = Vector3.Dot(boneToCamera, transform.forward);
        float lateralDistanceSquared = Mathf.Max(
            0f,
            boneToCamera.sqrMagnitude - forwardDistance * forwardDistance);
        float sphereForward = lateralDistanceSquared < safetyRadius * safetyRadius
            ? Mathf.Sqrt(safetyRadius * safetyRadius - lateralDistanceSquared)
            : 0f;
        return Mathf.Max(0f, Mathf.Max(minimumForwardClearance, sphereForward) - forwardDistance);
    }

    private Vector3 GetCameraCollisionHalfExtents(CharacterAnimationConfig.CameraSettings settings)
    {
        if (playerCamera == null)
            return Vector3.one * 0.05f;

        float nearClip = Mathf.Max(0.01f, playerCamera.nearClipPlane);
        float halfHeight = Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * nearClip;
        float halfWidth = halfHeight * Mathf.Max(0.1f, playerCamera.aspect);
        return new Vector3(
            Mathf.Max(0.01f, halfWidth),
            Mathf.Max(0.01f, halfHeight),
            Mathf.Max(0.01f, nearClip * 0.5f));
    }

    private Vector3 GetCameraCollisionCenterOffset(Quaternion cameraRotation)
    {
        float nearClip = playerCamera != null ? Mathf.Max(0.01f, playerCamera.nearClipPlane) : 0.05f;
        return cameraRotation * Vector3.forward * (nearClip * 0.5f);
    }

    private bool IsCameraCorrectionPathSafe(
        Vector3 from,
        Vector3 to,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings,
        out Collider blockingCollider)
    {
        blockingCollider = null;
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance > 0.0001f)
        {
            Vector3 centerOffset = GetCameraCollisionCenterOffset(cameraRotation);
            int hitCount = Physics.BoxCastNonAlloc(
                from + centerOffset,
                NearPlaneCollisionHalfExtents,
                delta / distance,
                cameraCollisionHits,
                cameraRotation,
                distance,
                settings.CollisionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = cameraCollisionHits[i].collider;
                if (hitCollider == null || IsIgnoredCameraCollider(hitCollider)) continue;
                blockingCollider = hitCollider;
                return false;
            }
        }

        return IsCameraPoseEnvironmentSafe(to, cameraRotation, settings, out blockingCollider);
    }

    private bool IsCameraPoseEnvironmentSafe(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings,
        out Collider blockingCollider)
    {
        blockingCollider = null;
        if (settings == null || !settings.EnableWallCollision) return true;

        Vector3 center = cameraPosition + GetCameraCollisionCenterOffset(cameraRotation);
        int overlapCount = Physics.OverlapBoxNonAlloc(
            center,
            NearPlaneCollisionHalfExtents,
            cameraOverlapHits,
            cameraRotation,
            settings.CollisionMask,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = cameraOverlapHits[i];
            if (overlap == null || IsIgnoredCameraCollider(overlap)) continue;
            blockingCollider = overlap;
            return false;
        }

        return true;
    }

    private bool ResolveEnvironmentPenetration(
        ref Vector3 cameraPosition,
        Quaternion cameraRotation,
        CharacterAnimationConfig.CameraSettings settings,
        out Collider blockingCollider)
    {
        blockingCollider = null;
        if (settings == null || !settings.EnableWallCollision) return false;

        InitializeCameraCollisionProbe();
        if (cameraCollisionProbe == null) return false;

        cameraCollisionProbe.size = NearPlaneCollisionHalfExtents * 2f;
        bool corrected = false;
        float correctionBudget = Mathf.Max(0.01f, settings.MaximumPenetrationCorrection);
        int iterations = Mathf.Clamp(settings.PenetrationIterations, 1, 8);
        for (int iteration = 0; iteration < iterations && correctionBudget > 0.0001f; iteration++)
        {
            Vector3 probePosition = cameraPosition + GetCameraCollisionCenterOffset(cameraRotation);
            int overlapCount = Physics.OverlapBoxNonAlloc(
                probePosition,
                NearPlaneCollisionHalfExtents,
                cameraOverlapHits,
                cameraRotation,
                settings.CollisionMask,
                QueryTriggerInteraction.Ignore);
            bool correctedThisIteration = false;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider overlap = cameraOverlapHits[i];
                if (overlap == null || IsIgnoredCameraCollider(overlap)) continue;
                if (!Physics.ComputePenetration(
                        cameraCollisionProbe,
                        probePosition,
                        cameraRotation,
                        overlap,
                        overlap.transform.position,
                        overlap.transform.rotation,
                        out Vector3 direction,
                        out float distance) ||
                    distance <= 0f)
                    continue;

                float correctionDistance = Mathf.Min(
                    distance + Mathf.Max(0.001f, CameraCollisionSkinWidth * 0.1f),
                    correctionBudget);
                cameraPosition += direction * correctionDistance;
                correctionBudget -= correctionDistance;
                blockingCollider = overlap;
                corrected = true;
                correctedThisIteration = true;
                break;
            }

            if (!correctedThisIteration) break;
        }

        return corrected;
    }

    private void InitializeCameraCollisionProbe()
    {
        if (cameraCollisionProbe != null) return;

        cameraCollisionProbeObject = new GameObject("Player Camera Collision Probe")
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = LayerMask.NameToLayer("Ignore Raycast")
        };
        cameraCollisionProbe = cameraCollisionProbeObject.AddComponent<BoxCollider>();
        cameraCollisionProbe.isTrigger = true;
        cameraCollisionProbe.center = Vector3.zero;
        cameraCollisionProbe.enabled = false;
        cameraIgnoredColliders?.Add(cameraCollisionProbe);
    }

    private void ResetCharacterProtectionDebug()
    {
        CurrentCameraForwardFromHead = float.PositiveInfinity;
        CurrentCameraDistanceFromHead = float.PositiveInfinity;
        CurrentCameraDistanceFromNeck = float.PositiveInfinity;
        CurrentCameraDistanceFromChest = float.PositiveInfinity;
        CurrentCameraDistanceFromLeftShoulder = float.PositiveInfinity;
        CurrentCameraDistanceFromRightShoulder = float.PositiveInfinity;
        ClosestCameraBone = string.Empty;
        CurrentCharacterSafetyRadius = 0f;
        CurrentFaceSurfacePosition = Vector3.zero;
        CurrentFaceSurfaceForward = Vector3.zero;
        CurrentCameraFaceClearance = float.PositiveInfinity;
        RequiredCameraFaceClearance = 0f;
    }

    private void RefreshCharacterProtectionDebug(
        Vector3 cameraPosition,
        CharacterAnimationConfig.CameraSettings settings)
    {
        ResetCharacterProtectionDebug();
        float closestDistance = float.PositiveInfinity;
        float upperScale = settings != null && isCrouching
            ? settings.CrouchUpperBodyRadiusScale
            : 1f;
        RecordBoneDistance(
            animatedHeadBone,
            "Head",
            settings != null
                ? isCrouching ? settings.CrouchHeadSafetyRadius : settings.HeadSafetyRadius
                : 0f,
            cameraPosition,
            ref closestDistance);
        RecordBoneDistance(
            animatedNeckBone,
            "Neck",
            settings != null ? settings.NeckSafetyRadius * upperScale : 0f,
            cameraPosition,
            ref closestDistance);
        RecordBoneDistance(
            animatedChestBone,
            "Upper Chest",
            settings != null ? settings.ChestSafetyRadius * upperScale : 0f,
            cameraPosition,
            ref closestDistance);
        RecordBoneDistance(
            animatedLeftShoulderBone,
            "Left Shoulder",
            settings != null ? settings.ShoulderSafetyRadius * upperScale : 0f,
            cameraPosition,
            ref closestDistance);
        RecordBoneDistance(
            animatedRightShoulderBone,
            "Right Shoulder",
            settings != null ? settings.ShoulderSafetyRadius * upperScale : 0f,
            cameraPosition,
            ref closestDistance);

        if (animatedHeadBone != null)
        {
            Vector3 headToCamera = cameraPosition - animatedHeadBone.position;
            CurrentCameraForwardFromHead = Vector3.Dot(headToCamera, transform.forward);
        }

        if (settings != null && settings.EnableFaceSurfaceConstraint &&
            TryGetAnimatedFaceSurface(settings, out Vector3 facePosition, out Vector3 faceForward))
        {
            CurrentFaceSurfacePosition = facePosition;
            CurrentFaceSurfaceForward = faceForward;
            RequiredCameraFaceClearance = GetRequiredFaceClearance(settings);
            CurrentCameraFaceClearance = GetMinimumFaceClearance(
                cameraPosition,
                desiredCameraRotation,
                facePosition,
                faceForward);
        }
    }

    public void TeleportToCheckpoint(Vector3 position, Quaternion rotation)
    {
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
        {
            controller.enabled = false;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (controllerWasEnabled)
        {
            controller.enabled = true;
        }

        hasEnvironmentValidCameraPosition = false;
        currentCameraCollisionDistance = -1f;
        ClearMotionForCutscene();
    }

    private void RecordBoneDistance(
        Transform bone,
        string label,
        float safetyRadius,
        Vector3 cameraPosition,
        ref float closestDistance)
    {
        if (bone == null)
            return;

        float recordedDistance = Vector3.Distance(cameraPosition, bone.position);
        SetBoneDistance(label, recordedDistance);
        if (recordedDistance >= closestDistance) return;

        closestDistance = recordedDistance;
        ClosestCameraBone = label;
        CurrentCharacterSafetyRadius = safetyRadius;
    }

    private void SetBoneDistance(string label, float distance)
    {
        switch (label)
        {
            case "Head": CurrentCameraDistanceFromHead = distance; break;
            case "Neck": CurrentCameraDistanceFromNeck = distance; break;
            case "Upper Chest": CurrentCameraDistanceFromChest = distance; break;
            case "Left Shoulder": CurrentCameraDistanceFromLeftShoulder = distance; break;
            case "Right Shoulder": CurrentCameraDistanceFromRightShoulder = distance; break;
        }
    }

    private void WarnIfCameraUnsafe(CharacterAnimationConfig.CameraSettings settings)
    {
        if (FinalEnvironmentValid || settings == null || !settings.EnableRuntimeSafetyWarnings ||
            Time.unscaledTime < nextCameraSafetyWarningTime)
            return;

        nextCameraSafetyWarningTime = Time.unscaledTime + Mathf.Max(0.25f, settings.SafetyWarningCooldown);
        string colliderName = CurrentCameraBlockingCollider != null
            ? CurrentCameraBlockingCollider.name
            : "Unknown";
        Debug.LogWarning(
            $"Final camera pose intersects environment. Collider={colliderName}, " +
            $"Animation={CameraAnimationState}, Camera={FinalCameraPosition}, " +
            $"Desired={DesiredCameraPosition}, Correction={FinalCameraCorrection}.",
            this);
    }

    private float GetMinimumCameraRootForward(CharacterAnimationConfig.CameraSettings settings)
    {
        return isCrouching
            ? Mathf.Max(settings.MinimumCameraLocalForward, settings.CrouchMinimumCameraLocalForward)
            : settings.MinimumCameraLocalForward;
    }

    private Transform ResolveHeadBone()
    {
        return ResolveHumanoidBone(HumanBodyBones.Head, "head", "head_01");
    }

    private Transform ResolveNamedTransform(params string[] names)
    {
        if (names == null || names.Length == 0) return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int childIndex = 0; childIndex < children.Length; childIndex++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (string.Equals(
                        children[childIndex].name,
                        names[nameIndex],
                        StringComparison.OrdinalIgnoreCase))
                    return children[childIndex];
            }
        }

        return null;
    }

    private Transform ResolveHumanoidBone(HumanBodyBones humanoidBone, params string[] fallbackNames)
    {
        if (playerAnimator != null && playerAnimator.isHuman)
        {
            Transform resolved = playerAnimator.GetBoneTransform(humanoidBone);
            if (resolved != null) return resolved;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            for (int nameIndex = 0; nameIndex < fallbackNames.Length; nameIndex++)
            {
                if (string.Equals(
                        children[i].name,
                        fallbackNames[nameIndex],
                        StringComparison.OrdinalIgnoreCase))
                    return children[i];
            }
        }

        return null;
    }

    private void ConfigureCameraHeadHider(CharacterAnimationConfig.CameraSettings settings)
    {
        if (playerCamera == null || animatedHeadBone == null || settings == null) return;

        cameraHeadHider = playerCamera.GetComponent<PlayerCameraHeadHider>();
        if (cameraHeadHider == null)
            cameraHeadHider = playerCamera.gameObject.AddComponent<PlayerCameraHeadHider>();
        cameraHeadHider.Configure(
            animatedHeadBone,
            settings.EnableCharacterInteriorProtection && settings.HideHeadForPlayerCamera,
            settings.HeadScaleEmergencyOnly,
            settings.CharacterHideDistance,
            settings.HiddenHeadScale);
    }

    private void ConfigureCameraProximityClip(CharacterAnimationConfig.CameraSettings settings)
    {
        if (playerCamera == null || settings == null) return;

        cameraProximityClip = playerCamera.GetComponent<PlayerCameraProximityClip>();
        if (cameraProximityClip == null)
            cameraProximityClip = playerCamera.gameObject.AddComponent<PlayerCameraProximityClip>();
        cameraProximityClip.Configure(
            transform,
            settings.EnableCharacterInteriorProtection && settings.EnableProximityClip,
            settings.ProximityClipShader,
            settings.ProximityClipRadius);
    }

    private bool IsIgnoredCameraCollider(Collider target)
    {
        return cameraIgnoredColliders != null && cameraIgnoredColliders.Contains(target);
    }

    private static float DampFactor(float speed)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.deltaTime);
    }

    private void RefreshAnimationCameraPose()
    {
        if (cameraRoot == null) return;

        AnimationCameraPose pose = ResolveActiveCameraPose();

        if (pose == null)
        {
            targetCameraRootLocalPosition = defaultCameraRootLocalPosition;
            currentCameraPoseBlendSpeed = defaultCameraPoseBlendSpeed;
            return;
        }

        targetCameraRootLocalPosition = pose.localPosition;
        currentCameraPoseBlendSpeed = pose.blendSpeed > 0f
            ? pose.blendSpeed
            : defaultCameraPoseBlendSpeed;
    }

    private AnimationCameraPose ResolveActiveCameraPose()
    {
        string poseKey = !string.IsNullOrEmpty(cameraPoseOverrideKey)
            ? cameraPoseOverrideKey
            : currentAnimationState;

        if (string.IsNullOrEmpty(poseKey) || animationCameraPoses == null)
        {
            return null;
        }

        for (int i = 0; i < animationCameraPoses.Length; i++)
        {
            AnimationCameraPose pose = animationCameraPoses[i];
            if (pose != null && pose.key == poseKey)
            {
                return pose;
            }
        }

        return null;
    }

    private float NormalizeSignedAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void OnDrawGizmosSelected()
    {
        CharacterAnimationConfig.CameraSettings settings = CameraSettings;
        if (settings == null || !settings.DrawSolverGizmos) return;

        Vector3 anchor = Application.isPlaying
            ? CameraCollisionAnchorPosition
            : transform.TransformPoint(settings.CollisionAnchorLocalPosition);
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 1f);
        Gizmos.DrawWireSphere(anchor, 0.035f);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(anchor, DesiredCameraPosition);
            Gizmos.DrawWireSphere(DesiredCameraPosition, 0.025f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(anchor, EnvironmentSafeCameraPosition);
            Gizmos.DrawWireSphere(EnvironmentSafeCameraPosition, 0.03f);

            Gizmos.color = CharacterInteriorConstraintActive ? Color.magenta : Color.gray;
            Gizmos.DrawLine(EnvironmentSafeCameraPosition, CharacterCorrectedCameraPosition);
            Gizmos.DrawWireSphere(CharacterCorrectedCameraPosition, 0.03f);

            Gizmos.color = FinalEnvironmentValid ? Color.green : Color.red;
            Gizmos.DrawLine(DesiredCameraPosition, FinalCameraPosition);
            Gizmos.DrawWireSphere(FinalCameraPosition, 0.04f);
        }

        DrawCameraBoneGizmo(animatedHeadBone, settings.HeadSafetyRadius, new Color(1f, 0.35f, 0.2f, 1f));
        if (settings.TrackUpperBodyBones)
        {
            DrawCameraBoneGizmo(animatedNeckBone, settings.NeckSafetyRadius, new Color(1f, 0.6f, 0.2f, 1f));
            DrawCameraBoneGizmo(animatedChestBone, settings.ChestSafetyRadius, new Color(0.8f, 0.35f, 1f, 1f));
            DrawCameraBoneGizmo(animatedLeftShoulderBone, settings.ShoulderSafetyRadius, new Color(0.5f, 0.4f, 1f, 1f));
            DrawCameraBoneGizmo(animatedRightShoulderBone, settings.ShoulderSafetyRadius, new Color(0.5f, 0.4f, 1f, 1f));
        }

        if (playerCamera == null) return;

        Quaternion cameraRotation = Application.isPlaying
            ? desiredCameraRotation
            : playerCamera.transform.rotation;
        Vector3 cameraPosition = Application.isPlaying
            ? FinalCameraPosition
            : playerCamera.transform.position;
        Vector3 halfExtents = Application.isPlaying
            ? NearPlaneCollisionHalfExtents
            : GetCameraCollisionHalfExtents(settings);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            cameraPosition + GetCameraCollisionCenterOffset(cameraRotation),
            cameraRotation,
            Vector3.one);
        Gizmos.color = FinalEnvironmentValid ? new Color(0.2f, 1f, 0.4f, 1f) : Color.red;
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        Gizmos.matrix = previousMatrix;
    }

    private static void DrawCameraBoneGizmo(Transform bone, float radius, Color color)
    {
        if (bone == null || radius <= 0f) return;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(bone.position, radius);
        Gizmos.DrawSphere(bone.position, 0.018f);
    }
}

[DisallowMultipleComponent]
internal sealed class PlayerCameraProximityClip : MonoBehaviour
{
    private const string DefaultShaderName =
        "LightTower/Character/First Person Proximity Clip Lit";

    private static readonly int ClipCenterRadiusId =
        Shader.PropertyToID("_LightTowerFirstPersonClipCenterRadius");
    private static readonly int ClipEnabledId =
        Shader.PropertyToID("_LightTowerFirstPersonClipEnabled");
    private static readonly string[] CopiedLocalKeywords =
    {
        "_NORMALMAP",
        "_PARALLAXMAP",
        "_RECEIVE_SHADOWS_OFF",
        "_DETAIL_MULX2",
        "_DETAIL_SCALED",
        "_SURFACE_TYPE_TRANSPARENT",
        "_ALPHATEST_ON",
        "_ALPHAPREMULTIPLY_ON",
        "_ALPHAMODULATE_ON",
        "_EMISSION",
        "_METALLICSPECGLOSSMAP",
        "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
        "_OCCLUSIONMAP",
        "_SPECULARHIGHLIGHTS_OFF",
        "_ENVIRONMENTREFLECTIONS_OFF",
        "_SPECULAR_SETUP"
    };

    private readonly List<RendererMaterialState> rendererMaterialStates =
        new List<RendererMaterialState>();

    private Camera targetCamera;
    private Transform playerRoot;
    private bool clipForThisCamera;
    private bool visibilityFallbackActive;
    private float clipRadius = 0.2f;

    public bool Available { get; private set; }
    public bool WillClipForPlayerCamera =>
        Available && clipForThisCamera && visibilityFallbackActive;

    public void Configure(
        Transform targetPlayerRoot,
        bool shouldClip,
        Shader configuredShader,
        float radius)
    {
        DisableGlobalClip();
        RestoreOriginalMaterials();

        targetCamera = GetComponent<Camera>();
        playerRoot = targetPlayerRoot;
        clipForThisCamera = shouldClip;
        visibilityFallbackActive = false;
        clipRadius = Mathf.Clamp(radius, 0.05f, 0.3f);

        Shader clipShader = configuredShader != null
            ? configuredShader
            : Shader.Find(DefaultShaderName);
        if (!clipForThisCamera || targetCamera == null || playerRoot == null ||
            clipShader == null || !clipShader.isSupported)
        {
            Available = false;
            enabled = false;
            return;
        }

        SkinnedMeshRenderer[] renderers =
            playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            CreateRuntimeMaterialOverride(renderers[rendererIndex], clipShader);

        Available = rendererMaterialStates.Count > 0;
        enabled = Available;
        if (!Available)
        {
            Debug.LogWarning(
                "First-person proximity clip could not find a compatible player SkinnedMeshRenderer. " +
                "The Head bone emergency fallback will remain available.",
                this);
        }
    }

    public void SetVisibilityFallback(bool active)
    {
        visibilityFallbackActive = active;
        if (!WillClipForPlayerCamera)
            DisableGlobalClip();
    }

    public void PrepareForCameraRender(Camera camera)
    {
        if (camera != targetCamera || !WillClipForPlayerCamera)
        {
            DisableGlobalClip();
            return;
        }

        Vector3 center = targetCamera.transform.position;
        Shader.SetGlobalVector(
            ClipCenterRadiusId,
            new Vector4(center.x, center.y, center.z, clipRadius));
        Shader.SetGlobalFloat(ClipEnabledId, 1f);
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnPreCull()
    {
        PrepareForCameraRender(targetCamera);
    }

    private void OnPostRender()
    {
        DisableGlobalClip();
    }

    private void OnDisable()
    {
        UnsubscribeFromRendering();
        DisableGlobalClip();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRendering();
        DisableGlobalClip();
        RestoreOriginalMaterials();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        PrepareForCameraRender(camera);
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == targetCamera)
            DisableGlobalClip();
    }

    private void CreateRuntimeMaterialOverride(
        SkinnedMeshRenderer targetRenderer,
        Shader clipShader)
    {
        if (targetRenderer == null) return;

        Material[] originalMaterials = targetRenderer.sharedMaterials;
        if (originalMaterials == null || originalMaterials.Length == 0) return;

        Material[] runtimeMaterials = new Material[originalMaterials.Length];
        bool createdAnyMaterial = false;
        for (int materialIndex = 0; materialIndex < originalMaterials.Length; materialIndex++)
        {
            Material original = originalMaterials[materialIndex];
            if (original == null) continue;

            runtimeMaterials[materialIndex] = original;
            if (original.shader == null ||
                original.shader.name != "Universal Render Pipeline/Lit")
                continue;

            Material runtimeMaterial = new Material(clipShader)
            {
                name = original.name + " (First Person Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            CopyMatchingMaterialProperties(original, runtimeMaterial);
            runtimeMaterial.renderQueue = original.renderQueue;
            runtimeMaterial.enableInstancing = original.enableInstancing;
            runtimeMaterial.doubleSidedGI = original.doubleSidedGI;
            runtimeMaterial.globalIlluminationFlags = original.globalIlluminationFlags;
            runtimeMaterials[materialIndex] = runtimeMaterial;
            createdAnyMaterial = true;
        }

        if (!createdAnyMaterial) return;

        rendererMaterialStates.Add(new RendererMaterialState
        {
            Renderer = targetRenderer,
            OriginalMaterials = originalMaterials,
            RuntimeMaterials = runtimeMaterials
        });
        targetRenderer.sharedMaterials = runtimeMaterials;
    }

    private static void CopyMatchingMaterialProperties(Material source, Material destination)
    {
        Shader destinationShader = destination.shader;
        int propertyCount = destinationShader.GetPropertyCount();
        for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
        {
            string propertyName = destinationShader.GetPropertyName(propertyIndex);
            if (!source.HasProperty(propertyName)) continue;

            switch (destinationShader.GetPropertyType(propertyIndex))
            {
                case ShaderPropertyType.Color:
                    destination.SetColor(propertyName, source.GetColor(propertyName));
                    break;
                case ShaderPropertyType.Vector:
                    destination.SetVector(propertyName, source.GetVector(propertyName));
                    break;
                case ShaderPropertyType.Texture:
                    destination.SetTexture(propertyName, source.GetTexture(propertyName));
                    destination.SetTextureOffset(propertyName, source.GetTextureOffset(propertyName));
                    destination.SetTextureScale(propertyName, source.GetTextureScale(propertyName));
                    break;
                default:
                    destination.SetFloat(propertyName, source.GetFloat(propertyName));
                    break;
            }
        }

        for (int keywordIndex = 0; keywordIndex < CopiedLocalKeywords.Length; keywordIndex++)
        {
            string keyword = CopiedLocalKeywords[keywordIndex];
            if (source.IsKeywordEnabled(keyword))
                destination.EnableKeyword(keyword);
        }

        destination.SetShaderPassEnabled(
            "ShadowCaster",
            source.GetShaderPassEnabled("ShadowCaster"));
        destination.SetShaderPassEnabled(
            "MotionVectors",
            source.GetShaderPassEnabled("MotionVectors"));
    }

    private void RestoreOriginalMaterials()
    {
        for (int stateIndex = 0; stateIndex < rendererMaterialStates.Count; stateIndex++)
        {
            RendererMaterialState state = rendererMaterialStates[stateIndex];
            if (state.Renderer != null)
                state.Renderer.sharedMaterials = state.OriginalMaterials;

            if (state.RuntimeMaterials == null) continue;
            for (int materialIndex = 0;
                 materialIndex < state.RuntimeMaterials.Length;
                 materialIndex++)
            {
                Material runtimeMaterial = state.RuntimeMaterials[materialIndex];
                if (runtimeMaterial == null) continue;
                if (state.OriginalMaterials != null &&
                    materialIndex < state.OriginalMaterials.Length &&
                    runtimeMaterial == state.OriginalMaterials[materialIndex])
                    continue;
                if (Application.isPlaying)
                    Destroy(runtimeMaterial);
                else
                    DestroyImmediate(runtimeMaterial);
            }
        }

        rendererMaterialStates.Clear();
        Available = false;
    }

    private static void DisableGlobalClip()
    {
        Shader.SetGlobalFloat(ClipEnabledId, 0f);
    }

    private void UnsubscribeFromRendering()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private sealed class RendererMaterialState
    {
        public SkinnedMeshRenderer Renderer;
        public Material[] OriginalMaterials;
        public Material[] RuntimeMaterials;
    }
}

[DisallowMultipleComponent]
internal sealed class PlayerCameraHeadHider : MonoBehaviour
{
    private Transform headBone;
    private Camera targetCamera;
    private Vector3 visibleScale;
    private float hideDistance = 0.9f;
    private float hiddenScale = 0.001f;
    private bool hideForThisCamera;
    private bool headIsHidden;
    private bool forceVisibilityFallback;
    private bool emergencyOnly = true;

    public bool WillHideForPlayerCamera =>
        hideForThisCamera &&
        headBone != null &&
        (forceVisibilityFallback ||
         (!emergencyOnly &&
          (targetCamera == null ||
           Vector3.Distance(targetCamera.transform.position, headBone.position) <= hideDistance)));

    public bool BoneScaleFallbackActive => headIsHidden;

    public void Configure(
        Transform targetHeadBone,
        bool shouldHide,
        bool useOnlyAsEmergency,
        float distance,
        float scale)
    {
        RestoreHead();
        headBone = targetHeadBone;
        hideForThisCamera = shouldHide;
        emergencyOnly = useOnlyAsEmergency;
        hideDistance = Mathf.Max(0.05f, distance);
        hiddenScale = Mathf.Clamp(scale, 0.0001f, 0.1f);
        enabled = hideForThisCamera && headBone != null;
    }

    public void SetVisibilityFallback(bool active)
    {
        forceVisibilityFallback = active;
        if (!active && emergencyOnly)
            RestoreHead();
    }

    public void PrepareForCameraRender(Camera camera)
    {
        if (camera == targetCamera) HideHead();
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnPreCull()
    {
        HideHead();
    }

    private void OnPostRender()
    {
        RestoreHead();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        RestoreHead();
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        RestoreHead();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == targetCamera) HideHead();
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == targetCamera) RestoreHead();
    }

    private void HideHead()
    {
        if (!hideForThisCamera || headBone == null || headIsHidden) return;
        if (emergencyOnly && !forceVisibilityFallback) return;
        if (!forceVisibilityFallback && targetCamera != null &&
            Vector3.Distance(targetCamera.transform.position, headBone.position) > hideDistance)
            return;

        visibleScale = headBone.localScale;
        headBone.localScale = visibleScale * hiddenScale;
        headIsHidden = true;
    }

    private void RestoreHead()
    {
        if (!headIsHidden || headBone == null) return;
        headBone.localScale = visibleScale;
        headIsHidden = false;
    }
}
