using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

    private string currentAnimationState;
    private string cameraPoseOverrideKey;
    private Vector3 defaultCameraRootLocalPosition;
    private Vector3 targetCameraRootLocalPosition;
    private float currentCameraPoseBlendSpeed;
    private Vector3 defaultCameraPositionInRoot;
    private float currentCameraCollisionDistance = -1f;
    private readonly RaycastHit[] cameraCollisionHits = new RaycastHit[32];
    private readonly Collider[] cameraOverlapHits = new Collider[16];
    private HashSet<Collider> cameraIgnoredColliders;
    private Transform animatedHeadBone;
    private PlayerCameraHeadHider cameraHeadHider;

    public bool CameraCollisionActive { get; private set; }
    public bool CameraHeadProtectionActive { get; private set; }
    public float CurrentCameraForwardFromHead { get; private set; }
    public float CurrentCameraDistanceFromHead { get; private set; }
    public float CurrentLocomotionMovementScale { get; private set; } = 1f;
    public bool AnimationGrounded => animationGrounded;
    public bool GroundSnapActive { get; private set; }
    public float CurrentGroundDistance { get; private set; } = float.PositiveInfinity;
    public float LastUngroundedDuration { get; private set; }
    public Transform ProtectedHeadBone => animatedHeadBone;
    public bool HeadRenderSuppressionEnabled => cameraHeadHider != null && cameraHeadHider.enabled;

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
        }

        cameraIgnoredColliders = new HashSet<Collider>(GetComponentsInChildren<Collider>(true));

       
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
        ApplyCameraWallCollision();
        ApplyAnimatedHeadProtection();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        CameraCollisionActive = false;
        GroundSnapActive = false;
        pendingGroundSnapDistance = 0f;
        currentCameraCollisionDistance = -1f;
        if (cameraRoot != null && playerCamera != null)
            playerCamera.transform.position = cameraRoot.TransformPoint(defaultCameraPositionInRoot);
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
                    landStateTimer = landStateHoldTime;

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
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity * Time.deltaTime;
       
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

        rawInputX = Input.GetAxisRaw("Horizontal");
        rawInputZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = (transform.right * rawInputX + transform.forward * rawInputZ).normalized;
        hasMoveInput = inputDirection.sqrMagnitude > 0.01f;

        bool wantsToRun = Input.GetKey(runKey);

        
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
        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleCrouch()
    {
        if (!CanCrouch) return;

        if (Input.GetKeyDown(crouchKey))
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

        if (isGrounded && Input.GetKeyDown(jumpKey))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
            jumpStartedThisFrame = true;
            landStateTimer = 0f;
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

            if (landStateTimer > 0f)
            {
                animancerController.PlayLanding(landingImpactVelocity, lastGroundHadMoveInput);
                landStateTimer -= Time.deltaTime;
                RefreshAnimancerCameraPoseState();
                return;
            }

            animancerController.SetMovement(moveDirection, transform.forward);
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

    private void ApplyCameraWallCollision()
    {
        if (cameraRoot == null || playerCamera == null) return;

        CharacterAnimationConfig.CameraSettings settings = CameraSettings;
        Vector3 desiredPosition = cameraRoot.TransformPoint(defaultCameraPositionInRoot);
        if (settings == null || !settings.EnableWallCollision)
        {
            CameraCollisionActive = false;
            currentCameraCollisionDistance = -1f;
            playerCamera.transform.position = desiredPosition;
            return;
        }

        Vector3 anchor = transform.TransformPoint(settings.CollisionAnchorLocalPosition);
        Vector3 cameraOffset = desiredPosition - anchor;
        float desiredDistance = cameraOffset.magnitude;
        if (desiredDistance <= 0.0001f)
        {
            CameraCollisionActive = false;
            playerCamera.transform.position = desiredPosition;
            return;
        }

        Vector3 direction = cameraOffset / desiredDistance;
        float effectiveMinimumDistance = Mathf.Min(
            settings.MinimumDistance,
            desiredDistance * Mathf.Clamp(settings.MinimumDistanceRatio, 0.05f, 0.95f));
        float allowedDistance = desiredDistance;
        int hitCount = Physics.SphereCastNonAlloc(
            anchor,
            settings.CollisionRadius,
            direction,
            cameraCollisionHits,
            desiredDistance,
            settings.CollisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = cameraCollisionHits[i];
            if (hit.collider == null || IsIgnoredCameraCollider(hit.collider)) continue;
            allowedDistance = Mathf.Min(allowedDistance, hit.distance - settings.CollisionPadding);
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(
            desiredPosition,
            settings.CollisionRadius,
            cameraOverlapHits,
            settings.CollisionMask,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = cameraOverlapHits[i];
            if (overlap == null || IsIgnoredCameraCollider(overlap)) continue;
            allowedDistance = Mathf.Min(allowedDistance, effectiveMinimumDistance);
            break;
        }

        allowedDistance = Mathf.Clamp(
            allowedDistance,
            effectiveMinimumDistance,
            desiredDistance);
        CameraCollisionActive = allowedDistance < desiredDistance - 0.001f;

        if (currentCameraCollisionDistance < 0f)
            currentCameraCollisionDistance = allowedDistance;

        float responseSpeed = allowedDistance < currentCameraCollisionDistance
            ? settings.PullInSpeed
            : settings.ReturnSpeed;
        currentCameraCollisionDistance = Mathf.Lerp(
            currentCameraCollisionDistance,
            allowedDistance,
            DampFactor(responseSpeed));
        playerCamera.transform.position = anchor + direction * currentCameraCollisionDistance;
    }

    private float GetMinimumCameraRootForward(CharacterAnimationConfig.CameraSettings settings)
    {
        return isCrouching
            ? Mathf.Max(settings.MinimumCameraLocalForward, settings.CrouchMinimumCameraLocalForward)
            : settings.MinimumCameraLocalForward;
    }

    private void ApplyAnimatedHeadProtection()
    {
        CameraHeadProtectionActive = false;
        CurrentCameraForwardFromHead = float.PositiveInfinity;
        CurrentCameraDistanceFromHead = float.PositiveInfinity;

        CharacterAnimationConfig.CameraSettings settings = CameraSettings;
        if (settings == null || !settings.EnableCharacterInteriorProtection ||
            !settings.TrackAnimatedHead || animatedHeadBone == null || playerCamera == null)
            return;

        Vector3 playerForward = transform.forward;
        Vector3 headToCamera = playerCamera.transform.position - animatedHeadBone.position;
        CurrentCameraForwardFromHead = Vector3.Dot(headToCamera, playerForward);
        CurrentCameraDistanceFromHead = headToCamera.magnitude;
        float safetyRadius = isCrouching
            ? settings.CrouchHeadSafetyRadius
            : settings.HeadSafetyRadius;
        if (CurrentCameraDistanceFromHead >= safetyRadius) return;

        // A nearby wall has priority over geometric head clearance. The camera-only
        // head suppression keeps the view clean while collision holds it inside the rig.
        if (CameraCollisionActive) return;

        float lateralDistanceSquared = Mathf.Max(
            0f,
            headToCamera.sqrMagnitude - CurrentCameraForwardFromHead * CurrentCameraForwardFromHead);
        float radiusSquared = safetyRadius * safetyRadius;
        float sphereForward = lateralDistanceSquared < radiusSquared
            ? Mathf.Sqrt(radiusSquared - lateralDistanceSquared)
            : 0f;
        float requiredForward = isCrouching
            ? settings.CrouchHeadForwardClearance
            : settings.HeadForwardClearance;
        float correction = Mathf.Max(requiredForward, sphereForward) - CurrentCameraForwardFromHead;
        if (correction <= 0f) return;

        playerCamera.transform.position += playerForward * correction;
        Vector3 correctedHeadToCamera = playerCamera.transform.position - animatedHeadBone.position;
        CurrentCameraForwardFromHead = Vector3.Dot(correctedHeadToCamera, playerForward);
        CurrentCameraDistanceFromHead = correctedHeadToCamera.magnitude;
        CameraHeadProtectionActive = true;
    }

    private Transform ResolveHeadBone()
    {
        if (playerAnimator != null && playerAnimator.isHuman)
        {
            Transform humanoidHead = playerAnimator.GetBoneTransform(HumanBodyBones.Head);
            if (humanoidHead != null) return humanoidHead;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (string.Equals(children[i].name, "head", StringComparison.OrdinalIgnoreCase))
                return children[i];
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
            settings.CharacterHideDistance,
            settings.HiddenHeadScale);
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

    public void Configure(
        Transform targetHeadBone,
        bool shouldHide,
        float distance,
        float scale)
    {
        RestoreHead();
        headBone = targetHeadBone;
        hideForThisCamera = shouldHide;
        hideDistance = Mathf.Max(0.05f, distance);
        hiddenScale = Mathf.Clamp(scale, 0.0001f, 0.1f);
        enabled = hideForThisCamera && headBone != null;
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
        if (targetCamera != null &&
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
