using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
        [SerializeField] private Image[] p1RoundIcons; // Player 1'in yuvarlakları (İçi boş/dolu ayarlanacak)
        [SerializeField] private Image[] p2RoundIcons; // Player 2'nin yuvarlakları
        [SerializeField] private Color roundWonColor = Color.yellow; // Dolu yuvarlak rengi
        [SerializeField] private Color roundEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Boş yuvarlak rengi

        [Header("UI - Center Announcements")]
        [SerializeField] private TextMeshProUGUI centerAnnouncementText; // "P1 Wins", "FIGHT!" vs. yazacak text

        [Header("UI - Match Over Screen")]
        [SerializeField] private GameObject matchOverPanel; // Oyun bitince açılacak ekran (Rövanş, Ana Menü butonları burada olacak)
        [SerializeField] private TextMeshProUGUI matchWinnerNameText; // Match Over ekranında kimin kazandığını yazan text

        private FighterController player1;
        private FighterController player2;
        private MatchSpawner matchSpawner;

        private int p1RoundsWon = 0;
        private int p2RoundsWon = 0;
        private float currentTime;
        private bool isRoundActive = false;
        private Coroutine roundTimerCoroutine;

        public void Initialize(FighterController p1, FighterController p2, MatchSpawner spawner)
        {
            player1 = p1;
            player2 = p2;
            matchSpawner = spawner;

            p1RoundsWon = 0;
            p2RoundsWon = 0;
            UpdateRoundIcons();

            if (matchOverPanel != null) matchOverPanel.SetActive(false);

            player1.OnHealthChanged += CheckHealthKO;
            player2.OnHealthChanged += CheckHealthKO;

            StartRoundSequence();
        }

        private void StartRoundSequence()
        {
            isRoundActive = false;
            currentTime = maxRoundTime;
            UpdateTimerUI();

            // Karakterlerin canını ve pozisyonunu sıfırla
            player1.ResetHealth();
            player2.ResetHealth();
            matchSpawner.ResetFighterPositions(); // Bu metodu MatchSpawner'a ekleyeceğiz

            StartCoroutine(RoundStartAnimation());
        }

        private IEnumerator RoundStartAnimation()
        {
            ToggleControls(false);

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.gameObject.SetActive(true);
                centerAnnouncementText.text = "3";
                yield return new WaitForSeconds(1f);
                centerAnnouncementText.text = "2";
                yield return new WaitForSeconds(1f);
                centerAnnouncementText.text = "1";
                yield return new WaitForSeconds(1f);
                centerAnnouncementText.text = "FIGHT!";
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
                UpdateTimerUI();

                if (currentTime <= 0)
                {
                    TimeUp();
                }
            }
        }

        private void UpdateTimerUI()
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(currentTime).ToString();
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
            string announcement = "DRAW!"; // Beraberlik durumu

            if (winner == player1)
            {
                p1RoundsWon++;
                announcement = $"{player1.Data.displayName} WINS!";
            }
            else if (winner == player2)
            {
                p2RoundsWon++;
                announcement = $"{player2.Data.displayName} WINS!";
            }

            UpdateRoundIcons();

            if (centerAnnouncementText != null)
            {
                centerAnnouncementText.gameObject.SetActive(true);
                centerAnnouncementText.text = announcement;
            }

            yield return new WaitForSeconds(3f); // Yazı 3 saniye ekranda kalsın

            if (centerAnnouncementText != null)
                centerAnnouncementText.gameObject.SetActive(false);

            CheckMatchWinner();
        }

        private void UpdateRoundIcons()
        {
            for (int i = 0; i < p1RoundIcons.Length; i++)
            {
                if (p1RoundIcons[i] != null)
                    p1RoundIcons[i].color = (i < p1RoundsWon) ? roundWonColor : roundEmptyColor;
            }

            for (int i = 0; i < p2RoundIcons.Length; i++)
            {
                if (p2RoundIcons[i] != null)
                    p2RoundIcons[i].color = (i < p2RoundsWon) ? roundWonColor : roundEmptyColor;
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
                // Kimse maçı henüz kazanmadıysa yeni rounda geç
                StartRoundSequence();
            }
        }

        private void EndMatch(FighterController matchWinner)
        {
            if (matchOverPanel != null)
            {
                matchOverPanel.SetActive(true);
                if (matchWinnerNameText != null)
                {
                    string playerSide = (matchWinner == player1) ? "PLAYER 1" : "PLAYER 2";
                    matchWinnerNameText.text = $"{playerSide}\n{matchWinner.Data.displayName}\nWINS THE MATCH!";
                }
            }
        }

       private void ToggleControls(bool canControl)
        {
            if (player1 != null)
            {
                if (player1.TryGetComponent(out FighterMotor m1)) 
                {
                    m1.enabled = canControl;
                    if (!canControl) m1.StopMovement(); // Hareketi ve yürüme animasyonunu anında durdur
                }
                if (player1.TryGetComponent(out FighterCombat c1)) c1.enabled = canControl;
                if (player1.TryGetComponent(out FighterDefense d1)) d1.enabled = canControl;
            }

            if (player2 != null)
            {
                if (player2.TryGetComponent(out FighterMotor m2)) 
                {
                    m2.enabled = canControl;
                    if (!canControl) m2.StopMovement(); // Hareketi ve yürüme animasyonunu anında durdur
                }
                if (player2.TryGetComponent(out FighterCombat c2)) c2.enabled = canControl;
                if (player2.TryGetComponent(out FighterDefense d2)) d2.enabled = canControl;
            }
        }

        private void OnDestroy()
        {
            if (player1 != null) player1.OnHealthChanged -= CheckHealthKO;
            if (player2 != null) player2.OnHealthChanged -= CheckHealthKO;
        }
        
        // --- MATCH OVER EKRANI BUTONLARI İÇİN METOTLAR ---
        
        public void Btn_Rematch()
        {
            // Mevcut harita ve karakterlerle maçı baştan başlat
            matchSpawner.RestartCurrentMatch(); 
        }

        public void Btn_CharacterSelection()
        {
            matchSpawner.ClearMatch();
            GameManager.instance.SetGameState(EGameState.PVPM_MENU_CHARACTERSELECTION);
        }

        public void Btn_MainMenu()
        {
            matchSpawner.ClearMatch();
            GameManager.instance.SetGameState(EGameState.PLAYMODESELECTION); // Veya ana menü state'in hangisiyse
        }
    }
}