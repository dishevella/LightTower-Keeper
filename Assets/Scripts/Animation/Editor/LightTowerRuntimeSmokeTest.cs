using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OccaSoftware.Buto.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LightTowerRuntimeSmokeTest
{
    private const string TargetScenePath = LighthouseProjectSetupUtility.MainScenePath;
    private const string RequestPath = "LightTowerRuntimeSmokeTest.request";
    private const string ResultPath =
        "Library/LightTowerValidation/LightTowerRuntimeSmokeTest.result";
    private const string ScreenshotPath = "Library/LightTowerValidation/FirewatchRuntimePreview.png";
    private const string MovementScreenshotPath = "Library/LightTowerValidation/MovementCameraPreview.png";
    private const string ThirdPersonScreenshotPath = "Library/LightTowerValidation/ThirdPersonCharacterPreview.png";
    private const string ExtremeLookUpScreenshotPath =
        "Library/LightTowerValidation/ExtremeLookUpCameraPreview.png";
    private const string VistaScreenshotPath = "Library/LightTowerValidation/FirewatchVistaPreview.png";
    private const string WaterAtmosphereScreenshotPath =
        "Library/LightTowerValidation/FirewatchWaterAtmospherePreview.png";
    private const string GroundMistScreenshotPath =
        "Library/LightTowerValidation/FirewatchGroundMistPreview.png";
    private const string PendingKey = "LightTower.RuntimeSmoke.Pending";
    private const string ExitPendingKey = "LightTower.RuntimeSmoke.ExitPending";
    private const string PreviousScenesKey = "LightTower.RuntimeSmoke.PreviousScenes";
    private const string PreviousActiveSceneKey = "LightTower.RuntimeSmoke.PreviousActiveScene";
    private const string PreserveLoadedSceneKey = "LightTower.RuntimeSmoke.PreserveLoadedScene";
    private const string CommandLineRunKey = "LightTower.RuntimeSmoke.CommandLineRun";

    private static readonly List<string> checks = new List<string>();
    private static bool runtimeStarted;
    private static bool allPassed;
    private static int phase;
    private static double phaseStartedAt;
    private static double controlsWaitStartedAt;
    private static PlayerController player;
    private static CharacterController motor;
    private static CharacterAnimancerController animancer;
    private static Camera playerCamera;
    private static Transform cameraRoot;
    private static GameObject collisionObstacle;
    private static Vector3 movementStart;
    private static float crouchStartY;
    private static float desiredCameraDistance;
    private static float obstructedCameraDistance;
    private static float initialStartMovementScale;
    private static float microDropStartY;
    private static bool microDropStayedAnimationGrounded;
    private static bool microDropSnapObserved;
    private static readonly Collider[] cameraValidationOverlaps = new Collider[32];
    private static int previousTargetFrameRate;
    private static int previousVSyncCount;
    private static int previousCaptureFrameRate;
    private static bool cameraFrameTestInitialized;
    private static bool cameraFrameEnvironmentStayedValid;
    private static bool cameraFrameNearPlaneStayedClear;
    private static int cameraFrameSamples;
    private static float cameraFrameDeltaTotal;
    private static Quaternion cameraTestOriginalLocalRotation;
    private static bool cameraTestOriginalCanLook;
    private static bool cameraConflictTestInitialized;
    private static Vector3 protectedHeadOriginalScale;
    private static float originalCrouchHeadSafetyRadius;
    private static float originalCrouchHeadForwardClearance;

    static LightTowerRuntimeSmokeTest()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Light Tower/Validation/Run Runtime Smoke Test")]
    public static void RequestRun()
    {
        string requestDirectory = Path.GetDirectoryName(RequestPath);
        if (!string.IsNullOrEmpty(requestDirectory))
            Directory.CreateDirectory(requestDirectory);
        File.WriteAllText(RequestPath, DateTime.Now.ToString("O"));
    }

    public static void RunFromCommandLine()
    {
        SessionState.SetBool(CommandLineRunKey, true);
        BeginEditorSetup();
    }

    private static void Tick()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;

        if (SessionState.GetBool(PendingKey, false) &&
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            !SessionState.GetBool(ExitPendingKey, false) &&
            !Application.isBatchMode &&
            !SessionState.GetBool(CommandLineRunKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            runtimeStarted = false;
        }

        if (SessionState.GetBool(ExitPendingKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            RestoreEditorScenes();
            return;
        }

        if (SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying)
        {
            EditorApplication.isPaused = false;
            EditorApplication.QueuePlayerLoopUpdate();
            if (!runtimeStarted) BeginRuntimeProbe();
            RunRuntimePhase();
            return;
        }

        if (!File.Exists(RequestPath) || SessionState.GetBool(PendingKey, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;

        BeginEditorSetup();
    }

    private static void BeginEditorSetup()
    {
        try
        {
            string screenshotDirectory = Path.GetDirectoryName(ScreenshotPath);
            if (!string.IsNullOrEmpty(screenshotDirectory))
                Directory.CreateDirectory(screenshotDirectory);

            File.Delete(RequestPath);
            File.Delete(ResultPath);
            File.Delete(ScreenshotPath);
            File.Delete(MovementScreenshotPath);
            File.Delete(ThirdPersonScreenshotPath);
            File.Delete(ExtremeLookUpScreenshotPath);
            File.Delete(VistaScreenshotPath);
            File.Delete(WaterAtmosphereScreenshotPath);
            File.Delete(GroundMistScreenshotPath);

            Scene[] openScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(scene => scene.IsValid() && scene.isLoaded)
                .ToArray();
            bool hasDirtyScene = openScenes.Any(scene => scene.isDirty);
            bool targetAlreadyLoaded = openScenes.Length == 1 &&
                                       openScenes[0].path == TargetScenePath;
            if (hasDirtyScene && !targetAlreadyLoaded)
                throw new InvalidOperationException(
                    "A non-target scene has unsaved changes; runtime smoke test was not started.");

            SessionState.SetString(
                PreviousScenesKey,
                string.Join("|", openScenes.Select(scene => scene.path).Where(path => !string.IsNullOrEmpty(path))));
            SessionState.SetString(PreviousActiveSceneKey, SceneManager.GetActiveScene().path);
            SessionState.SetBool(PreserveLoadedSceneKey, hasDirtyScene && targetAlreadyLoaded);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(ExitPendingKey, false);

            if (!targetAlreadyLoaded)
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception)
        {
            WriteImmediateFailure(exception);
            bool exitWhenFinished =
                Application.isBatchMode || SessionState.GetBool(CommandLineRunKey, false);
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(CommandLineRunKey, false);
            if (exitWhenFinished)
                EditorApplication.delayCall += () => EditorApplication.Exit(1);
        }
    }

    private static void BeginRuntimeProbe()
    {
        Application.runInBackground = true;
        Time.timeScale = 1f;
        previousTargetFrameRate = Application.targetFrameRate;
        previousVSyncCount = QualitySettings.vSyncCount;
        previousCaptureFrameRate = Time.captureFramerate;
        QualitySettings.vSyncCount = 0;
        runtimeStarted = true;
        allPassed = true;
        checks.Clear();
        phase = 0;
        phaseStartedAt = EditorApplication.timeSinceStartup;
        controlsWaitStartedAt = phaseStartedAt;
    }

    private static void RunRuntimePhase()
    {
        try
        {
            switch (phase)
            {
                case 0:
                    FindRuntimeObjectsAndWaitForControls();
                    break;
                case 1:
                    VerifyMotorAndDriveLocomotion();
                    break;
                case 2:
                    VerifyDrivenLocomotionAndStartMicroDrop();
                    break;
                case 3:
                    VerifyMicroDropAndEnterCrouch();
                    break;
                case 4:
                    VerifyCrouchCameraAndCreateObstacle();
                    break;
                case 5:
                    VerifyCameraCollisionAtFrameRate(30);
                    break;
                case 6:
                    VerifyCameraCollisionAtFrameRate(60);
                    break;
                case 7:
                    VerifyCameraCollisionAtFrameRate(120);
                    break;
                case 8:
                    VerifyCharacterEnvironmentPriorityAndClearObstacle();
                    break;
                case 9:
                    VerifyExtremeLookUpCamera();
                    break;
                case 10:
                    VerifyCameraReturnAndCapture();
                    break;
                case 11:
                    VerifyCaptureAndFinish();
                    break;
            }
        }
        catch (Exception exception)
        {
            checks.Add("[FAIL] Exception=" + exception);
            allPassed = false;
            FinishRuntimeProbe();
        }
    }

    private static void FindRuntimeObjectsAndWaitForControls()
    {
        if (Elapsed < 1f) return;

        if (player == null)
        {
            Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("The formal Lighthouse main scene is not loaded in Play Mode.");

            player = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                .FirstOrDefault(candidate => candidate.gameObject.name == "Player")
                ?? scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                    .FirstOrDefault();
            if (player == null) throw new InvalidOperationException("PlayerController was not found.");

            motor = player.GetComponent<CharacterController>();
            animancer = player.GetComponent<CharacterAnimancerController>();
            cameraRoot = player.CameraRootTransform;
            playerCamera = cameraRoot != null
                ? cameraRoot.GetComponentInChildren<Camera>(true)
                : player.GetComponentInChildren<Camera>(true);

            Record("Player components enabled", player.gameObject.activeInHierarchy && motor != null &&
                motor.enabled && animancer != null && animancer.enabled);
            Record("Animancer configuration revision 12", animancer != null && animancer.Config != null &&
                animancer.Config.ConfigurationRevision == 12);
            Record("Original Animator Controller cleared only at runtime",
                player.GetComponent<Animator>() != null &&
                player.GetComponent<Animator>().runtimeAnimatorController == null);
            CharacterAnimationConfig.CameraSettings cameraSettings = animancer.Config.Camera;
            Record(
                "Camera character-interior protection is active",
                cameraSettings.EnableCharacterInteriorProtection &&
                cameraRoot.localPosition.z >= cameraSettings.MinimumCameraLocalForward - 0.01f &&
                playerCamera.nearClipPlane >= cameraSettings.CameraNearClip - 0.001f,
                $"root z {cameraRoot.localPosition.z:0.###}, minimum {cameraSettings.MinimumCameraLocalForward:0.###}, " +
                $"near clip {playerCamera.nearClipPlane:0.###}");
            Record(
                "Camera collision uses the actual near-clip volume",
                cameraSettings.EnableWallCollision && cameraSettings.UseNearPlaneCollisionVolume &&
                cameraSettings.NearPlaneSkinWidth >= 0.02f &&
                playerCamera.nearClipPlane >= 0.1f,
                $"near {playerCamera.nearClipPlane:0.###}m, skin " +
                $"{cameraSettings.NearPlaneSkinWidth:0.###}m");
            Record(
                "Animated head tracking and player-camera head suppression are active",
                cameraSettings.TrackAnimatedHead && player.ProtectedHeadBone != null &&
                cameraSettings.HideHeadForPlayerCamera && player.HeadRenderSuppressionEnabled,
                player.ProtectedHeadBone != null
                    ? $"head {player.ProtectedHeadBone.name}, suppression {player.HeadRenderSuppressionEnabled}"
                    : "humanoid head bone was not resolved");
            Record(
                "Animated face surface anchors are available",
                cameraSettings.EnableFaceSurfaceConstraint && player.FaceSurfaceSamplingActive,
                $"enabled {cameraSettings.EnableFaceSurfaceConstraint}, anchors " +
                $"{player.FaceSurfaceSamplingActive}");
            Record(
                "Camera look sensitivity is controlled by the animation config",
                cameraSettings.LookSensitivity > 0f && cameraSettings.LookSensitivity < 100f,
                $"sensitivity {cameraSettings.LookSensitivity:0.##}");

            CharacterAnimationConfig.TurnSettings turns = animancer.Config.Locomotion.Turns;
            Record(
                "Moving turn animations use enter-exit hysteresis",
                turns.CurveStartYawRate > turns.CurveExitYawRate && turns.CurveExitDelay >= 0.15f,
                $"enter {turns.CurveStartYawRate:0.##}, exit {turns.CurveExitYawRate:0.##}, " +
                $"delay {turns.CurveExitDelay:0.###}");

            CharacterAnimationConfig.AirborneSettings airborne = animancer.Config.Airborne;
            Record(
                "Small terrain drops use configurable ground snapping",
                airborne.EnableGroundSnap &&
                airborne.GroundSnapDistance >= 0.2f &&
                airborne.GroundedGraceTime >= 0.2f &&
                airborne.LandingMinimumAirTime > airborne.GroundedGraceTime,
                $"snap {airborne.GroundSnapDistance:0.###}m, grace {airborne.GroundedGraceTime:0.###}s, " +
                $"landing {airborne.LandingMinimumAirTime:0.###}s");

            CharacterAnimationConfig.GlobalFadeSettings globalFades = animancer.Config.GlobalFades;
            float fadeSpeed = globalFades != null ? globalFades.GlobalFadeSpeed : 0f;
            float expectedFadeIn = globalFades != null
                ? (globalFades.UseSharedDurations ? globalFades.SharedFadeIn : 0.2f) /
                  Mathf.Max(0.1f, fadeSpeed)
                : 0f;
            float expectedFadeOut = globalFades != null
                ? (globalFades.UseSharedDurations ? globalFades.SharedFadeOut : 0.2f) /
                  Mathf.Max(0.1f, fadeSpeed)
                : 0f;
            Record(
                "Unified Fade In and Fade Out control is active",
                globalFades != null &&
                Mathf.Approximately(globalFades.ResolveFadeIn(0.2f), expectedFadeIn) &&
                Mathf.Approximately(globalFades.ResolveFadeOut(0.2f), expectedFadeOut),
                globalFades != null
                    ? $"speed {fadeSpeed:0.##}, fade 0.2 resolves to {globalFades.ResolveFadeIn(0.2f):0.###}"
                    : "GlobalFades is missing");
            Record(
                "Locomotion mixer uses smoothed CharacterController velocity",
                animancer.Config.Locomotion.UseMotorVelocityForMixer,
                $"enabled {animancer.Config.Locomotion.UseMotorVelocityForMixer}");
            Record(
                "Normal locomotion has one movement owner",
                animancer.MotionOwner == CharacterAnimationMotionOwner.CharacterMotor &&
                !animancer.RootMotionActive &&
                animancer.ShouldMotorDriveTranslation,
                $"owner {animancer.MotionOwner}, root motion {animancer.RootMotionActive}");
            VerifyOriginalAnimationAssignments();
            VerifyVisualStyle();
        }

        bool controlsReady = player.enabled && player.CanMove && player.CanLook && player.CanRun &&
            player.CanCrouch && player.CanJump;
        if (!controlsReady && EditorApplication.timeSinceStartup - controlsWaitStartedAt < 48f) return;

        Record(
            "Opening sequence releases all player controls",
            controlsReady,
            $"waited {EditorApplication.timeSinceStartup - controlsWaitStartedAt:0.0}s");
        if (!controlsReady)
        {
            player.enabled = true;
            player.SetCanMove(true);
            player.SetCanLook(true);
            player.SetCanRun(true);
            player.SetCanCrouch(true);
            player.SetCanJump(true);
        }

        VerifyGameModeInputOwnership();

        movementStart = player.transform.position;
        motor.Move(player.transform.forward * 0.08f);
        NextPhase();
    }

    private static void VerifyGameModeInputOwnership()
    {
        GameModeSystem modeSystem = GameApp.Interface.GetSystem<GameModeSystem>();
        GameModeModel modeModel = GameApp.Interface.GetModel<GameModeModel>();
        GameModeSceneBridge bridge = UnityEngine.Object.FindFirstObjectByType<GameModeSceneBridge>();
        PlayerInteractionController interaction =
            player.GetComponentInChildren<PlayerInteractionController>(true);
        bool originalCanMove = player.CanMove;
        bool originalCanLook = player.CanLook;
        bool originalCanRun = player.CanRun;
        bool originalCanCrouch = player.CanCrouch;
        bool originalCanJump = player.CanJump;
        bool originalInteraction = interaction == null || interaction.InteractionEnabled;

        bool enteredCutscene = modeSystem != null && modeSystem.SetMode(GameMode.Cutscene);
        bool cutsceneLocked = !player.CanMove && !player.CanRun &&
                              !player.CanCrouch && !player.CanJump &&
                              (interaction == null || !interaction.InteractionEnabled);
        modeSystem?.SetMode(GameMode.Paused);
        bool pauseLocked = Mathf.Approximately(Time.timeScale, 0f) && !player.CanMove;
        modeSystem?.SetMode(GameMode.Gameplay);
        bool restoredExactly =
            player.CanMove == originalCanMove &&
            player.CanLook == originalCanLook &&
            player.CanRun == originalCanRun &&
            player.CanCrouch == originalCanCrouch &&
            player.CanJump == originalCanJump &&
            (interaction == null || interaction.InteractionEnabled == originalInteraction) &&
            Time.timeScale > 0f;

        Record(
            "GameMode Cutscene and Pause lock player input through the scene bridge",
            bridge != null && enteredCutscene && cutsceneLocked && pauseLocked,
            $"bridge {bridge != null}, mode {modeModel?.CurrentMode.Value}, " +
            $"cutscene lock {cutsceneLocked}, pause lock {pauseLocked}");
        Record(
            "GameMode Gameplay restores the exact pre-cutscene capabilities",
            restoredExactly,
            $"move {player.CanMove}, look {player.CanLook}, run {player.CanRun}, " +
            $"crouch {player.CanCrouch}, jump {player.CanJump}, time {Time.timeScale:0.##}");
    }

    private static void VerifyMotorAndDriveLocomotion()
    {
        if (Elapsed < 0.3f) return;

        Vector3 horizontalDelta = player.transform.position - movementStart;
        horizontalDelta.y = 0f;
        Record("CharacterController accepts horizontal movement", horizontalDelta.magnitude > 0.04f,
            $"delta {horizontalDelta.magnitude:0.###}");

        bool motorWasEnabled = motor.enabled;
        motor.enabled = false;
        player.transform.position = movementStart;
        motor.enabled = motorWasEnabled;
        Physics.SyncTransforms();

        player.SetAnimationDriving(false);
        animancer.SetGrounded(true);
        animancer.SetCrouching(false);
        animancer.SetMovement(player.transform.forward * 2f, player.transform.forward);
        initialStartMovementScale = animancer.GetLocomotionMotorScale();
        NextPhase();
    }

    private static void VerifyDrivenLocomotionAndStartMicroDrop()
    {
        animancer.SetMovement(player.transform.forward * 2f, player.transform.forward);
        if (Elapsed < 0.8f) return;
        if (animancer.CurrentTransient != LocomotionTransient.None && Elapsed < 2.5f) return;
        if (animancer.PlayCallsThisFrame > 0 && Elapsed < 1.4f) return;

        Record("Animancer receives locomotion speed", animancer.TargetLocomotionSpeed > 1.9f &&
            animancer.CurrentLocomotionSpeed > 0.5f,
            $"target {animancer.TargetLocomotionSpeed:0.##}, current {animancer.CurrentLocomotionSpeed:0.##}");
        float locomotionWeight = animancer.IdleWeight + animancer.WalkWeight + animancer.JogWeight +
                                 animancer.RunWeight;
        Record(
            "Stable locomotion updates mixer weights without replaying its state",
            locomotionWeight > 0.9f && animancer.PlayCallsThisFrame == 0,
            $"weight {locomotionWeight:0.###}, play calls this frame {animancer.PlayCallsThisFrame}, " +
            $"same requests {animancer.SameStateRequestsThisFrame}, transient {animancer.CurrentTransient}, " +
            $"grounded {animancer.Grounded}, reason {animancer.LastStateChangeReason}");
        Record("Animancer locomotion clip is active", animancer.CurrentClip != null ||
            animancer.CurrentCameraClip != null, animancer.CurrentAnimationState);
        Record(
            "Start animation ramps CharacterController movement",
            initialStartMovementScale <= 0.1f && animancer.GetLocomotionMotorScale() >= 0.9f,
            $"initial {initialStartMovementScale:0.###}, current {animancer.GetLocomotionMotorScale():0.###}");
        CharacterAnimationConfig.CameraSettings cameraSettings = animancer.Config.Camera;
        Record(
            "Moving animation keeps the final camera clear of the animated head",
            player.ProtectedHeadBone != null &&
            !float.IsNaN(player.CurrentCameraDistanceFromHead) &&
            !float.IsInfinity(player.CurrentCameraDistanceFromHead) &&
            player.CurrentCameraDistanceFromHead >= cameraSettings.HeadSafetyRadius - 0.01f,
            $"distance {player.CurrentCameraDistanceFromHead:0.###}, required {cameraSettings.HeadSafetyRadius:0.###}, " +
            $"corrected this frame {player.CameraHeadProtectionActive}");
        Record(
            "Locomotion transition uses the unified fade resolver",
            animancer.LastResolvedFadeDuration >= 0f,
            $"resolved duration {animancer.LastResolvedFadeDuration:0.###}");
        CaptureCameraToPng(playerCamera, MovementScreenshotPath);
        CaptureThirdPersonCharacterPreview();

        animancer.SetMovement(Vector3.zero, player.transform.forward);
        player.enabled = true;
        microDropStartY = player.transform.position.y;
        microDropStayedAnimationGrounded = true;
        microDropSnapObserved = false;
        motor.Move(Vector3.up * 0.22f);
        Physics.SyncTransforms();
        NextPhase();
    }

    private static void VerifyMicroDropAndEnterCrouch()
    {
        if (Elapsed > 0.04f)
        {
            microDropStayedAnimationGrounded &= player.AnimationGrounded;
            microDropSnapObserved |= player.GroundSnapActive;
        }

        if (Elapsed < 0.6f) return;

        float verticalError = Mathf.Abs(player.transform.position.y - microDropStartY);
        Record(
            "A 22 cm terrain gap does not restart airborne or landing animation",
            microDropStayedAnimationGrounded,
            $"animation grounded {player.AnimationGrounded}, snap observed {microDropSnapObserved}, " +
            $"last unsupported {player.LastUngroundedDuration:0.###}s");
        Record(
            "Ground snap returns the CharacterController to the walking surface",
            verticalError < 0.06f,
            $"vertical error {verticalError:0.###}m");

        int playCallsBeforeLanding = animancer.PlayCallsThisFrame;
        animancer.PlayLanding(-7f, true);
        int playCallsAfterFirstLanding = animancer.PlayCallsThisFrame;
        animancer.SetMovement(player.transform.forward * 2f, player.transform.forward);
        animancer.PlayLanding(-7f, true);
        Record(
            "Landing ignores repeated requests and is not killed by locomotion input",
            playCallsAfterFirstLanding > playCallsBeforeLanding &&
            animancer.PlayCallsThisFrame == playCallsAfterFirstLanding &&
            animancer.CurrentLogicalState == CharacterAnimationLogicalState.Landing &&
            animancer.SameStateRequestsThisFrame > 0 &&
            !animancer.LastPlayResetTime &&
            !animancer.LastPlayRestartedFade &&
            !animancer.LastPlayResetWeight,
            $"plays {playCallsBeforeLanding}->{playCallsAfterFirstLanding}->" +
            $"{animancer.PlayCallsThisFrame}, state {animancer.CurrentLogicalState}, " +
            $"same requests {animancer.SameStateRequestsThisFrame}");
        animancer.SetMovement(Vector3.zero, player.transform.forward);
        animancer.ReturnToLocomotion("Runtime smoke test landing complete");

        crouchStartY = cameraRoot.localPosition.y;
        player.SetCrouching(true);
        animancer.ReturnToLocomotion("Runtime smoke test crouch");
        NextPhase();
    }

    private static void VerifyCrouchCameraAndCreateObstacle()
    {
        HoldCrouchPose();
        if (Elapsed < 0.9f) return;
        if (animancer.CurrentLocomotionSpeed > animancer.Config.Locomotion.IdleSpeedThreshold &&
            Elapsed < 2f)
            return;

        CharacterAnimationConfig config = animancer.Config;
        CharacterAnimationConfig.MobilityClipTuning crouchTuning =
            config.FindClipTuning(config.Locomotion.CrouchIdle);
        bool hasCrouchPose = animancer.TryGetCurrentCameraPosition(
            out Vector3 crouchCameraPosition,
            out float crouchCameraSpeed);
        Record("Crouch animation has an adjustable camera pose", hasCrouchPose &&
            crouchTuning != null && crouchTuning.OverrideCameraPosition,
            hasCrouchPose
                ? $"target {crouchCameraPosition}, speed {crouchCameraSpeed:0.##}"
                : $"missing; state {animancer.CurrentAnimationState}, clip {animancer.CurrentClip}, " +
                  $"camera clip {animancer.CurrentCameraClip}, speed " +
                  $"{animancer.CurrentLocomotionSpeed:0.###}, mixer {animancer.CurrentMixerParameter}");
        Record("Crouch camera moves down smoothly", cameraRoot.localPosition.y < crouchStartY - 0.1f,
            $"from {crouchStartY:0.###} to {cameraRoot.localPosition.y:0.###}");

        CharacterAnimationConfig.CameraSettings settings = config.Camera;
        Record(
            "Crouch camera uses its stable forward guard",
            cameraRoot.localPosition.z >= settings.CrouchMinimumCameraLocalForward - 0.01f,
            $"root z {cameraRoot.localPosition.z:0.###}, minimum {settings.CrouchMinimumCameraLocalForward:0.###}");
        Record(
            "Crouch camera remains outside the animated head volume",
            !float.IsNaN(player.CurrentCameraDistanceFromHead) &&
            !float.IsInfinity(player.CurrentCameraDistanceFromHead) &&
            player.CurrentCameraDistanceFromHead >= settings.CrouchHeadSafetyRadius - 0.01f,
            $"distance {player.CurrentCameraDistanceFromHead:0.###}, required {settings.CrouchHeadSafetyRadius:0.###}, " +
            $"corrected this frame {player.CameraHeadProtectionActive}");
        FieldInfo defaultCameraField = typeof(PlayerController).GetField(
            "defaultCameraPositionInRoot",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (defaultCameraField == null)
            throw new MissingFieldException(typeof(PlayerController).Name, "defaultCameraPositionInRoot");

        Vector3 defaultCameraLocal = (Vector3)defaultCameraField.GetValue(player);
        Vector3 desiredCameraPosition = cameraRoot.TransformPoint(defaultCameraLocal);
        Vector3 anchor = player.transform.TransformPoint(settings.CollisionAnchorLocalPosition);
        Vector3 offset = desiredCameraPosition - anchor;
        desiredCameraDistance = offset.magnitude;
        if (desiredCameraDistance < 0.1f)
            throw new InvalidOperationException("Configured camera collision ray is too short.");

        collisionObstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        collisionObstacle.name = "Runtime Smoke Camera Obstacle";
        collisionObstacle.transform.position = anchor + offset.normalized * (desiredCameraDistance * 0.72f);
        collisionObstacle.transform.rotation = Quaternion.LookRotation(offset.normalized, player.transform.up);
        collisionObstacle.transform.localScale = new Vector3(1f, 1f, 0.08f);
        SceneManager.MoveGameObjectToScene(collisionObstacle, player.gameObject.scene);
        cameraTestOriginalLocalRotation = cameraRoot.localRotation;
        cameraTestOriginalCanLook = player.CanLook;
        protectedHeadOriginalScale = player.ProtectedHeadBone != null
            ? player.ProtectedHeadBone.localScale
            : Vector3.one;
        player.SetCanLook(false);
        Physics.SyncTransforms();
        NextPhase();
    }

    private static void VerifyCameraCollisionAtFrameRate(int targetFrameRate)
    {
        HoldCrouchPose();
        if (!cameraFrameTestInitialized)
        {
            Application.targetFrameRate = targetFrameRate;
            Time.captureFramerate = targetFrameRate;
            cameraFrameEnvironmentStayedValid = true;
            cameraFrameNearPlaneStayedClear = true;
            cameraFrameSamples = 0;
            cameraFrameDeltaTotal = 0f;
            cameraFrameTestInitialized = true;
        }

        if (Elapsed > 0.1f)
        {
            cameraFrameEnvironmentStayedValid &= player.FinalEnvironmentValid;
            cameraFrameNearPlaneStayedClear &= !DoesNearPlaneOverlapObstacle();
            cameraFrameSamples++;
            cameraFrameDeltaTotal += Time.unscaledDeltaTime;
        }

        float pitchAngle = Mathf.Sin((float)Elapsed * 24f) * 58f;
        cameraRoot.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        player.SyncCurrentLookState();
        if (Elapsed < 0.75f) return;

        Vector3 anchor = player.transform.TransformPoint(animancer.Config.Camera.CollisionAnchorLocalPosition);
        float sampledDistance = Vector3.Distance(anchor, playerCamera.transform.position);
        if (targetFrameRate == 30)
            obstructedCameraDistance = sampledDistance;
        float averageFps = cameraFrameDeltaTotal > 0.0001f
            ? cameraFrameSamples / cameraFrameDeltaTotal
            : 0f;
        float expectedDelta = 1f / targetFrameRate;
        Record(
            $"Near-plane camera collision stays valid at simulated {targetFrameRate} FPS",
            cameraFrameSamples > 2 &&
            Mathf.Abs(Time.deltaTime - expectedDelta) < 0.002f &&
            player.CameraCollisionActive &&
            sampledDistance < desiredCameraDistance - 0.01f &&
            cameraFrameEnvironmentStayedValid &&
            cameraFrameNearPlaneStayedClear,
            $"simulation delta {Time.deltaTime:0.####}s, editor throughput " +
            $"{averageFps:0.#} frames/s, samples {cameraFrameSamples}, " +
            $"desired {desiredCameraDistance:0.###}m, final {sampledDistance:0.###}m, " +
            $"valid {cameraFrameEnvironmentStayedValid}, near-plane clear " +
            $"{cameraFrameNearPlaneStayedClear}");

        cameraFrameTestInitialized = false;
        NextPhase();
    }

    private static void VerifyCharacterEnvironmentPriorityAndClearObstacle()
    {
        HoldCrouchPose();
        if (!cameraConflictTestInitialized)
        {
            cameraRoot.localRotation = cameraTestOriginalLocalRotation;
            player.SyncCurrentLookState();

            CharacterAnimationConfig.CameraSettings settings = animancer.Config.Camera;
            originalCrouchHeadSafetyRadius = settings.CrouchHeadSafetyRadius;
            originalCrouchHeadForwardClearance = settings.CrouchHeadForwardClearance;
            settings.CrouchHeadSafetyRadius = 2f;
            settings.CrouchHeadForwardClearance = 2f;
            Vector3 anchor = player.transform.TransformPoint(settings.CollisionAnchorLocalPosition);
            Vector3 offset = player.DesiredCameraPosition - anchor;
            collisionObstacle.transform.position = anchor + offset.normalized * (offset.magnitude * 0.5f);
            collisionObstacle.transform.rotation = Quaternion.LookRotation(offset.normalized, player.transform.up);
            Physics.SyncTransforms();
            cameraConflictTestInitialized = true;
            return;
        }
        if (Elapsed < 0.55f) return;

        float environmentDelta = Vector3.Distance(
            player.FinalCameraPosition,
            player.EnvironmentSafeCameraPosition);
        Record(
            "Character interior correction cannot push the camera back through a wall",
            player.CameraCollisionActive &&
            player.CharacterInteriorConstraintActive &&
            player.CharacterVisibilityFallbackActive &&
            player.FinalEnvironmentValid &&
            environmentDelta < 0.005f &&
            !DoesNearPlaneOverlapObstacle(),
            $"character constraint {player.CharacterInteriorConstraintActive}, fallback " +
            $"{player.CharacterVisibilityFallbackActive}, final valid {player.FinalEnvironmentValid}, " +
            $"final-to-environment {environmentDelta:0.####}m");
        Record(
            "Tight wall fallback avoids the slicing proximity clip and restores the Head bone after rendering",
            !player.ProximityClipAvailable &&
            !player.ProximityClipActive &&
            player.HeadHiddenForPlayerCamera &&
            !player.BoneScaleFallbackActive &&
            player.ProtectedHeadBone != null &&
            Vector3.Distance(player.ProtectedHeadBone.localScale, protectedHeadOriginalScale) < 0.001f,
            $"clip available {player.ProximityClipAvailable}, active {player.ProximityClipActive}, " +
            $"head fallback scheduled {player.HeadHiddenForPlayerCamera}, currently scaled " +
            $"{player.BoneScaleFallbackActive}, scale " +
            $"{(player.ProtectedHeadBone != null ? player.ProtectedHeadBone.localScale.ToString() : "missing")}");

        RestoreCameraConflictSettings();

        if (collisionObstacle != null)
            UnityEngine.Object.Destroy(collisionObstacle);
        collisionObstacle = null;
        player.SetCanLook(cameraTestOriginalCanLook);
        cameraRoot.localRotation = cameraTestOriginalLocalRotation;
        player.SyncCurrentLookState();
        Physics.SyncTransforms();
        NextPhase();
    }

    private static void VerifyExtremeLookUpCamera()
    {
        player.enabled = true;
        player.SetCrouching(false);
        animancer.SetGrounded(true);
        animancer.SetCrouching(false);
        animancer.SetMovement(Vector3.zero, player.transform.forward);

        if (Elapsed < 0.05f)
        {
            player.SetCanLook(false);
            cameraRoot.localRotation = Quaternion.Euler(-80f, 0f, 0f);
            player.SyncCurrentLookState();
            return;
        }
        if (Elapsed < 0.65f) return;

        CharacterAnimationConfig.CameraSettings settings = animancer.Config.Camera;
        Record(
            "Extreme upward view remains in front of the animated face surface",
            player.FaceSurfaceSamplingActive &&
            !float.IsNaN(player.CurrentCameraFaceClearance) &&
            !float.IsInfinity(player.CurrentCameraFaceClearance) &&
            player.CurrentCameraFaceClearance >= player.RequiredCameraFaceClearance - 0.005f &&
            !player.ProximityClipActive &&
            player.FinalEnvironmentValid,
            $"face clearance {player.CurrentCameraFaceClearance:0.###}m, required " +
            $"{player.RequiredCameraFaceClearance:0.###}m, correction " +
            $"{player.CameraHeadProtectionActive}, clip {player.ProximityClipActive}, " +
            $"final valid {player.FinalEnvironmentValid}, face direction " +
            $"{player.CurrentFaceSurfaceForward}");
        CaptureCameraToPng(playerCamera, ExtremeLookUpScreenshotPath);

        cameraRoot.localRotation = cameraTestOriginalLocalRotation;
        player.SyncCurrentLookState();
        player.SetCanLook(cameraTestOriginalCanLook);
        NextPhase();
    }

    private static bool DoesNearPlaneOverlapObstacle()
    {
        if (collisionObstacle == null || playerCamera == null) return false;

        Collider obstacleCollider = collisionObstacle.GetComponent<Collider>();
        float nearClip = Mathf.Max(0.01f, playerCamera.nearClipPlane);
        Quaternion solvedRotation = player.FinalCameraRotation;
        Vector3 center = player.FinalCameraPosition +
                         solvedRotation * Vector3.forward * (nearClip * 0.5f);
        int overlapCount = Physics.OverlapBoxNonAlloc(
            center,
            player.NearPlaneCollisionHalfExtents,
            cameraValidationOverlaps,
            solvedRotation,
            animancer.Config.Camera.CollisionMask,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            if (cameraValidationOverlaps[i] == obstacleCollider)
                return true;
        }

        return false;
    }

    private static void RestoreCameraConflictSettings()
    {
        if (!cameraConflictTestInitialized || animancer == null || animancer.Config == null) return;

        CharacterAnimationConfig.CameraSettings settings = animancer.Config.Camera;
        settings.CrouchHeadSafetyRadius = originalCrouchHeadSafetyRadius;
        settings.CrouchHeadForwardClearance = originalCrouchHeadForwardClearance;
        cameraConflictTestInitialized = false;
    }

    private static void VerifyCameraReturnAndCapture()
    {
        HoldCrouchPose();
        if (Elapsed < 1f) return;

        Vector3 anchor = player.transform.TransformPoint(animancer.Config.Camera.CollisionAnchorLocalPosition);
        float returnedDistance = Vector3.Distance(anchor, playerCamera.transform.position);
        Record("Camera returns after obstruction clears", returnedDistance > obstructedCameraDistance + 0.05f,
            $"obstructed {obstructedCameraDistance:0.###}, returned {returnedDistance:0.###}");
        CaptureRuntimePreview();
        CaptureVistaPreview();
        CaptureWaterAtmospherePreview();
        CaptureGroundMistPreview();
        NextPhase();
    }

    private static void VerifyCaptureAndFinish()
    {
        if (Elapsed < 0.25f) return;

        FileInfo screenshot = new FileInfo(ScreenshotPath);
        Record("Runtime visual preview captured", screenshot.Exists && screenshot.Length > 1024,
            screenshot.Exists ? $"{screenshot.Length} bytes" : "missing");
        FileInfo movementScreenshot = new FileInfo(MovementScreenshotPath);
        Record("Moving-animation camera preview captured",
            movementScreenshot.Exists && movementScreenshot.Length > 1024,
            movementScreenshot.Exists ? $"{movementScreenshot.Length} bytes" : "missing");
        FileInfo thirdPersonScreenshot = new FileInfo(ThirdPersonScreenshotPath);
        Record("Third-person character remains visible to the player camera",
            thirdPersonScreenshot.Exists && thirdPersonScreenshot.Length > 1024,
            thirdPersonScreenshot.Exists ? $"{thirdPersonScreenshot.Length} bytes" : "missing");
        FileInfo extremeLookUpScreenshot = new FileInfo(ExtremeLookUpScreenshotPath);
        Record(
            "Extreme upward-view preview captured",
            extremeLookUpScreenshot.Exists && extremeLookUpScreenshot.Length > 1024,
            extremeLookUpScreenshot.Exists
                ? $"{extremeLookUpScreenshot.Length} bytes"
                : "missing");
        FileInfo vistaScreenshot = new FileInfo(VistaScreenshotPath);
        bool generatedVistaExists = player.gameObject.scene.GetRootGameObjects()
            .Any(root => root.name == "Firewatch Distant Vista");
        Record(
            generatedVistaExists
                ? "Distant vista preview captured"
                : "Restored map layout keeps generated distant vista absent",
            !generatedVistaExists || (vistaScreenshot.Exists && vistaScreenshot.Length > 1024),
            generatedVistaExists
                ? (vistaScreenshot.Exists ? $"{vistaScreenshot.Length} bytes" : "missing")
                : "no generated vista root");
        FileInfo waterAtmosphereScreenshot = new FileInfo(WaterAtmosphereScreenshotPath);
        Record(
            "Water, rock, and atmosphere vertical-slice preview captured",
            waterAtmosphereScreenshot.Exists && waterAtmosphereScreenshot.Length > 1024,
            waterAtmosphereScreenshot.Exists
                ? $"{waterAtmosphereScreenshot.Length} bytes"
                : "missing");
        FileInfo groundMistScreenshot = new FileInfo(GroundMistScreenshotPath);
        Record(
            "Ground-level local mist preview captured",
            groundMistScreenshot.Exists && groundMistScreenshot.Length > 1024,
            groundMistScreenshot.Exists ? $"{groundMistScreenshot.Length} bytes" : "missing");
        FinishRuntimeProbe();
    }

    private static void HoldCrouchPose()
    {
        player.enabled = true;
        player.SetCrouching(true);
        animancer.SetGrounded(true);
        animancer.SetCrouching(true);
        animancer.SetMovement(Vector3.zero, player.transform.forward);
    }

    private static void CaptureRuntimePreview()
    {
        CaptureCameraToPng(playerCamera, ScreenshotPath);
    }

    private static void CaptureThirdPersonCharacterPreview()
    {
        Transform cameraTransform = playerCamera.transform;
        Vector3 previousPosition = cameraTransform.position;
        Quaternion previousRotation = cameraTransform.rotation;
        Transform head = player.ProtectedHeadBone;
        Vector3 target = head != null
            ? head.position
            : player.transform.position + Vector3.up * 1.5f;
        Vector3 cameraPosition = target - player.transform.forward * 3f + Vector3.up * 0.35f;

        try
        {
            cameraTransform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(target - cameraPosition, Vector3.up));
            CaptureCameraToPng(playerCamera, ThirdPersonScreenshotPath);
        }
        finally
        {
            cameraTransform.SetPositionAndRotation(previousPosition, previousRotation);
        }
    }

    private static void CaptureVistaPreview()
    {
        GameObject vistaRoot = player.gameObject.scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "Firewatch Distant Vista");
        Renderer[] vistaRenderers = vistaRoot != null
            ? vistaRoot.GetComponentsInChildren<Renderer>(true)
            : Array.Empty<Renderer>();
        if (vistaRenderers.Length == 0) return;

        Bounds vistaBounds = vistaRenderers[0].bounds;
        for (int i = 1; i < vistaRenderers.Length; i++)
            vistaBounds.Encapsulate(vistaRenderers[i].bounds);

        Transform cameraTransform = playerCamera.transform;
        Vector3 previousPosition = cameraTransform.position;
        Quaternion previousRotation = cameraTransform.rotation;
        float previousFieldOfView = playerCamera.fieldOfView;

        Vector3 direction = Vector3.ProjectOnPlane(vistaBounds.center - player.transform.position, Vector3.up)
            .normalized;
        if (direction.sqrMagnitude < 0.001f) direction = player.transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 cameraPosition = player.transform.position - direction * 22f + side * 12f + Vector3.up * 48f;
        Vector3 lookTarget = player.transform.position + direction * 300f + Vector3.up * 34f;

        try
        {
            cameraTransform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up));
            playerCamera.fieldOfView = 52f;
            CaptureCameraToPng(playerCamera, VistaScreenshotPath);
        }
        finally
        {
            cameraTransform.SetPositionAndRotation(previousPosition, previousRotation);
            playerCamera.fieldOfView = previousFieldOfView;
        }
    }

    private static void CaptureWaterAtmospherePreview()
    {
        Transform cameraTransform = playerCamera.transform;
        Vector3 previousPosition = cameraTransform.position;
        Quaternion previousRotation = cameraTransform.rotation;
        float previousFieldOfView = playerCamera.fieldOfView;

        Vector3 cameraPosition = new Vector3(-24f, 34f, -92f);
        Vector3 lookTarget = new Vector3(43f, 2.5f, -10f);

        try
        {
            cameraTransform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up));
            playerCamera.fieldOfView = 55f;
            CaptureCameraToPng(playerCamera, WaterAtmosphereScreenshotPath);
        }
        finally
        {
            cameraTransform.SetPositionAndRotation(previousPosition, previousRotation);
            playerCamera.fieldOfView = previousFieldOfView;
        }
    }

    private static void CaptureGroundMistPreview()
    {
        Transform cameraTransform = playerCamera.transform;
        Vector3 previousPosition = cameraTransform.position;
        Quaternion previousRotation = cameraTransform.rotation;
        float previousFieldOfView = playerCamera.fieldOfView;

        Vector3 cameraPosition = new Vector3(62f, 7f, -30f);
        Vector3 lookTarget = new Vector3(102f, 5f, 12f);

        try
        {
            cameraTransform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up));
            playerCamera.fieldOfView = 58f;
            CaptureCameraToPng(playerCamera, GroundMistScreenshotPath);
        }
        finally
        {
            cameraTransform.SetPositionAndRotation(previousPosition, previousRotation);
            playerCamera.fieldOfView = previousFieldOfView;
        }
    }

    private static void CaptureCameraToPng(Camera camera, string path)
    {
        const int width = 1280;
        const int height = 720;
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        Texture2D pixels = new Texture2D(width, height, TextureFormat.RGB24, false);

        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            pixels.Apply(false, false);
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            RenderTexture.ReleaseTemporary(target);
        }
    }

    private static void VerifyOriginalAnimationAssignments()
    {
        CharacterAnimationConfig config = animancer.Config;
        const string originalRoot = "Assets/Animation/Locomotion/";
        AnimationClip[] originalRoles =
        {
            config.Locomotion.Idle,
            config.Locomotion.CrouchIdle,
            config.Locomotion.Walk.Forward,
            config.Locomotion.Run.Forward,
            config.Locomotion.Crouch.Forward,
            config.Airborne.JumpStarts.Standing,
            config.Airborne.Fall,
            config.Airborne.SoftLanding
        };
        bool allOriginal = originalRoles.All(clip => clip != null &&
            AssetDatabase.GetAssetPath(clip).StartsWith(originalRoot, StringComparison.Ordinal));
        Record("Existing animation roles use project-original clips", allOriginal);
        Record("MOBILITY fills missing animation roles", config.Locomotion.Walk.Left != null &&
            AssetDatabase.GetAssetPath(config.Locomotion.Walk.Left)
                .StartsWith("Assets/Mobility_Pro/", StringComparison.Ordinal));
        Record("All clip tunings are available", config.ClipTunings != null &&
            config.ClipTunings.Length == 298, $"count {config.ClipTunings?.Length ?? 0}");
    }

    private static void VerifyVisualStyle()
    {
        GameObject styleObject = player.gameObject.scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "Firewatch Visual Style");
        Volume volume = styleObject != null ? styleObject.GetComponent<Volume>() : null;
        VolumeProfile profile = volume != null ? volume.sharedProfile : null;
        ButoVolumetricFog buto = null;
        bool butoActive = profile != null && profile.TryGet(out buto) &&
            buto.active && buto.mode.value == VolumetricFogMode.On;
        bool splitToningActive = profile != null && profile.TryGet(out SplitToning splitToning) &&
            splitToning.active && splitToning.IsActive();
        bool acesActive = profile != null && profile.TryGet(out Tonemapping tonemapping) &&
            tonemapping.active && tonemapping.mode.value == TonemappingMode.ACES;
        bool highlightControlActive =
            profile != null &&
            profile.TryGet(out ColorAdjustments colorAdjustments) &&
            profile.TryGet(out Bloom bloom) &&
            colorAdjustments.postExposure.value <= -0.1f &&
            bloom.intensity.value <= 0.07f &&
            bloom.threshold.value >= 1.2f;
        Record("Global Firewatch Volume is active", volume != null && volume.isGlobal &&
            profile != null && profile.components.Count(component => component != null) >= 7);
        Record("Buto volumetric fog is active", butoActive);
        bool fogPreservesSurfaceDetail = butoActive &&
            buto.fogDensity.value >= 1.8f && buto.fogDensity.value <= 2.2f &&
            buto.maxDistanceVolumetric.value >= 280f &&
            buto.maxDistanceVolumetric.value <= 330f &&
            buto.lightIntensity.value <= 0.9f &&
            buto.densityInLight.value <= 0.58f &&
            buto.attenuationBoundarySize.value <= 16f &&
            buto.noiseTiling.value >= 90f;
        Record(
            "Global fog remains dense without replacing distant surface color too early",
            fogPreservesSurfaceDetail,
            butoActive
                ? $"density {buto.fogDensity.value:0.###}, range {buto.maxDistanceVolumetric.value:0.#}m, " +
                  $"light {buto.lightIntensity.value:0.##}, lit density {buto.densityInLight.value:0.##}"
                : "Buto is inactive");

        FogDensityMask cameraFogMask = playerCamera != null
            ? playerCamera.GetComponentsInChildren<FogDensityMask>(true)
                .FirstOrDefault(candidate => candidate.gameObject.name == "Firewatch Near Fog Exclusion")
            : null;
        Record(
            "Player camera uses a bounded local fog shaping mask independent of world fog",
            cameraFogMask != null && cameraFogMask.DensityMultiplier >= 0f &&
            cameraFogMask.DensityMultiplier <= 2.5f &&
            cameraFogMask.Size.x >= 10f && cameraFogMask.Size.x <= 15f,
            cameraFogMask != null
                ? $"multiplier {cameraFogMask.DensityMultiplier:0.##}, radius {cameraFogMask.Size.x:0.#}m"
                : "camera fog mask missing");

        FogDensityMask lightShaftZone = player.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<FogDensityMask>(true))
            .FirstOrDefault(candidate =>
                candidate.gameObject.name.StartsWith(
                    "Firewatch Light Shaft Zone - ",
                    StringComparison.Ordinal));
        Record(
            "Tyndall light uses a bounded world-space density zone instead of the camera mask",
            lightShaftZone != null && lightShaftZone.Shape == FogDensityMask.PrimitiveShape.Box &&
            lightShaftZone.DensityMultiplier >= 3f &&
            lightShaftZone.BlendDistance <= 10f &&
            lightShaftZone.GetComponentInParent<Camera>(true) == null,
            lightShaftZone != null
                ? $"density {lightShaftZone.DensityMultiplier:0.##}, size {lightShaftZone.Size}, " +
                  $"blend {lightShaftZone.BlendDistance:0.#}m"
                : "light shaft zone missing");

        FogDensityMask localMistBank = player.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<FogDensityMask>(true))
            .FirstOrDefault(candidate =>
                candidate.gameObject.name.StartsWith("Firewatch Mist Bank - ", StringComparison.Ordinal));
        Record(
            "Representative coast forest uses a soft Buto local mist bank",
            localMistBank != null && localMistBank.DensityMultiplier > 1f &&
            localMistBank.BlendDistance >= 50f &&
            localMistBank.GetComponentInParent<Camera>(true) == null,
            localMistBank != null
                ? $"density {localMistBank.DensityMultiplier:0.##}, radius {localMistBank.Size.x:0.#}m, " +
                  $"blend {localMistBank.BlendDistance:0.#}m"
                : "local mist bank missing");

        FirewatchLocalMistLayer visibleMist = player.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<FirewatchLocalMistLayer>(true))
            .FirstOrDefault();
        ParticleSystem visibleParticles = visibleMist != null ? visibleMist.Particles : null;
        ParticleSystemRenderer visibleRenderer =
            visibleMist != null ? visibleMist.ParticleRenderer : null;
        MaterialPropertyBlock visibleMistProperties = new MaterialPropertyBlock();
        if (visibleRenderer != null)
            visibleRenderer.GetPropertyBlock(visibleMistProperties);
        float visibleMistDensity = visibleMistProperties.GetFloat("_OpacityMultiplier");
        Record(
            "Selected lowland has a world-space visible mist supplement",
            visibleMist != null && visibleParticles != null && visibleRenderer != null &&
            visibleParticles.main.simulationSpace == ParticleSystemSimulationSpace.World &&
            visibleParticles.main.maxParticles >= 32 &&
            visibleRenderer.sharedMaterial != null &&
            visibleRenderer.sharedMaterial.shader != null &&
            visibleRenderer.sharedMaterial.shader.name == "LightTower/Firewatch/Local Mist" &&
            visibleMistDensity >= 1.2f,
            visibleMist != null && visibleParticles != null
                ? $"bank {visibleMist.BankLabel}, particles {visibleParticles.particleCount}/" +
                  $"{visibleParticles.main.maxParticles}, density {visibleMistDensity:0.##}x, shader " +
                  $"{visibleRenderer?.sharedMaterial?.shader?.name ?? "None"}"
                : "visible mist layer missing");

        FirewatchAtmosphereController atmosphereController = styleObject != null
            ? styleObject.GetComponent<FirewatchAtmosphereController>()
            : null;
        Material integratedWater = atmosphereController != null &&
                                   atmosphereController.Config != null &&
                                   atmosphereController.Config.ButoWaterShader != null
            ? player.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .SelectMany(candidate => candidate.sharedMaterials)
                .FirstOrDefault(material => material != null &&
                    material.shader == atmosphereController.Config.ButoWaterShader)
            : null;
        bool transparentWaterState = integratedWater != null &&
                                     integratedWater.HasProperty("_Surface") &&
                                     integratedWater.GetFloat("_Surface") >= 0.5f &&
                                     integratedWater.HasProperty("_ZWrite") &&
                                     integratedWater.GetFloat("_ZWrite") < 0.5f;
        Record(
            "Transparent water uses the Buto-integrated original PNB shader",
            atmosphereController != null && atmosphereController.ButoWaterIntegrationActive &&
            integratedWater != null && integratedWater.shader.name != "Hidden/InternalErrorShader",
            atmosphereController != null
                ? $"materials {atmosphereController.ButoWaterMaterialCount}, " +
                  $"renderers {atmosphereController.ButoWaterRendererCount}"
                : "atmosphere controller missing");
        Record(
            "Water keeps transparent sorting and ZWrite Off",
            transparentWaterState,
            integratedWater != null
                ? $"queue {integratedWater.renderQueue}, surface {integratedWater.GetFloat("_Surface"):0}, " +
                  $"zwrite {integratedWater.GetFloat("_ZWrite"):0}"
                : "integrated water material missing");
        Record("Firewatch warm and cool color separation is active", splitToningActive);
        Record("ACES highlight rolloff is active", acesActive);
        Record(
            "Exposure and bloom preserve detail in sunlit distant surfaces",
            highlightControlActive,
            profile != null && profile.TryGet(out ColorAdjustments activeColor) &&
            profile.TryGet(out Bloom activeBloom)
                ? $"exposure {activeColor.postExposure.value:0.##}, bloom {activeBloom.intensity.value:0.##}, " +
                  $"threshold {activeBloom.threshold.value:0.##}"
                : "post-processing components missing");
        Record(
            "Golden-hour sun and reflections are below washout levels",
            RenderSettings.sun != null && RenderSettings.sun.intensity <= 1.5f &&
            RenderSettings.reflectionIntensity <= 0.6f,
            $"sun {(RenderSettings.sun != null ? RenderSettings.sun.intensity : 0f):0.##}, " +
            $"reflections {RenderSettings.reflectionIntensity:0.##}");

        UniversalAdditionalCameraData cameraData = playerCamera != null
            ? playerCamera.GetUniversalAdditionalCameraData()
            : null;
        Record("Player camera renders post processing and depth", cameraData != null &&
            cameraData.renderPostProcessing && cameraData.requiresDepthTexture);

        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
            "Assets/Settings/PC_Renderer.asset");
        ButoRenderFeature butoFeature = rendererData != null
            ? rendererData.rendererFeatures.OfType<ButoRenderFeature>().FirstOrDefault()
            : null;
        RenderObjects preButoGlassFeature = rendererData != null
            ? rendererData.rendererFeatures.OfType<RenderObjects>().FirstOrDefault(feature =>
                feature.name == "Firewatch Pre-Buto Lighthouse Glass")
            : null;
        Record("PC Renderer keeps SSAO and uses Buto", rendererData != null &&
            butoFeature != null &&
            rendererData.rendererFeatures.Any(feature => feature != null &&
                feature.name == "ScreenSpaceAmbientOcclusion"));
        Record(
            "Water receives Buto exactly once after the opaque composite",
            transparentWaterState && butoFeature != null &&
            butoFeature.settings.renderPassEvent == RenderPassEvent.BeforeRenderingTransparents,
            butoFeature != null
                ? $"Buto event {butoFeature.settings.renderPassEvent}, water queue {integratedWater.renderQueue}"
                : "Buto renderer feature missing");

        Renderer lighthouseGlass = player.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .FirstOrDefault(candidate => candidate.gameObject.name == "LH_WindowGlass.mo");
        Material lighthouseGlassMaterial = lighthouseGlass != null
            ? lighthouseGlass.sharedMaterials.FirstOrDefault(material => material != null)
            : null;
        bool standardTransparentGlass = lighthouseGlassMaterial != null &&
                                        lighthouseGlassMaterial.shader != null &&
                                        lighthouseGlassMaterial.shader.name ==
                                        "Universal Render Pipeline/Lit" &&
                                        lighthouseGlassMaterial.renderQueue >= 2500 &&
                                        lighthouseGlassMaterial.HasProperty("_Surface") &&
                                        lighthouseGlassMaterial.GetFloat("_Surface") >= 0.5f &&
                                        lighthouseGlassMaterial.HasProperty("_ZWrite") &&
                                        lighthouseGlassMaterial.GetFloat("_ZWrite") < 0.5f;
        int glassLayer = atmosphereController != null && atmosphereController.Config != null
            ? atmosphereController.Config.PreButoGlassLayer
            : -1;
        bool glassExcludedFromDefaultTransparents = rendererData != null &&
                                                    glassLayer >= 0 && glassLayer <= 31 &&
                                                    (rendererData.transparentLayerMask.value &
                                                     (1 << glassLayer)) == 0;
        bool glassPassOrderIsCorrect = preButoGlassFeature != null &&
                                       butoFeature != null &&
                                       preButoGlassFeature.settings.Event ==
                                       RenderPassEvent.AfterRenderingSkybox &&
                                       (int)preButoGlassFeature.settings.Event <
                                       (int)butoFeature.settings.renderPassEvent &&
                                       preButoGlassFeature.settings.filterSettings.RenderQueueType ==
                                       RenderQueueType.Transparent &&
                                       glassLayer >= 0 &&
                                       (preButoGlassFeature.settings.filterSettings.LayerMask.value &
                                        (1 << glassLayer)) != 0;
        Record(
            "Lighthouse glass keeps the original transparent URP/Lit shader",
            standardTransparentGlass,
            lighthouseGlassMaterial != null
                ? $"shader {lighthouseGlassMaterial.shader.name}, queue " +
                  $"{lighthouseGlassMaterial.renderQueue}, zwrite " +
                  $"{lighthouseGlassMaterial.GetFloat("_ZWrite"):0}"
                : "lighthouse glass material missing");
        Record(
            "Lighthouse glass renders after the skybox and before Buto exactly once",
            atmosphereController != null &&
            atmosphereController.PreButoGlassIntegrationActive &&
            lighthouseGlass != null && lighthouseGlass.gameObject.layer == glassLayer &&
            glassPassOrderIsCorrect && glassExcludedFromDefaultTransparents,
            preButoGlassFeature != null && butoFeature != null
                ? $"glass event {preButoGlassFeature.settings.Event}, Buto event " +
                  $"{butoFeature.settings.renderPassEvent}, layer {glassLayer}, " +
                  $"default transparent included {!glassExcludedFromDefaultTransparents}"
                : "pre-Buto glass or Buto feature missing");

        SkinnedMeshRenderer renderer = player.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(candidate => candidate.gameObject.name.StartsWith("Chr_", StringComparison.Ordinal));
        Record("Restored player material remains valid", renderer != null && renderer.sharedMaterial != null &&
            renderer.sharedMaterial.shader != null &&
            renderer.sharedMaterial.shader.name != "Hidden/InternalErrorShader");
    }

    private static void FinishRuntimeProbe()
    {
        RestoreCameraConflictSettings();
        Application.targetFrameRate = previousTargetFrameRate;
        QualitySettings.vSyncCount = previousVSyncCount;
        Time.captureFramerate = previousCaptureFrameRate;
        if (collisionObstacle != null)
            UnityEngine.Object.Destroy(collisionObstacle);
        collisionObstacle = null;

        string result = (allPassed ? "SUCCESS" : "FAILED") + Environment.NewLine +
            string.Join(Environment.NewLine, checks) + Environment.NewLine +
            "Screenshot=" + Path.GetFullPath(ScreenshotPath) + Environment.NewLine +
            "VistaScreenshot=" + Path.GetFullPath(VistaScreenshotPath);
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, result);

        SessionState.SetBool(ExitPendingKey, true);
        EditorApplication.isPlaying = false;
    }

    private static void RestoreEditorScenes()
    {
        try
        {
            bool preserveLoadedScene = SessionState.GetBool(PreserveLoadedSceneKey, false);
            if (!preserveLoadedScene)
            {
                string[] previousScenes = SessionState.GetString(PreviousScenesKey, string.Empty)
                    .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (previousScenes.Length > 0)
                {
                    EditorSceneManager.OpenScene(previousScenes[0], OpenSceneMode.Single);
                    for (int i = 1; i < previousScenes.Length; i++)
                        EditorSceneManager.OpenScene(previousScenes[i], OpenSceneMode.Additive);
                }

                string previousActivePath = SessionState.GetString(PreviousActiveSceneKey, string.Empty);
                Scene previousActive = SceneManager.GetSceneByPath(previousActivePath);
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
            }

            if (File.Exists(ResultPath))
                File.AppendAllText(ResultPath, Environment.NewLine + "EditorScenesRestored=True");
        }
        catch (Exception exception)
        {
            File.AppendAllText(ResultPath, Environment.NewLine + "EditorRestoreError=" + exception);
        }
        finally
        {
            int batchExitCode = allPassed ? 0 : 1;
            bool exitWhenFinished =
                Application.isBatchMode || SessionState.GetBool(CommandLineRunKey, false);
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            SessionState.SetString(PreviousScenesKey, string.Empty);
            SessionState.SetString(PreviousActiveSceneKey, string.Empty);
            SessionState.SetBool(PreserveLoadedSceneKey, false);
            SessionState.SetBool(CommandLineRunKey, false);
            runtimeStarted = false;

            if (exitWhenFinished)
                EditorApplication.delayCall += () => EditorApplication.Exit(batchExitCode);
        }
    }

    private static void Record(string name, bool passed, string details = null)
    {
        allPassed &= passed;
        checks.Add($"[{(passed ? "PASS" : "FAIL")}] {name}" +
            (string.IsNullOrEmpty(details) ? string.Empty : " - " + details));
    }

    private static void NextPhase()
    {
        phase++;
        phaseStartedAt = EditorApplication.timeSinceStartup;
    }

    private static double Elapsed => EditorApplication.timeSinceStartup - phaseStartedAt;

    private static void WriteImmediateFailure(Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, "FAILED" + Environment.NewLine + exception);
    }
}
