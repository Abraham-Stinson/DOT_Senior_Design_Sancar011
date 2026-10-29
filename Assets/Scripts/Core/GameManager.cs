using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameProject.Runtime.Manager
{
    public class GameManager : MonoBehaviour
    {
        #region Fields
        public static GameManager instance;
        public EGameState gameState { get; private set; }
        public static event Action<EGameState> OnGameStateChanged;
        private bool initialized;

        #endregion
        #region Unity Methods
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            SetGameState(EGameState.FIRSTSTART);
        }
        private void Update()
        {
            if (gameState == EGameState.FIRSTSTART)
            {
                bool isKeyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
                bool isMousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                bool isGamepadPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

                if (isKeyboardPressed || isMousePressed || isGamepadPressed)
                {
                    SetGameState(EGameState.PLAYMODESELECTION);
                }
            }
        }
        #endregion
        #region Methods
        public void SetGameState(EGameState newState)
        {
            if (initialized && gameState == newState) return;

            initialized = true;
            gameState = newState;
            Debug.Log($"{nameof(GameManager)}: Switched to {gameState}");
            OnGameStateChanged?.Invoke(gameState);
        }

        public bool IsGame() => gameState == EGameState.STRM_PLAYSCREEN || gameState == EGameState.PVPM_PLAYSCREEN;
        #endregion
    }
}

