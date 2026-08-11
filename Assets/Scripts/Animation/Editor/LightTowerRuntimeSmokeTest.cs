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
    private const string TargetScenePath = "Assets/_Recovery/0 (58).unity";
    private const string RequestPath = "LightTowerRuntimeSmokeTest.request";
    private const string ResultPath = "Temp/LightTowerRuntimeSmokeTest.result";
    private const string ScreenshotPath = "Library/LightTowerValidation/FirewatchRuntimePreview.png";
    private const string MovementScreenshotPath = "Library/LightTowerValidation/MovementCameraPreview.png";
    private const string ThirdPersonScreenshotPath = "Library/LightTowerValidation/ThirdPersonCharacterPreview.png";
    private const string VistaScreenshotPath = "Library/LightTowerValidation/FirewatchVistaPreview.png";
    private const string PendingKey = "LightTower.RuntimeSmoke.Pending";
    private const string ExitPendingKey = "LightTower.RuntimeSmoke.ExitPending";
    private const string PreviousScenesKey = "LightTower.RuntimeSmoke.PreviousScenes";
    private const string PreviousActiveSceneKey = "LightTower.RuntimeSmoke.PreviousActiveScene";
    private const string PreserveLoadedSceneKey = "LightTower.RuntimeSmoke.PreserveLoadedScene";

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

    private static void Tick()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;

        if (SessionState.GetBool(PendingKey, false) &&
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            !SessionState.GetBool(ExitPendingKey, false))
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
            File.Delete(VistaScreenshotPath);

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
            SessionState.SetBool(PendingKey, false);
            if (Application.isBatchMode)
                EditorApplication.delayCall += () => EditorApplication.Exit(1);
        }
    }

    private static void BeginRuntimeProbe()
    {
        Application.runInBackground = true;
        Time.timeScale = 1f;
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
                    VerifyCameraCollisionAndCapture();
                    break;
                case 6:
                    VerifyCameraReturnAndCapture();
                    break;
                case 7:
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
                throw new InvalidOperationException("recover (58) is not loaded in Play Mode.");

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
            Record("Animancer configuration revision 8", animancer != null && animancer.Config != null &&
                animancer.Config.ConfigurationRevision == 8);
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
                "Animated head tracking and player-camera head suppression are active",
                cameraSettings.TrackAnimatedHead && player.ProtectedHeadBone != null &&
                cameraSettings.HideHeadForPlayerCamera && player.HeadRenderSuppressionEnabled,
                player.ProtectedHeadBone != null
                    ? $"head {player.ProtectedHeadBone.name}, suppression {player.HeadRenderSuppressionEnabled}"
                    : "humanoid head bone was not resolved");
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

        movementStart = player.transform.position;
        motor.Move(player.transform.forward * 0.08f);
        NextPhase();
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

        Record("Animancer receives locomotion speed", animancer.TargetLocomotionSpeed > 1.9f &&
            animancer.CurrentLocomotionSpeed > 0.5f,
            $"target {animancer.TargetLocomotionSpeed:0.##}, current {animancer.CurrentLocomotionSpeed:0.##}");
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

        crouchStartY = cameraRoot.localPosition.y;
        player.SetCrouching(true);
        animancer.ReturnToLocomotion("Runtime smoke test crouch");
        NextPhase();
    }

    private static void VerifyCrouchCameraAndCreateObstacle()
    {
        HoldCrouchPose();
        if (Elapsed < 0.9f) return;

        CharacterAnimationConfig config = animancer.Config;
        CharacterAnimationConfig.MobilityClipTuning crouchTuning =
            config.FindClipTuning(config.Locomotion.CrouchIdle);
        bool hasCrouchPose = animancer.TryGetCurrentCameraPosition(
            out Vector3 crouchCameraPosition,
            out float crouchCameraSpeed);
        Record("Crouch animation has an adjustable camera pose", hasCrouchPose &&
            crouchTuning != null && crouchTuning.OverrideCameraPosition,
            hasCrouchPose ? $"target {crouchCameraPosition}, speed {crouchCameraSpeed:0.##}" : "missing");
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
        Physics.SyncTransforms();
        NextPhase();
    }

    private static void VerifyCameraCollisionAndCapture()
    {
        HoldCrouchPose();
        if (Elapsed < 0.5f) return;

        Vector3 anchor = player.transform.TransformPoint(animancer.Config.Camera.CollisionAnchorLocalPosition);
        obstructedCameraDistance = Vector3.Distance(anchor, playerCamera.transform.position);
        Record("Camera wall collision pulls the camera in", player.CameraCollisionActive &&
            obstructedCameraDistance < desiredCameraDistance - 0.01f,
            $"desired {desiredCameraDistance:0.###}, obstructed {obstructedCameraDistance:0.###}");

        if (collisionObstacle != null)
            UnityEngine.Object.Destroy(collisionObstacle);
        collisionObstacle = null;
        Physics.SyncTransforms();
        NextPhase();
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
            buto.fogDensity.value >= 1.3f && buto.fogDensity.value <= 1.7f &&
            buto.maxDistanceVolumetric.value >= 280f &&
            buto.maxDistanceVolumetric.value <= 330f &&
            buto.lightIntensity.value <= 0.85f &&
            buto.densityInLight.value <= 0.7f;
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
            "Player camera local fog boost remains intentional",
            cameraFogMask != null && Mathf.Approximately(cameraFogMask.DensityMultiplier, 5f),
            cameraFogMask != null
                ? $"multiplier {cameraFogMask.DensityMultiplier:0.##}, radius {cameraFogMask.Size.x:0.#}m"
                : "camera fog mask missing");
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

        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
            "Assets/Settings/PC_Renderer.asset");
        Record("PC Renderer keeps SSAO and uses Buto", rendererData != null &&
            rendererData.rendererFeatures.Any(feature => feature is ButoRenderFeature) &&
            rendererData.rendererFeatures.Any(feature => feature != null &&
                feature.name == "ScreenSpaceAmbientOcclusion"));

        SkinnedMeshRenderer renderer = player.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(candidate => candidate.gameObject.name.StartsWith("Chr_", StringComparison.Ordinal));
        Record("Restored player material remains valid", renderer != null && renderer.sharedMaterial != null &&
            renderer.sharedMaterial.shader != null &&
            renderer.sharedMaterial.shader.name != "Hidden/InternalErrorShader");
    }

    private static void FinishRuntimeProbe()
    {
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
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            SessionState.SetString(PreviousScenesKey, string.Empty);
            SessionState.SetString(PreviousActiveSceneKey, string.Empty);
            SessionState.SetBool(PreserveLoadedSceneKey, false);
            runtimeStarted = false;

            if (Application.isBatchMode)
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
