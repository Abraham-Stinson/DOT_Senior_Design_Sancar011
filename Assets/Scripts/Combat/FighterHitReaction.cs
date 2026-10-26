
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(FighterController))]
[RequireComponent(typeof(FighterStateMachine))]
[RequireComponent(typeof(FighterCombat))]
public class FighterHitReaction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private FighterStateMachine stateMachine;
    [SerializeField] private FighterCombat combat;
    [SerializeField] private FighterMotor motor;

    [Header("Animation")]
    [SerializeField] private string hitAnimatorStateName =
        "Sancar_HitReaction";

    [SerializeField] private string idleAnimatorStateName =
        "Sancar_Idle";

    [Header("Safety")]
    [SerializeField] private float maximumReactionSeconds = 3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private static readonly int MoveDirection =
        Animator.StringToHash("MoveDirection");

    private bool reacting;
    private bool reactionAnimationObserved;
    private bool reactionAnimationFinished;

    private int stunFramesRemaining;
    private float reactionElapsedTime;

    public bool IsReacting => reacting;

    private void Awake()
    {
        if (fighter == null)
            fighter = GetComponent<FighterController>();

        if (stateMachine == null)
            stateMachine = GetComponent<FighterStateMachine>();

        if (combat == null)
            combat = GetComponent<FighterCombat>();

        if (motor == null)
            motor = GetComponent<FighterMotor>();
    }

    public void ReceiveHit(int hitStunFrames)
    {
        if (fighter == null || stateMachine == null)
            return;

        // A hit interrupts an attack already in progress.
        combat?.CancelAttack();

        motor?.StopMovement();

        if (fighter.Animator != null)
            fighter.Animator.SetInteger(MoveDirection, 0);

        // KO animation will be added with the round system.
        if (!fighter.IsAlive)
        {
            reacting = false;
            stunFramesRemaining = 0;

            stateMachine.EnterKO();

            if (showDebugLogs)
                Debug.Log($"{fighter.name}: KO", this);

            return;
        }

        stateMachine.EnterHitStun();

        // A new hit refreshes hitstun.
        stunFramesRemaining = Mathf.Max(1, hitStunFrames);

        reacting = true;
        reactionElapsedTime = 0f;
        reactionAnimationObserved = false;
        reactionAnimationFinished = false;

        PlayReactionAnimation();

        if (showDebugLogs)
        {
            Debug.Log(
                $"{fighter.name}: HIT REACTION STARTED " +
                $"({stunFramesRemaining} stun frames)",
                this
            );
        }
    }

    private void PlayReactionAnimation()
    {
        Animator animator = fighter.Animator;

        if (animator == null)
        {
            reactionAnimationFinished = true;
            return;
        }

        int stateHash = Animator.StringToHash(
            "Base Layer." + hitAnimatorStateName
        );

        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning(
                $"Hit animation state not found: " +
                hitAnimatorStateName,
                this
            );

            reactionAnimationFinished = true;
            return;
        }

        animator.Play(stateHash, 0, 0f);
    }

    private void FixedUpdate()
    {
        if (!reacting)
            return;

        if (stunFramesRemaining > 0)
            stunFramesRemaining--;
    }

    private void Update()
    {
        if (!reacting || fighter == null)
            return;

        reactionElapsedTime += Time.deltaTime;

        CheckReactionAnimation();

        bool stunFinished = stunFramesRemaining <= 0;

        // Never end hit reaction before both the
        // visual animation and hitstun have completed.
        if (stunFinished && reactionAnimationFinished)
        {
            FinishReaction();
            return;
        }

        // Emergency recovery if an animation gets stuck.
        if (reactionElapsedTime >= maximumReactionSeconds)
        {
            Debug.LogWarning(
                $"{fighter.name}: Hit reaction safety timeout",
                this
            );

            FinishReaction();
        }
    }

    private void CheckReactionAnimation()
    {
        if (reactionAnimationFinished)
            return;

        Animator animator = fighter.Animator;

        if (animator == null)
        {
            reactionAnimationFinished = true;
            return;
        }

        AnimatorStateInfo info =
            animator.GetCurrentAnimatorStateInfo(0);

        bool inReactionState =
            info.IsName(hitAnimatorStateName);

        if (inReactionState)
        {
            reactionAnimationObserved = true;

            if (info.normalizedTime >= 1f)
                reactionAnimationFinished = true;
        }
        else if (reactionAnimationObserved)
        {
            // The Animator automatically transitioned away.
            reactionAnimationFinished = true;
        }
    }

    private void FinishReaction()
    {
        if (!reacting)
            return;

        reacting = false;
        stunFramesRemaining = 0;

        if (!fighter.IsAlive)
        {
            stateMachine.EnterKO();
            return;
        }

        stateMachine.ReturnToIdle();

        Animator animator = fighter.Animator;

        if (animator != null)
        {
            animator.SetInteger(MoveDirection, 0);

            animator.Play(
                idleAnimatorStateName, 0, 0f
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"{fighter.name}: HIT REACTION FINISHED",
                this
            );
        }
    }

    private void OnDisable()
    {
        reacting = false;
        stunFramesRemaining = 0;
        reactionElapsedTime = 0f;

        if (stateMachine != null &&
            stateMachine.CurrentState == FighterState.HitStun)
        {
            stateMachine.ReturnToIdle();
        }
    }
}
