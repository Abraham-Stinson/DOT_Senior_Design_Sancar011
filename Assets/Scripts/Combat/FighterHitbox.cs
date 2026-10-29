
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

            // Skip invalid targets and do not hit ourselves.
            if (target == null ||
                target == fighter ||
                !target.IsAlive)
                continue;

            // One hit per target for each attack.
            if (!hitTargets.Add(target))
                continue;

            if (TryGetBlockingDefense(target, out FighterDefense defense))
            {
                ApplyBlockedHit(target, defense, attack);
                continue;
            }

            ApplyHit(target, attack);
        }
    }

    // Guard only works against attacks coming from the front.
    private bool TryGetBlockingDefense(
        FighterController target,
        out FighterDefense defense)
    {
        defense = target.GetComponent<FighterDefense>();

        if (defense == null || !defense.IsGuarding)
            return false;

        FighterFacing targetFacing =
            target.GetComponent<FighterFacing>();

        if (targetFacing == null)
            return true;

        float attackerDirection =
            fighter.transform.position.x -
            target.transform.position.x;

        return attackerDirection * targetFacing.FacingDirection > 0f;
    }

    private void ApplyBlockedHit(
        FighterController target,
        FighterDefense defense,
        AttackData attack)
    {
        defense.ReceiveBlockedHit();

        // Reduced pushback when the attack is blocked.
        ApplyPushback(target, attack, true);

        Debug.Log(
            $"{fighter.name}: ATTACK BLOCKED by {target.name}",
            this
        );
    }

    private void ApplyHit(
        FighterController target,
        AttackData attack)
    {
        target.ReceiveDamage(attack.damage);

        FighterHitReaction reaction =
            target.GetComponent<FighterHitReaction>();

        if (reaction != null)
            reaction.ReceiveHit(attack.hitStunFrames);

        // Full pushback after a successful hit.
        ApplyPushback(target, attack, false);

        Debug.Log(
            $"{fighter.name} hit {target.name} " +
            $"for {attack.damage} damage. " +
            $"Remaining HP: {target.CurrentHealth}",
            this
        );
    }

    private void ApplyPushback(
        FighterController target,
        AttackData attack,
        bool wasBlocked)
    {
        FighterPushback pushback =
            target.GetComponent<FighterPushback>();

        if (pushback != null)
        {
            pushback.ApplyPushback(
                fighter.transform.position.x,
                attack.pushbackDistance,
                wasBlocked
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
