using System;
using UnityEngine;

public enum CharacterAnimationLogicalState
{
    None,
    Locomotion,
    Airborne,
    Landing,
    Action,
    Parkour,
    Dead,
    LocomotionTransition
}

public enum CharacterMovementMode
{
    Free,
    Strafe,
    Crouch
}

public enum LocomotionGait
{
    Idle,
    Walk,
    Jog,
    Run,
    Crouch
}

public enum LocomotionTransient
{
    None,
    Starting,
    Stopping,
    CrouchEnter,
    CrouchExit,
    Turn,
    TurnLoop,
    Pivot,
    MovementCurve,
    JumpStart,
    Landing
}

public enum MobilityAnimationCategory
{
    Idle,
    LocomotionLoop,
    Start,
    Stop,
    CrouchTransition,
    TurnInPlace,
    MovementPivot,
    MovementCurve,
    Jump,
    Conversation,
    Fight,
    Death,
    Hop,
    Other
}

public enum CharacterAnimationSource
{
    ProjectOriginal,
    MobilityPro
}

public enum CharacterAnimationAction
{
    None,
    Dodge,
    Attack,
    Combo,
    Equip,
    Unequip,
    Interact,
    HitReaction,
    Stagger,
    Knockdown,
    GetUp,
    Death,
    Respawn,
    Pickup,
    Inspect,
    Radio,
    UseItem
}

public enum CharacterAnimationPriority
{
    Locomotion = 0,
    Interaction = 10,
    Airborne = 15,
    Attack = 20,
    Dodge = 30,
    HitReaction = 40,
    Stagger = 50,
    Parkour = 60,
    Knockdown = 70,
    Death = 100
}

public enum CharacterAnimationRootMotionStrategy
{
    Disabled,
    UseAnimationDelta,
    ForwardToCharacterMotor,
    CustomHandler
}

public enum CharacterAnimationMotionOwner
{
    CharacterMotor,
    AnimatorRootMotion,
    CharacterMotorRootMotion,
    Scripted
}

public enum ParkourAnimationType
{
    Vault,
    Mantle,
    LedgeGrab,
    LedgeHang,
    LedgeClimb,
    LedgeDrop,
    WallJump,
    TicTac,
    SpecialJump,
    SpecialLanding
}

[Serializable]
public struct ParkourAnimationRequest
{
    public ParkourAnimationType Type;
    public float ObstacleHeight;
    public float ObstacleDepth;
    public Vector3 StartPosition;
    public Vector3 TargetPosition;
    public Quaternion TargetRotation;
    public int Direction;
    public bool AllowRootMotion;
}

[Serializable]
public struct CharacterAnimationValidationResult
{
    public bool IsValid;
    [TextArea(3, 10)] public string Message;

    public static CharacterAnimationValidationResult Valid()
    {
        return new CharacterAnimationValidationResult
        {
            IsValid = true,
            Message = "Configuration looks usable for the currently assigned clips."
        };
    }

    public static CharacterAnimationValidationResult Warning(string message)
    {
        return new CharacterAnimationValidationResult
        {
            IsValid = false,
            Message = message
        };
    }
}
