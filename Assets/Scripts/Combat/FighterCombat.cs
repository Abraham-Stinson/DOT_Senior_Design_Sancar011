
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(FighterController))]
[RequireComponent(typeof(FighterStateMachine))]
public class FighterCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private FighterStateMachine stateMachine;
    [SerializeField] private FighterInputSource inputSource;
    [SerializeField] private FighterMotor motor;

    [Header("Safety")]
    [SerializeField] private float maxAttackDuration = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private static readonly int MoveDirection =
        Animator.StringToHash("MoveDirection");

    private AttackData currentAttack;

    private int elapsedCombatFrames;
    private float attackElapsedTime;

    private bool attackRunning;
    private bool combatTimingFinished;
    private bool hitWindowOpen;
    private bool attackStateObserved;

    public AttackData CurrentAttack => currentAttack;
    public bool IsAttacking => attackRunning;
    public bool IsHitWindowOpen => hitWindowOpen;

    private void Awake()
    {
        if (fighter == null)
            fighter = GetComponent<FighterController>();

        if (stateMachine == null)
            stateMachine = GetComponent<FighterStateMachine>();

        if (inputSource == null)
            inputSource = GetComponent<FighterInputSource>();

        if (motor == null)
            motor = GetComponent<FighterMotor>();
    }

    private void Update()
    {
        if (fighter == null ||
            fighter.Animator == null ||
            stateMachine == null)
            return;

        if (!attackRunning)
        {
            if (stateMachine.IsAttackAllowed &&
                inputSource != null &&
                inputSource.TryGetAttack(out AttackType type))
            {
                TryStartAttack(type);
            }

            return;
        }

        attackElapsedTime += Time.deltaTime;

        CheckAnimationCompletion();
    }

    public bool TryStartAttack(AttackType attackType)
    {
        if (attackRunning ||
            fighter == null ||
            fighter.Animator == null ||
            stateMachine == null)
            return false;

        if (!stateMachine.IsAttackAllowed)
            return false;

        AttackData attack = fighter.GetAttack(attackType);

        if (attack == null)
        {
            Debug.LogWarning(
                $"AttackData missing: {attackType}", this
            );
            return false;
        }

        string stateName = attack.animatorStateName;

        if (string.IsNullOrWhiteSpace(stateName))
        {
            Debug.LogWarning(
                "Animator State Name is empty!", this
            );
            return false;
        }

        int stateHash = Animator.StringToHash(
            "Base Layer." + stateName
        );

        if (!fighter.Animator.HasState(0, stateHash))
        {
            Debug.LogWarning(
                $"Animator state not found: {stateName}", this
            );
            return false;
        }

        if (!stateMachine.TryEnterAttack())
            return false;

        currentAttack = attack;
        elapsedCombatFrames = 0;
        attackElapsedTime = 0f;

        attackRunning = true;
        combatTimingFinished = false;
        hitWindowOpen = false;
        attackStateObserved = false;

        motor?.StopMovement();

        fighter.Animator.SetInteger(MoveDirection, 0);
        fighter.Animator.Play(stateHash, 0, 0f);

        if (showDebugLogs)
        {
            Debug.Log(
                $"{fighter.name} started {attackType}", this
            );
        }

        return true;
    }

    private void FixedUpdate()
    {
        if (!attackRunning ||
            combatTimingFinished ||
            currentAttack == null)
            return;

        elapsedCombatFrames++;

        int startupEnd = currentAttack.startupFrames;

        int activeEnd =
            startupEnd + currentAttack.activeFrames;

        int recoveryEnd =
            activeEnd + currentAttack.recoveryFrames;

        if (elapsedCombatFrames <= startupEnd)
            return;

        if (elapsedCombatFrames <= activeEnd)
        {
            if (!hitWindowOpen)
            {
                hitWindowOpen = true;
                stateMachine.SetAttackActive();

                if (showDebugLogs)
                    Debug.Log("HIT WINDOW OPEN", this);
            }

            return;
        }

        if (hitWindowOpen)
        {
            hitWindowOpen = false;

            if (showDebugLogs)
                Debug.Log("HIT WINDOW CLOSED", this);
        }

        stateMachine.SetAttackRecovery();

        if (elapsedCombatFrames >= recoveryEnd)
            combatTimingFinished = true;
    }

    private void CheckAnimationCompletion()
    {
        if (currentAttack == null)
        {
            FinishAttack();
            return;
        }

        AnimatorStateInfo info =
            fighter.Animator.GetCurrentAnimatorStateInfo(0);

        bool inAttackState =
            info.IsName(currentAttack.animatorStateName);

        if (inAttackState)
        {
            attackStateObserved = true;

            // Both animation and combat timing completed.
            if (combatTimingFinished &&
                info.normalizedTime >= 1f)
            {
                FinishAttack();
                return;
            }
        }
        else if (combatTimingFinished)
        {
            // The Animator may have transitioned away
            // from the attack state automatically.
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "Attack state ended early or changed. " +
                    "Restoring neutral state.",
                    this
                );
            }

            FinishAttack();
            return;
        }

        // Prevent permanent control lock
        // if an Animator clip or transition misbehaves.
        if (attackElapsedTime >= maxAttackDuration)
        {
            Debug.LogWarning(
                "Attack safety timeout reached. " +
                $"State observed: {attackStateObserved}",
                this
            );

            FinishAttack();
        }
    }

    public void CancelAttack()
    {
        if (!attackRunning)
            return;

        attackRunning = false;
        combatTimingFinished = false;
        hitWindowOpen = false;
        attackStateObserved = false;

        elapsedCombatFrames = 0;
        attackElapsedTime = 0f;
        currentAttack = null;

        motor?.StopMovement();

        if (showDebugLogs)
        {
            Debug.Log(
                $"{fighter.name}: ATTACK CANCELLED",
                this
            );
        }
    }

    private void FinishAttack()
    {
        if (!attackRunning)
            return;

        attackRunning = false;
        combatTimingFinished = false;
        hitWindowOpen = false;
        attackStateObserved = false;

        elapsedCombatFrames = 0;
        attackElapsedTime = 0f;
        currentAttack = null;

        stateMachine.ReturnToIdle();

        if (fighter != null && fighter.Animator != null)
        {
            fighter.Animator.SetInteger(MoveDirection, 0);

            string idleState = fighter.Data != null
                ? fighter.Data.idleAnimatorStateName
                : "Sancar_Idle";

            fighter.Animator.Play(idleState, 0, 0f);
        }

        if (showDebugLogs)
            Debug.Log("ATTACK FINISHED - READY", this);
    }

    private void OnDisable()
    {
        attackRunning = false;
        combatTimingFinished = false;
        hitWindowOpen = false;
        attackStateObserved = false;

        currentAttack = null;
        elapsedCombatFrames = 0;
        attackElapsedTime = 0f;

        if (stateMachine != null)
            stateMachine.ReturnToIdle();
    }
}
