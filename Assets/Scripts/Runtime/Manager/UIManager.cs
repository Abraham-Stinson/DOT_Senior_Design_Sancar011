using UnityEngine;
using UnityEngine.UI;

namespace GameProject.Runtime.Manager
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("MENU")]
        [SerializeField] private GameObject firstStartScreenUI;
        [SerializeField] private GameObject playModeSelectionScreenUI;
        [SerializeField] private GameObject storyMenuScreenUI;
        [SerializeField] private GameObject pvpCharacterSelectionScreenUI;

        [Header("HUD")]
        [SerializeField] private GameObject storyPlayScreenUI;
        [SerializeField] private GameObject pvpPlayScreenUI;
        [SerializeField] private GameObject pauseMenuUI;

        [Header("ENDGAME")]
        [SerializeField] private GameObject winScreenUI;
        [SerializeField] private GameObject loseScreenUI;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(EGameState newState)
        {
            HideAllScreens();

            switch (newState)
            {
                case EGameState.FIRSTSTART:
                    if (firstStartScreenUI) firstStartScreenUI.SetActive(true);
                    break;
                case EGameState.PLAYMODESELECTION:
                    if (playModeSelectionScreenUI) playModeSelectionScreenUI.SetActive(true);
                    break;
                case EGameState.STRM_MENU:
                    if (storyMenuScreenUI) storyMenuScreenUI.SetActive(true);
                    break;
                case EGameState.STRM_PLAYSCREEN:
                    if (storyPlayScreenUI) storyPlayScreenUI.SetActive(true);
                    break;
                case EGameState.PVPM_MENU_CHARACTERSELECTION:
                    if (pvpCharacterSelectionScreenUI) pvpCharacterSelectionScreenUI.SetActive(true);
                    break;
                case EGameState.PVPM_PLAYSCREEN:
                    if (pvpPlayScreenUI) pvpPlayScreenUI.SetActive(true);
                    break;
                case EGameState.WIN:
                    if (winScreenUI) winScreenUI.SetActive(true);
                    break;
                case EGameState.LOSE:
                    if (loseScreenUI) loseScreenUI.SetActive(true);
                    break;
            }
        }

        private void HideAllScreens()
        {
            if (firstStartScreenUI) firstStartScreenUI.SetActive(false);
            if (playModeSelectionScreenUI) playModeSelectionScreenUI.SetActive(false);
            if (storyMenuScreenUI) storyMenuScreenUI.SetActive(false);
            if (storyPlayScreenUI) storyPlayScreenUI.SetActive(false);
            if (pvpCharacterSelectionScreenUI) pvpCharacterSelectionScreenUI.SetActive(false);
            if (pvpPlayScreenUI) pvpPlayScreenUI.SetActive(false);
            if (pauseMenuUI) pauseMenuUI.SetActive(false);
            if (winScreenUI) winScreenUI.SetActive(false);
            if (loseScreenUI) loseScreenUI.SetActive(false);
        }

        public void ShowPlayModeSelection() => GameManager.instance.SetGameState(EGameState.PLAYMODESELECTION);
        public void ShowStoryMenu() => GameManager.instance.SetGameState(EGameState.STRM_MENU);
        public void StartStoryModePlay() => GameManager.instance.SetGameState(EGameState.STRM_PLAYSCREEN);
        public void ShowPvPCharacterSelection() => GameManager.instance.SetGameState(EGameState.PVPM_MENU_CHARACTERSELECTION);
        public void StartPvPModePlay() => GameManager.instance.SetGameState(EGameState.PVPM_PLAYSCREEN);

        public void PauseGame()
        {
            Time.timeScale = 0;
            if (pauseMenuUI) pauseMenuUI.SetActive(true);
        }

        public void ResumeGame()
        {
            Time.timeScale = 1;
            if (pauseMenuUI) pauseMenuUI.SetActive(false);
        }
    }
}