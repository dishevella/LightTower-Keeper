using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;

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

    [Header("Input Keys")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Control Flags")]
    public bool CanMove = true;
    public bool CanLook = true;
    public bool CanRun = true;
    public bool CanCrouch = true;
    public bool CanJump = true;

    private CharacterController controller;

    private float pitch;
    private float verticalVelocity;

    //the state specifically belong to playercontroller and it only can be read outside and only can be modity inside
    public bool isGrounded { get; private set; }
    public bool isRunning { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isJumping { get; private set; }

    private float currentSpeed;
    private Vector3 moveDierection;

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
        }
        if(playerCamera != null)
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
    }
    private void UpdateGroundedState()
    {
        isGrounded = controller.isGrounded;
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
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); //clamp means: clamp(x,a,b) ifx<a, then remains a, same with the other side

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);//player object control the yaw, and apply the pitch to the camera which controls the up and down
        }
        transform.Rotate(Vector3.up * mouseX); //equal to (0, mouseX,0), rotate by Y
    }
    void HandleMovement()
    {
        if (!CanMove)
        {
            moveDierection = Vector3.zero;
            currentSpeed = 0f;
            isRunning = false;
            return;
        }
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");//raw means 0,1 or -1. it turns immediately not like turning smoothly
        //forward means the front side of the character
        Vector3 inputDirection = (transform.right * inputX + transform.forward * inputZ).normalized; // normalize means unitlize, transverting every digit to 1 and not changing the directions

        bool hasMoveInput = inputDirection.sqrMagnitude > 0.01f;// length of vector square, which is to determine if there's a direction
        bool wantsToRun = Input.GetKey(runKey);

        isRunning = hasMoveInput && wantsToRun && CanRun && !isCrouching;

        if(isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
        }
        moveDierection = inputDirection * currentSpeed;
        Vector3 finalMove = moveDierection;
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
        if(Input.GetKeyDown(crouchKey))
        {
            isCrouching = !isCrouching; // invert the bool
        }
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed* Time.deltaTime);
        controller.height = newHeight;
        controller.center = new Vector3(0f, controller.height / 2f, 0f);
        if (cameraRoot != null)
        {
            float targetCameraY = isCrouching ? crouchCameraY : standingCameraY;
            Vector3 camLocalPos = cameraRoot.localPosition;
            camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
            cameraRoot.localPosition = camLocalPos;
        }
    }
    private void HandleJump()
    {
        if (!CanJump) return;
        if(isGrounded)
        {
            isJumping = false;
        }
        if(isGrounded&&Input.GetKeyDown(jumpKey))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
        }
    }
    private void ApplyFOV()
    {
        if (playerCamera == null) return;
        float targetFOV = isRunning ? runFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovSmoothSpeed * Time.deltaTime);
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
} 