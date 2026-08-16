using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MobilityProLocomotionConfigurator
{
    public const string TargetScenePath = "Assets/_Recovery/0 (7).unity";
    public const string ConfigAssetPath = "Assets/Animation/Animancer/MobilityProPlayerAnimationConfig.asset";
    private const string OriginalPlayerControllerPath = "Assets/Animation/Controller/Player.controller";
    private const string OriginalLocomotionRoot = "Assets/Animation/Locomotion/";
    private const string PackRoot = "Assets/Mobility_Pro/Animation/IPC/";
    private const string SplitJumpRoot = PackRoot + "Split_Jumps/";
    private const string AutoSessionKey = "LightTower.MobilityProLocomotionConfigurator.AutoConfigured.v12";
    // Increment when the generated scene or ScriptableObject layout changes.
    private const int ConfigurationRevision = 12;
    private static readonly string[] OriginalLocomotionClipNames =
    {
        "A_Idle_Standing_Masc",
        "A_Idle_Crouching_Masc",
        "A_Crouch_FwdStrafeF_Masc",
        "A_Walk_F_Masc",
        "A_Sprint_F_Masc",
        "A_Jump_Idle_Masc",
        "A_Jump_Walking_Masc",
        "A_Jump_Running_Masc",
        "A_InAir_FallShort_Masc",
        "A_Land_IdleSoft_Masc",
        "A_Land_Walking_Masc",
        "A_Land_Running_Masc"
    };
    private static bool waitingForEditorIdle;

    static MobilityProLocomotionConfigurator()
    {
        EditorApplication.delayCall += TryAutoConfigure;
    }

    [MenuItem("Tools/Light Tower/Animation/Configure MOBILITY PRO Player (recover 58)")]
    public static void ConfigureFromMenu()
    {
        Configure(true);
    }

    [MenuItem("Tools/Light Tower/Animation/Select MOBILITY PRO Player Config")]
    public static void SelectConfiguration()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<CharacterAnimationConfig>(ConfigAssetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    private static void TryAutoConfigure()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        if (SessionState.GetBool(AutoSessionKey, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WaitForEditorIdle();
            return;
        }

        Configure(false);
    }

    private static void WaitForEditorIdle()
    {
        if (waitingForEditorIdle) return;
        waitingForEditorIdle = true;
        EditorApplication.update += ConfigureWhenIdle;
    }

    private static void ConfigureWhenIdle()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= ConfigureWhenIdle;
        waitingForEditorIdle = false;
        TryAutoConfigure();
    }

    private static void Configure(bool forceRepopulate)
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WaitForEditorIdle();
            return;
        }

        try
        {
            CharacterAnimationConfig config = LoadOrCreateConfig(out bool created);
            if (created || forceRepopulate || config.ConfigurationRevision < ConfigurationRevision)
                PopulateConfig(config);

            SceneConfigurationResult sceneResult = ConfigureTargetScene(config);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(AutoSessionKey, true);

            string result =
                $"SUCCESS\nScene={TargetScenePath}\nPlayer={sceneResult.PlayerName}\n" +
                $"Config={ConfigAssetPath}\nConfiguredClips={CountConfiguredClips(config)}\n" +
                $"OriginalAnimatorControllerPreserved={sceneResult.OriginalAnimatorControllerPreserved}\n" +
                $"AnimancerAdded={sceneResult.AnimancerAdded}\nControllerAdded={sceneResult.ControllerAdded}\n" +
                $"Validation={config.ValidateConfiguration().Message}";
            WriteResult(result);
            Debug.Log("MOBILITY PRO locomotion configuration completed.\n" + result, config);
        }
        catch (Exception exception)
        {
            SessionState.SetBool(AutoSessionKey, false);
            WriteResult("FAILED\n" + exception);
            Debug.LogException(exception);
        }
    }

    private static CharacterAnimationConfig LoadOrCreateConfig(out bool created)
    {
        CharacterAnimationConfig config = AssetDatabase.LoadAssetAtPath<CharacterAnimationConfig>(ConfigAssetPath);
        created = config == null;
        if (!created) return config;

        EnsureFolder("Assets/Animation/Animancer");
        config = ScriptableObject.CreateInstance<CharacterAnimationConfig>();
        AssetDatabase.CreateAsset(config, ConfigAssetPath);
        return config;
    }

    private static void PopulateConfig(CharacterAnimationConfig config)
    {
        Undo.RecordObject(config, "Populate MOBILITY PRO locomotion configuration");
        config.SourceAnimationPack = "Project original locomotion first; Unity MOBILITY PRO 2.7B1 fills missing roles";
        config.ConfiguredScenePath = TargetScenePath;
        config.ConfigurationRevision = ConfigurationRevision;

        CharacterAnimationConfig.LocomotionSettings locomotion = config.Locomotion;
        locomotion.Idle = LoadIpc("MOB1_Stand_Relaxed_Idle_IPC");
        locomotion.IdleVariations = new[] { LoadIpc("MOB1_Stand_Relaxed_Idle_v2_IPC") };
        locomotion.CrouchIdle = LoadIpc("MOB1_Crouch_Idle_IPC");

        PopulateLoopSet(locomotion.Walk, "Walk", true);
        PopulateLoopSet(locomotion.Jog, "Jog", true);
        PopulateLoopSet(locomotion.Run, "Run", false);
        PopulateLoopSet(locomotion.Crouch, "CrouchWalk", true);

        PopulateStartSet(locomotion.Starts.Walk, "Walk");
        PopulateStartSet(locomotion.Starts.Jog, "Jog");
        PopulateRunStartSet(locomotion.Starts.Run);
        PopulateCrouchStartSet(locomotion.Starts.Crouch);
        PopulateForwardTurningStartSet(locomotion.ForwardTurningStarts.Walk, "Walk");
        PopulateForwardTurningStartSet(locomotion.ForwardTurningStarts.Jog, "Jog");
        PopulateForwardTurningStartSet(locomotion.ForwardTurningStarts.Run, "Run");
        PopulateForwardTurningStartSet(locomotion.ForwardTurningStarts.Crouch, "CrouchWalk", "MOB1_Crouch_To_CrouchWalk_");

        PopulateStopSet(locomotion.Stops.Walk, "Walk", true);
        PopulateStopSet(locomotion.Stops.Jog, "Jog", true);
        PopulateStopSet(locomotion.Stops.Run, "Run", false);
        PopulateCrouchStopSet(locomotion.Stops.Crouch);
        PopulateFootMatchedStopSet(locomotion.FootMatchedStops.Walk, "Walk", true, false);
        PopulateFootMatchedStopSet(locomotion.FootMatchedStops.Jog, "Jog", true, false);
        PopulateFootMatchedStopSet(locomotion.FootMatchedStops.Run, "Run", false, false);
        PopulateFootMatchedStopSet(locomotion.FootMatchedStops.Crouch, "CrouchWalk", true, true);

        locomotion.StandToCrouch = LoadIpc("MOB1_Stand_Relaxed_To_Crouch_IPC");
        locomotion.CrouchToStand = LoadIpc("MOB1_Crouch_To_Stand_Relaxed_IPC");
        PopulateTurns(locomotion.Turns);

        locomotion.EnableIdleVariations = true;
        locomotion.EnableStartTransitions = true;
        locomotion.EnableStopTransitions = true;
        locomotion.EnableFootMatchedStops = true;
        locomotion.EnableCrouchTransitions = true;
        locomotion.LocomotionCrossFade = 0.14f;
        locomotion.LocomotionFadeOut = 0.14f;
        locomotion.StartCrossFade = 0.08f;
        locomotion.StartFadeOut = 0.1f;
        locomotion.StopCrossFade = 0.1f;
        locomotion.StopFadeOut = 0.12f;
        locomotion.CrouchCrossFade = 0.1f;
        locomotion.CrouchFadeOut = 0.12f;
        locomotion.StartBlendOutNormalizedTime = 0.7f;
        locomotion.StopBlendOutNormalizedTime = 0.82f;
        locomotion.CrouchBlendOutNormalizedTime = 0.82f;
        locomotion.StartMovementDelayNormalizedTime = 0.04f;
        locomotion.StartMovementFullSpeedNormalizedTime = 0.45f;
        locomotion.StartInputThreshold = 0.15f;
        locomotion.StopInputThreshold = 0.08f;
        locomotion.AccelerationSmoothTime = 0.1f;
        locomotion.DecelerationSmoothTime = 0.16f;
        locomotion.DirectionSmoothTime = 0.11f;
        locomotion.MaximumVisualAcceleration = 30f;
        locomotion.IdleSpeedThreshold = 0.05f;
        locomotion.SynchronizeLocomotionCycles = true;
        locomotion.GlobalPlaybackSpeed = 1f;
        locomotion.MinimumPlaybackSpeed = 0.85f;
        locomotion.MaximumPlaybackSpeed = 1.2f;

        PopulateAirborne(config.Airborne);
        RestoreOriginalAnimatorAnimations(config);

        config.Holding.Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Locomotion/Holding.anim");
        config.Holding.AvatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/AvatarMask/Mask_Arm_R.mask");
        config.Holding.DefaultWeight = 1f;
        config.Holding.FadeIn = 0.12f;
        config.Holding.FadeOut = 0.15f;

        config.Quality.StabilizeFeet = true;
        config.Quality.FeetPivotActive = 1f;
        config.Quality.KeepAnimatorControllerAssetAssignedInEditMode = true;
        config.Quality.ClearRuntimeAnimatorControllerOnInitialize = true;
        config.DefaultLocomotionRootMotion = CharacterAnimationRootMotionStrategy.Disabled;

        if (config.GlobalFades == null) config.GlobalFades = new CharacterAnimationConfig.GlobalFadeSettings();
        config.GlobalFades.GlobalFadeSpeed = 1f;
        config.GlobalFades.UseSharedDurations = false;
        config.GlobalFades.SharedFadeIn = 0.1f;
        config.GlobalFades.SharedFadeOut = 0.12f;

        if (config.Camera == null) config.Camera = new CharacterAnimationConfig.CameraSettings();
        config.Camera.LookSensitivity = 30f;
        config.Camera.EnablePerAnimationPositions = true;
        config.Camera.CrouchDownBlendSpeed = 10f;
        config.Camera.StandUpBlendSpeed = 10f;
        config.Camera.EnableWallCollision = true;
        config.Camera.CollisionMask = ~0;
        config.Camera.CollisionRadius = 0.16f;
        config.Camera.CollisionPadding = 0.03f;
        config.Camera.MinimumDistance = 0.36f;
        config.Camera.MinimumDistanceRatio = 0.65f;
        config.Camera.PullInSpeed = 30f;
        config.Camera.ReturnSpeed = 10f;
        config.Camera.CollisionAnchorLocalPosition = new Vector3(0f, 1.25f, 0.05f);
        config.Camera.CameraNearClip = 0.18f;
        config.Camera.UseNearPlaneCollisionVolume = true;
        config.Camera.NearPlaneSkinWidth = 0.03f;
        config.Camera.PenetrationIterations = 4;
        config.Camera.MaximumPenetrationCorrection = 0.5f;
        config.Camera.EnableCharacterInteriorProtection = true;
        config.Camera.MinimumCameraLocalForward = 0.38f;
        config.Camera.CrouchMinimumCameraLocalForward = 0.55f;
        config.Camera.TrackAnimatedHead = true;
        config.Camera.EnableFaceSurfaceConstraint = true;
        config.Camera.FaceSurfaceClearance = 0.025f;
        config.Camera.CrouchFaceSurfaceClearance = 0.035f;
        config.Camera.FaceAnchorFallbackDepth = 0.48f;
        config.Camera.FaceDirectionFollow = 0.45f;
        config.Camera.LookUpExtraFaceClearance = 0.14f;
        config.Camera.LookUpFaceClearanceStartAngle = 20f;
        config.Camera.LookUpFaceClearanceFullAngle = 70f;
        config.Camera.HeadForwardClearance = 0.35f;
        config.Camera.CrouchHeadForwardClearance = 0.45f;
        config.Camera.HeadSafetyRadius = 0.3f;
        config.Camera.CrouchHeadSafetyRadius = 0.42f;
        config.Camera.TrackUpperBodyBones = true;
        config.Camera.NeckSafetyRadius = 0.24f;
        config.Camera.ChestSafetyRadius = 0.28f;
        config.Camera.ShoulderSafetyRadius = 0.22f;
        config.Camera.CrouchUpperBodyRadiusScale = 1.15f;
        config.Camera.EnableProximityClip = false;
        config.Camera.ProximityClipShader = Shader.Find(
            "LightTower/Character/First Person Proximity Clip Lit");
        config.Camera.ProximityClipRadius = 0.2f;
        config.Camera.HideHeadForPlayerCamera = true;
        config.Camera.HeadScaleEmergencyOnly = true;
        config.Camera.CharacterHideDistance = 0.9f;
        config.Camera.HiddenHeadScale = 0.001f;
        config.Camera.InteriorProtectionBlendSpeed = 30f;
        config.Camera.EnableRuntimeSafetyWarnings = true;
        config.Camera.SafetyWarningCooldown = 2f;
        config.Camera.DrawSolverGizmos = true;

        PopulateClipTuningLibrary(config);

        EditorUtility.SetDirty(config);
    }

    private static void PopulateLoopSet(CharacterAnimationConfig.DirectionalClipSet set, string gait, bool includeBackward)
    {
        set.Forward = LoadIpc($"MOB1_{gait}_F_Loop_IPC");
        set.Left = LoadIpc($"MOB1_{gait}_L_Loop_IPC");
        set.Right = LoadIpc($"MOB1_{gait}_R_Loop_IPC");
        set.ForwardLeft = LoadIpc($"MOB1_{gait}_FL_Loop_IPC");
        set.ForwardRight = LoadIpc($"MOB1_{gait}_FR_Loop_IPC");
        if (!includeBackward) return;
        set.Backward = LoadIpc($"MOB1_{gait}_B_Loop_IPC");
        set.BackwardLeft = LoadIpc($"MOB1_{gait}_BL_BkPd_Loop_IPC");
        set.BackwardRight = LoadIpc($"MOB1_{gait}_BR_BkPd_Loop_IPC");
    }

    private static void PopulateStartSet(CharacterAnimationConfig.DirectionalClipSet set, string gait)
    {
        string prefix = $"MOB1_Stand_Relaxed_To_{gait}_";
        set.Forward = LoadIpc(prefix + "F_IPC");
        set.Backward = LoadIpc(prefix + "B_IPC");
        set.Left = LoadIpc(prefix + "L_IPC");
        set.Right = LoadIpc(prefix + "R_IPC");
        set.ForwardLeft = LoadIpc(prefix + "L45_Fwd_IPC");
        set.ForwardRight = LoadIpc(prefix + "R45_Fwd_IPC");
        set.BackwardLeft = LoadIpc(prefix + "L135_Fwd_IPC");
        set.BackwardRight = LoadIpc(prefix + "R135_Fwd_IPC");
    }

    private static void PopulateRunStartSet(CharacterAnimationConfig.DirectionalClipSet set)
    {
        string prefix = "MOB1_Stand_Relaxed_To_Run_";
        set.Forward = LoadIpc(prefix + "F_IPC");
        set.Left = LoadIpc(prefix + "L_IPC");
        set.Right = LoadIpc(prefix + "R_IPC");
        set.ForwardLeft = LoadIpc(prefix + "L45_Fwd_IPC");
        set.ForwardRight = LoadIpc(prefix + "R45_Fwd_IPC");
        set.BackwardLeft = LoadIpc(prefix + "L135_Fwd_IPC");
        set.BackwardRight = LoadIpc(prefix + "R135_Fwd_IPC");
        set.Backward = LoadIpc(prefix + "L180_Fwd_IPC");
    }

    private static void PopulateCrouchStartSet(CharacterAnimationConfig.DirectionalClipSet set)
    {
        string prefix = "MOB1_Crouch_To_CrouchWalk_";
        set.Forward = LoadIpc(prefix + "F_IPC");
        set.Backward = LoadIpc(prefix + "B_IPC");
        set.Left = LoadIpc(prefix + "L_IPC");
        set.Right = LoadIpc(prefix + "R_IPC");
        set.ForwardLeft = LoadIpc(prefix + "L45_Fwd_IPC");
        set.ForwardRight = LoadIpc(prefix + "R45_Fwd_IPC");
        set.BackwardLeft = LoadIpc(prefix + "L135_Fwd_IPC");
        set.BackwardRight = LoadIpc(prefix + "R135_Fwd_IPC");
    }

    private static void PopulateForwardTurningStartSet(
        CharacterAnimationConfig.DirectionalClipSet set,
        string gait,
        string customPrefix = null)
    {
        string prefix = customPrefix ?? $"MOB1_Stand_Relaxed_To_{gait}_";
        set.Forward = LoadIpc(prefix + "F_IPC");
        set.ForwardLeft = LoadIpc(prefix + "L45_Fwd_IPC");
        set.Left = LoadIpc(prefix + "L90_Fwd_IPC");
        set.BackwardLeft = LoadIpc(prefix + "L135_Fwd_IPC");
        set.Backward = LoadIpc(prefix + "L180_Fwd_IPC");
        set.BackwardRight = LoadIpc(prefix + "R135_Fwd_IPC");
        set.Right = LoadIpc(prefix + "R90_Fwd_IPC");
        set.ForwardRight = LoadIpc(prefix + "R45_Fwd_IPC");
    }

    private static void PopulateStopSet(CharacterAnimationConfig.DirectionalClipSet set, string gait, bool includeBackward)
    {
        string prefix = $"MOB1_{gait}_";
        set.Forward = LoadIpc(prefix + "F_To_Stand_Relaxed_IPC");
        set.Left = LoadIpc(prefix + "L_To_Stand_Relaxed_IPC");
        set.Right = LoadIpc(prefix + "R_To_Stand_Relaxed_IPC");
        set.ForwardLeft = set.Forward;
        set.ForwardRight = set.Forward;
        if (!includeBackward) return;
        set.Backward = LoadIpc(prefix + "B_To_Stand_Relaxed_IPC");
        set.BackwardLeft = set.Backward;
        set.BackwardRight = set.Backward;
    }

    private static void PopulateCrouchStopSet(CharacterAnimationConfig.DirectionalClipSet set)
    {
        string prefix = "MOB1_CrouchWalk_";
        set.Forward = LoadIpc(prefix + "F_To_Crouch_IPC");
        set.Backward = LoadIpc(prefix + "B_To_Crouch_IPC");
        set.Left = LoadIpc(prefix + "L_To_Crouch_IPC");
        set.Right = LoadIpc(prefix + "R_To_Crouch_IPC");
        set.ForwardLeft = set.Forward;
        set.ForwardRight = set.Forward;
        set.BackwardLeft = set.Backward;
        set.BackwardRight = set.Backward;
    }

    private static void PopulateFootMatchedStopSet(
        CharacterAnimationConfig.FootMatchedDirectionalClipSet set,
        string gait,
        bool includeBackward,
        bool crouch)
    {
        PopulateStopVariant(set.Neutral, gait, includeBackward, crouch, string.Empty);
        PopulateStopVariant(set.LeftFoot, gait, includeBackward, crouch, "_LU");
        PopulateStopVariant(set.RightFoot, gait, includeBackward, crouch, "_RU");
    }

    private static void PopulateStopVariant(
        CharacterAnimationConfig.DirectionalClipSet set,
        string gait,
        bool includeBackward,
        bool crouch,
        string footSuffix)
    {
        string target = crouch ? "Crouch" : "Stand_Relaxed";
        string prefix = $"MOB1_{gait}_";
        set.Forward = LoadIpc(prefix + $"F_To_{target}{footSuffix}_IPC");
        set.Left = LoadIpc(prefix + $"L_To_{target}{footSuffix}_IPC");
        set.Right = LoadIpc(prefix + $"R_To_{target}{footSuffix}_IPC");
        set.ForwardLeft = set.Forward;
        set.ForwardRight = set.Forward;
        if (!includeBackward) return;
        set.Backward = LoadIpc(prefix + $"B_To_{target}{footSuffix}_IPC");
        set.BackwardLeft = set.Backward;
        set.BackwardRight = set.Backward;
    }

    private static void PopulateTurns(CharacterAnimationConfig.TurnSettings turns)
    {
        turns.EnableTurnInPlace = true;
        turns.EnableContinuousTurnLoops = true;
        turns.EnableMovementPivots = true;
        turns.EnableMovementCurves = true;
        turns.MinimumTurnAngle = 25f;
        turns.Turn90Threshold = 67.5f;
        turns.Turn135Threshold = 112.5f;
        turns.Turn180Threshold = 157.5f;
        turns.TurnInputSettleTime = 0.08f;
        turns.TurnCrossFade = 0.1f;
        turns.TurnBlendOutNormalizedTime = 0.82f;
        turns.TurnLoopStartYawRate = 75f;
        turns.TurnLoopExitDelay = 0.08f;
        turns.TurnLoopFadeIn = 0.08f;
        turns.TurnLoopFadeOut = 0.12f;
        turns.MinimumPivotAngle = 55f;
        turns.Pivot180Threshold = 135f;
        turns.MinimumPivotSpeed = 1.5f;
        turns.PivotFadeIn = 0.08f;
        turns.PivotFadeOut = 0.12f;
        turns.PivotBlendOutNormalizedTime = 0.8f;
        turns.CurveStartYawRate = 45f;
        turns.CurveExitYawRate = 15f;
        turns.MinimumCurveSpeed = 0.5f;
        turns.CurveExitDelay = 0.2f;
        turns.CurveFadeIn = 0.12f;
        turns.CurveFadeOut = 0.14f;
        turns.StandLeft45 = LoadIpc("MOB1_Stand_Relaxed_L_45_IPC");
        turns.StandLeft90 = LoadIpc("MOB1_Stand_Relaxed_L_90_IPC");
        turns.StandLeft135 = LoadIpc("MOB1_Stand_Relaxed_L_135_IPC");
        turns.StandRight45 = LoadIpc("MOB1_Stand_Relaxed_R_45_IPC");
        turns.StandRight90 = LoadIpc("MOB1_Stand_Relaxed_R_90_IPC");
        turns.StandRight135 = LoadIpc("MOB1_Stand_Relaxed_R_135_IPC");
        turns.StandLeft180 = LoadIpc("MOB1_Stand_Relaxed_L_180_IPC");
        turns.StandRight180 = LoadIpc("MOB1_Stand_Relaxed_R_180_IPC");
        turns.StandTurnLeftLoop = LoadIpc("MOB1_Stand_Rlx_Turn_In_Place_L_Loop_IPC");
        turns.StandTurnRightLoop = LoadIpc("MOB1_Stand_Rlx_Turn_In_Place_R_Loop_IPC");
        turns.CrouchLeft45 = LoadIpc("MOB1_Crouch_L_45_IPC");
        turns.CrouchLeft90 = LoadIpc("MOB1_Crouch_L_90_IPC");
        turns.CrouchLeft135 = LoadIpc("MOB1_Crouch_L_135_IPC");
        turns.CrouchRight45 = LoadIpc("MOB1_Crouch_R_45_IPC");
        turns.CrouchRight90 = LoadIpc("MOB1_Crouch_R_90_IPC");
        turns.CrouchRight135 = LoadIpc("MOB1_Crouch_R_135_IPC");
        turns.CrouchLeft180 = LoadIpc("MOB1_Crouch_L_180_IPC");
        turns.CrouchRight180 = LoadIpc("MOB1_Crouch_R_180_IPC");
        turns.CrouchTurnLeftLoop = LoadIpc("MOB1_Crouch_Rlx_Turn_In_Place_L_Loop_IPC");
        turns.CrouchTurnRightLoop = LoadIpc("MOB1_Crouch_Rlx_Turn_In_Place_R_Loop_IPC");
        PopulateDirectionalTurns(turns.Walk, "Walk");
        PopulateDirectionalTurns(turns.Jog, "Jog");
        PopulateDirectionalTurns(turns.Run, "Run");
        PopulateCurveSet(turns.WalkCurves, "Walk", false);
        PopulateCurveSet(turns.JogCurves, "Jog", false);
        PopulateCurveSet(turns.RunCurves, "Run", false);
        PopulateCurveSet(turns.CrouchCurves, "CrouchWalk", false);
        PopulateCurveSet(turns.WalkBackpedalCurves, "Walk", true);
        PopulateCurveSet(turns.JogBackpedalCurves, "Jog", true);
        PopulateCurveSet(turns.CrouchBackpedalCurves, "CrouchWalk", true);
    }

    private static void PopulateDirectionalTurns(CharacterAnimationConfig.DirectionalTurnSet set, string gait)
    {
        set.Left90 = LoadIpc($"MOB1_{gait}_L_90_IPC");
        set.Right90 = LoadIpc($"MOB1_{gait}_R_90_IPC");
        set.Left180 = LoadIpc($"MOB1_{gait}_L_180_IPC");
        set.Right180 = LoadIpc($"MOB1_{gait}_R_180_IPC");
    }

    private static void PopulateCurveSet(CharacterAnimationConfig.DirectionalLoopSet set, string gait, bool backpedal)
    {
        string suffix = backpedal ? "BkPd" : "CIR";
        set.Left = LoadIpc($"MOB1_{gait}_L_{suffix}_Loop_IPC");
        set.Right = LoadIpc($"MOB1_{gait}_R_{suffix}_Loop_IPC");
    }

    private static void PopulateClipTuningLibrary(CharacterAnimationConfig config)
    {
        Dictionary<AnimationClip, CharacterAnimationConfig.MobilityClipTuning> existing =
            (config.ClipTunings ?? Array.Empty<CharacterAnimationConfig.MobilityClipTuning>())
            .Where(tuning => tuning != null && tuning.Clip != null)
            .GroupBy(tuning => tuning.Clip)
            .ToDictionary(group => group.Key, group => group.First());

        string[] modelPaths = AssetDatabase.FindAssets("t:Model", new[] { PackRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        List<CharacterAnimationConfig.MobilityClipTuning> tunings =
            new List<CharacterAnimationConfig.MobilityClipTuning>(modelPaths.Length);
        for (int i = 0; i < modelPaths.Length; i++)
        {
            AnimationClip clip = LoadAnimationClip(modelPaths[i]);
            MobilityAnimationCategory category = CategorizeClip(clip.name, modelPaths[i]);
            if (!existing.TryGetValue(clip, out CharacterAnimationConfig.MobilityClipTuning tuning))
                tuning = CreateDefaultTuning(clip, category, CharacterAnimationSource.MobilityPro);

            tuning.Source = CharacterAnimationSource.MobilityPro;
            tuning.Category = category;
            tuning.Role = GetClipRole(clip.name, category);
            if (tuning.Transition == null) tuning.Transition = new ClipTransition { Clip = clip };
            else tuning.Transition.Clip = clip;
            if (tuning.EndNormalizedTime <= 0f) tuning.EndNormalizedTime = 1f;
            tuning.ApplyEndTimeToTransition();
            tunings.Add(tuning);
        }

        for (int i = 0; i < OriginalLocomotionClipNames.Length; i++)
        {
            AnimationClip clip = LoadOriginal(OriginalLocomotionClipNames[i]);
            MobilityAnimationCategory category = CategorizeOriginalClip(clip.name);
            bool isNew = !existing.TryGetValue(clip, out CharacterAnimationConfig.MobilityClipTuning tuning);
            if (isNew)
                tuning = CreateDefaultTuning(clip, category, CharacterAnimationSource.ProjectOriginal);

            tuning.Source = CharacterAnimationSource.ProjectOriginal;
            tuning.Category = category;
            tuning.Role = "Original Animator / " + clip.name;
            if (tuning.Transition == null) tuning.Transition = new ClipTransition { Clip = clip };
            else tuning.Transition.Clip = clip;
            if (tuning.EndNormalizedTime <= 0f) tuning.EndNormalizedTime = 1f;
            if (isNew) ApplyLegacyCameraPoseDefaults(tuning, clip.name);
            tuning.ApplyEndTimeToTransition();
            tunings.Add(tuning);
        }

        config.ClipTunings = tunings
            .OrderBy(tuning => tuning.Category)
            .ThenBy(tuning => tuning.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CharacterAnimationConfig.MobilityClipTuning CreateDefaultTuning(
        AnimationClip clip,
        MobilityAnimationCategory category,
        CharacterAnimationSource source)
    {
        float fadeIn;
        float fadeOut;
        float blendOut;
        switch (category)
        {
            case MobilityAnimationCategory.Idle:
                fadeIn = 0.2f;
                fadeOut = 0.2f;
                blendOut = 0.95f;
                break;
            case MobilityAnimationCategory.Start:
                fadeIn = 0.08f;
                fadeOut = 0.1f;
                blendOut = 0.72f;
                break;
            case MobilityAnimationCategory.Stop:
                fadeIn = 0.08f;
                fadeOut = 0.12f;
                blendOut = 0.84f;
                break;
            case MobilityAnimationCategory.TurnInPlace:
            case MobilityAnimationCategory.MovementPivot:
                fadeIn = 0.08f;
                fadeOut = 0.12f;
                blendOut = 0.82f;
                break;
            case MobilityAnimationCategory.MovementCurve:
            case MobilityAnimationCategory.LocomotionLoop:
                fadeIn = 0.12f;
                fadeOut = 0.14f;
                blendOut = 0.95f;
                break;
            case MobilityAnimationCategory.Jump:
            case MobilityAnimationCategory.Hop:
                fadeIn = 0.08f;
                fadeOut = 0.12f;
                blendOut = 0.86f;
                break;
            default:
                fadeIn = 0.12f;
                fadeOut = 0.12f;
                blendOut = 0.88f;
                break;
        }

        ClipTransition transition = new ClipTransition { Clip = clip };
        transition.FadeDuration = fadeIn;
        transition.Speed = 1f;
        transition.NormalizedStartTime = 0f;
        return new CharacterAnimationConfig.MobilityClipTuning
        {
            Source = source,
            Category = category,
            Role = GetClipRole(clip.name, category),
            Transition = transition,
            FadeOut = fadeOut,
            BlendOutNormalizedTime = blendOut,
            EndNormalizedTime = 1f,
            OverrideRuntime = true,
            ApplyFootIK = true,
            CameraLocalPosition = new Vector3(0.028f, 1.3f, 0.368f),
            CameraBlendSpeed = 8f
        };
    }

    private static MobilityAnimationCategory CategorizeOriginalClip(string name)
    {
        if (name.Contains("Jump") || name.Contains("InAir") || name.Contains("Land"))
            return MobilityAnimationCategory.Jump;
        if (name.Contains("Idle")) return MobilityAnimationCategory.Idle;
        if (name.Contains("Walk") || name.Contains("Sprint") || name.Contains("Crouch_Fwd"))
            return MobilityAnimationCategory.LocomotionLoop;
        return MobilityAnimationCategory.Other;
    }

    private static void ApplyLegacyCameraPoseDefaults(
        CharacterAnimationConfig.MobilityClipTuning tuning,
        string clipName)
    {
        switch (clipName)
        {
            case "A_Sprint_F_Masc":
                tuning.OverrideCameraPosition = true;
                tuning.CameraLocalPosition = new Vector3(-0.05f, 1.4f, 0.7f);
                tuning.CameraBlendSpeed = 18f;
                break;
            case "A_Idle_Crouching_Masc":
                tuning.OverrideCameraPosition = true;
                tuning.CameraLocalPosition = new Vector3(0.05f, 1.05f, 0.76f);
                tuning.CameraBlendSpeed = 10f;
                break;
            case "A_Crouch_FwdStrafeF_Masc":
                tuning.OverrideCameraPosition = true;
                tuning.CameraLocalPosition = new Vector3(0.15f, 1.05f, 0.75f);
                tuning.CameraBlendSpeed = 10f;
                break;
            case "A_Jump_Idle_Masc":
                tuning.OverrideCameraPosition = true;
                tuning.CameraLocalPosition = new Vector3(0f, 1.5f, 0.75f);
                tuning.CameraBlendSpeed = 8f;
                break;
        }
    }

    private static MobilityAnimationCategory CategorizeClip(string name, string path)
    {
        if (name.Contains("_Conv_")) return MobilityAnimationCategory.Conversation;
        if (name.Contains("_Fgt_")) return MobilityAnimationCategory.Fight;
        if (name.Contains("_Death_")) return MobilityAnimationCategory.Death;
        if (name.Contains("_Hop")) return MobilityAnimationCategory.Hop;
        if (path.Contains("Split_Jumps") || name.Contains("_Jump")) return MobilityAnimationCategory.Jump;
        if (name.Contains("_Idle")) return MobilityAnimationCategory.Idle;
        if (name.Contains("Stand_Relaxed_To_Crouch") || name.Contains("Crouch_To_Stand_Relaxed"))
            return MobilityAnimationCategory.CrouchTransition;
        if (name.Contains("Stand_Relaxed_To_") || name.Contains("Crouch_To_CrouchWalk"))
            return MobilityAnimationCategory.Start;
        if (name.Contains("_To_Stand_Relaxed") || name.Contains("_To_Crouch"))
            return MobilityAnimationCategory.Stop;
        if (name.Contains("Turn_In_Place") ||
            ((name.Contains("Stand_Relaxed_") || name.Contains("MOB1_Crouch_")) &&
             (name.Contains("_45_") || name.Contains("_90_") || name.Contains("_135_") || name.Contains("_180_"))))
            return MobilityAnimationCategory.TurnInPlace;
        if ((name.Contains("_Walk_") || name.Contains("_Jog_") || name.Contains("_Run_")) &&
            (name.Contains("_90_") || name.Contains("_180_")))
            return MobilityAnimationCategory.MovementPivot;
        if (name.Contains("_CIR_Loop") || name.Contains("_L_BkPd_Loop") || name.Contains("_R_BkPd_Loop"))
            return MobilityAnimationCategory.MovementCurve;
        if (name.Contains("_Loop")) return MobilityAnimationCategory.LocomotionLoop;
        return MobilityAnimationCategory.Other;
    }

    private static string GetClipRole(string name, MobilityAnimationCategory category)
    {
        return category + " / " + name.Replace("MOB1_", string.Empty).Replace("_IPC", string.Empty);
    }

    private static void PopulateAirborne(CharacterAnimationConfig.AirborneSettings airborne)
    {
        PopulateAirbornePhase(airborne.JumpStarts, "Start");
        PopulateAirbornePhase(airborne.JumpAir, "Air");
        PopulateAirbornePhase(airborne.JumpLandings, "Land");
        airborne.Fall = airborne.JumpAir.Standing;
        airborne.ShortFall = airborne.JumpAir.Standing;
        airborne.LongFall = airborne.JumpAir.Standing;
        airborne.SoftLanding = airborne.JumpLandings.Standing;
        airborne.HardLanding = airborne.JumpLandings.Standing;
        airborne.MovingLanding = airborne.JumpLandings.Walk.LeftFoot.Forward;
        airborne.JumpFadeDuration = 0.08f;
        airborne.JumpFadeOut = 0.1f;
        airborne.AirFadeDuration = 0.1f;
        airborne.AirFadeOut = 0.12f;
        airborne.FallFadeDuration = 0.12f;
        airborne.FallFadeOut = 0.14f;
        airborne.LandingFadeDuration = 0.1f;
        airborne.LandingFadeOut = 0.12f;
        airborne.JumpStartBlendOutNormalizedTime = 0.78f;
        airborne.LandingBlendOutNormalizedTime = 0.88f;
        airborne.EnableGroundSnap = true;
        airborne.GroundCollisionMask = ~0;
        airborne.GroundSnapDistance = 0.35f;
        airborne.GroundProbeRadiusScale = 0.85f;
        airborne.GroundSnapSpeed = 12f;
        airborne.JumpGroundSnapDelay = 0.12f;
        airborne.GroundedGraceTime = 0.28f;
        airborne.FallAnimationMinSpeed = -4.5f;
        airborne.LandingMinimumAirTime = 0.32f;
        airborne.LandingMinimumFallSpeed = -6.5f;
        airborne.AlternateTakeoffFoot = true;
    }

    private static void RestoreOriginalAnimatorAnimations(CharacterAnimationConfig config)
    {
        CharacterAnimationConfig.LocomotionSettings locomotion = config.Locomotion;
        locomotion.Idle = LoadOriginal("A_Idle_Standing_Masc");
        locomotion.CrouchIdle = LoadOriginal("A_Idle_Crouching_Masc");
        locomotion.Walk.Forward = LoadOriginal("A_Walk_F_Masc");
        locomotion.Run.Forward = LoadOriginal("A_Sprint_F_Masc");
        locomotion.Crouch.Forward = LoadOriginal("A_Crouch_FwdStrafeF_Masc");

        CharacterAnimationConfig.AirborneSettings airborne = config.Airborne;
        airborne.JumpStarts.Standing = LoadOriginal("A_Jump_Idle_Masc");
        SetForwardForBothFeet(airborne.JumpStarts.Walk, LoadOriginal("A_Jump_Walking_Masc"));
        SetForwardForBothFeet(airborne.JumpStarts.Run, LoadOriginal("A_Jump_Running_Masc"));

        AnimationClip fall = LoadOriginal("A_InAir_FallShort_Masc");
        airborne.JumpAir.Standing = fall;
        airborne.Fall = fall;
        airborne.ShortFall = fall;
        airborne.LongFall = fall;

        AnimationClip idleLanding = LoadOriginal("A_Land_IdleSoft_Masc");
        AnimationClip walkLanding = LoadOriginal("A_Land_Walking_Masc");
        AnimationClip runLanding = LoadOriginal("A_Land_Running_Masc");
        airborne.JumpLandings.Standing = idleLanding;
        SetForwardForBothFeet(airborne.JumpLandings.Walk, walkLanding);
        SetForwardForBothFeet(airborne.JumpLandings.Run, runLanding);
        airborne.SoftLanding = idleLanding;
        airborne.MovingLanding = walkLanding;
    }

    private static void SetForwardForBothFeet(
        CharacterAnimationConfig.FootedDirectionalClipSet set,
        AnimationClip clip)
    {
        set.LeftFoot.Forward = clip;
        set.RightFoot.Forward = clip;
    }

    private static void PopulateAirbornePhase(CharacterAnimationConfig.AirbornePhaseSet phaseSet, string phase)
    {
        phaseSet.Standing = LoadSplit($"MOB1_Stand_Relaxed_Jump_{phase}_IPC");
        PopulateFootedPhase(phaseSet.Walk, "Walk", phase, true);
        PopulateFootedPhase(phaseSet.Jog, "Jog", phase, false);
        PopulateFootedPhase(phaseSet.Run, "Run", phase, false);
    }

    private static void PopulateFootedPhase(
        CharacterAnimationConfig.FootedDirectionalClipSet set,
        string gait,
        string phase,
        bool includeBackward)
    {
        PopulateJumpDirections(set.LeftFoot, gait, "LU", phase, includeBackward);
        PopulateJumpDirections(set.RightFoot, gait, "RU", phase, includeBackward);
    }

    private static void PopulateJumpDirections(
        CharacterAnimationConfig.DirectionalClipSet set,
        string gait,
        string foot,
        string phase,
        bool includeBackward)
    {
        set.Forward = LoadSplitWithTypoFallback($"MOB1_{gait}_F_Jump_{foot}_{phase}_IPC");
        set.Left = LoadSplitWithTypoFallback($"MOB1_{gait}_L_Jump_{foot}_{phase}_IPC");
        set.Right = LoadSplitWithTypoFallback($"MOB1_{gait}_R_Jump_{foot}_{phase}_IPC");
        if (includeBackward)
            set.Backward = LoadSplitWithTypoFallback($"MOB1_{gait}_B_Jump_{foot}_{phase}_IPC");
    }

    private static SceneConfigurationResult ConfigureTargetScene(CharacterAnimationConfig config)
    {
        if (!File.Exists(ToAbsolutePath(TargetScenePath)))
            throw new FileNotFoundException("Target scene was not found.", TargetScenePath);

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene targetScene = SceneManager.GetSceneByPath(TargetScenePath);
        bool openedForConfiguration = !targetScene.IsValid() || !targetScene.isLoaded;
        if (openedForConfiguration)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);

        try
        {
            PlayerController player = targetScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                .FirstOrDefault(candidate => candidate.gameObject.name == "Player")
                ?? targetScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                    .FirstOrDefault();
            if (player == null) throw new InvalidOperationException("No PlayerController was found in recover scene 0 (7).");

            GameObject playerObject = player.gameObject;
            Animator animator = playerObject.GetComponent<Animator>();
            if (animator == null) throw new InvalidOperationException("The scene player has no Animator component.");
            RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
            if (originalController == null)
            {
                originalController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(OriginalPlayerControllerPath);
                if (originalController == null)
                    throw new FileNotFoundException("The original Player Animator Controller was not found.", OriginalPlayerControllerPath);
                animator.runtimeAnimatorController = originalController;
            }

            AnimancerComponent animancer = playerObject.GetComponent<AnimancerComponent>();
            bool animancerAdded = animancer == null;
            if (animancerAdded) animancer = Undo.AddComponent<AnimancerComponent>(playerObject);

            CharacterAnimancerController controller = playerObject.GetComponent<CharacterAnimancerController>();
            bool controllerAdded = controller == null;
            if (controllerAdded) controller = Undo.AddComponent<CharacterAnimancerController>(playerObject);

            player.enabled = true;
            animancer.enabled = true;
            controller.enabled = true;
            animancer.Animator = animator;
            SerializedObject controllerObject = new SerializedObject(controller);
            SetObjectReference(controllerObject, "animancer", animancer);
            SetObjectReference(controllerObject, "animator", animator);
            SetObjectReference(controllerObject, "config", config);
            SetBoolean(controllerObject, "initializeOnAwake", true);
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject playerObjectData = new SerializedObject(player);
            SetObjectReference(playerObjectData, "animancerController", controller);
            SetBoolean(playerObjectData, "driveLocomotionAnimation", true);
            SetBoolean(playerObjectData, "preferAnimancerAnimation", true);
            MatchConfigSpeedsToScene(config, playerObjectData);
            playerObjectData.ApplyModifiedPropertiesWithoutUndo();

            InventoryHeldItemController heldItemController = playerObject.GetComponent<InventoryHeldItemController>();
            if (heldItemController != null)
            {
                SerializedObject heldItemData = new SerializedObject(heldItemController);
                SetObjectReference(heldItemData, "animancerController", controller);
                heldItemData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(heldItemController);
            }

            animator.applyRootMotion = false;
            animator.stabilizeFeet = config.Quality.StabilizeFeet;
            animator.feetPivotActive = config.Quality.FeetPivotActive;
            EditorUtility.SetDirty(animancer);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(config);
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene))
                    throw new InvalidOperationException("Unity could not save the configured recover scene 0 (7).");
            }

            return new SceneConfigurationResult
            {
                PlayerName = playerObject.name,
                AnimancerAdded = animancerAdded,
                ControllerAdded = controllerAdded,
                OriginalAnimatorControllerPreserved = originalController != null && animator.runtimeAnimatorController == originalController
            };
        }
        finally
        {
            if (openedForConfiguration && targetScene.IsValid() && targetScene.isLoaded)
                EditorSceneManager.CloseScene(targetScene, true);
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    private static void MatchConfigSpeedsToScene(CharacterAnimationConfig config, SerializedObject player)
    {
        SerializedProperty walkSpeed = player.FindProperty("walkSpeed");
        SerializedProperty runSpeed = player.FindProperty("runSpeed");
        SerializedProperty crouchSpeed = player.FindProperty("crouchSpeed");
        if (walkSpeed != null) config.Locomotion.WalkSpeed = Mathf.Max(0.1f, walkSpeed.floatValue);
        if (runSpeed != null) config.Locomotion.RunSpeed = Mathf.Max(config.Locomotion.WalkSpeed + 0.2f, runSpeed.floatValue);
        config.Locomotion.JogSpeed = Mathf.Lerp(config.Locomotion.WalkSpeed, config.Locomotion.RunSpeed, 0.5f);
        if (crouchSpeed != null) config.Locomotion.CrouchSpeed = Mathf.Max(0.1f, crouchSpeed.floatValue);
    }

    private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null) throw new MissingFieldException(target.targetObject.GetType().Name, propertyName);
        property.objectReferenceValue = value;
    }

    private static void SetBoolean(SerializedObject target, string propertyName, bool value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null) throw new MissingFieldException(target.targetObject.GetType().Name, propertyName);
        property.boolValue = value;
    }

    private static AnimationClip LoadIpc(string fileName)
    {
        return LoadAnimationClip(PackRoot + fileName + ".fbx");
    }

    private static AnimationClip LoadOriginal(string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            OriginalLocomotionRoot + clipName + ".anim");
        if (clip == null)
            throw new InvalidOperationException("Original animation clip is missing: " + clipName);
        return clip;
    }

    private static AnimationClip LoadSplit(string fileName)
    {
        return LoadAnimationClip(SplitJumpRoot + fileName + ".fbx");
    }

    private static AnimationClip LoadSplitWithTypoFallback(string fileName)
    {
        string path = SplitJumpRoot + fileName + ".fbx";
        if (!File.Exists(ToAbsolutePath(path)) && fileName.Contains("_LU_Air_IPC"))
            path = SplitJumpRoot + fileName.Replace("_LU_Air_IPC", "_LU_ Air_IPC") + ".fbx";
        return LoadAnimationClip(path);
    }

    private static AnimationClip LoadAnimationClip(string assetPath)
    {
        if (!File.Exists(ToAbsolutePath(assetPath)))
            throw new FileNotFoundException("Required MOBILITY PRO animation is missing.", assetPath);

        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        if (clip == null) throw new InvalidOperationException("No AnimationClip sub-asset was imported from " + assetPath);
        return clip;
    }

    private static int CountConfiguredClips(CharacterAnimationConfig config)
    {
        SerializedObject serialized = new SerializedObject(config);
        SerializedProperty iterator = serialized.GetIterator();
        int count = 0;
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = true;
            if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue is AnimationClip)
                count++;
        }
        return count;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Unity project root could not be resolved.");
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void WriteResult(string result)
    {
        string resultPath = ToAbsolutePath("Temp/CodexMobilityProConfiguration.result");
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath) ?? ToAbsolutePath("Temp"));
        File.WriteAllText(resultPath, result);
    }

    private struct SceneConfigurationResult
    {
        public string PlayerName;
        public bool AnimancerAdded;
        public bool ControllerAdded;
        public bool OriginalAnimatorControllerPreserved;
    }
}
