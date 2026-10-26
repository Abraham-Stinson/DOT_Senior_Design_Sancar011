
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FighterHurtbox : MonoBehaviour
{
    [SerializeField] private FighterController owner;

    public FighterController Owner => owner;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<FighterController>();

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void Reset()
    {
        owner = GetComponentInParent<FighterController>();

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }
}
