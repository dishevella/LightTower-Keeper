using UnityEngine;

public class LighthouseBeamControlController : ControllerAbstract
{
    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MonoBehaviour[] additionalComponentsToLock;

    [Header("Beam")]
    [SerializeField] private Transform beamYawPivot;
    [SerializeField] private Transform beamPitchPivot;
    [SerializeField] private float yawSpeed = 70f;
    [SerializeField] private float pitchSpeed = 45f;
    [SerializeField] private float minPitch = -12f;
    [SerializeField] private float maxPitch = 18f;
    [SerializeField] private bool invertVertical;

    [Header("Completion")]
    [SerializeField] private float inputThreshold = 0.05f;
    [SerializeField] private float minimumActiveSweepDuration = 6f;
    [SerializeField] private float idleToFinishDuration = 1.2f;

    [Header("Optional State")]
    [SerializeField] private GameObject[] objectsToEnableWhileControlling;
    [SerializeField] private GameObject[] objectsToDisableWhileControlling;

    private LighthouseDutyModel dutyModel;
    private bool activePhase;
    private bool controlling;
    private bool cachedCanMove;
    private bool cachedCanLook;
    private bool cachedCanRun;
    private bool cachedCanCrouch;
    private bool cachedCanJump;
    private bool cachedCursorLocked;
    private bool[] cachedComponentStates;
    private float currentPitch;
    private float activeSweepTime;
    private float idleTime;

    private void Start()
    {
        dutyModel = this.GetModel<LighthouseDutyModel>();

        if (dutyModel != null)
        {
            dutyModel.LightActivated.OnValueChanged += OnLightActivatedChanged;
        }

        this.GetEvent().Register<Day1NightDutyStartedEvent>(OnDay1NightDutyStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDestroy()
    {
        if (dutyModel != null)
        {
            dutyModel.LightActivated.OnValueChanged -= OnLightActivatedChanged;
        }

        if (controlling)
        {
            EndControl();
        }
    }

    private void Update()
    {
        if (!controlling)
        {
            return;
        }

        float yawInput = Input.GetAxis("Mouse X");
        float pitchInput = Input.GetAxis("Mouse Y");

        bool hasYawInput = Mathf.Abs(yawInput) > inputThreshold;
        bool hasPitchInput = beamPitchPivot != null && Mathf.Abs(pitchInput) > inputThreshold;
        bool hasMeaningfulInput = hasYawInput || hasPitchInput;

        if (hasYawInput && beamYawPivot != null)
        {
            beamYawPivot.Rotate(0f, yawInput * yawSpeed * Time.deltaTime, 0f, Space.Self);
        }

        if (hasPitchInput)
        {
            float pitchDelta = (invertVertical ? pitchInput : -pitchInput) * pitchSpeed * Time.deltaTime;
            currentPitch = Mathf.Clamp(currentPitch + pitchDelta, minPitch, maxPitch);
            beamPitchPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        if (hasMeaningfulInput)
        {
            activeSweepTime += Time.deltaTime;
            idleTime = 0f;
            return;
        }

        if (activeSweepTime < minimumActiveSweepDuration)
        {
            return;
        }

        idleTime += Time.deltaTime;
        if (idleTime >= idleToFinishDuration)
        {
            CompleteSweep();
        }
    }

    private void OnDay1NightDutyStarted(Day1NightDutyStartedEvent evt)
    {
        activePhase = true;

        if (controlling)
        {
            EndControl();
        }

        SetObjectsActive(objectsToEnableWhileControlling, false);
        SetObjectsActive(objectsToDisableWhileControlling, true);
    }

    private void OnLightActivatedChanged(bool activated)
    {
        if (!activePhase || !activated || dutyModel == null) return;
        if (dutyModel.BeamSweepCompleted.Value || controlling) return;

        BeginControl();
    }

    private void BeginControl()
    {
        if (playerController == null || beamYawPivot == null)
        {
            return;
        }

        CachePlayerState();
        ApplyControlState();

        activeSweepTime = 0f;
        idleTime = 0f;
        controlling = true;
        currentPitch = beamPitchPivot != null
            ? Mathf.Clamp(NormalizeSignedAngle(beamPitchPivot.localEulerAngles.x), minPitch, maxPitch)
            : 0f;

        SetObjectsActive(objectsToEnableWhileControlling, true);
        SetObjectsActive(objectsToDisableWhileControlling, false);
    }

    private void CompleteSweep()
    {
        if (dutyModel != null)
        {
            dutyModel.BeamSweepCompleted.Value = true;
        }

        activePhase = false;
        EndControl();
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
            MonoBehaviour component = additionalComponentsToLock[i];
            cachedComponentStates[i] = component != null && component.enabled;
        }
    }

    private void ApplyControlState()
    {
        if (playerController != null)
        {
            playerController.ClearMotionForCutscene();
            playerController.ForceStandUp();
            playerController.SetCanMove(false);
            playerController.SetCanLook(false);
            playerController.SetCanRun(false);
            playerController.SetCanCrouch(false);
            playerController.SetCanJump(false);
            playerController.SetCursorLocked(true);
        }

        SetAdditionalComponentsLocked(true);
    }

    private void EndControl()
    {
        if (!controlling)
        {
            return;
        }

        controlling = false;

        if (playerController != null)
        {
            playerController.SetCanMove(cachedCanMove);
            playerController.SetCanLook(cachedCanLook);
            playerController.SetCanRun(cachedCanRun);
            playerController.SetCanCrouch(cachedCanCrouch);
            playerController.SetCanJump(cachedCanJump);
            playerController.SetCursorLocked(cachedCursorLocked);
        }

        SetAdditionalComponentsLocked(false);
        SetObjectsActive(objectsToEnableWhileControlling, false);
        SetObjectsActive(objectsToDisableWhileControlling, true);
    }

    private void SetAdditionalComponentsLocked(bool locked)
    {
        if (additionalComponentsToLock == null) return;

        for (int i = 0; i < additionalComponentsToLock.Length; i++)
        {
            MonoBehaviour component = additionalComponentsToLock[i];
            if (component == null) continue;

            if (locked)
            {
                component.enabled = false;
            }
            else if (cachedComponentStates != null && i < cachedComponentStates.Length)
            {
                component.enabled = cachedComponentStates[i];
            }
        }
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

    private float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
    }
}
