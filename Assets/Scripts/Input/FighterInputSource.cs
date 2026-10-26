
using UnityEngine;

public abstract class FighterInputSource : MonoBehaviour
{
    public abstract float GetHorizontalInput();

    public virtual bool TryGetAttack(
        out AttackType attackType)
    {
        attackType = default;
        return false;
    }

    // Whether the defense key is currently held.
    public virtual bool IsGuardHeld()
    {
        return false;
    }
}
