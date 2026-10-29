using System;
using UnityEngine;

public enum FighterControlType
{
    Player1,
    Player2,
    AI
}

public class FighterController : MonoBehaviour
{
    [Header("Character Definition")]
    [SerializeField] private CharacterData characterData;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;

    [Header("Control Source")]
    [SerializeField] private FighterControlType controlType = FighterControlType.Player1;

    public event Action<int, int> OnHealthChanged;

    // Runtime data: each fighter has its own values.
    private int currentHealth;
    private float movementSpeed;

    private bool isInitialized;

    public CharacterData Data => characterData;
    public Animator Animator => animator;
    public Transform VisualRoot => visualRoot;

    public FighterControlType ControlType => controlType;

    public int CurrentHealth => currentHealth;

    public int MaxHealth =>
        characterData != null ? characterData.maxHealth : 0;

    public float MovementSpeed => movementSpeed;

    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (visualRoot == null && animator != null)
            visualRoot = animator.transform;

        if (characterData != null)
        {
            Initialize(characterData);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: CharacterData atanmamis!",
                this
            );
        }
    }

    public void Initialize(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogError(
                $"{name}: Gecersiz CharacterData!",
                this
            );

            return;
        }

        characterData = data;

        currentHealth = characterData.maxHealth;
        movementSpeed = characterData.movementSpeed;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            if (characterData.animatorController != null)
            {
                animator.runtimeAnimatorController =
                    characterData.animatorController;
            }

            animator.applyRootMotion = false;
        }

        isInitialized = true;
    }

    public void SetControlType(FighterControlType newType)
    {
        controlType = newType;
    }

    public AttackData GetAttack(AttackType attackType)
    {
        if (!isInitialized || characterData == null)
            return null;

        return characterData.GetBasicAttack(attackType);
    }

    public void ResetHealth()
    {
        if (!isInitialized)
            return;

        currentHealth = characterData.maxHealth;
    }

    public void ReceiveDamage(int damage)
    {
        if (!isInitialized || !IsAlive)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - Mathf.Max(0, damage)
        );
    }

    public void RestoreHealth(int amount)
    {
        if (!isInitialized)
            return;

        currentHealth = Mathf.Min(
            MaxHealth,
            currentHealth + Mathf.Max(0, amount)
        );
    }

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            visualRoot = animator.transform;
    }
}
