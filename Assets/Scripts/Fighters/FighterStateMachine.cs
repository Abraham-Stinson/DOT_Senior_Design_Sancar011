
using UnityEngine;

public enum FighterState
{
    Idle,
    Walking,
    AttackStartup,
    AttackActive,
    AttackRecovery,
    Guard,
    HitStun,
    BlockStun,
    Special,
    KO
}

[RequireComponent(typeof(FighterController))]
public class FighterStateMachine : MonoBehaviour
{
    public FighterState CurrentState { get; private set; }
        = FighterState.Idle;

    public bool IsMovementAllowed =>
        CurrentState == FighterState.Idle ||
        CurrentState == FighterState.Walking;

    public bool IsAttackAllowed => IsMovementAllowed;

    public bool IsGuardAllowed => IsMovementAllowed;

    public bool IsBusy => !IsMovementAllowed;

    public bool TryEnterAttack()
    {
        if (!IsAttackAllowed)
            return false;

        ChangeState(FighterState.AttackStartup);
        return true;
    }

    public bool TryEnterGuard()
    {
        if (!IsGuardAllowed)
            return false;

        ChangeState(FighterState.Guard);
        return true;
    }

    public void SetWalking(bool walking)
    {
        if (!IsMovementAllowed)
            return;

        ChangeState(
            walking ? FighterState.Walking : FighterState.Idle
        );
    }

    public void SetAttackActive()
    {
        if (CurrentState == FighterState.AttackStartup)
            ChangeState(FighterState.AttackActive);
    }

    public void SetAttackRecovery()
    {
        if (CurrentState == FighterState.AttackStartup ||
            CurrentState == FighterState.AttackActive)
        {
            ChangeState(FighterState.AttackRecovery);
        }
    }

    public void EnterHitStun()
    {
        if (CurrentState == FighterState.KO)
            return;

        ChangeState(FighterState.HitStun);
    }

    public void EnterBlockStun()
    {
        if (CurrentState == FighterState.KO)
            return;

        ChangeState(FighterState.BlockStun);
    }

    public void EnterKO()
    {
        ChangeState(FighterState.KO);
    }

    public void ReturnToIdle()
    {
        if (CurrentState == FighterState.KO)
            return;

        ChangeState(FighterState.Idle);
    }

    private void ChangeState(FighterState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
    }
}
