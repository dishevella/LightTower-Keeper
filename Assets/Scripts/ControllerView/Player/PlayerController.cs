using System;
using UnityEngine;

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
    private float timeSinceGrounded;
    private bool animationGrounded;

    private float jumpStartTimer;

    public bool isGrounded { get; private set; }
    public bool isRunning { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isJumping { get; private set; }

    public Transform CameraRootTransform => cameraRoot;

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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight / 2f, 0f);

        if (cameraRoot != null)
        {
            Vector3 camLocalPos = cameraRoot.localPosition;
            camLocalPos.y = standingCameraY;
            cameraRoot.localPosition = camLocalPos;
            defaultCameraRootLocalPosition = cameraRoot.localPosition;
            targetCameraRootLocalPosition = defaultCameraRootLocalPosition;
            currentCameraPoseBlendSpeed = defaultCameraPoseBlendSpeed;
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = normalFOV;
        }

       
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        UpdateGroundedState();
        HandleLook();
        HandleCrouch();
        HandleJump();
        HandleMovement();
        ApplyGravity();
        ApplyFOV();
        UpdateAnimationState();
        ApplyAnimationCameraPose();

        wasGroundedLastFrame = isGrounded;

        if (isGrounded)
        {
            lastGroundHadMoveInput = hasMoveInput;
            lastGroundWasRunning = isRunning;
        }
    }

    private void UpdateGroundedState()
    {
        bool wasGrounded = isGrounded;

        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            timeSinceGrounded = 0f;
        }
        else
        {
            timeSinceGrounded += Time.deltaTime;
        }

        
        bool allowAirAnimation =
            timeSinceGrounded >= fallAnimationMinAirTime &&
            verticalVelocity <= fallAnimationMinSpeed;

        
        animationGrounded = isGrounded || !allowAirAnimation;

        if (!wasGrounded && isGrounded)
        {
            bool shouldPlayLand =
                timeSinceGrounded >= minAirTimeForLand ||
                lastAirborneVerticalVelocity <= minFallSpeedForLand;

            if (shouldPlayLand)
            {
                landStateTimer = landStateHoldTime;
            }

            lastAirborneVerticalVelocity = 0f;
        }

        if (!isGrounded)
        {
            lastAirborneVerticalVelocity = verticalVelocity;
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundStickForce;
        }
    }


    private void HandleLook()
    {
        if (!CanLook) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
       
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

            Vector3 lockedMove = Vector3.zero;
            lockedMove.y = verticalVelocity;
            controller.Move(lockedMove * Time.deltaTime);
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

        Vector3 finalMove = moveDirection;
        finalMove.y = verticalVelocity;
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
            isCrouching = !isCrouching;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.height = newHeight;
        controller.center = new Vector3(0f, controller.height / 2f, 0f);

        if (cameraRoot != null)
        {
            float crouchYOffset = isCrouching ? crouchCameraY - standingCameraY : 0f;
            float targetCameraY = targetCameraRootLocalPosition.y + crouchYOffset;
            Vector3 camLocalPos = cameraRoot.localPosition;
            camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
            cameraRoot.localPosition = camLocalPos;
        }
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

        if (!driveLocomotionAnimation || playerAnimator == null) return;

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
        isCrouching = false;
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

    private void ApplyAnimationCameraPose()
    {
        if (cameraRoot == null) return;

        float blendSpeed = currentCameraPoseBlendSpeed > 0f
            ? currentCameraPoseBlendSpeed
            : defaultCameraPoseBlendSpeed;

        Vector3 cameraRootLocalPos = cameraRoot.localPosition;
        cameraRootLocalPos.x = Mathf.Lerp(
            cameraRootLocalPos.x,
            targetCameraRootLocalPosition.x,
            blendSpeed * Time.deltaTime);
        cameraRootLocalPos.z = Mathf.Lerp(
            cameraRootLocalPos.z,
            targetCameraRootLocalPosition.z,
            blendSpeed * Time.deltaTime);

        cameraRoot.localPosition = cameraRootLocalPos;
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
