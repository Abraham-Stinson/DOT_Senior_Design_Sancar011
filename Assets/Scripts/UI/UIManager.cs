using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        [SerializeField] private GameObject pvpMapSelectionScreenUI;

        [Header("HUD")]
        [SerializeField] private GameObject storyPlayScreenUI;
        [SerializeField] private GameObject pvpPlayScreenUI;
        [SerializeField] private GameObject pauseMenuUI;

        [Header("ENDGAME")]
        [SerializeField] private GameObject winScreenUI;
        [SerializeField] private GameObject loseScreenUI;
        [Tooltip("PvP maç sonu ekranı (Rematch / Character Selection / Main Menu butonları)")]
        [SerializeField] private GameObject pvpMatchOverScreenUI;
        [Tooltip("Maç sonu ekranında PvP HUD'u (can barları, round ikonları) arkada görünür kalsın mı?")]
        [SerializeField] private bool keepPvPHUDOnMatchOver = true;

        private TextMeshProUGUI pressToStartLabel;
        private bool menusStyled = false;

        private static readonly Color WarmIvoryColor = new Color(0.92f, 0.88f, 0.76f, 1f);
        private static readonly Color ButtonFillColor = new Color(0.11f, 0.12f, 0.15f, 0.94f);
        private static readonly Color ButtonBorderColor = new Color(0.78f, 0.70f, 0.50f, 0.45f);
        private static readonly Color ButtonHoverBorderColor = new Color(0.94f, 0.86f, 0.65f, 0.85f);
        private static readonly Color ButtonLabelColor = new Color(0.94f, 0.94f, 0.96f, 1f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            EnsureMenusStyled();
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            // Başlangıç ekranında yazıda yumuşak saydamlık nefesi ve herhangi bir tuşla geçiş
            if (firstStartScreenUI != null && firstStartScreenUI.activeInHierarchy)
            {
                if (pressToStartLabel != null)
                {
                    float alpha = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f));
                    pressToStartLabel.color = new Color(WarmIvoryColor.r, WarmIvoryColor.g, WarmIvoryColor.b, alpha);
                    pressToStartLabel.rectTransform.localScale = Vector3.one;
                }

                Keyboard kb = Keyboard.current;
                if (kb != null && kb.anyKey.wasPressedThisFrame)
                {
                    ShowPlayModeSelection();
                }
            }
        }

        private void HandleGameStateChanged(EGameState newState)
        {
            EnsureMenusStyled();
            HideAllScreens();

            switch (newState)
            {
                case EGameState.FIRSTSTART:
                    ActivateWithFade(firstStartScreenUI);
                    break;
                case EGameState.PLAYMODESELECTION:
                    ActivateWithFade(playModeSelectionScreenUI);
                    break;
                case EGameState.STRM_MENU:
                    ActivateWithFade(storyMenuScreenUI);
                    break;
                case EGameState.STRM_PLAYSCREEN:
                    if (storyPlayScreenUI) storyPlayScreenUI.SetActive(true);
                    break;
                case EGameState.PVPM_MENU_CHARACTERSELECTION:
                    ActivateWithFade(pvpCharacterSelectionScreenUI);
                    break;
                case EGameState.PVPM_MENU_MAPSELECTION:
                    ActivateWithFade(pvpMapSelectionScreenUI);
                    break;
                case EGameState.PVPM_PLAYSCREEN:
                    if (pvpPlayScreenUI) pvpPlayScreenUI.SetActive(true);
                    break;
                case EGameState.WIN:
                    ActivateWithFade(winScreenUI);
                    break;
                case EGameState.LOSE:
                    ActivateWithFade(loseScreenUI);
                    break;
                case EGameState.PVPM_MATCHOVER:
                    if (keepPvPHUDOnMatchOver && pvpPlayScreenUI) pvpPlayScreenUI.SetActive(true);
                    ActivateWithFade(pvpMatchOverScreenUI);
                    break;
            }
        }

        private void ActivateWithFade(GameObject screen)
        {
            if (screen == null) return;
            screen.SetActive(true);

            if (!screen.TryGetComponent(out CanvasGroup cg))
            {
                cg = screen.AddComponent<CanvasGroup>();
            }

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeCanvasGroupRoutine(cg, 0.16f));
            }
            else
            {
                cg.alpha = 1f;
            }
        }

        private IEnumerator FadeCanvasGroupRoutine(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (cg == null) yield break;
                cg.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;
        }

        private void HideAllScreens()
        {
            if (firstStartScreenUI) firstStartScreenUI.SetActive(false);
            if (playModeSelectionScreenUI) playModeSelectionScreenUI.SetActive(false);
            if (storyMenuScreenUI) storyMenuScreenUI.SetActive(false);
            if (storyPlayScreenUI) storyPlayScreenUI.SetActive(false);
            if (pvpCharacterSelectionScreenUI) pvpCharacterSelectionScreenUI.SetActive(false);
            if (pvpMapSelectionScreenUI) pvpMapSelectionScreenUI.SetActive(false);
            if (pvpPlayScreenUI) pvpPlayScreenUI.SetActive(false);
            if (pauseMenuUI) pauseMenuUI.SetActive(false);
            if (winScreenUI) winScreenUI.SetActive(false);
            if (loseScreenUI) loseScreenUI.SetActive(false);
            if (pvpMatchOverScreenUI) pvpMatchOverScreenUI.SetActive(false);
        }

        private void EnsureMenusStyled()
        {
            if (menusStyled) return;
            menusStyled = true;

            StyleFirstStartScreen();
            StylePlayModeScreen();
            StyleMatchOverScreen();
        }

        private void StyleFirstStartScreen()
        {
            if (firstStartScreenUI == null) return;

            if (firstStartScreenUI.TryGetComponent(out Image bg))
            {
                bg.color = new Color(0.05f, 0.05f, 0.07f, 0.96f);
            }

            TextMeshProUGUI[] texts = firstStartScreenUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0)
            {
                pressToStartLabel = texts[0];
                pressToStartLabel.richText = false;
                pressToStartLabel.enableAutoSizing = false;
                pressToStartLabel.fontSize = 32f;
                pressToStartLabel.text = "PRESS ANY KEY TO START";
                pressToStartLabel.color = WarmIvoryColor;
                pressToStartLabel.fontStyle = FontStyles.Bold;
                DecorateText(pressToStartLabel.gameObject);
            }
        }

        private void StylePlayModeScreen()
        {
            if (playModeSelectionScreenUI == null) return;

            if (playModeSelectionScreenUI.TryGetComponent(out Image bg))
            {
                bg.color = new Color(0.05f, 0.05f, 0.07f, 0.96f);
            }

            TextMeshProUGUI[] screenTexts = playModeSelectionScreenUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < screenTexts.Length; i++)
            {
                if (screenTexts[i].GetComponentInParent<Button>() != null) continue;
                screenTexts[i].richText = false;
                screenTexts[i].enableAutoSizing = false;
                screenTexts[i].fontSize = 42f;
                screenTexts[i].text = "SELECT GAME MODE";
                screenTexts[i].color = new Color(0.92f, 0.88f, 0.78f, 1f);
                screenTexts[i].fontStyle = FontStyles.Bold;
                DecorateText(screenTexts[i].gameObject);
            }

            Button[] buttons = playModeSelectionScreenUI.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                StyleArcadeButton(buttons[i], ButtonFillColor, ButtonBorderColor);
            }
        }

        private void StyleMatchOverScreen()
        {
            if (pvpMatchOverScreenUI == null) return;

            if (pvpMatchOverScreenUI.TryGetComponent(out Image bg))
            {
                bg.color = new Color(0.04f, 0.04f, 0.06f, 0.86f);
            }

            Button[] buttons = pvpMatchOverScreenUI.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                StyleArcadeButton(buttons[i], ButtonFillColor, ButtonBorderColor);
            }
        }

        private void StyleArcadeButton(Button btn, Color baseBg, Color borderAccent)
        {
            if (btn == null) return;

            if (btn.TryGetComponent(out Image img))
            {
                img.color = baseBg;
            }

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
            cb.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.58f, 0.64f, 0.65f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            if (!btn.TryGetComponent(out Outline outline))
            {
                outline = btn.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = borderAccent;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (!btn.TryGetComponent(out Shadow shadow))
            {
                shadow = btn.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            shadow.effectDistance = new Vector2(3.5f, -3.5f);

            TextMeshProUGUI tmpLabel = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpLabel != null)
            {
                tmpLabel.color = ButtonLabelColor;
                tmpLabel.fontStyle = FontStyles.Bold;
                tmpLabel.text = tmpLabel.text.ToUpperInvariant();
                DecorateText(tmpLabel.gameObject);
            }

            Text legacyLabel = btn.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                legacyLabel.color = btn.interactable ? ButtonLabelColor : new Color(0.60f, 0.62f, 0.68f, 0.75f);
                legacyLabel.fontStyle = (FontStyle)FontStyles.Bold;
                DecorateText(legacyLabel.gameObject);
            }

            if (btn.interactable)
            {
                AddButtonHoverAnimation(btn.gameObject, outline, borderAccent);
            }
        }

        private void AddButtonHoverAnimation(GameObject btnObj, Outline outline, Color baseAccent)
        {
            if (!btnObj.TryGetComponent(out EventTrigger trigger))
            {
                trigger = btnObj.AddComponent<EventTrigger>();
            }

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((_) =>
            {
                if (rt != null) rt.localScale = new Vector3(1.03f, 1.03f, 1f);
                if (outline != null) outline.effectColor = ButtonHoverBorderColor;
            });
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((_) =>
            {
                if (rt != null) rt.localScale = Vector3.one;
                if (outline != null) outline.effectColor = baseAccent;
            });
            trigger.triggers.Add(exitEntry);
        }

        private void DecorateText(GameObject textObj)
        {
            if (textObj == null) return;
            if (textObj.TryGetComponent(out Outline outline))
            {
                outline.enabled = false;
            }

            if (!textObj.TryGetComponent(out Shadow shadow))
            {
                shadow = textObj.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
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
