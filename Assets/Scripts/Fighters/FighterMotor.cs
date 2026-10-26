
using UnityEngine;

[RequireComponent(typeof(FighterController))]
[RequireComponent(typeof(FighterStateMachine))]
[RequireComponent(typeof(FighterFacing))]
public class FighterMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private FighterInputSource inputSource;
    [SerializeField] private FighterStateMachine stateMachine;
    [SerializeField] private FighterFacing facing;

    [Header("Arena Movement Limits")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    private static readonly int MoveDirection =
        Animator.StringToHash("MoveDirection");

    private void Awake()
    {
        if (fighter == null)
            fighter = GetComponent<FighterController>();

        if (inputSource == null)
            inputSource = GetComponent<FighterInputSource>();

        if (stateMachine == null)
            stateMachine = GetComponent<FighterStateMachine>();

        if (facing == null)
            facing = GetComponent<FighterFacing>();
    }

    private void Update()
    {
        if (fighter == null ||
            fighter.Animator == null ||
            stateMachine == null)
            return;

        if (!stateMachine.IsMovementAllowed)
        {
            StopMovement();
            return;
        }

        float horizontal = inputSource != null
            ? inputSource.GetHorizontalInput()
            : 0f;

        int worldDirection =
            horizontal > 0.1f ? 1 :
            horizontal < -0.1f ? -1 : 0;

        Vector3 position = transform.position;

        float targetX = Mathf.Clamp(
            position.x +
            worldDirection * fighter.MovementSpeed * Time.deltaTime,
            minX,
            maxX
        );

        bool moving =
            Mathf.Abs(targetX - position.x) > 0.0001f;

        position.x = targetX;
        transform.position = position;

        // Convert world movement into relative movement.
        // Forward = towards the direction we are facing.
        int facingDirection =
            facing != null ? facing.FacingDirection : 1;

        int animationDirection = moving
            ? worldDirection * facingDirection
            : 0;

        fighter.Animator.SetInteger(
            MoveDirection,
            animationDirection
        );

        stateMachine.SetWalking(moving);
    }

    public void StopMovement()
    {
        if (fighter != null && fighter.Animator != null)
            fighter.Animator.SetInteger(MoveDirection, 0);
    }

    private void OnDisable()
    {
        StopMovement();
    }
}
