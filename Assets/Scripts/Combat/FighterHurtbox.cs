
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FighterHurtbox : MonoBehaviour
{
    [SerializeField] private FighterController owner;

    [Tooltip(
        "The hurtbox is authored for a RIGHT-facing fighter. " +
        "When the fighter turns left, its local X offset is mirrored " +
        "so it stays on the body (the sprite is flipped with flipX)."
    )]
    [SerializeField] private bool mirrorWithFacing = true;

    private BoxCollider2D box;

    private bool baseCaptured;
    private float baseLocalX;
    private float baseOffsetX;

    public FighterController Owner => owner;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<FighterController>();

        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        CaptureBaseOffsets();
    }

    private void CaptureBaseOffsets()
    {
        if (baseCaptured)
            return;

        if (box == null)
            box = GetComponent<BoxCollider2D>();

        baseLocalX = transform.localPosition.x;
        baseOffsetX = box.offset.x;
        baseCaptured = true;
    }

    // Called by FighterFacing whenever the fighter turns around.
    public void SetFacingRight(bool facingRight)
    {
        if (!mirrorWithFacing)
            return;

        CaptureBaseOffsets();

        float sign = facingRight ? 1f : -1f;

        Vector3 localPosition = transform.localPosition;
        localPosition.x = baseLocalX * sign;
        transform.localPosition = localPosition;

        Vector2 offset = box.offset;
        offset.x = baseOffsetX * sign;
        box.offset = offset;
    }

    private void Reset()
    {
        owner = GetComponentInParent<FighterController>();

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
    }
}
