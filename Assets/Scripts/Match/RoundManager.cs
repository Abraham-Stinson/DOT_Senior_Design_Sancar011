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
        [SerializeField] private Color roundWonColor = new Color(1f, 0.82f, 0.18f, 1f);
        [SerializeField] private Color roundEmptyColor = new Color(0.14f, 0.16f, 0.22f, 0.85f);

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

                ShowAnimatedAnnouncement("3", new Color(1f, 0.88f, 0.35f, 1f), 1f, 1.65f);
                yield return new WaitForSeconds(1f);

                ShowAnimatedAnnouncement("2", new Color(1f, 0.68f, 0.22f, 1f), 1f, 1.75f);
                yield return new WaitForSeconds(1f);

                ShowAnimatedAnnouncement("1", new Color(1f, 0.42f, 0.22f, 1f), 1f, 1.85f);
                yield return new WaitForSeconds(1f);

                ShowAnimatedAnnouncement("FIGHT!", new Color(1f, 0.22f, 0.25f, 1f), 1f, 2.2f);
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
                timerText.color = new Color(1f, 0.25f, 0.28f, 1f);
                if (pulse && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PulseRectTransform(timerText.rectTransform, 1.28f, 0.22f));
                }
            }
            else
            {
                timerText.color = new Color(1f, 0.88f, 0.42f, 1f);
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
            string announcement = "DRAW!";
            Color announceColor = new Color(0.9f, 0.9f, 0.95f, 1f);

            if (winner == player1)
            {
                p1RoundsWon++;
                announcement = $"{player1.Data.displayName.ToUpperInvariant()}\n<size=68%><color=#FFD147>WINS THE ROUND!</color></size>";
                announceColor = new Color(0.35f, 0.82f, 1f, 1f);
            }
            else if (winner == player2)
            {
                p2RoundsWon++;
                announcement = $"{player2.Data.displayName.ToUpperInvariant()}\n<size=68%><color=#FFD147>WINS THE ROUND!</color></size>";
                announceColor = new Color(1f, 0.38f, 0.42f, 1f);
            }

            UpdateRoundIcons(animateNewest: true);

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.gameObject.SetActive(true);
                ShowAnimatedAnnouncement(announcement, announceColor, 3f, 1.55f);
            }

            yield return new WaitForSeconds(3f);

            if (centerAnnouncementText != null)
                centerAnnouncementText.gameObject.SetActive(false);

            CheckMatchWinner();
        }

        private void UpdateRoundIcons(bool animateNewest = false)
        {
            for (int i = 0; i < p1RoundIcons.Length; i++)
            {
                if (p1RoundIcons[i] == null) continue;
                bool won = i < p1RoundsWon;
                p1RoundIcons[i].color = won ? roundWonColor : roundEmptyColor;

                if (animateNewest && won && i == p1RoundsWon - 1 && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PulseRectTransform(p1RoundIcons[i].rectTransform, 1.45f, 0.32f));
                }
            }

            for (int i = 0; i < p2RoundIcons.Length; i++)
            {
                if (p2RoundIcons[i] == null) continue;
                bool won = i < p2RoundsWon;
                p2RoundIcons[i].color = won ? roundWonColor : roundEmptyColor;

                if (animateNewest && won && i == p2RoundsWon - 1 && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PulseRectTransform(p2RoundIcons[i].rectTransform, 1.45f, 0.32f));
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
                bool isP1 = (matchWinner == player1);
                string playerSide = isP1 ? "PLAYER 1" : "PLAYER 2";
                string sideColorHex = isP1 ? "#45B6FE" : "#FF4757";
                string charName = matchWinner.Data != null ? matchWinner.Data.displayName.ToUpperInvariant() : "CHAMPION";

                matchWinnerNameText.richText = true;
                matchWinnerNameText.color = Color.white;
                matchWinnerNameText.text =
                    $"<size=62%><color={sideColorHex}>{playerSide}</color></size>\n" +
                    $"<size=115%><color=#FFD86B>{charName}</color></size>\n" +
                    $"<size=72%><color=#FFFFFF>WINS THE MATCH!</color></size>";
            }

            GameManager.instance.SetGameState(EGameState.PVPM_MATCHOVER);
        }

        private void ShowAnimatedAnnouncement(string text, Color baseColor, float totalDuration, float startScale)
        {
            if (centerAnnouncementText == null) return;

            centerAnnouncementText.richText = true;
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
            float popDuration = Mathf.Min(0.22f, duration * 0.35f);
            float fadeStart = Mathf.Max(popDuration, duration - 0.22f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (elapsed <= popDuration)
                {
                    float t = Mathf.Clamp01(elapsed / popDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    float scale = Mathf.Lerp(startScale, 1f, eased);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    label.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(t * 2f));
                }
                else if (elapsed >= fadeStart)
                {
                    float t = Mathf.InverseLerp(fadeStart, duration, elapsed);
                    float scale = Mathf.Lerp(1f, 1.08f, t);
                    rt.localScale = new Vector3(scale, scale, 1f);
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
                timerText.color = new Color(1f, 0.88f, 0.42f, 1f);
                timerText.fontStyle = FontStyles.Bold;
                timerText.alignment = TextAlignmentOptions.Center;

                // Timer arkasına şık koyu rozet plaka oluştur
                Transform timerParent = timerText.transform.parent;
                if (timerParent != null && timerText.transform.Find("TimerBackdrop") == null)
                {
                    GameObject badge = new GameObject("TimerBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
                    badge.transform.SetParent(timerParent, false);
                    badge.transform.SetSiblingIndex(timerText.transform.GetSiblingIndex());

                    RectTransform tr = timerText.rectTransform;
                    RectTransform br = badge.GetComponent<RectTransform>();
                    br.anchorMin = tr.anchorMin;
                    br.anchorMax = tr.anchorMax;
                    br.pivot = tr.pivot;
                    br.anchoredPosition = tr.anchoredPosition;
                    br.sizeDelta = new Vector2(Mathf.Max(tr.sizeDelta.x, 96f), Mathf.Max(tr.sizeDelta.y + 16f, 68f));

                    Image badgeImg = badge.GetComponent<Image>();
                    badgeImg.color = new Color(0.06f, 0.07f, 0.11f, 0.92f);
                    badgeImg.raycastTarget = false;

                    Outline badgeOutline = badge.GetComponent<Outline>();
                    badgeOutline.effectColor = new Color(0.92f, 0.74f, 0.25f, 0.85f);
                    badgeOutline.effectDistance = new Vector2(2f, -2f);

                    Shadow badgeShadow = badge.GetComponent<Shadow>();
                    badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                    badgeShadow.effectDistance = new Vector2(3f, -3f);
                }

                AddTextShadowAndOutline(timerText.gameObject, new Color(0f, 0f, 0f, 0.95f), 2.5f);
            }

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.color = new Color(1f, 0.85f, 0.25f, 1f);
                centerAnnouncementText.fontStyle = FontStyles.Bold;
                centerAnnouncementText.alignment = TextAlignmentOptions.Center;
                centerAnnouncementText.enableWordWrapping = false;
                centerAnnouncementText.overflowMode = TextOverflowModes.Overflow;
                AddTextShadowAndOutline(centerAnnouncementText.gameObject, new Color(0f, 0f, 0f, 0.95f), 3.5f);
            }

            if (matchWinnerNameText != null)
            {
                matchWinnerNameText.color = Color.white;
                matchWinnerNameText.fontStyle = FontStyles.Bold;
                matchWinnerNameText.alignment = TextAlignmentOptions.Center;
                AddTextShadowAndOutline(matchWinnerNameText.gameObject, new Color(0f, 0f, 0f, 0.95f), 3f);
            }

            StyleRoundIcons(p1RoundIcons);
            StyleRoundIcons(p2RoundIcons);
        }

        private void StyleRoundIcons(Image[] icons)
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;
                if (!icons[i].TryGetComponent(out Outline outline))
                {
                    outline = icons[i].gameObject.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0.95f, 0.78f, 0.28f, 0.9f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                if (!icons[i].TryGetComponent(out Shadow shadow))
                {
                    shadow = icons[i].gameObject.AddComponent<Shadow>();
                }
                shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                shadow.effectDistance = new Vector2(2.5f, -2.5f);
            }
        }

        private void AddTextShadowAndOutline(GameObject target, Color color, float dist)
        {
            if (target == null) return;
            if (!target.TryGetComponent(out Outline outline))
            {
                outline = target.AddComponent<Outline>();
            }
            outline.effectColor = color;
            outline.effectDistance = new Vector2(dist * 0.65f, -dist * 0.65f);

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
