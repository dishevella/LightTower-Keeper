using System;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Light Tower/Animation/Character Animation Config", fileName = "CharacterAnimationConfig")]
public class CharacterAnimationConfig : ScriptableObject
{
    [Serializable]
    public class DirectionalClipSet
    {
        [HorizontalGroup("Forward"), PreviewField(52), AssetsOnly] public AnimationClip ForwardLeft;
        [HorizontalGroup("Forward"), PreviewField(52), AssetsOnly] public AnimationClip Forward;
        [HorizontalGroup("Forward"), PreviewField(52), AssetsOnly] public AnimationClip ForwardRight;
        [HorizontalGroup("Sides"), PreviewField(52), AssetsOnly] public AnimationClip Left;
        [HorizontalGroup("Sides"), PreviewField(52), AssetsOnly] public AnimationClip Right;
        [HorizontalGroup("Backward"), PreviewField(52), AssetsOnly] public AnimationClip BackwardLeft;
        [HorizontalGroup("Backward"), PreviewField(52), AssetsOnly] public AnimationClip Backward;
        [HorizontalGroup("Backward"), PreviewField(52), AssetsOnly] public AnimationClip BackwardRight;

        public bool HasAnyClip()
        {
            return Forward != null || Backward != null || Left != null || Right != null ||
                   ForwardLeft != null || ForwardRight != null || BackwardLeft != null || BackwardRight != null;
        }

        public int CountClips()
        {
            int count = 0;
            if (Forward != null) count++;
            if (Backward != null) count++;
            if (Left != null) count++;
            if (Right != null) count++;
            if (ForwardLeft != null) count++;
            if (ForwardRight != null) count++;
            if (BackwardLeft != null) count++;
            if (BackwardRight != null) count++;
            return count;
        }

        public AnimationClip GetBest(Vector2 direction)
        {
            if (!HasAnyClip()) return null;
            if (direction.sqrMagnitude < 0.0001f) return FirstAssigned();

            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            AnimationClip selected;
            if (angle >= -22.5f && angle < 22.5f) selected = Forward;
            else if (angle >= 22.5f && angle < 67.5f) selected = ForwardRight;
            else if (angle >= 67.5f && angle < 112.5f) selected = Right;
            else if (angle >= 112.5f && angle < 157.5f) selected = BackwardRight;
            else if (angle >= 157.5f || angle < -157.5f) selected = Backward;
            else if (angle >= -157.5f && angle < -112.5f) selected = BackwardLeft;
            else if (angle >= -112.5f && angle < -67.5f) selected = Left;
            else selected = ForwardLeft;

            if (selected != null) return selected;

            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);
            if (absY >= absX)
            {
                selected = direction.y >= 0f ? Forward : Backward;
                if (selected != null) return selected;
            }
            else
            {
                selected = direction.x >= 0f ? Right : Left;
                if (selected != null) return selected;
            }

            return FirstAssigned();
        }

