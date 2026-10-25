using System;
using Unity.VisualScripting;
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
        private bool firstOpen = true;

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
            firstOpen = true;
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
            if (!firstOpen && gameState == newState) return;

            gameState = newState;
            Debug.Log($"Game Object: {gameObject.name}.Compopent:{this.GetType().Name} : Switched to the {gameState}");
            OnGameStateChanged?.Invoke(gameState);
        }

        public bool IsGame() => gameState == EGameState.STRM_PLAYSCREEN || gameState == EGameState.PVPM_PLAYSCREEN;
        #endregion
    }
}

