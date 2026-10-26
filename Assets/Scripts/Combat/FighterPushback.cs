
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(FighterController))]
public class FighterPushback : MonoBehaviour
{
    [Header("Pushback Settings")]
    [Min(0.01f)]
    [SerializeField] private float pushDuration = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float blockedPushMultiplier = 0.35f;

    [Header("Arena Limits")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool isPushing;
    private float elapsedTime;
    private float previousProgress;
    private float totalDistance;
    private float direction;

    public bool IsPushing => isPushing;

    // attackerX: world X position of the attacker.
    public void ApplyPushback(
        float attackerX,
        float distance,
        bool wasBlocked)
    {
        float finalDistance = Mathf.Max(0f, distance);

        if (wasBlocked)
            finalDistance *= blockedPushMultiplier;

        if (finalDistance <= 0f)
            return;

        float difference = transform.position.x - attackerX;

        // Push away from the attacker.
        // Exact overlap uses the existing facing direction
        // as a fallback.
        if (Mathf.Abs(difference) > 0.001f)
        {
            direction = Mathf.Sign(difference);
        }
        else
        {
            FighterFacing facing =
                GetComponent<FighterFacing>();

            direction = facing != null
                ? -facing.FacingDirection
                : 1f;
        }

        totalDistance = finalDistance;
        elapsedTime = 0f;
        previousProgress = 0f;
        isPushing = true;

        if (showDebugLogs)
        {
            Debug.Log(
                $"{name}: PUSHBACK START " +
                $"Distance={finalDistance:F2} " +
                $"Direction={direction}",
                this
            );
        }
    }

    private void LateUpdate()
    {
        if (!isPushing)
            return;

        elapsedTime += Time.deltaTime;

        float duration = Mathf.Max(0.01f, pushDuration);

        float t = Mathf.Clamp01(elapsedTime / duration);

        // Ease-out: faster at the beginning,
        // slower near the end.
        float progress = 1f - Mathf.Pow(1f - t, 3f);

        float step = (progress - previousProgress)
                     * totalDistance;

        previousProgress = progress;

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(
            position.x + step * direction,
            minX,
            maxX
        );

        transform.position = position;

        if (t >= 1f)
        {
            isPushing = false;

            if (showDebugLogs)
                Debug.Log($"{name}: PUSHBACK END", this);
        }
    }

    public void StopPushback()
    {
        isPushing = false;
        elapsedTime = 0f;
        previousProgress = 0f;
        totalDistance = 0f;
    }

    private void OnDisable()
    {
        StopPushback();
    }

    private void OnValidate()
    {
        pushDuration = Mathf.Max(0.01f, pushDuration);

        if (maxX < minX)
            maxX = minX;
    }
}