        public AnimationClip FirstAssigned()
        {
            if (Forward != null) return Forward;
            if (ForwardLeft != null) return ForwardLeft;
            if (ForwardRight != null) return ForwardRight;
            if (Left != null) return Left;
            if (Right != null) return Right;
            if (Backward != null) return Backward;
            if (BackwardLeft != null) return BackwardLeft;
            return BackwardRight;
        }
    }

    [Serializable]
    public class GaitDirectionalClipSet
    {
        [FoldoutGroup("Walk")] public DirectionalClipSet Walk = new DirectionalClipSet();
        [FoldoutGroup("Jog")] public DirectionalClipSet Jog = new DirectionalClipSet();
        [FoldoutGroup("Run")] public DirectionalClipSet Run = new DirectionalClipSet();
        [FoldoutGroup("Crouch")] public DirectionalClipSet Crouch = new DirectionalClipSet();

        public DirectionalClipSet Get(LocomotionGait gait)
        {
            switch (gait)
            {
                case LocomotionGait.Run: return Run;
                case LocomotionGait.Jog: return Jog;
                case LocomotionGait.Crouch: return Crouch;
                default: return Walk;
            }
        }
    }

    [Serializable]
    public class FootMatchedDirectionalClipSet
    {
        [FoldoutGroup("Neutral")] public DirectionalClipSet Neutral = new DirectionalClipSet();
        [FoldoutGroup("Left Foot Up")] public DirectionalClipSet LeftFoot = new DirectionalClipSet();
        [FoldoutGroup("Right Foot Up")] public DirectionalClipSet RightFoot = new DirectionalClipSet();

        public AnimationClip GetBest(Vector2 direction, bool useLeftFoot)
        {
            DirectionalClipSet preferred = useLeftFoot ? LeftFoot : RightFoot;
            DirectionalClipSet fallback = useLeftFoot ? RightFoot : LeftFoot;
            AnimationClip clip = preferred.GetBest(direction);
            if (clip != null) return clip;
            clip = fallback.GetBest(direction);
            return clip != null ? clip : Neutral.GetBest(direction);
        }
    }

    [Serializable]
    public class GaitFootMatchedClipSet
    {
        [FoldoutGroup("Walk")] public FootMatchedDirectionalClipSet Walk = new FootMatchedDirectionalClipSet();
        [FoldoutGroup("Jog")] public FootMatchedDirectionalClipSet Jog = new FootMatchedDirectionalClipSet();
        [FoldoutGroup("Run")] public FootMatchedDirectionalClipSet Run = new FootMatchedDirectionalClipSet();
        [FoldoutGroup("Crouch")] public FootMatchedDirectionalClipSet Crouch = new FootMatchedDirectionalClipSet();

        public FootMatchedDirectionalClipSet Get(LocomotionGait gait)
        {
            switch (gait)
            {
                case LocomotionGait.Run: return Run;
                case LocomotionGait.Jog: return Jog;
                case LocomotionGait.Crouch: return Crouch;
                default: return Walk;
            }
        }
    }

    [Serializable]
    public class FootedDirectionalClipSet
    {
        [FoldoutGroup("Left Foot Up")] public DirectionalClipSet LeftFoot = new DirectionalClipSet();
        [FoldoutGroup("Right Foot Up")] public DirectionalClipSet RightFoot = new DirectionalClipSet();

        public AnimationClip GetBest(Vector2 direction, bool useLeftFoot)
        {
            DirectionalClipSet preferred = useLeftFoot ? LeftFoot : RightFoot;
            DirectionalClipSet fallback = useLeftFoot ? RightFoot : LeftFoot;
            AnimationClip clip = preferred.GetBest(direction);
            return clip != null ? clip : fallback.GetBest(direction);
        }
    }

    [Serializable]
    public class AirbornePhaseSet
    {
        [BoxGroup("Standing"), PreviewField(55), AssetsOnly] public AnimationClip Standing;
        [FoldoutGroup("Walk")] public FootedDirectionalClipSet Walk = new FootedDirectionalClipSet();
        [FoldoutGroup("Jog")] public FootedDirectionalClipSet Jog = new FootedDirectionalClipSet();
        [FoldoutGroup("Run")] public FootedDirectionalClipSet Run = new FootedDirectionalClipSet();

        public AnimationClip GetBest(LocomotionGait gait, Vector2 direction, bool useLeftFoot)
        {
            AnimationClip clip = null;
            switch (gait)
            {
                case LocomotionGait.Run:
                    clip = Run.GetBest(direction, useLeftFoot);
                    break;
                case LocomotionGait.Jog:
                    clip = Jog.GetBest(direction, useLeftFoot);
                    break;
                case LocomotionGait.Walk:
                    clip = Walk.GetBest(direction, useLeftFoot);
                    break;
            }

            return clip != null ? clip : Standing;
        }
    }

    [Serializable]
    public class TurnSettings
    {
        [BoxGroup("Features")] public bool EnableTurnInPlace = true;
        [BoxGroup("Features")] public bool EnableContinuousTurnLoops = true;
        [BoxGroup("Features")] public bool EnableMovementPivots = true;
        [BoxGroup("Features")] public bool EnableMovementCurves = true;

        [BoxGroup("Discrete Turn Rules"), Range(0f, 180f)] public float MinimumTurnAngle = 25f;
        [BoxGroup("Discrete Turn Rules"), Range(0f, 180f)] public float Turn90Threshold = 67.5f;
        [BoxGroup("Discrete Turn Rules"), Range(0f, 180f)] public float Turn135Threshold = 112.5f;
        [BoxGroup("Discrete Turn Rules"), Range(0f, 180f)] public float Turn180Threshold = 157.5f;
        [BoxGroup("Discrete Turn Rules"), MinValue(0f)] public float TurnInputSettleTime = 0.08f;
        [BoxGroup("Discrete Turn Rules"), MinValue(0f)] public float TurnCrossFade = 0.1f;
        [BoxGroup("Discrete Turn Rules"), Range(0f, 1f)] public float TurnBlendOutNormalizedTime = 0.82f;

        [BoxGroup("Continuous Turn Rules"), MinValue(0f)] public float TurnLoopStartYawRate = 75f;
        [BoxGroup("Continuous Turn Rules"), MinValue(0f)] public float TurnLoopExitDelay = 0.08f;
        [BoxGroup("Continuous Turn Rules"), MinValue(0f)] public float TurnLoopFadeIn = 0.08f;
        [BoxGroup("Continuous Turn Rules"), MinValue(0f)] public float TurnLoopFadeOut = 0.12f;

        [BoxGroup("Pivot Rules"), Range(0f, 180f)] public float MinimumPivotAngle = 55f;
        [BoxGroup("Pivot Rules"), Range(0f, 180f)] public float Pivot180Threshold = 135f;
        [BoxGroup("Pivot Rules"), MinValue(0f)] public float MinimumPivotSpeed = 1.5f;
        [BoxGroup("Pivot Rules"), MinValue(0f)] public float PivotFadeIn = 0.08f;
        [BoxGroup("Pivot Rules"), MinValue(0f)] public float PivotFadeOut = 0.12f;
        [BoxGroup("Pivot Rules"), Range(0f, 1f)] public float PivotBlendOutNormalizedTime = 0.8f;

        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveStartYawRate = 25f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float MinimumCurveSpeed = 0.5f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveExitDelay = 0.1f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveFadeIn = 0.12f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveFadeOut = 0.14f;

        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandLeft45;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandLeft90;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandLeft135;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandRight90;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandRight45;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandRight135;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandLeft180;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandRight180;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandTurnLeftLoop;
        [BoxGroup("Standing"), PreviewField(45), AssetsOnly] public AnimationClip StandTurnRightLoop;

        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft45;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft90;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft135;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchRight90;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchRight45;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchRight135;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft180;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchRight180;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchTurnLeftLoop;
        [BoxGroup("Crouch"), PreviewField(45), AssetsOnly] public AnimationClip CrouchTurnRightLoop;

        [FoldoutGroup("Movement Pivot/Walk")] public DirectionalTurnSet Walk = new DirectionalTurnSet();
        [FoldoutGroup("Movement Pivot/Jog")] public DirectionalTurnSet Jog = new DirectionalTurnSet();
        [FoldoutGroup("Movement Pivot/Run")] public DirectionalTurnSet Run = new DirectionalTurnSet();
        [FoldoutGroup("Movement Curves/Walk")] public DirectionalLoopSet WalkCurves = new DirectionalLoopSet();
        [FoldoutGroup("Movement Curves/Jog")] public DirectionalLoopSet JogCurves = new DirectionalLoopSet();
        [FoldoutGroup("Movement Curves/Run")] public DirectionalLoopSet RunCurves = new DirectionalLoopSet();
        [FoldoutGroup("Movement Curves/Crouch")] public DirectionalLoopSet CrouchCurves = new DirectionalLoopSet();
        [FoldoutGroup("Backpedal Curves/Walk")] public DirectionalLoopSet WalkBackpedalCurves = new DirectionalLoopSet();
        [FoldoutGroup("Backpedal Curves/Jog")] public DirectionalLoopSet JogBackpedalCurves = new DirectionalLoopSet();
        [FoldoutGroup("Backpedal Curves/Crouch")] public DirectionalLoopSet CrouchBackpedalCurves = new DirectionalLoopSet();

        public AnimationClip GetDiscreteTurn(bool crouching, float signedAngle)
        {
            bool left = signedAngle < 0f;
            float angle = Mathf.Abs(signedAngle);
            if (crouching)
            {
                if (angle >= Turn180Threshold) return left ? CrouchLeft180 : CrouchRight180;
                if (angle >= Turn135Threshold) return left ? CrouchLeft135 : CrouchRight135;
                if (angle >= Turn90Threshold) return left ? CrouchLeft90 : CrouchRight90;
                return left ? CrouchLeft45 : CrouchRight45;
            }

            if (angle >= Turn180Threshold) return left ? StandLeft180 : StandRight180;
            if (angle >= Turn135Threshold) return left ? StandLeft135 : StandRight135;
            if (angle >= Turn90Threshold) return left ? StandLeft90 : StandRight90;
            return left ? StandLeft45 : StandRight45;
        }

        public AnimationClip GetPivot(LocomotionGait gait, float signedAngle)
        {
            DirectionalTurnSet set = gait == LocomotionGait.Run ? Run : gait == LocomotionGait.Jog ? Jog : Walk;
            return set.Get(signedAngle, Pivot180Threshold);
        }

        public AnimationClip GetTurnLoop(bool crouching, float signedYawRate)
        {
            if (crouching)
                return signedYawRate < 0f ? CrouchTurnLeftLoop : CrouchTurnRightLoop;
            return signedYawRate < 0f ? StandTurnLeftLoop : StandTurnRightLoop;
        }

        public AnimationClip GetMovementCurve(LocomotionGait gait, bool movingBackward, float signedYawRate)
        {
            DirectionalLoopSet set;
            if (movingBackward)
            {
                set = gait == LocomotionGait.Crouch
                    ? CrouchBackpedalCurves
                    : gait == LocomotionGait.Jog
                        ? JogBackpedalCurves
                        : WalkBackpedalCurves;
            }
            else
            {
                set = gait == LocomotionGait.Run
                    ? RunCurves
                    : gait == LocomotionGait.Jog
                        ? JogCurves
                        : gait == LocomotionGait.Crouch
                            ? CrouchCurves
                            : WalkCurves;
            }

            return set.Get(signedYawRate);
        }
    }

    [Serializable]
    public class DirectionalLoopSet
    {
        [PreviewField(45), AssetsOnly] public AnimationClip Left;
        [PreviewField(45), AssetsOnly] public AnimationClip Right;

        public AnimationClip Get(float signedValue)
        {
            return signedValue < 0f ? Left : Right;
        }
    }

    [Serializable]
    public class DirectionalTurnSet
    {
        [PreviewField(45), AssetsOnly] public AnimationClip Left90;
        [PreviewField(45), AssetsOnly] public AnimationClip Right90;
        [PreviewField(45), AssetsOnly] public AnimationClip Left180;
        [PreviewField(45), AssetsOnly] public AnimationClip Right180;

        public AnimationClip Get(float signedAngle, float turn180Threshold)
        {
            bool left = signedAngle < 0f;
            bool turn180 = Mathf.Abs(signedAngle) >= turn180Threshold;
            return turn180 ? (left ? Left180 : Right180) : (left ? Left90 : Right90);
        }
    }

    [Serializable]
    public class LocomotionSettings
    {
        [BoxGroup("Idle"), Required, PreviewField(65), AssetsOnly] public AnimationClip Idle;
        [BoxGroup("Idle"), PreviewField(55), AssetsOnly] public AnimationClip[] IdleVariations = Array.Empty<AnimationClip>();
        [BoxGroup("Idle"), Required, PreviewField(65), AssetsOnly] public AnimationClip CrouchIdle;
        [BoxGroup("Idle")] public bool EnableIdleVariations = true;
        [BoxGroup("Idle"), MinValue(0f)] public float IdleVariationInterval = 9f;
        [BoxGroup("Idle"), MinValue(0f)] public float IdleVariationCrossFade = 0.2f;

        [FoldoutGroup("Loops/Walk")] public DirectionalClipSet Walk = new DirectionalClipSet();
        [FoldoutGroup("Loops/Jog")] public DirectionalClipSet Jog = new DirectionalClipSet();
        [FoldoutGroup("Loops/Run")] public DirectionalClipSet Run = new DirectionalClipSet();
        [FoldoutGroup("Loops/Crouch")] public DirectionalClipSet Crouch = new DirectionalClipSet();

        [FoldoutGroup("Transitions/Starts")] public GaitDirectionalClipSet Starts = new GaitDirectionalClipSet();
        [FoldoutGroup("Transitions/Forward Turning Starts")] public GaitDirectionalClipSet ForwardTurningStarts = new GaitDirectionalClipSet();
        [FoldoutGroup("Transitions/Stops")] public GaitDirectionalClipSet Stops = new GaitDirectionalClipSet();
        [FoldoutGroup("Transitions/Foot Matched Stops")] public GaitFootMatchedClipSet FootMatchedStops = new GaitFootMatchedClipSet();
        [FoldoutGroup("Transitions/Crouch"), PreviewField(55), AssetsOnly] public AnimationClip StandToCrouch;
        [FoldoutGroup("Transitions/Crouch"), PreviewField(55), AssetsOnly] public AnimationClip CrouchToStand;

        [BoxGroup("Transition Rules")] public bool EnableStartTransitions = true;
        [BoxGroup("Transition Rules")] public bool EnableStopTransitions = true;
        [BoxGroup("Transition Rules")] public bool EnableFootMatchedStops = true;
        [BoxGroup("Transition Rules")] public bool UseForwardTurningStarts;
        [BoxGroup("Transition Rules")] public bool EnableCrouchTransitions = true;
        [BoxGroup("Transition Rules"), LabelText("Locomotion Fade In"), MinValue(0f)] public float LocomotionCrossFade = 0.12f;
        [BoxGroup("Transition Rules"), LabelText("Locomotion Fade Out"), MinValue(0f)] public float LocomotionFadeOut = 0.14f;
        [BoxGroup("Transition Rules"), LabelText("Start Fade In"), MinValue(0f)] public float StartCrossFade = 0.08f;
        [BoxGroup("Transition Rules"), LabelText("Start Fade Out"), MinValue(0f)] public float StartFadeOut = 0.1f;
        [BoxGroup("Transition Rules"), LabelText("Stop Fade In"), MinValue(0f)] public float StopCrossFade = 0.1f;
        [BoxGroup("Transition Rules"), LabelText("Stop Fade Out"), MinValue(0f)] public float StopFadeOut = 0.12f;
        [BoxGroup("Transition Rules"), LabelText("Crouch Fade In"), MinValue(0f)] public float CrouchCrossFade = 0.1f;
        [BoxGroup("Transition Rules"), LabelText("Crouch Fade Out"), MinValue(0f)] public float CrouchFadeOut = 0.12f;
        [BoxGroup("Transition Rules"), Range(0f, 1f)] public float StartBlendOutNormalizedTime = 0.72f;
        [BoxGroup("Transition Rules"), Range(0f, 1f)] public float StopBlendOutNormalizedTime = 0.82f;
        [BoxGroup("Transition Rules"), Range(0f, 1f)] public float CrouchBlendOutNormalizedTime = 0.82f;
        [BoxGroup("Transition Rules"), MinValue(0f)] public float StartInputThreshold = 0.15f;
        [BoxGroup("Transition Rules"), MinValue(0f)] public float StopInputThreshold = 0.08f;

        [BoxGroup("Velocity Smoothing"), MinValue(0f)] public float AccelerationSmoothTime = 0.1f;
        [BoxGroup("Velocity Smoothing"), MinValue(0f)] public float DecelerationSmoothTime = 0.14f;
        [BoxGroup("Velocity Smoothing"), MinValue(0f)] public float DirectionSmoothTime = 0.1f;
        [BoxGroup("Velocity Smoothing"), MinValue(0f)] public float MaximumVisualAcceleration = 30f;
        [BoxGroup("Velocity Smoothing"), MinValue(0f)] public float IdleSpeedThreshold = 0.05f;

        [BoxGroup("Mixer Speed Thresholds"), MinValue(0f)] public float WalkSpeed = 2f;
        [BoxGroup("Mixer Speed Thresholds"), MinValue(0f)] public float JogSpeed = 3f;
        [BoxGroup("Mixer Speed Thresholds"), MinValue(0f)] public float RunSpeed = 4f;
        [BoxGroup("Mixer Speed Thresholds"), MinValue(0f)] public float CrouchSpeed = 1f;
        [BoxGroup("Mixer Speed Thresholds"), MinValue(0f)] public float GaitHysteresis = 0.15f;

        [BoxGroup("Playback")] public bool SynchronizeLocomotionCycles = true;
        [BoxGroup("Playback"), Range(0.25f, 2f)] public float GlobalPlaybackSpeed = 1f;
        [BoxGroup("Playback"), Range(0.25f, 2f)] public float MinimumPlaybackSpeed = 0.85f;
        [BoxGroup("Playback"), Range(0.25f, 2f)] public float MaximumPlaybackSpeed = 1.2f;

        [FoldoutGroup("Turns And Pivots")] public TurnSettings Turns = new TurnSettings();
    }

    [Serializable]
    public class AirborneSettings
    {
        [FoldoutGroup("Directional Jump/Start")] public AirbornePhaseSet JumpStarts = new AirbornePhaseSet();
        [FoldoutGroup("Directional Jump/Air")] public AirbornePhaseSet JumpAir = new AirbornePhaseSet();
        [FoldoutGroup("Directional Jump/Landing")] public AirbornePhaseSet JumpLandings = new AirbornePhaseSet();

        [BoxGroup("Fallback Fall"), PreviewField(55), AssetsOnly] public AnimationClip Fall;
        [BoxGroup("Fallback Fall"), PreviewField(55), AssetsOnly] public AnimationClip ShortFall;
        [BoxGroup("Fallback Fall"), PreviewField(55), AssetsOnly] public AnimationClip LongFall;
        [BoxGroup("Fallback Landing"), PreviewField(55), AssetsOnly] public AnimationClip SoftLanding;
        [BoxGroup("Fallback Landing"), PreviewField(55), AssetsOnly] public AnimationClip HardLanding;
        [BoxGroup("Fallback Landing"), PreviewField(55), AssetsOnly] public AnimationClip MovingLanding;

        [BoxGroup("Timing"), LabelText("Jump Fade In"), MinValue(0f)] public float JumpFadeDuration = 0.08f;
        [BoxGroup("Timing"), LabelText("Jump Fade Out"), MinValue(0f)] public float JumpFadeOut = 0.1f;
        [BoxGroup("Timing"), LabelText("Air Fade In"), MinValue(0f)] public float AirFadeDuration = 0.1f;
        [BoxGroup("Timing"), LabelText("Air Fade Out"), MinValue(0f)] public float AirFadeOut = 0.12f;
        [BoxGroup("Timing"), LabelText("Fall Fade In"), MinValue(0f)] public float FallFadeDuration = 0.12f;
        [BoxGroup("Timing"), LabelText("Fall Fade Out"), MinValue(0f)] public float FallFadeOut = 0.14f;
        [BoxGroup("Timing"), LabelText("Landing Fade In"), MinValue(0f)] public float LandingFadeDuration = 0.1f;
        [BoxGroup("Timing"), LabelText("Landing Fade Out"), MinValue(0f)] public float LandingFadeOut = 0.12f;
        [BoxGroup("Timing"), Range(0f, 1f)] public float JumpStartBlendOutNormalizedTime = 0.78f;
        [BoxGroup("Timing"), Range(0f, 1f)] public float LandingBlendOutNormalizedTime = 0.88f;
        [BoxGroup("Timing"), MinValue(0f)] public float ShortFallTime = 0.35f;
        [BoxGroup("Timing"), MinValue(0f)] public float LongFallTime = 1.1f;
        [BoxGroup("Timing"), MinValue(0f)] public float LandingLockTime = 0.15f;
        [BoxGroup("Timing"), MinValue(0f)] public float InterruptibleAfter = 0.1f;
        [BoxGroup("Landing Rules")] public float HardLandingMinFallSpeed = -8f;
        [BoxGroup("Landing Rules"), MinValue(0f)] public float HardLandingMinFallHeight = 2.5f;
        [BoxGroup("Foot Selection")] public bool AlternateTakeoffFoot = true;
    }

    [Serializable]
    public class HoldingSettings
    {
        [PreviewField(65), AssetsOnly] public AnimationClip Clip;
        [AssetsOnly] public AvatarMask AvatarMask;
        [Range(0f, 1f)] public float DefaultWeight = 1f;
        [MinValue(0f)] public float FadeIn = 0.12f;
        [MinValue(0f)] public float FadeOut = 0.15f;
        public bool Loop = true;
    }

    [Serializable]
    public class QualitySettings
    {
        public bool StabilizeFeet = true;
        [Range(0f, 1f)] public float FeetPivotActive = 1f;
        public bool KeepAnimatorControllerAssetAssignedInEditMode = true;
        public bool ClearRuntimeAnimatorControllerOnInitialize = true;
    }

    [Serializable]
    public class ActionSettings
    {
        public CharacterAnimationAction Action;
        [PreviewField(55), AssetsOnly] public AnimationClip Clip;
        [MinValue(0f)] public float FadeIn = 0.08f;
        [MinValue(0f)] public float FadeOut = 0.12f;
        public CharacterAnimationPriority Priority = CharacterAnimationPriority.Interaction;
        public bool Loop;
        public bool AllowMovement = true;
        public bool LockRotation;
        public bool Interruptible = true;
        [Range(0f, 1f)] public float InterruptibleNormalizedTime = 0.75f;
        public CharacterAnimationRootMotionStrategy RootMotion = CharacterAnimationRootMotionStrategy.Disabled;
        public CharacterAnimationLogicalState ReturnState = CharacterAnimationLogicalState.Locomotion;
    }

    [Serializable]
    public class ParkourSettings
    {
        public ParkourAnimationType Type;
        [PreviewField(55), AssetsOnly] public AnimationClip Clip;
        [MinValue(0f)] public float MinHeight;
        [MinValue(0f)] public float MaxHeight = 2f;
        [MinValue(0f)] public float Duration = 1f;
        [MinValue(0f)] public float EnterFade = 0.08f;
        [MinValue(0f)] public float ExitFade = 0.12f;
        public CharacterAnimationRootMotionStrategy RootMotion = CharacterAnimationRootMotionStrategy.ForwardToCharacterMotor;
        [Range(0f, 1f)] public float InterruptibleStart = 0.8f;
        public Vector3 MotionWarpingOffset;
        public float HandIKWeight = 1f;
        public float FootIKWeight = 1f;
    }

    [Serializable]
    public class LayerSettings
    {
        [BoxGroup("Base")] public string BaseLayerName = "Base";
        [BoxGroup("Base"), Range(0f, 1f)] public float BaseDefaultWeight = 1f;
        [BoxGroup("Upper Body"), MinValue(0f)] public float UpperBodyFadeDuration = 0.12f;
        [BoxGroup("Upper Body"), Range(0f, 1f)] public float UpperBodyDefaultWeight;
        [BoxGroup("Additive"), AssetsOnly] public AvatarMask AdditiveMask;
        [BoxGroup("Additive"), MinValue(0f)] public float AdditiveFadeDuration = 0.12f;
        [BoxGroup("Additive"), Range(0f, 1f)] public float AdditiveDefaultWeight;
        [BoxGroup("Reaction"), AssetsOnly] public AvatarMask ReactionMask;
        [BoxGroup("Reaction"), MinValue(0f)] public float ReactionFadeDuration = 0.08f;
        [BoxGroup("Reaction"), Range(0f, 1f)] public float ReactionDefaultWeight;
    }

    [Serializable]
    public class MobilityClipTuning
    {
        [HorizontalGroup("Info", 0.25f), ReadOnly] public MobilityAnimationCategory Category;
        [HorizontalGroup("Info"), ReadOnly] public string Role;
        [DrawWithUnity, LabelText("Animation / Preview")] public ClipTransition Transition = new ClipTransition();
        [HorizontalGroup("Exit"), LabelText("Fade Out"), MinValue(0f)] public float FadeOut = 0.12f;
        [HorizontalGroup("Exit"), LabelText("Fade Out Start"), Range(0f, 2f)] public float BlendOutNormalizedTime = 0.85f;
        [HorizontalGroup("Exit"), LabelText("End Time"), Range(0.01f, 2f), OnValueChanged(nameof(ApplyEndTimeToTransition))]
        public float EndNormalizedTime = 1f;
        [HorizontalGroup("Runtime")] public bool OverrideRuntime = true;
        [HorizontalGroup("Runtime")] public bool ApplyFootIK = true;

        public AnimationClip Clip => Transition != null ? Transition.Clip : null;
        public string DisplayName => Clip != null ? Clip.name : "Missing Animation";

        public void ApplyEndTimeToTransition()
        {
            if (Transition == null) return;
            EndNormalizedTime = Mathf.Max(0.01f, EndNormalizedTime);
            AnimancerEvent.Sequence.Serializable serializedEvents = Transition.SerializedEvents;
            if (serializedEvents == null)
            {
                serializedEvents = new AnimancerEvent.Sequence.Serializable();
                Transition.SerializedEvents = serializedEvents;
            }

            serializedEvents.SetNormalizedEndTime(EndNormalizedTime);
            Transition.Events.NormalizedEndTime = EndNormalizedTime;
        }
    }

    [TabGroup("Setup"), InfoBox("The original Animator Controller remains serialized in the scene for rollback. At runtime Animancer temporarily clears it after the MOBILITY PRO configuration is ready.")]
    public QualitySettings Quality = new QualitySettings();
    [TabGroup("Setup"), ReadOnly] public string SourceAnimationPack;
    [TabGroup("Setup"), ReadOnly] public string ConfiguredScenePath;
    [TabGroup("Setup"), ReadOnly] public int ConfigurationRevision;
    [TabGroup("Locomotion")] public LocomotionSettings Locomotion = new LocomotionSettings();
    [TabGroup("Airborne")] public AirborneSettings Airborne = new AirborneSettings();
    [TabGroup("Holding")] public HoldingSettings Holding = new HoldingSettings();
    [TabGroup("Actions"), TableList] public ActionSettings[] Actions = Array.Empty<ActionSettings>();
    [TabGroup("Parkour"), TableList] public ParkourSettings[] Parkour = Array.Empty<ParkourSettings>();
    [TabGroup("Layers")] public LayerSettings Layers = new LayerSettings();
    [TabGroup("Clip Tuning"), InfoBox("Each Animancer transition has a preview eye button. Fade Duration is Fade In; Fade Out, Fade Out Start, and End Time are directly below it.")]
    [TabGroup("Clip Tuning"), Searchable, ListDrawerSettings(DefaultExpandedState = false, ShowIndexLabels = false, NumberOfItemsPerPage = 20)]
    public MobilityClipTuning[] ClipTunings = Array.Empty<MobilityClipTuning>();

    [TabGroup("Root Motion")] public CharacterAnimationRootMotionStrategy DefaultLocomotionRootMotion = CharacterAnimationRootMotionStrategy.Disabled;
    [TabGroup("Root Motion")] public CharacterAnimationRootMotionStrategy DefaultParkourRootMotion = CharacterAnimationRootMotionStrategy.ForwardToCharacterMotor;
    [TabGroup("Events")] public bool UseCodeDrivenEvents = true;

    [TabGroup("Validation"), ShowInInspector, ReadOnly]
    public CharacterAnimationValidationResult LastValidation { get; private set; }

    public LocomotionGait ResolveGait(float speed, bool crouching)
    {
        if (crouching) return LocomotionGait.Crouch;
        float runBoundary = (Locomotion.JogSpeed + Locomotion.RunSpeed) * 0.5f;
        float jogBoundary = (Locomotion.WalkSpeed + Locomotion.JogSpeed) * 0.5f;
        if (speed >= runBoundary) return LocomotionGait.Run;
        if (speed >= jogBoundary) return LocomotionGait.Jog;
        return speed > Locomotion.IdleSpeedThreshold ? LocomotionGait.Walk : LocomotionGait.Idle;
    }

    public ActionSettings FindAction(CharacterAnimationAction action)
    {
        if (Actions == null) return null;
        for (int i = 0; i < Actions.Length; i++)
        {
            ActionSettings settings = Actions[i];
            if (settings != null && settings.Action == action) return settings;
        }
        return null;
    }

    public ParkourSettings FindParkour(ParkourAnimationRequest request)
    {
        if (Parkour == null) return null;
        for (int i = 0; i < Parkour.Length; i++)
        {
            ParkourSettings settings = Parkour[i];
            if (settings == null || settings.Type != request.Type || settings.Clip == null) continue;
            if (request.ObstacleHeight >= settings.MinHeight && request.ObstacleHeight <= settings.MaxHeight) return settings;
        }
        return null;
    }

    public MobilityClipTuning FindClipTuning(AnimationClip clip)
    {
        if (clip == null || ClipTunings == null) return null;
        for (int i = 0; i < ClipTunings.Length; i++)
        {
            MobilityClipTuning tuning = ClipTunings[i];
            if (tuning != null && tuning.Clip == clip) return tuning;
        }
        return null;
    }

    public CharacterAnimationValidationResult ValidateConfiguration()
    {
        if (Locomotion == null) return LastValidation = CharacterAnimationValidationResult.Warning("Locomotion settings are missing.");
        if (Locomotion.Idle == null) return LastValidation = CharacterAnimationValidationResult.Warning("Standing idle is required.");
        if (Locomotion.CrouchIdle == null) return LastValidation = CharacterAnimationValidationResult.Warning("Crouch idle is required.");
        if (!Locomotion.Walk.HasAnyClip()) return LastValidation = CharacterAnimationValidationResult.Warning("At least one walk clip is required.");
        if (Locomotion.WalkSpeed <= Locomotion.IdleSpeedThreshold ||
            Locomotion.JogSpeed <= Locomotion.WalkSpeed ||
            Locomotion.RunSpeed <= Locomotion.JogSpeed)
            return LastValidation = CharacterAnimationValidationResult.Warning("Speed thresholds must be ordered: Idle < Walk < Jog < Run.");
        if (Locomotion.MinimumPlaybackSpeed > Locomotion.MaximumPlaybackSpeed)
            return LastValidation = CharacterAnimationValidationResult.Warning("Minimum playback speed cannot exceed maximum playback speed.");
        return LastValidation = CharacterAnimationValidationResult.Valid();
    }

    [TabGroup("Validation"), Button(ButtonSizes.Medium)]
    private void ValidateConfigurationButton()
    {
        ValidateConfiguration();
    }
}
