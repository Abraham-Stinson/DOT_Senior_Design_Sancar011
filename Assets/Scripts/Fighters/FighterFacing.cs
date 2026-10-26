
using UnityEngine;

[DefaultExecutionOrder(-150)]
[RequireComponent(typeof(FighterController))]
public class FighterFacing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private FighterHitbox hitbox;

    [Header("Facing Settings")]
    [SerializeField] private bool facingRight = true;

    private FighterController target;

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
    }

    private void Start()
    {
        FindOpponent();
        ApplyFacing();
    }

    private void Update()
    {
        if (target == null || !target.isActiveAndEnabled)
            FindOpponent();

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
    }

    public void RefreshTarget()
    {
        FindOpponent();
    }
}
