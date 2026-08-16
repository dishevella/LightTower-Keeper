using UnityEngine;

[DefaultExecutionOrder(-9000)]
public sealed class GameModeSceneBridge : ControllerAbstract
{
    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractionController interactionController;
    [SerializeField] private bool lockLookDuringCutscene;

    [Header("Optional Scene Presentation")]
    [SerializeField] private GameObject[] gameplayUiRoots;
    [SerializeField] private MonoBehaviour[] ordinaryNpcBehaviours;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera cinematicCamera;

    private PlayerCapabilitySnapshot playerSnapshot;
    private bool snapshotValid;
    private bool[] cachedNpcStates;
    private bool[] cachedUiStates;
    private float timeScaleBeforePause = 1f;
    private bool pauseOwned;

    private struct PlayerCapabilitySnapshot
    {
        public bool canMove;
        public bool canLook;
        public bool canRun;
        public bool canCrouch;
        public bool canJump;
        public bool interactionEnabled;
    }

    private void Awake()
    {
        ResolveReferences();
        this.GetEvent().Register<GameModeChangedEvent>(OnGameModeChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
        ApplyMode(this.GetModel<GameModeModel>()?.CurrentMode.Value ?? GameMode.Boot);
    }

    private void OnDisable()
    {
        RestoreSnapshot();
        RestoreTimeScale();
    }

    private void OnGameModeChanged(GameModeChangedEvent evt)
    {
        ApplyMode(evt.NewMode);
    }

    private void ApplyMode(GameMode mode)
    {
        ResolveReferences();
        if (mode == GameMode.Gameplay)
        {
            RestoreSnapshot();
            RestoreTimeScale();
            SetCameraMode(false);
            return;
        }

        CaptureSnapshotIfNeeded();
        bool lockLook = mode != GameMode.Cutscene || lockLookDuringCutscene;
        ApplyPlayerLock(lockLook);
        ApplySceneLock();
        SetCameraMode(mode == GameMode.Cutscene);

        if (mode == GameMode.Paused && !pauseOwned)
        {
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            pauseOwned = true;
        }
        else if (mode != GameMode.Paused)
        {
            RestoreTimeScale();
        }
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        if (interactionController == null && playerController != null)
        {
            interactionController = playerController.GetComponentInChildren<PlayerInteractionController>(true);
        }
    }

    private void CaptureSnapshotIfNeeded()
    {
        if (snapshotValid)
        {
            return;
        }

        if (playerController != null)
        {
            playerSnapshot.canMove = playerController.CanMove;
            playerSnapshot.canLook = playerController.CanLook;
            playerSnapshot.canRun = playerController.CanRun;
            playerSnapshot.canCrouch = playerController.CanCrouch;
            playerSnapshot.canJump = playerController.CanJump;
        }

        playerSnapshot.interactionEnabled =
            interactionController == null || interactionController.InteractionEnabled;
        cachedNpcStates = CaptureEnabledStates(ordinaryNpcBehaviours);
        cachedUiStates = CaptureActiveStates(gameplayUiRoots);
        snapshotValid = true;
    }

    private void ApplyPlayerLock(bool lockLook)
    {
        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetCanRun(false);
            playerController.SetCanCrouch(false);
            playerController.SetCanJump(false);
            if (lockLook)
            {
                playerController.SetCanLook(false);
            }
        }

        interactionController?.SetInteractionEnabled(false);
    }

    private void ApplySceneLock()
    {
        SetEnabled(ordinaryNpcBehaviours, false);
        SetActive(gameplayUiRoots, false);
    }

    private void RestoreSnapshot()
    {
        if (!snapshotValid)
        {
            SetActive(gameplayUiRoots, true);
            return;
        }

        if (playerController != null)
        {
            playerController.SetCanMove(playerSnapshot.canMove);
            playerController.SetCanLook(playerSnapshot.canLook);
            playerController.SetCanRun(playerSnapshot.canRun);
            playerController.SetCanCrouch(playerSnapshot.canCrouch);
            playerController.SetCanJump(playerSnapshot.canJump);
        }

        interactionController?.SetInteractionEnabled(playerSnapshot.interactionEnabled);
        RestoreEnabledStates(ordinaryNpcBehaviours, cachedNpcStates);
        RestoreActiveStates(gameplayUiRoots, cachedUiStates);
        snapshotValid = false;
    }

    private void SetCameraMode(bool cinematic)
    {
        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = !cinematic;
        }

        if (cinematicCamera != null)
        {
            cinematicCamera.enabled = cinematic;
        }
    }

    private void RestoreTimeScale()
    {
        if (!pauseOwned) return;
        Time.timeScale = timeScaleBeforePause;
        pauseOwned = false;
    }

    private static bool[] CaptureEnabledStates(MonoBehaviour[] behaviours)
    {
        if (behaviours == null) return null;
        bool[] states = new bool[behaviours.Length];
        for (int i = 0; i < behaviours.Length; i++)
        {
            states[i] = behaviours[i] != null && behaviours[i].enabled;
        }

        return states;
    }

    private static bool[] CaptureActiveStates(GameObject[] objects)
    {
        if (objects == null) return null;
        bool[] states = new bool[objects.Length];
        for (int i = 0; i < objects.Length; i++)
        {
            states[i] = objects[i] != null && objects[i].activeSelf;
        }

        return states;
    }

    private static void SetEnabled(MonoBehaviour[] behaviours, bool enabled)
    {
        if (behaviours == null) return;
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null) behaviour.enabled = enabled;
        }
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject target in objects)
        {
            if (target != null) target.SetActive(active);
        }
    }

    private static void RestoreEnabledStates(MonoBehaviour[] behaviours, bool[] states)
    {
        if (behaviours == null || states == null) return;
        for (int i = 0; i < behaviours.Length && i < states.Length; i++)
        {
            if (behaviours[i] != null) behaviours[i].enabled = states[i];
        }
    }

    private static void RestoreActiveStates(GameObject[] objects, bool[] states)
    {
        if (objects == null || states == null) return;
        for (int i = 0; i < objects.Length && i < states.Length; i++)
        {
            if (objects[i] != null) objects[i].SetActive(states[i]);
        }
    }
}
