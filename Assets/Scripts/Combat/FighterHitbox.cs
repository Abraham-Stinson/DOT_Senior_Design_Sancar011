
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FighterController))]
[RequireComponent(typeof(FighterCombat))]
public class FighterHitbox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private FighterCombat combat;

    [Header("Hitbox")]
    [SerializeField] private Transform hitOrigin;
    [SerializeField] private bool facingRight = true;
    [SerializeField] private float hitboxHeight = 1.1f;
    [SerializeField] private float verticalOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private readonly HashSet<FighterController> hitTargets
        = new HashSet<FighterController>();

    private bool wasHitWindowOpen;

    private void Awake()
    {
        if (fighter == null)
            fighter = GetComponent<FighterController>();

        if (combat == null)
            combat = GetComponent<FighterCombat>();

        if (hitOrigin == null)
            hitOrigin = transform;
    }

    private void FixedUpdate()
    {
        if (fighter == null || combat == null)
            return;

        bool windowOpen = combat.IsHitWindowOpen;
        AttackData attack = combat.CurrentAttack;

        // Clear old targets after each attack window.
        if (!windowOpen)
        {
            if (wasHitWindowOpen)
                hitTargets.Clear();

            wasHitWindowOpen = false;
            return;
        }

        if (attack == null)
            return;

        if (!wasHitWindowOpen)
        {
            hitTargets.Clear();
            wasHitWindowOpen = true;
        }

        CheckHits(attack);
    }

    private void CheckHits(AttackData attack)
    {
        float range = Mathf.Max(0.01f, attack.range);

        Vector2 size = new Vector2(
            range,
            hitboxHeight
        );

        Vector2 center = GetHitboxCenter(range);

        // Make sure collider positions are current.
        Physics2D.SyncTransforms();

        Collider2D[] overlaps =
            Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D overlap in overlaps)
        {
            FighterHurtbox hurtbox =
                overlap.GetComponent<FighterHurtbox>();

            if (hurtbox == null)
                continue;

            FighterController target = hurtbox.Owner;

            if (target == null)
                continue;

            // Do not hit ourselves.
            if (target == fighter)
                continue;

            if (!target.IsAlive)
                continue;

            // One hit per target for each attack.
            if (hitTargets.Contains(target))
                continue;

            hitTargets.Add(target);

            
            
hitTargets.Add(target);

// Check whether the target is defending.
FighterDefense defense =
    target.GetComponent<FighterDefense>();

FighterFacing targetFacing =
    target.GetComponent<FighterFacing>();

// Guard only works against attacks from the front.
bool attackFromFront = true;

if (targetFacing != null)
{
    float attackerDirection =
        fighter.transform.position.x -
        target.transform.position.x;

    attackFromFront =
        attackerDirection * targetFacing.FacingDirection > 0f;
}

if (defense != null &&
    defense.IsGuarding &&
    attackFromFront)
{
    defense.ReceiveBlockedHit();

    
FighterPushback targetPushback =
    target.GetComponent<FighterPushback>();

if (targetPushback != null)
{
    targetPushback.ApplyPushback(
        fighter.transform.position.x,
        attack.pushbackDistance,
        true
    );
}


    Debug.Log(
        $"{fighter.name}: ATTACK BLOCKED by {target.name}",
        this
    );

    continue;
}

// Normal damage if the attack was not blocked.

target.ReceiveDamage(attack.damage);

FighterHitReaction reaction =
    target.GetComponent<FighterHitReaction>();

if (reaction != null)
{
    reaction.ReceiveHit(attack.hitStunFrames);
}

// Apply full pushback after a successful hit.

FighterPushback normalHitPushback =
    target.GetComponent<FighterPushback>();

if (normalHitPushback != null)
{
    normalHitPushback.ApplyPushback(
        fighter.transform.position.x,
        attack.pushbackDistance,
        false
    );
}


Debug.Log(
    $"{fighter.name} hit {target.name} " +
    $"for {attack.damage} damage. " +
    $"Remaining HP: {target.CurrentHealth}",
    this
);



            Debug.Log(
                $"{fighter.name} hit {target.name} " +
                $"for {attack.damage} damage. " +
                $"Remaining HP: {target.CurrentHealth}",
                this
            );
        }
    }

    private Vector2 GetHitboxCenter(float range)
    {
        float direction = facingRight ? 1f : -1f;

        Vector3 origin = hitOrigin != null
            ? hitOrigin.position
            : transform.position;

        return new Vector2(
            origin.x + direction * range * 0.5f,
            origin.y + verticalOffset
        );
    }

    public void SetFacingRight(bool value)
    {
        facingRight = value;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        FighterCombat activeCombat = combat != null
            ? combat
            : GetComponent<FighterCombat>();

        float range = 1f;

        if (activeCombat != null &&
            activeCombat.CurrentAttack != null)
        {
            range = Mathf.Max(
                0.01f,
                activeCombat.CurrentAttack.range
            );
        }

        Vector2 center = GetHitboxCenter(range);

        Gizmos.color =
            activeCombat != null &&
            activeCombat.IsHitWindowOpen
                ? Color.red
                : Color.yellow;

        Gizmos.DrawWireCube(
            center,
            new Vector3(range, hitboxHeight, 0f)
        );
    }

    private void OnDisable()
    {
        hitTargets.Clear();
        wasHitWindowOpen = false;
    }
}
