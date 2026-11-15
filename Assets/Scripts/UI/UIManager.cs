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
            // Başlangıç ekranında yazıyı nefes alır gibi parlat ve klavyeden Space/Enter ile geçişe izin ver
            if (firstStartScreenUI != null && firstStartScreenUI.activeInHierarchy)
            {
                if (pressToStartLabel != null)
                {
                    float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3.5f);
                    pressToStartLabel.color = new Color(1f, 0.86f, 0.35f, pulse);
                    float scale = 1f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.04f;
                    pressToStartLabel.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }

                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
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
                StartCoroutine(FadeCanvasGroupRoutine(cg, 0.18f));
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
                bg.color = new Color(0.05f, 0.06f, 0.10f, 0.96f);
            }

            TextMeshProUGUI[] texts = firstStartScreenUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0)
            {
                pressToStartLabel = texts[0];
                pressToStartLabel.richText = true;
                pressToStartLabel.text = "PRESS ANY KEY OR CLICK TO START";
                pressToStartLabel.color = new Color(1f, 0.86f, 0.35f, 1f);
                pressToStartLabel.fontStyle = FontStyles.Bold;
                DecorateText(pressToStartLabel.gameObject);
            }

            // Üst kısma oyun başlığı ekle (yoksa)
            if (firstStartScreenUI.transform.Find("GameTitleHeader") == null && pressToStartLabel != null)
            {
                GameObject titleObj = new GameObject("GameTitleHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(firstStartScreenUI.transform, false);

                RectTransform rt = titleObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.68f);
                rt.anchorMax = new Vector2(0.5f, 0.68f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(1200f, 220f);

                TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
                titleText.font = pressToStartLabel.font;
                titleText.richText = true;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.fontStyle = FontStyles.Bold;
                titleText.fontSize = 92f;
                titleText.color = new Color(1f, 0.84f, 0.25f, 1f);
                titleText.text = "SANCAR <color=#FF3B4E>011</color>\n<size=36%><color=#CFD8DC>ARENA FIGHTER</color></size>";
                titleText.raycastTarget = false;
                DecorateText(titleObj);
            }
        }

        private void StylePlayModeScreen()
        {
            if (playModeSelectionScreenUI == null) return;

            if (playModeSelectionScreenUI.TryGetComponent(out Image bg))
            {
                bg.color = new Color(0.05f, 0.06f, 0.10f, 0.96f);
            }

            // Mevcut başlığı ("Select A Path") düzenle
            TextMeshProUGUI[] screenTexts = playModeSelectionScreenUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < screenTexts.Length; i++)
            {
                if (screenTexts[i].GetComponentInParent<Button>() != null) continue;
                screenTexts[i].text = "SELECT GAME MODE";
                screenTexts[i].color = new Color(1f, 0.84f, 0.28f, 1f);
                screenTexts[i].fontStyle = FontStyles.Bold;
                DecorateText(screenTexts[i].gameObject);
            }

            Button[] buttons = playModeSelectionScreenUI.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                bool isMulti = buttons[i].name.Contains("Multi");
                Color accent = isMulti ? new Color(1f, 0.28f, 0.34f, 1f) : new Color(0.25f, 0.72f, 1f, 0.65f);
                StyleArcadeButton(buttons[i], new Color(0.10f, 0.12f, 0.19f, 0.96f), accent);
            }
        }

        private void StyleMatchOverScreen()
        {
            if (pvpMatchOverScreenUI == null) return;

            if (pvpMatchOverScreenUI.TryGetComponent(out Image bg))
            {
                bg.color = new Color(0.04f, 0.05f, 0.09f, 0.84f);
            }

            Button[] buttons = pvpMatchOverScreenUI.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Color accent = new Color(0.95f, 0.78f, 0.25f, 1f);
                if (buttons[i].name.Contains("Revenge"))
                    accent = new Color(1f, 0.28f, 0.34f, 1f);
                else if (buttons[i].name.Contains("Character"))
                    accent = new Color(0.25f, 0.72f, 1f, 1f);

                StyleArcadeButton(buttons[i], new Color(0.10f, 0.12f, 0.18f, 0.96f), accent);
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
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.58f, 0.65f, 0.65f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            if (!btn.TryGetComponent(out Outline outline))
            {
                outline = btn.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = borderAccent;
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            if (!btn.TryGetComponent(out Shadow shadow))
            {
                shadow = btn.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(5f, -5f);

            TextMeshProUGUI tmpLabel = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpLabel != null)
            {
                tmpLabel.color = new Color(0.96f, 0.97f, 1f, 1f);
                tmpLabel.fontStyle = FontStyles.Bold;
                tmpLabel.text = tmpLabel.text.ToUpperInvariant();
                DecorateText(tmpLabel.gameObject);
            }

            Text legacyLabel = btn.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                legacyLabel.color = btn.interactable ? new Color(1f, 0.92f, 0.65f, 1f) : new Color(0.65f, 0.68f, 0.74f, 0.8f);
                legacyLabel.fontStyle = FontStyle.Bold;
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
                if (rt != null) rt.localScale = new Vector3(1.06f, 1.06f, 1f);
                if (outline != null) outline.effectColor = new Color(1f, 0.88f, 0.35f, 1f);
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
            if (!textObj.TryGetComponent(out Outline outline))
            {
                outline = textObj.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            if (!textObj.TryGetComponent(out Shadow shadow))
            {
                shadow = textObj.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(3f, -3f);
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
