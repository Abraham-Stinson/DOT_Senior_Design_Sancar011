
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameProject.Runtime.CharacterMovement
{
    public class SancarMovement : MonoBehaviour
    {
        #region Fields
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float minX = -8f;
        [SerializeField] private float maxX = 8f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private static readonly int MoveDirection =
            Animator.StringToHash("MoveDirection");

        // Movement states
        private const string IdleState = "Sancar_Idle";

        // Attack states
        private const string LightPunchState = "Sancar_LightPunch";
        private const string HeavyPunchState = "Sancar_HeavyPunch";
        private const string LightKickState = "Sancar_LightKick";
        private const string HeavyKickState = "Sancar_HeavyKick";

        // Defense states
        private const string BlockStartState = "Sancar_BlockStart";
        private const string BlockHoldState = "Sancar_BlockHold";
        private const string BlockEndState = "Sancar_BlockEnd";

        // Damage state
        private const string HitReactionState = "Sancar_HitReaction";

        private Coroutine currentAction;

        private bool isBusy;
        private bool isAttacking;
        private bool isDefending;
        private bool isTakingHit;

        public bool IsAttacking => isAttacking;
        public bool IsTakingHit => isTakingHit;

        public bool IsBlocking =>
            isDefending &&
            !isTakingHit &&
            Keyboard.current != null &&
            Keyboard.current.lKey.isPressed;

        #endregion
        #region Unity Methods
        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError(
                    "Sancar: Animator bulunamadi!", this
                );
                enabled = false;
                return;
            }

            animator.applyRootMotion = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            // Hit Reaction en yuksek oncelige sahiptir.
            if (keyboard.hKey.wasPressedThisFrame)
            {
                TakeHit();
                return;
            }

            // Bir animasyon tamamlanana kadar
            // normal kontrolleri beklet.
            if (isBusy)
                return;

            // Savunma
            if (keyboard.lKey.isPressed)
            {
                BeginAction(PlayDefense());
                return;
            }

            // Saldirilar
            if (HandleAttacks(keyboard))
                return;

            // Yurume
            HandleMovement(keyboard);
        }
        #endregion
        #region Methods
        private bool HandleAttacks(Keyboard keyboard)
        {
            if (keyboard.jKey.wasPressedThisFrame)
            {
                BeginAction(PlayAttack(LightPunchState));
                return true;
            }

            if (keyboard.kKey.wasPressedThisFrame)
            {
                BeginAction(PlayAttack(HeavyPunchState));
                return true;
            }

            if (keyboard.uKey.wasPressedThisFrame)
            {
                BeginAction(PlayAttack(LightKickState));
                return true;
            }

            if (keyboard.iKey.wasPressedThisFrame)
            {
                BeginAction(PlayAttack(HeavyKickState));
                return true;
            }

            return false;
        }

        private void BeginAction(IEnumerator routine)
        {
            // Onceki action varsa durdur.
            if (currentAction != null)
            {
                StopCoroutine(currentAction);
                currentAction = null;
            }

            currentAction = StartCoroutine(routine);
        }

        private IEnumerator PlayAttack(string stateName)
        {
            isBusy = true;
            isAttacking = true;

            animator.SetInteger(MoveDirection, 0);

            yield return PlayAnimationOnce(stateName);

            animator.Play(IdleState, 0, 0f);

            isAttacking = false;
            isBusy = false;
            currentAction = null;
        }

        private IEnumerator PlayDefense()
        {
            isBusy = true;
            isDefending = true;

            animator.SetInteger(MoveDirection, 0);

            // 1. Blok pozisyonuna gec.
            yield return PlayAnimationOnce(BlockStartState);

            // 2. Savunma pozunu koru.
            animator.Play(BlockHoldState, 0, 0f);

            while (Keyboard.current != null &&
                   Keyboard.current.lKey.isPressed)
            {
                yield return null;
            }

            // Tus birakildi.
            isDefending = false;

            // 3. Normal garde don.
            yield return PlayAnimationOnce(BlockEndState);

            animator.Play(IdleState, 0, 0f);

            isBusy = false;
            currentAction = null;
        }

        // Test tusu ve ileride hasar sistemi
        // tarafindan cagrilabilecek fonksiyon.
        public void TakeHit()
        {
            // Hit Reaction zaten oynuyorsa yeniden baslatma.
            if (isTakingHit)
                return;

            // Saldiri veya savunma coroutine'ini kes.
            if (currentAction != null)
            {
                StopCoroutine(currentAction);
                currentAction = null;
            }

            // Onceki aksiyon durumlarini temizle.
            isBusy = false;
            isAttacking = false;
            isDefending = false;

            BeginAction(PlayHitReaction());
        }

        private IEnumerator PlayHitReaction()
        {
            isBusy = true;
            isTakingHit = true;

            animator.SetInteger(MoveDirection, 0);

            // Darbe tepkisini bastan sona oynat.
            yield return PlayAnimationOnce(HitReactionState);

            // Normal garde don.
            animator.Play(IdleState, 0, 0f);

            isTakingHit = false;
            isBusy = false;
            currentAction = null;
        }

        private IEnumerator PlayAnimationOnce(string stateName)
        {
            animator.Play(stateName, 0, 0f);

            // Animator state degisikligini islesin.
            yield return null;

            float elapsed = 0f;
            const float timeout = 5f;

            while (elapsed < timeout)
            {
                AnimatorStateInfo info =
                    animator.GetCurrentAnimatorStateInfo(0);

                if (!info.IsName(stateName))
                {
                    Debug.LogWarning(
                        "Animator state bulunamadi: " + stateName,
                        this
                    );
                    yield break;
                }

                if (info.normalizedTime >= 1f)
                    yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning(
                "Animasyon zaman asimina ugradi: " + stateName,
                this
            );
        }

        private void HandleMovement(Keyboard keyboard)
        {
            bool moveRight = keyboard.dKey.isPressed;
            bool moveLeft = keyboard.aKey.isPressed;

            int direction = 0;

            if (moveRight && !moveLeft)
                direction = 1;
            else if (moveLeft && !moveRight)
                direction = -1;

            Vector3 targetPosition = transform.position;

            targetPosition.x = Mathf.Clamp(
                targetPosition.x +
                direction * moveSpeed * Time.deltaTime,
                minX,
                maxX
            );

            bool canMove = !Mathf.Approximately(
                targetPosition.x,
                transform.position.x
            );

            transform.position = targetPosition;

            animator.SetInteger(
                MoveDirection,
                canMove ? direction : 0
            );
        }

        private void OnDisable()
        {
            currentAction = null;
            isBusy = false;
            isAttacking = false;
            isDefending = false;
            isTakingHit = false;
        }
        #endregion
    }
    

}
