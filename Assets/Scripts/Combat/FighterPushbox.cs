
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(FighterController))]
public class FighterPushbox : MonoBehaviour
{
    private static readonly List<FighterPushbox> Fighters =
        new List<FighterPushbox>();

    [Header("Pushbox Settings")]
    [Min(0.1f)]
    [SerializeField] private float width = 0.9f;

    [Min(0.1f)]
    [SerializeField] private float height = 2f;

    [SerializeField] private float centerY = 1.1f;

    [Header("Arena Limits")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    public float HalfWidth => width * 0.5f;

    private void OnEnable()
    {
        if (!Fighters.Contains(this))
            Fighters.Add(this);
    }

    private void OnDisable()
    {
        Fighters.Remove(this);
    }

    private void LateUpdate()
    {
        foreach (FighterPushbox other in Fighters)
        {
            if (other == null || other == this)
                continue;

            // Each pair is processed only once.
            if (GetInstanceID() > other.GetInstanceID())
                continue;

            ResolveOverlap(other);
        }
    }

    private void ResolveOverlap(FighterPushbox other)
    {
        float x1 = transform.position.x;
        float x2 = other.transform.position.x;

        float minimumDistance =
            HalfWidth + other.HalfWidth;

        float delta = x2 - x1;
        float currentDistance = Mathf.Abs(delta);

        if (currentDistance >= minimumDistance)
            return;

        float direction;

        if (Mathf.Abs(delta) < 0.0001f)
        {
            direction = GetInstanceID() <
                other.GetInstanceID() ? 1f : -1f;
        }
        else
        {
            direction = Mathf.Sign(delta);
        }

        float penetration =
            minimumDistance - currentDistance;

        // Initially share correction between both fighters.
        float firstMove = penetration * 0.5f;
        float secondMove = penetration * 0.5f;

        float targetX1 = Mathf.Clamp(
            x1 - direction * firstMove,
            minX,
            maxX
        );

        float targetX2 = Mathf.Clamp(
            x2 + direction * secondMove,
            other.minX,
            other.maxX
        );

        // If one fighter is against an arena boundary,
        // give the remaining correction to the other.
        float remaining = minimumDistance -
            Mathf.Abs(targetX2 - targetX1);

        if (remaining > 0.0001f)
        {
            targetX2 = Mathf.Clamp(
                targetX2 + direction * remaining,
                other.minX,
                other.maxX
            );
        }

        remaining = minimumDistance -
            Mathf.Abs(targetX2 - targetX1);

        if (remaining > 0.0001f)
        {
            targetX1 = Mathf.Clamp(
                targetX1 - direction * remaining,
                minX,
                maxX
            );
        }

        SetX(transform, targetX1);
        SetX(other.transform, targetX2);
    }

    private static void SetX(Transform target, float x)
    {
        Vector3 position = target.position;
        position.x = x;
        target.position = position;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = new Color(
            0.2f, 0.8f, 1f, 1f
        );

        Vector3 center = transform.position +
            new Vector3(0f, centerY, 0f);

        Gizmos.DrawWireCube(
            center,
            new Vector3(width, height, 0.15f)
        );
    }

    private void OnValidate()
    {
        width = Mathf.Max(0.1f, width);
        height = Mathf.Max(0.1f, height);

        if (maxX < minX)
            maxX = minX;
    }
}
