
using UnityEngine;

[DefaultExecutionOrder(-75)]
[RequireComponent(typeof(FighterController))]
[RequireComponent(typeof(FighterStateMachine))]
public class FighterDefense : MonoBehaviour
{
    private enum DefensePhase
    {
        None,
        Starting,
        Holding,
        Ending,
        BlockStun
    }

    [Header("References")]
    [SerializeField] private FighterController fighter;
    [SerializeField] private FighterStateMachine stateMachine;
    [SerializeField] private FighterInputSource inputSource;
    [SerializeField] private FighterMotor motor;

    [Header("Animator State Names")]
    [SerializeField] private string blockStartState =
        "Sancar_BlockStart";

    [SerializeField] private string blockHoldState =
        "Sancar_BlockHold";

    [SerializeField] private string blockEndState =
        "Sancar_BlockEnd";

    [SerializeField] private string idleState =
        "Sancar_Idle";

    [Header("BlockStun")]
    [Min(1)]
    [SerializeField] private int blockStunFrames = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private static readonly int MoveDirection =
        Animator.StringToHash("MoveDirection");

    private DefensePhase phase = DefensePhase.None;
    private int stunRemaining;
    private bool stateObserved;
    private float phaseElapsed;

    public bool IsGuarding =>
        phase == DefensePhase.Starting ||
        phase == DefensePhase.Holding ||
        phase == DefensePhase.BlockStun;

    public bool IsInBlockStun =>
        phase == DefensePhase.BlockStun;

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
            stateMachine == null ||
            inputSource == null)
            return;

        if (!fighter.IsAlive)
        {
            phase = DefensePhase.None;
            return;
        }

        bool held = inputSource.IsGuardHeld();

        if (phase == DefensePhase.None)
        {
            if (held && stateMachine.IsGuardAllowed)
                StartGuard();

            return;
        }

        if (phase == DefensePhase.BlockStun)
            return;

        phaseElapsed += Time.deltaTime;

        if (phase == DefensePhase.Starting)
        {
            if (AnimationFinished(blockStartState))
            {
                if (held)
                    EnterHold();
                else
                    BeginGuardEnd();
            }
        }
        else if (phase == DefensePhase.Holding)
        {
            if (!held)
                BeginGuardEnd();
        }
        else if (phase == DefensePhase.Ending)
        {
            if (AnimationFinished(blockEndState))
                FinishGuard();
        }
    }

    private void FixedUpdate()
    {
        if (phase != DefensePhase.BlockStun)
            return;

        if (stunRemaining > 0)
            stunRemaining--;

        if (stunRemaining <= 0)
            FinishBlockStun();
    }

    private void StartGuard()
    {
        if (!stateMachine.TryEnterGuard())
            return;

        motor?.StopMovement();
        phase = DefensePhase.Starting;

        PlayPhaseAnimation(blockStartState);

        if (showDebugLogs)
            Debug.Log($"{name}: GUARD START", this);
    }

    private void EnterHold()
    {
        if (phase == DefensePhase.BlockStun)
            return;

        phase = DefensePhase.Holding;
        PlayPhaseAnimation(blockHoldState);
    }

    private void BeginGuardEnd()
    {
        phase = DefensePhase.Ending;
        PlayPhaseAnimation(blockEndState);
    }

    private void FinishGuard()
    {
        phase = DefensePhase.None;

        stateMachine.ReturnToIdle();
        PlayAnimation(idleState);

        if (showDebugLogs)
            Debug.Log($"{name}: GUARD END", this);
    }

    // Called by FighterHitbox when an attack is blocked.
    public void ReceiveBlockedHit()
    {
        if (!IsGuarding || !fighter.IsAlive)
            return;

        phase = DefensePhase.BlockStun;
        stunRemaining = blockStunFrames;

        stateMachine.EnterBlockStun();
        motor?.StopMovement();

        // Keep the guard pose during blockstun.
        PlayPhaseAnimation(blockHoldState);

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: BLOCK SUCCESS - " +
                $"{stunRemaining} stun frames",
                this
            );
        }
    }

    private void FinishBlockStun()
    {
        if (phase != DefensePhase.BlockStun)
            return;

        stunRemaining = 0;

        stateMachine.ReturnToIdle();

        if (inputSource != null &&
            inputSource.IsGuardHeld())
        {
            // Return to guard without replaying BlockStart.
            if (stateMachine.TryEnterGuard())
            {
                EnterHold();
                return;
            }
        }

        // Stay locked until BlockEnd finishes.
        if (stateMachine.TryEnterGuard())
        {
            BeginGuardEnd();
        }
        else
        {
            phase = DefensePhase.None;
            PlayAnimation(idleState);
        }
    }

    private void PlayPhaseAnimation(string stateName)
    {
        phaseElapsed = 0f;
        stateObserved = false;

        PlayAnimation(stateName);
    }

    private void PlayAnimation(string stateName)
    {
        Animator animator = fighter.Animator;

        if (animator == null)
            return;

        animator.SetInteger(MoveDirection, 0);

        int hash = Animator.StringToHash(
            "Base Layer." + stateName
        );

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning(
                $"Animator state missing: {stateName}",
                this
            );
            return;
        }

        animator.Play(hash, 0, 0f);
    }

    private bool AnimationFinished(string stateName)
    {
        Animator animator = fighter.Animator;

        if (animator == null)
            return true;

        AnimatorStateInfo info =
            animator.GetCurrentAnimatorStateInfo(0);

        if (info.IsName(stateName))
        {
            stateObserved = true;

            if (info.normalizedTime >= 1f)
                return true;
        }
        else if (stateObserved)
        {
            // Animator transitioned out of this state.
            return true;
        }

        // Safety against an incorrect animation setup.
        if (phaseElapsed >= 2f)
        {
            Debug.LogWarning(
                $"Defense animation timeout: {stateName}",
                this
            );
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        phase = DefensePhase.None;
        stunRemaining = 0;
        phaseElapsed = 0f;

        if (stateMachine != null &&
            (stateMachine.CurrentState == FighterState.Guard ||
             stateMachine.CurrentState == FighterState.BlockStun))
        {
            stateMachine.ReturnToIdle();
        }
    }
}
