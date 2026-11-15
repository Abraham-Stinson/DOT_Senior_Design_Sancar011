
using UnityEngine;

[DefaultExecutionOrder(-150)]
[RequireComponent(typeof(FighterController))]
public class FighterFacing : MonoBehaviour
{
    // How often to look for an opponent when we do not have one.
    // FindObjectsByType is expensive, so it must not run every frame.
    private const float OpponentSearchInterval = 0.25f;

    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private FighterHitbox hitbox;
    [SerializeField] private FighterHurtbox hurtbox;

    [Header("Facing Settings")]
    [SerializeField] private bool facingRight = true;

    private FighterController target;
    private float nextOpponentSearchTime;

    public bool FacingRight => facingRight;
    public int FacingDirection => facingRight ? 1 : -1;

    private void Awake()
    {
        if (fighter == null)
            fighter = GetComponent<FighterController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (hitbox == null)
            hitbox = GetComponent<FighterHitbox>();

        if (hurtbox == null)
            hurtbox = GetComponentInChildren<FighterHurtbox>(true);
    }

    private void Start()
    {
        FindOpponent();
        UpdateFacing();
        ApplyFacing();
    }

    private void Update()
    {
        if (target == null || !target.isActiveAndEnabled)
        {
            if (Time.time < nextOpponentSearchTime)
                return;

            nextOpponentSearchTime = Time.time + OpponentSearchInterval;
            FindOpponent();
        }

        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (target == null)
            return;

        float distanceX =
            target.transform.position.x - transform.position.x;

        if (Mathf.Abs(distanceX) < 0.01f)
            return;

        bool shouldFaceRight = distanceX > 0f;

        if (facingRight != shouldFaceRight)
        {
            facingRight = shouldFaceRight;
            ApplyFacing();
        }
    }

    private void FindOpponent()
    {
        FighterController[] fighters =
            FindObjectsByType<FighterController>(
                FindObjectsSortMode.None
            );

        target = null;
        float closestDistance = float.MaxValue;

        foreach (FighterController candidate in fighters)
        {
            if (candidate == fighter ||
                !candidate.isActiveAndEnabled)
                continue;

            float distance = Mathf.Abs(
                candidate.transform.position.x -
                transform.position.x
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = candidate;
            }
        }
    }

    private void ApplyFacing()
    {
        // Our original Sancar sprites face right.
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;

        if (hitbox != null)
            hitbox.SetFacingRight(facingRight);

        // The hurtbox is offset from the root (the body is not centered
        // in the sprite), so it has to be mirrored together with the sprite.
        if (hurtbox != null)
            hurtbox.SetFacingRight(facingRight);
    }

    public void RefreshTarget()
    {
        FindOpponent();
        UpdateFacing();
    }
}
