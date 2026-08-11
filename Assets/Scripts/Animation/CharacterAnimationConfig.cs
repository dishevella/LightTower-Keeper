using System;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class InlineClipTuningAttribute : Attribute
{
}

[CreateAssetMenu(menuName = "Light Tower/Animation/Character Animation Config", fileName = "CharacterAnimationConfig")]
public class CharacterAnimationConfig : ScriptableObject
{
    [Serializable]
    public class DirectionalClipSet
    {
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip ForwardLeft;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip Forward;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip ForwardRight;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip Left;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip Right;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip BackwardLeft;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip Backward;
        [InlineClipTuning, PreviewField(52), AssetsOnly] public AnimationClip BackwardRight;

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
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip Standing;
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

        [BoxGroup("Movement Curve Rules"), LabelText("Curve Enter Yaw Rate"), MinValue(0f)]
        [Tooltip("Yaw speed required to enter a moving turn animation. A higher value prevents small camera turns from changing locomotion clips.")]
        public float CurveStartYawRate = 45f;
        [BoxGroup("Movement Curve Rules"), LabelText("Curve Exit Yaw Rate"), MinValue(0f)]
        [Tooltip("Once a moving turn is active, yaw must fall below this lower threshold before its exit delay starts.")]
        public float CurveExitYawRate = 15f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float MinimumCurveSpeed = 0.5f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveExitDelay = 0.2f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveFadeIn = 0.12f;
        [BoxGroup("Movement Curve Rules"), MinValue(0f)] public float CurveFadeOut = 0.14f;

        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandLeft45;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandLeft90;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandLeft135;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandRight90;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandRight45;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandRight135;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandLeft180;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandRight180;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandTurnLeftLoop;
        [BoxGroup("Standing"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip StandTurnRightLoop;

        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft45;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft90;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft135;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchRight90;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchRight45;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchRight135;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchLeft180;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchRight180;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchTurnLeftLoop;
        [BoxGroup("Crouch"), InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip CrouchTurnRightLoop;

        [FoldoutGroup("Movement Pivot - Walk")] public DirectionalTurnSet Walk = new DirectionalTurnSet();
        [FoldoutGroup("Movement Pivot - Jog")] public DirectionalTurnSet Jog = new DirectionalTurnSet();
        [FoldoutGroup("Movement Pivot - Run")] public DirectionalTurnSet Run = new DirectionalTurnSet();
        [FoldoutGroup("Movement Curves - Walk")] public DirectionalLoopSet WalkCurves = new DirectionalLoopSet();
        [FoldoutGroup("Movement Curves - Jog")] public DirectionalLoopSet JogCurves = new DirectionalLoopSet();
        [FoldoutGroup("Movement Curves - Run")] public DirectionalLoopSet RunCurves = new DirectionalLoopSet();
        [FoldoutGroup("Movement Curves - Crouch")] public DirectionalLoopSet CrouchCurves = new DirectionalLoopSet();
        [FoldoutGroup("Backpedal Curves - Walk")] public DirectionalLoopSet WalkBackpedalCurves = new DirectionalLoopSet();
        [FoldoutGroup("Backpedal Curves - Jog")] public DirectionalLoopSet JogBackpedalCurves = new DirectionalLoopSet();
        [FoldoutGroup("Backpedal Curves - Crouch")] public DirectionalLoopSet CrouchBackpedalCurves = new DirectionalLoopSet();

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
        [InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip Left;
        [InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip Right;

        public AnimationClip Get(float signedValue)
        {
            return signedValue < 0f ? Left : Right;
        }
    }

    [Serializable]
    public class DirectionalTurnSet
    {
        [InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip Left90;
        [InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip Right90;
        [InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip Left180;
        [InlineClipTuning, PreviewField(45), AssetsOnly] public AnimationClip Right180;

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
        [BoxGroup("Idle"), InlineClipTuning, Required, PreviewField(65), AssetsOnly] public AnimationClip Idle;
        [BoxGroup("Idle"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip[] IdleVariations = Array.Empty<AnimationClip>();
        [BoxGroup("Idle"), InlineClipTuning, Required, PreviewField(65), AssetsOnly] public AnimationClip CrouchIdle;
        [BoxGroup("Idle")] public bool EnableIdleVariations = true;
        [BoxGroup("Idle"), MinValue(0f)] public float IdleVariationInterval = 9f;
        [BoxGroup("Idle"), MinValue(0f)] public float IdleVariationCrossFade = 0.2f;

        [FoldoutGroup("Loops - Walk")] public DirectionalClipSet Walk = new DirectionalClipSet();
        [FoldoutGroup("Loops - Jog")] public DirectionalClipSet Jog = new DirectionalClipSet();
        [FoldoutGroup("Loops - Run")] public DirectionalClipSet Run = new DirectionalClipSet();
        [FoldoutGroup("Loops - Crouch")] public DirectionalClipSet Crouch = new DirectionalClipSet();

        [FoldoutGroup("Transitions - Starts")] public GaitDirectionalClipSet Starts = new GaitDirectionalClipSet();
        [FoldoutGroup("Transitions - Forward Turning Starts")] public GaitDirectionalClipSet ForwardTurningStarts = new GaitDirectionalClipSet();
        [FoldoutGroup("Transitions - Stops")] public GaitDirectionalClipSet Stops = new GaitDirectionalClipSet();
        [FoldoutGroup("Transitions - Foot Matched Stops")] public GaitFootMatchedClipSet FootMatchedStops = new GaitFootMatchedClipSet();
        [FoldoutGroup("Transitions - Crouch"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip StandToCrouch;
        [FoldoutGroup("Transitions - Crouch"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip CrouchToStand;

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
        [BoxGroup("Start Movement Sync"), LabelText("Movement Delay"), Range(0f, 1f)]
        [Tooltip("Normalized start-animation time before the CharacterController begins horizontal movement.")]
        public float StartMovementDelayNormalizedTime = 0.04f;
        [BoxGroup("Start Movement Sync"), LabelText("Reach Full Speed"), Range(0f, 1f)]
        [Tooltip("Normalized start-animation time at which horizontal movement reaches its requested speed.")]
        public float StartMovementFullSpeedNormalizedTime = 0.45f;
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

        [HideInInspector] public TurnSettings Turns = new TurnSettings();
    }

    [Serializable]
    public class AirborneSettings
    {
        [FoldoutGroup("Directional Jump - Start")] public AirbornePhaseSet JumpStarts = new AirbornePhaseSet();
        [FoldoutGroup("Directional Jump - Air")] public AirbornePhaseSet JumpAir = new AirbornePhaseSet();
        [FoldoutGroup("Directional Jump - Landing")] public AirbornePhaseSet JumpLandings = new AirbornePhaseSet();

        [BoxGroup("Fallback Fall"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip Fall;
        [BoxGroup("Fallback Fall"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip ShortFall;
        [BoxGroup("Fallback Fall"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip LongFall;
        [BoxGroup("Fallback Landing"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip SoftLanding;
        [BoxGroup("Fallback Landing"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip HardLanding;
        [BoxGroup("Fallback Landing"), InlineClipTuning, PreviewField(55), AssetsOnly] public AnimationClip MovingLanding;

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

        [BoxGroup("Ground Contact"), LabelText("Enable Ground Snap")]
        [Tooltip("Keeps locomotion grounded across small downward steps instead of restarting fall and landing animations.")]
        public bool EnableGroundSnap = true;
        [BoxGroup("Ground Contact"), LabelText("Ground Collision Mask")]
        public LayerMask GroundCollisionMask = ~0;
        [BoxGroup("Ground Contact"), LabelText("Snap Distance"), Range(0f, 1f), SuffixLabel("m", true)]
        public float GroundSnapDistance = 0.35f;
        [BoxGroup("Ground Contact"), LabelText("Probe Radius Scale"), Range(0.5f, 0.98f)]
        public float GroundProbeRadiusScale = 0.85f;
        [BoxGroup("Ground Contact"), LabelText("Snap Speed"), MinValue(0.1f), SuffixLabel("m/s", true)]
        public float GroundSnapSpeed = 12f;
        [BoxGroup("Ground Contact"), LabelText("Jump Snap Delay"), MinValue(0f), SuffixLabel("seconds", true)]
        [Tooltip("Prevents the ground probe from pulling a deliberate jump back down immediately after takeoff.")]
        public float JumpGroundSnapDelay = 0.12f;
        [BoxGroup("Ground Contact"), LabelText("Airborne Grace Time"), MinValue(0f), SuffixLabel("seconds", true)]
        [Tooltip("Walking remains in locomotion during shorter unsupported intervals.")]
        public float GroundedGraceTime = 0.28f;
        [BoxGroup("Ground Contact"), LabelText("Fall Animation Speed"), MaxValue(0f), SuffixLabel("m/s", true)]
        public float FallAnimationMinSpeed = -4.5f;
        [BoxGroup("Ground Contact"), LabelText("Landing Minimum Air Time"), MinValue(0f), SuffixLabel("seconds", true)]
        public float LandingMinimumAirTime = 0.32f;
        [BoxGroup("Ground Contact"), LabelText("Landing Minimum Fall Speed"), MaxValue(0f), SuffixLabel("m/s", true)]
        public float LandingMinimumFallSpeed = -6.5f;

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
    public class GlobalFadeSettings
    {
        [BoxGroup("Unified Control"), LabelText("Global Fade Speed"), Range(0.1f, 5f)]
        [Tooltip("Controls every Fade In and Fade Out together. 2 is twice as fast; 0.5 is twice as slow.")]
        public float GlobalFadeSpeed = 1f;

        [BoxGroup("Unified Control"), LabelText("Use Shared Durations")]
        [Tooltip("When enabled, all per-animation fade durations are replaced by the shared values below.")]
        public bool UseSharedDurations;

        [BoxGroup("Unified Control"), LabelText("Shared Fade In"), MinValue(0f), SuffixLabel("seconds", true)]
        [ShowIf(nameof(UseSharedDurations))]
        public float SharedFadeIn = 0.1f;

        [BoxGroup("Unified Control"), LabelText("Shared Fade Out"), MinValue(0f), SuffixLabel("seconds", true)]
        [ShowIf(nameof(UseSharedDurations))]
        public float SharedFadeOut = 0.12f;

        public float ResolveFadeIn(float localDuration)
        {
            float duration = UseSharedDurations ? SharedFadeIn : localDuration;
            return Mathf.Max(0f, duration) / Mathf.Max(0.1f, GlobalFadeSpeed);
        }

        public float ResolveFadeOut(float localDuration)
        {
            float duration = UseSharedDurations ? SharedFadeOut : localDuration;
            return Mathf.Max(0f, duration) / Mathf.Max(0.1f, GlobalFadeSpeed);
        }
    }

    [Serializable]
    public class CameraSettings
    {
        [BoxGroup("Look Input"), LabelText("Mouse Look Sensitivity"), Range(1f, 200f)]
        [Tooltip("Controls horizontal and vertical mouse look speed. Lower values rotate the view more slowly.")]
        public float LookSensitivity = 65f;

        [BoxGroup("Per Animation")] public bool EnablePerAnimationPositions = true;
        [BoxGroup("Crouch Height"), LabelText("Crouch Down Speed"), MinValue(0.01f)]
        public float CrouchDownBlendSpeed = 10f;
        [BoxGroup("Crouch Height"), LabelText("Stand Up Speed"), MinValue(0.01f)]
        public float StandUpBlendSpeed = 10f;

        [BoxGroup("Wall Collision")] public bool EnableWallCollision = true;
        [BoxGroup("Wall Collision")] public LayerMask CollisionMask = ~0;
        [BoxGroup("Wall Collision"), MinValue(0.01f)] public float CollisionRadius = 0.16f;
        [BoxGroup("Wall Collision"), MinValue(0f)] public float CollisionPadding = 0.04f;
        [BoxGroup("Wall Collision"), MinValue(0.01f)] public float MinimumDistance = 0.36f;
        [BoxGroup("Wall Collision"), LabelText("Near Pose Minimum Ratio"), Range(0.05f, 0.95f)]
        public float MinimumDistanceRatio = 0.65f;
        [BoxGroup("Wall Collision"), MinValue(0.01f)] public float PullInSpeed = 30f;
        [BoxGroup("Wall Collision"), MinValue(0.01f)] public float ReturnSpeed = 10f;
        [BoxGroup("Wall Collision"), LabelText("Anchor (Player Local)")]
        public Vector3 CollisionAnchorLocalPosition = new Vector3(0f, 1.25f, 0.05f);
        [BoxGroup("Wall Collision"), Range(0.01f, 0.3f)] public float CameraNearClip = 0.12f;

        [BoxGroup("Character Interior Protection")] public bool EnableCharacterInteriorProtection = true;
        [BoxGroup("Character Interior Protection"), LabelText("Minimum Camera Forward"), MinValue(0f)]
        [Tooltip("Stable player-local Z minimum used while standing. It does not follow head-bob animation.")]
        public float MinimumCameraLocalForward = 0.55f;
        [BoxGroup("Character Interior Protection"), LabelText("Crouch Minimum Camera Forward"), MinValue(0f)]
        [Tooltip("Stable player-local Z minimum used while crouching. Increase it if crouch poses lean the head into the camera.")]
        public float CrouchMinimumCameraLocalForward = 0.85f;
        [BoxGroup("Character Interior Protection"), LabelText("Track Animated Head")]
        public bool TrackAnimatedHead = true;
        [BoxGroup("Character Interior Protection"), LabelText("Head Forward Clearance"), MinValue(0.01f)]
        [Tooltip("Forward distance restored if the standing camera enters the animated Head safety volume.")]
        public float HeadForwardClearance = 0.35f;
        [BoxGroup("Character Interior Protection"), LabelText("Crouch Head Forward Clearance"), MinValue(0.01f)]
        [Tooltip("Forward distance restored if the crouching camera enters the animated Head safety volume.")]
        public float CrouchHeadForwardClearance = 0.45f;
        [BoxGroup("Character Interior Protection"), LabelText("Head Safety Radius"), MinValue(0.01f)]
        [Tooltip("Minimum three-dimensional distance from the standing animated Head bone to the final camera.")]
        public float HeadSafetyRadius = 0.3f;
        [BoxGroup("Character Interior Protection"), LabelText("Crouch Head Safety Radius"), MinValue(0.01f)]
        [Tooltip("Minimum three-dimensional distance from the crouching animated Head bone to the final camera.")]
        public float CrouchHeadSafetyRadius = 0.42f;
        [BoxGroup("Character Interior Protection"), LabelText("Hide Head For Player Camera")]
        [Tooltip("Temporarily scales only the animated Head bone while the player camera is close. The body, clothing, held items, and other cameras remain visible.")]
        public bool HideHeadForPlayerCamera = true;
        [BoxGroup("Character Interior Protection"), LabelText("Head Hide Distance"), MinValue(0.05f)]
        [Tooltip("The Head bone is hidden only while the player camera is within this distance. At normal third-person distance the complete character remains visible.")]
        [ShowIf(nameof(HideHeadForPlayerCamera))]
        public float CharacterHideDistance = 0.9f;
        [BoxGroup("Character Interior Protection"), LabelText("Head Fallback Scale"), Range(0.0001f, 0.1f)]
        [Tooltip("Fallback scale used for head-attached geometry while the player camera renders.")]
        [ShowIf(nameof(HideHeadForPlayerCamera))]
        public float HiddenHeadScale = 0.001f;
        [BoxGroup("Character Interior Protection"), LabelText("Protection Blend Speed"), MinValue(0.01f)]
        public float InteriorProtectionBlendSpeed = 30f;
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
        [HorizontalGroup("Info", 0.2f), ReadOnly] public CharacterAnimationSource Source;
        [HorizontalGroup("Info", 0.25f), ReadOnly] public MobilityAnimationCategory Category;
        [HorizontalGroup("Info"), ReadOnly] public string Role;
        [DrawWithUnity, LabelText("Transition"), LabelWidth(155)]
        [Tooltip("Fade Duration controls how quickly this animation fades in when it becomes active.")]
        public ClipTransition Transition = new ClipTransition();
        [BoxGroup("Exit"), LabelText("Fade Out"), LabelWidth(155), MinValue(0f), SuffixLabel("seconds", true)]
        [Tooltip("Controls how quickly this animation fades out. A cross-fade uses the larger of the incoming Fade Duration and outgoing Fade Out.")]
        public float FadeOut = 0.12f;
        [BoxGroup("Exit"), LabelText("Fade Out Start"), LabelWidth(155), Range(0f, 2f)]
        public float BlendOutNormalizedTime = 0.85f;
        [SerializeField, HideInInspector] public float EndNormalizedTime = 1f;
        [BoxGroup("Runtime"), LabelWidth(155)] public bool OverrideRuntime = true;
        [BoxGroup("Runtime"), LabelWidth(155)] public bool ApplyFootIK = true;

        [BoxGroup("Camera Position"), LabelText("Use For This Animation"), LabelWidth(155)]
        public bool OverrideCameraPosition;
        [BoxGroup("Camera Position"), LabelText("Local Position"), LabelWidth(155), EnableIf(nameof(OverrideCameraPosition))]
        public Vector3 CameraLocalPosition = new Vector3(0.028f, 1.3f, 0.368f);
        [BoxGroup("Camera Position"), LabelText("Position Blend Speed"), LabelWidth(155), MinValue(0.01f), EnableIf(nameof(OverrideCameraPosition))]
        public float CameraBlendSpeed = 8f;

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

        public void SyncEndTimeFromTransition()
        {
            if (Transition == null) return;

            AnimancerEvent.Sequence.Serializable serializedEvents = Transition.SerializedEvents;
            float normalizedEndTime = serializedEvents != null
                ? serializedEvents.GetNormalizedEndTime(Transition.Speed)
                : AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(Transition.Speed);
            EndNormalizedTime = Mathf.Max(0.01f, normalizedEndTime);
            Transition.Events.NormalizedEndTime = EndNormalizedTime;
        }
    }

    [TabGroup("Animation Tabs", "Overview")]
    [Title("Runtime Setup")]
    [InfoBox("The original Animator Controller remains serialized in the scene for rollback. At runtime Animancer temporarily clears it after the MOBILITY PRO configuration is ready.")]
    [InlineProperty, HideLabel]
    public QualitySettings Quality = new QualitySettings();

    [TabGroup("Animation Tabs", "Overview")]
    [Title("Configuration Source")]
    [ReadOnly, LabelWidth(190)] public string SourceAnimationPack;
    [TabGroup("Animation Tabs", "Overview"), ReadOnly, LabelWidth(190)] public string ConfiguredScenePath;
    [TabGroup("Animation Tabs", "Overview"), ReadOnly, LabelWidth(190)] public int ConfigurationRevision;

    [TabGroup("Animation Tabs", "Overview")]
    [Title("Runtime Defaults")]
    [LabelWidth(190)] public CharacterAnimationRootMotionStrategy DefaultLocomotionRootMotion = CharacterAnimationRootMotionStrategy.Disabled;
    [TabGroup("Animation Tabs", "Overview"), LabelWidth(190)] public CharacterAnimationRootMotionStrategy DefaultParkourRootMotion = CharacterAnimationRootMotionStrategy.ForwardToCharacterMotor;
    [TabGroup("Animation Tabs", "Overview"), LabelWidth(190)] public bool UseCodeDrivenEvents = true;

    [TabGroup("Animation Tabs", "Camera")]
    [Title("Camera Follow And Collision")]
    [InfoBox("Crouch down/up speed is global. Expand any animation card to set its final camera position and blend speed. Wall collision pulls the camera toward the player before it can enter geometry.")]
    [InlineProperty, HideLabel]
    public CameraSettings Camera = new CameraSettings();

    [TabGroup("Animation Tabs", "Locomotion")]
    [Title("Idle, Movement Loops And Transitions")]
    [InfoBox("Each assigned clip is an action card. Its Animancer Transition contains the preview eye, Fade In, playback speed, Start Time, End Time, and timeline; project-specific exit, IK, and camera controls follow underneath.")]
    [InlineProperty, HideLabel]
    public LocomotionSettings Locomotion = new LocomotionSettings();

    [TabGroup("Animation Tabs", "Turns")]
    [Title("Turn In Place, Pivots And Movement Curves")]
    [ShowInInspector, InlineProperty, HideLabel]
    private TurnSettings TurnAnimations
    {
        get
        {
            if (Locomotion == null) Locomotion = new LocomotionSettings();
            if (Locomotion.Turns == null) Locomotion.Turns = new TurnSettings();
            return Locomotion.Turns;
        }
        set
        {
            if (Locomotion == null) Locomotion = new LocomotionSettings();
            Locomotion.Turns = value ?? new TurnSettings();
        }
    }

    [TabGroup("Animation Tabs", "Airborne")]
    [Title("Jump, Airborne And Landing")]
    [InlineProperty, HideLabel]
    public AirborneSettings Airborne = new AirborneSettings();

    [TabGroup("Animation Tabs", "Holding")]
    [Title("Upper Body Holding Pose")]
    [InlineProperty, HideLabel]
    public HoldingSettings Holding = new HoldingSettings();

    [TabGroup("Animation Tabs", "Actions")]
    [Title("Code-Driven Actions")]
    [ListDrawerSettings(DefaultExpandedState = false, ShowIndexLabels = true)]
    public ActionSettings[] Actions = Array.Empty<ActionSettings>();

    [TabGroup("Animation Tabs", "Parkour")]
    [Title("Traversal Actions")]
    [ListDrawerSettings(DefaultExpandedState = false, ShowIndexLabels = true)]
    public ParkourSettings[] Parkour = Array.Empty<ParkourSettings>();

    [TabGroup("Animation Tabs", "Layers")]
    [Title("Masks And Layer Blending")]
    [InlineProperty, HideLabel]
    public LayerSettings Layers = new LayerSettings();

    [TabGroup("Animation Tabs", "Tuning")]
    [Title("Global Fade Control")]
    [InfoBox("Global Fade Speed adjusts every animation and layer fade together. Enable shared durations only when all transitions should use one Fade In/Fade Out pair.")]
    [InlineProperty, HideLabel]
    public GlobalFadeSettings GlobalFades = new GlobalFadeSettings();

    [TabGroup("Animation Tabs", "Tuning")]
    [Title("Animation Parameter Library")]
    [InfoBox("The same parameters are shown directly inside each locomotion, turn, and airborne action card. This searchable library remains available for batch inspection.")]
    [Button("Open Animation Preview", ButtonSizes.Large)]
    private void OpenAnimationPreview()
    {
#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = this;
        UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Light Tower/Animation/Open Animation Preview");
#endif
    }

    [TabGroup("Animation Tabs", "Tuning")]
    [Searchable, ListDrawerSettings(DefaultExpandedState = false, ShowIndexLabels = false, NumberOfItemsPerPage = 20)]
    public MobilityClipTuning[] ClipTunings = Array.Empty<MobilityClipTuning>();

    [TabGroup("Animation Tabs", "Validation")]
    [Title("Configuration Health")]
    [ShowInInspector, ReadOnly]
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

    [TabGroup("Animation Tabs", "Validation"), Button(ButtonSizes.Medium)]
    private void ValidateConfigurationButton()
    {
        ValidateConfiguration();
    }
}
