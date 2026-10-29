using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameProject.Runtime.Manager
{
    public class PvPHUDManager : MonoBehaviour
    {
        [Header("Player 1 HUD Elements (Sol Taraf)")]
        [SerializeField] private Image p1PortraitImage;
        [SerializeField] private TextMeshProUGUI p1NameText;
        [SerializeField] private Image p1HealthBarFill; // Image Type: Filled -> Horizontal

        [Header("Player 2 HUD Elements (Sağ Taraf)")]
        [SerializeField] private Image p2PortraitImage;
        [SerializeField] private TextMeshProUGUI p2NameText;
        [SerializeField] private Image p2HealthBarFill; // Image Type: Filled -> Horizontal

        private FighterController boundPlayer1;
        private FighterController boundPlayer2;

        public void BindFightersToHUD(FighterController p1Fighter, FighterController p2Fighter)
        {
            UnbindFighters();

            boundPlayer1 = p1Fighter;
            boundPlayer2 = p2Fighter;

            // 1. Oyuncu UI Bilgilerini CharacterData'dan Çek
            if (boundPlayer1 != null && boundPlayer1.Data != null)
            {
                if (p1PortraitImage != null)
                    p1PortraitImage.sprite = boundPlayer1.Data.portrait;

                if (p1NameText != null)
                    p1NameText.text = boundPlayer1.Data.displayName;

                UpdateP1HealthUI(boundPlayer1.CurrentHealth, boundPlayer1.MaxHealth);
                boundPlayer1.OnHealthChanged += UpdateP1HealthUI;
            }

            // 2. Oyuncu UI Bilgilerini CharacterData'dan Çek
            if (boundPlayer2 != null && boundPlayer2.Data != null)
            {
                if (p2PortraitImage != null)
                    p2PortraitImage.sprite = boundPlayer2.Data.portrait;

                if (p2NameText != null)
                    p2NameText.text = boundPlayer2.Data.displayName;

                UpdateP2HealthUI(boundPlayer2.CurrentHealth, boundPlayer2.MaxHealth);
                boundPlayer2.OnHealthChanged += UpdateP2HealthUI;
            }
        }

        private void UpdateP1HealthUI(int currentHealth, int maxHealth)
        {
            if (p1HealthBarFill != null && maxHealth > 0)
            {
                p1HealthBarFill.fillAmount = (float)currentHealth / maxHealth;
            }
        }

        private void UpdateP2HealthUI(int currentHealth, int maxHealth)
        {
            if (p2HealthBarFill != null && maxHealth > 0)
            {
                p2HealthBarFill.fillAmount = (float)currentHealth / maxHealth;
            }
        }

        private void UnbindFighters()
        {
            if (boundPlayer1 != null)
                boundPlayer1.OnHealthChanged -= UpdateP1HealthUI;

            if (boundPlayer2 != null)
                boundPlayer2.OnHealthChanged -= UpdateP2HealthUI;
        }

        private void OnDestroy()
        {
            UnbindFighters();
        }
    }
}