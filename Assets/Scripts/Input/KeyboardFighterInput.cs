
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FighterController))]
public class KeyboardFighterInput : FighterInputSource
{
    private FighterController fighter;

    private void Awake()
    {
        fighter = GetComponent<FighterController>();
    }

    public override float GetHorizontalInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || fighter == null)
            return 0f;

        bool left = false;
        bool right = false;

        switch (fighter.ControlType)
        {
            case FighterControlType.Player1:
                left = keyboard.aKey.isPressed;
                right = keyboard.dKey.isPressed;
                break;

            case FighterControlType.Player2:
                left = keyboard.leftArrowKey.isPressed;
                right = keyboard.rightArrowKey.isPressed;
                break;

            case FighterControlType.AI:
                return 0f;
        }

        if (left == right)
            return 0f;

        return right ? 1f : -1f;
    }

    public override bool TryGetAttack(
        out AttackType attackType)
    {
        attackType = default;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || fighter == null)
            return false;

        switch (fighter.ControlType)
        {
            case FighterControlType.Player1:
                if (keyboard.jKey.wasPressedThisFrame)
                    attackType = AttackType.LightPunch;
                else if (keyboard.kKey.wasPressedThisFrame)
                    attackType = AttackType.HeavyPunch;
                else if (keyboard.uKey.wasPressedThisFrame)
                    attackType = AttackType.LightKick;
                else if (keyboard.iKey.wasPressedThisFrame)
                    attackType = AttackType.HeavyKick;
                else
                    return false;

                return true;

            case FighterControlType.Player2:
                if (keyboard.numpad1Key.wasPressedThisFrame)
                    attackType = AttackType.LightPunch;
                else if (keyboard.numpad2Key.wasPressedThisFrame)
                    attackType = AttackType.HeavyPunch;
                else if (keyboard.numpad4Key.wasPressedThisFrame)
                    attackType = AttackType.LightKick;
                else if (keyboard.numpad5Key.wasPressedThisFrame)
                    attackType = AttackType.HeavyKick;
                else
                    return false;

                return true;
        }

        return false;
    }

    public override bool IsGuardHeld()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || fighter == null)
            return false;

        switch (fighter.ControlType)
        {
            case FighterControlType.Player1:
                return keyboard.spaceKey.isPressed;

            case FighterControlType.Player2:
                return keyboard.rightCtrlKey.isPressed;

            default:
                return false;
        }
    }
}
