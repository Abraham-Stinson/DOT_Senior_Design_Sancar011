using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameProject.Runtime.Manager
{
    public class RoundManager : MonoBehaviour
    {
        [Header("Round Settings")]
        [SerializeField] private int maxRoundTime = 90;
        [SerializeField] private int roundsToWin = 2; // Best of 3 (2 alan kazanır)

        [Header("UI - Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("UI - Round Indicators (Yuvarlaklar)")]
        [SerializeField] private Image[] p1RoundIcons;
        [SerializeField] private Image[] p2RoundIcons;
        [SerializeField] private Color roundWonColor = new Color(0.92f, 0.74f, 0.24f, 1f);
        [SerializeField] private Color roundEmptyColor = new Color(0.18f, 0.19f, 0.22f, 0.75f);

        [Header("UI - Center Announcements")]
        [SerializeField] private TextMeshProUGUI centerAnnouncementText;

        [Header("UI - Match Over Screen")]
        [SerializeField] private TextMeshProUGUI matchWinnerNameText;

        private FighterController player1;
        private FighterController player2;
        private MatchSpawner matchSpawner;

        private int p1RoundsWon = 0;
        private int p2RoundsWon = 0;
        private float currentTime;
        private bool isRoundActive = false;
        private Coroutine roundTimerCoroutine;
        private Coroutine announcementAnimCoroutine;
        private bool uiStyled = false;

        private void Awake()
        {
            EnsureRoundUIStyling();
        }

        public void Initialize(FighterController p1, FighterController p2, MatchSpawner spawner)
        {
            EnsureRoundUIStyling();

            // Önceki maçtan kalan abonelik ve coroutine'leri temizle (Rematch durumunda önemli)
            StopMatch();

            player1 = p1;
            player2 = p2;
            matchSpawner = spawner;

            if (player1 == null || player2 == null)
            {
                Debug.LogWarning($"{nameof(RoundManager)}: Fighter eksik, round sistemi başlatılamadı.");
                return;
            }

            p1RoundsWon = 0;
            p2RoundsWon = 0;
            UpdateRoundIcons(animateNewest: false);

            player1.OnHealthChanged += CheckHealthKO;
            player2.OnHealthChanged += CheckHealthKO;

            StartRoundSequence();
        }

        /// <summary>
        /// Maçı tamamen durdurur: coroutine'ler, event abonelikleri ve duyuru yazısı temizlenir.
        /// MatchSpawner.ClearMatch() tarafından karakterler yok edilmeden önce çağrılır.
        /// </summary>
        public void StopMatch()
        {
            isRoundActive = false;
            StopAllCoroutines();
            roundTimerCoroutine = null;
            announcementAnimCoroutine = null;

            UnsubscribeFromPlayers();
            player1 = null;
            player2 = null;

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.rectTransform.localScale = Vector3.one;
                centerAnnouncementText.gameObject.SetActive(false);
            }
        }

        private void UnsubscribeFromPlayers()
        {
            if (player1 != null) player1.OnHealthChanged -= CheckHealthKO;
            if (player2 != null) player2.OnHealthChanged -= CheckHealthKO;
        }

        private void StartRoundSequence()
        {
            isRoundActive = false;
            currentTime = maxRoundTime;
            UpdateTimerUI(pulse: false);

            // Karakterlerin canını ve pozisyonunu sıfırla
            player1.ResetHealth();
            player2.ResetHealth();
            matchSpawner.ResetFighterPositions();

            StartCoroutine(RoundStartAnimation());
        }

        private IEnumerator RoundStartAnimation()
        {
            ToggleControls(false);

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.gameObject.SetActive(true);
                Color warmIvory = new Color(0.96f, 0.88f, 0.68f, 1f);
                Color fightCrimson = new Color(0.90f, 0.22f, 0.22f, 1f);

                ShowAnimatedAnnouncement("3", warmIvory, 1f, 1.25f);
                yield return new WaitForSeconds(1f);

                ShowAnimatedAnnouncement("2", warmIvory, 1f, 1.25f);
                yield return new WaitForSeconds(1f);

                ShowAnimatedAnnouncement("1", warmIvory, 1f, 1.25f);
                yield return new WaitForSeconds(1f);

                ShowAnimatedAnnouncement("FIGHT!", fightCrimson, 1f, 1.32f);
                yield return new WaitForSeconds(1f);

                centerAnnouncementText.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            ToggleControls(true);
            isRoundActive = true;
            roundTimerCoroutine = StartCoroutine(RoundTimer());
        }

        private IEnumerator RoundTimer()
        {
            while (isRoundActive && currentTime > 0)
            {
                yield return new WaitForSeconds(1f);
                currentTime--;
                UpdateTimerUI(pulse: currentTime <= 10f);

                if (currentTime <= 0)
                {
                    TimeUp();
                }
            }
        }

        private void UpdateTimerUI(bool pulse = false)
        {
            if (timerText == null) return;

            int seconds = Mathf.Max(0, Mathf.CeilToInt(currentTime));
            timerText.text = seconds.ToString("00");

            if (seconds <= 10)
            {
                timerText.color = new Color(0.92f, 0.24f, 0.24f, 1f);
                if (pulse && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PulseRectTransform(timerText.rectTransform, 1.12f, 0.20f));
                }
            }
            else
            {
                timerText.color = new Color(0.95f, 0.90f, 0.76f, 1f);
                timerText.rectTransform.localScale = Vector3.one;
            }
        }

        private void CheckHealthKO(int currentHealth, int maxHealth)
        {
            if (!isRoundActive) return;

            if (player1.CurrentHealth <= 0 && player2.CurrentHealth <= 0)
            {
                EndRound(null); // Çifte KO (Beraberlik)
            }
            else if (player1.CurrentHealth <= 0)
            {
                EndRound(player2); // P2 kazandı
            }
            else if (player2.CurrentHealth <= 0)
            {
                EndRound(player1); // P1 kazandı
            }
        }

        private void TimeUp()
        {
            isRoundActive = false;

            // Oransal canı yüksek olan kazanır
            float p1HealthRatio = (float)player1.CurrentHealth / player1.MaxHealth;
            float p2HealthRatio = (float)player2.CurrentHealth / player2.MaxHealth;

            if (p1HealthRatio > p2HealthRatio)
            {
                EndRound(player1);
            }
            else if (p2HealthRatio > p1HealthRatio)
            {
                EndRound(player2);
            }
            else
            {
                EndRound(null); // Can oranları tamamen eşitse (Beraberlik)
            }
        }

        private void EndRound(FighterController winner)
        {
            isRoundActive = false;
            if (roundTimerCoroutine != null) StopCoroutine(roundTimerCoroutine);
            ToggleControls(false);

            StartCoroutine(RoundEndSequence(winner));
        }

        private IEnumerator RoundEndSequence(FighterController winner)
        {
            string announcement = "DRAW";
            Color announceColor = new Color(0.96f, 0.88f, 0.68f, 1f);

            if (winner == player1)
            {
                p1RoundsWon++;
                announcement = $"{player1.Data.displayName.ToUpperInvariant()} WINS";
            }
            else if (winner == player2)
            {
                p2RoundsWon++;
                announcement = $"{player2.Data.displayName.ToUpperInvariant()} WINS";
            }

            UpdateRoundIcons(animateNewest: true);

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.gameObject.SetActive(true);
                ShowAnimatedAnnouncement(announcement, announceColor, 3f, 1.25f);
            }

            yield return new WaitForSeconds(3f);

            if (centerAnnouncementText != null)
                centerAnnouncementText.gameObject.SetActive(false);

            CheckMatchWinner();
        }

        private void UpdateRoundIcons(bool animateNewest = false)
        {
            Color wonColor = new Color(0.92f, 0.74f, 0.24f, 1f);
            Color emptyColor = new Color(0.18f, 0.19f, 0.22f, 0.75f);

            for (int i = 0; i < p1RoundIcons.Length; i++)
            {
                if (p1RoundIcons[i] == null) continue;
                bool won = i < p1RoundsWon;
                p1RoundIcons[i].color = won ? wonColor : emptyColor;

                if (animateNewest && won && i == p1RoundsWon - 1 && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PulseRectTransform(p1RoundIcons[i].rectTransform, 1.25f, 0.26f));
                }
            }

            for (int i = 0; i < p2RoundIcons.Length; i++)
            {
                if (p2RoundIcons[i] == null) continue;
                bool won = i < p2RoundsWon;
                p2RoundIcons[i].color = won ? wonColor : emptyColor;

                if (animateNewest && won && i == p2RoundsWon - 1 && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PulseRectTransform(p2RoundIcons[i].rectTransform, 1.25f, 0.26f));
                }
            }
        }

        private void CheckMatchWinner()
        {
            if (p1RoundsWon >= roundsToWin)
            {
                EndMatch(player1);
            }
            else if (p2RoundsWon >= roundsToWin)
            {
                EndMatch(player2);
            }
            else
            {
                StartRoundSequence();
            }
        }

        private void EndMatch(FighterController matchWinner)
        {
            if (matchWinnerNameText != null && matchWinner != null)
            {
                string charName = matchWinner.Data != null ? matchWinner.Data.displayName.ToUpperInvariant() : "CHAMPION";
                matchWinnerNameText.richText = true;
                matchWinnerNameText.color = new Color(0.95f, 0.88f, 0.70f, 1f);
                matchWinnerNameText.text = $"{charName}\n<size=65%>WINS THE MATCH</size>";
            }

            GameManager.instance.SetGameState(EGameState.PVPM_MATCHOVER);
        }

        private void ShowAnimatedAnnouncement(string text, Color baseColor, float totalDuration, float startScale)
        {
            if (centerAnnouncementText == null) return;

            centerAnnouncementText.richText = false;
            centerAnnouncementText.text = text;
            centerAnnouncementText.color = baseColor;

            if (announcementAnimCoroutine != null)
                StopCoroutine(announcementAnimCoroutine);

            if (gameObject.activeInHierarchy)
                announcementAnimCoroutine = StartCoroutine(AnimateAnnouncementRoutine(centerAnnouncementText, baseColor, totalDuration, startScale));
        }

        private IEnumerator AnimateAnnouncementRoutine(TextMeshProUGUI label, Color baseColor, float duration, float startScale)
        {
            RectTransform rt = label.rectTransform;
            float elapsed = 0f;
            float popDuration = Mathf.Min(0.18f, duration * 0.30f);
            float fadeStart = Mathf.Max(popDuration, duration - 0.18f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (elapsed <= popDuration)
                {
                    float t = Mathf.Clamp01(elapsed / popDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    float scale = Mathf.Lerp(startScale, 1f, eased);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    label.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(t * 2.5f));
                }
                else if (elapsed >= fadeStart)
                {
                    float t = Mathf.InverseLerp(fadeStart, duration, elapsed);
                    rt.localScale = Vector3.one;
                    label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
                }
                else
                {
                    rt.localScale = Vector3.one;
                    label.color = baseColor;
                }

                yield return null;
            }

            rt.localScale = Vector3.one;
            label.color = baseColor;
        }

        private IEnumerator PulseRectTransform(RectTransform rt, float peakScale, float duration)
        {
            if (rt == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curve = Mathf.Sin(t * Mathf.PI);
                float s = Mathf.Lerp(1f, peakScale, curve);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private void EnsureRoundUIStyling()
        {
            if (uiStyled) return;
            uiStyled = true;

            if (timerText != null)
            {
                RectTransform tr = timerText.rectTransform;
                tr.anchorMin = new Vector2(0.5f, 0.5f);
                tr.anchorMax = new Vector2(0.5f, 0.5f);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = new Vector2(0f, 466f);
                tr.sizeDelta = new Vector2(96f, 48f);

                timerText.enableAutoSizing = false;
                timerText.fontSize = 42f;
                timerText.color = new Color(0.95f, 0.90f, 0.76f, 1f);
                timerText.fontStyle = FontStyles.Bold;
                timerText.alignment = TextAlignmentOptions.Center;

                AddTextShadow(timerText.gameObject, new Color(0f, 0f, 0f, 0.9f), 2f);
            }

            if (centerAnnouncementText != null)
            {
                RectTransform cr = centerAnnouncementText.rectTransform;
                cr.anchorMin = new Vector2(0.5f, 0.5f);
                cr.anchorMax = new Vector2(0.5f, 0.5f);
                cr.pivot = new Vector2(0.5f, 0.5f);
                cr.anchoredPosition = new Vector2(0f, 50f);
                cr.sizeDelta = new Vector2(900f, 140f);

                centerAnnouncementText.enableAutoSizing = false;
                centerAnnouncementText.fontSize = 76f;
                centerAnnouncementText.color = new Color(0.96f, 0.88f, 0.68f, 1f);
                centerAnnouncementText.fontStyle = FontStyles.Bold;
                centerAnnouncementText.alignment = TextAlignmentOptions.Center;
                centerAnnouncementText.enableWordWrapping = false;
                centerAnnouncementText.overflowMode = TextOverflowModes.Overflow;
                AddTextShadow(centerAnnouncementText.gameObject, new Color(0f, 0f, 0f, 0.92f), 3f);
            }

            if (matchWinnerNameText != null)
            {
                matchWinnerNameText.enableAutoSizing = false;
                matchWinnerNameText.fontSize = 48f;
                matchWinnerNameText.color = new Color(0.95f, 0.88f, 0.70f, 1f);
                matchWinnerNameText.fontStyle = FontStyles.Bold;
                matchWinnerNameText.alignment = TextAlignmentOptions.Center;
                AddTextShadow(matchWinnerNameText.gameObject, new Color(0f, 0f, 0f, 0.92f), 2.5f);
            }

            StyleRoundIcons(p1RoundIcons, isLeftSide: true);
            StyleRoundIcons(p2RoundIcons, isLeftSide: false);
        }

        private void StyleRoundIcons(Image[] icons, bool isLeftSide)
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;

                RectTransform rt = icons[i].rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float x = isLeftSide ? (-188f + i * 22f) : (188f - i * 22f);
                rt.anchoredPosition = new Vector2(x, 444f);
                rt.sizeDelta = new Vector2(14f, 14f);

                if (!icons[i].TryGetComponent(out Outline outline))
                {
                    outline = icons[i].gameObject.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0.85f, 0.70f, 0.30f, 0.55f);
                outline.effectDistance = new Vector2(1f, -1f);

                if (!icons[i].TryGetComponent(out Shadow shadow))
                {
                    shadow = icons[i].gameObject.AddComponent<Shadow>();
                }
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void AddTextShadow(GameObject target, Color color, float dist)
        {
            if (target == null) return;

            if (target.TryGetComponent(out Outline outline))
            {
                outline.enabled = false;
            }

            if (!target.TryGetComponent(out Shadow shadow))
            {
                shadow = target.AddComponent<Shadow>();
            }
            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(dist, -dist);
        }

        private void ToggleControls(bool canControl)
        {
            if (player1 != null)
            {
                if (player1.TryGetComponent(out FighterMotor m1))
                {
                    m1.enabled = canControl;
                    if (!canControl) m1.StopMovement();
                }
                if (player1.TryGetComponent(out FighterCombat c1)) c1.enabled = canControl;
                if (player1.TryGetComponent(out FighterDefense d1)) d1.enabled = canControl;
            }

            if (player2 != null)
            {
                if (player2.TryGetComponent(out FighterMotor m2))
                {
                    m2.enabled = canControl;
                    if (!canControl) m2.StopMovement();
                }
                if (player2.TryGetComponent(out FighterCombat c2)) c2.enabled = canControl;
                if (player2.TryGetComponent(out FighterDefense d2)) d2.enabled = canControl;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromPlayers();
        }

        // --- MATCH OVER EKRANI BUTONLARI İÇİN METOTLAR ---

        public void Btn_Rematch()
        {
            if (matchSpawner == null) return;

            GameManager.instance.SetGameState(EGameState.PVPM_PLAYSCREEN);
            matchSpawner.RestartCurrentMatch();
        }

        public void Btn_CharacterSelection()
        {
            if (matchSpawner != null) matchSpawner.ClearMatch();
            GameManager.instance.SetGameState(EGameState.PVPM_MENU_CHARACTERSELECTION);
        }

        public void Btn_MainMenu()
        {
            if (matchSpawner != null) matchSpawner.ClearMatch();
            GameManager.instance.SetGameState(EGameState.PLAYMODESELECTION);
        }
    }
}
