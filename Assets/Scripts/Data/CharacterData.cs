
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCharacter",
    menuName = "Sancar 011/Characters/Character Data"
)]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterID;
    public string displayName;
    [TextArea(2, 5)]
    public string description;

    [Header("Presentation")]
    public Sprite portrait;
    public GameObject fighterPrefab;
    public RuntimeAnimatorController animatorController;
    public string idleAnimatorStateName = "Sancar_Idle";

    [Header("Base Stats")]
    [Min(1)]
    public int maxHealth = 1000;

    [Min(0f)]
    public float movementSpeed = 3f;

    [Header("Basic Attacks")]
    public AttackData lightPunch;
    public AttackData heavyPunch;
    public AttackData lightKick;
    public AttackData heavyKick;

    // Special abilities and story unlocks
    // will be added in their own data layer.
    public AttackData GetBasicAttack(AttackType type)
    {
        switch (type)
        {
            case AttackType.LightPunch:
                return lightPunch;

            case AttackType.HeavyPunch:
                return heavyPunch;

            case AttackType.LightKick:
                return lightKick;

            case AttackType.HeavyKick:
                return heavyKick;

            default:
                return null;
        }
    }
}
