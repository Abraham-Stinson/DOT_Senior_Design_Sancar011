
using UnityEngine;

public enum AttackType
{
    LightPunch,
    HeavyPunch,
    LightKick,
    HeavyKick
}

[CreateAssetMenu(
    fileName = "NewAttack",
    menuName = "Sancar 011/Combat/Attack Data"
)]
public class AttackData : ScriptableObject
{
    [Header("Identity")]
    public string attackID;
    public string displayName;
    public AttackType attackType;
    
    [Header("Hitbox Settings")]
    [Min(0.1f)]
    public float range = 1.5f;

    [Header("Damage")]
    [Min(0)]
    public int damage = 35;

    [Min(0f)]
    public float attackRange = 1.5f;

    [Header("Combat Timing - 60 FPS")]
    [Min(0)]
    public int startupFrames = 5;

    [Min(1)]
    public int activeFrames = 2;

    [Min(0)]
    public int recoveryFrames = 9;

    [Header("Animation")]
    public string animatorStateName;

    [Header("Hit Reaction")]
    [Min(0)]
    public int hitStunFrames = 12;

    [Min(0f)]
    public float pushbackDistance = 0f;

    public int TotalFrames =>
        startupFrames + activeFrames + recoveryFrames;

    public float DurationSeconds =>
        TotalFrames / 60f;
}
