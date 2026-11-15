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
        [SerializeField] private Image p1HealthBarFill;

        [Header("Player 2 HUD Elements (Sağ Taraf)")]
        [SerializeField] private Image p2PortraitImage;
        [SerializeField] private TextMeshProUGUI p2NameText;
        [SerializeField] private Image p2HealthBarFill;

        [Header("Health Bar Animation & Visuals")]
        [SerializeField] private float mainBarLerpSpeed = 14f;
        [SerializeField] private float trailDelay = 0.38f;
        [SerializeField] private float trailLerpSpeed = 2.8f;
        [SerializeField] private Color fullHealthColor = new Color(0.18f, 0.92f, 0.42f, 1f);
        [SerializeField] private Color midHealthColor = new Color(1f, 0.78f, 0.15f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.18f, 0.22f, 1f);
        [SerializeField] private Color damageTrailColor = new Color(1f, 0.28f, 0.18f, 0.92f);
        [SerializeField] private Color barBackgroundColor = new Color(0.08f, 0.09f, 0.14f, 0.95f);

        private FighterController boundPlayer1;
        private FighterController boundPlayer2;

        private Image p1TrailBarFill;
        private Image p2TrailBarFill;
        private RectTransform p1BgRect;
        private RectTransform p2BgRect;

        private float p1TargetRatio = 1f;
        private float p1CurrentFill = 1f;
        private float p1TrailFill = 1f;
        private float p1TrailTimer = 0f;
        private float p1FlashTimer = 0f;

        private float p2TargetRatio = 1f;
        private float p2CurrentFill = 1f;
        private float p2TrailFill = 1f;
        private float p2TrailTimer = 0f;
        private float p2FlashTimer = 0f;

        private Vector2 p1BarBasePos;
        private Vector2 p2BarBasePos;
        private Vector2 p1PortraitBasePos;
        private Vector2 p2PortraitBasePos;
        private bool hudStyled = false;

        private void Awake()
        {
            EnsureHUDStyling();
        }

        public void BindFightersToHUD(FighterController p1Fighter, FighterController p2Fighter)
        {
            EnsureHUDStyling();
            UnbindFighters();

            boundPlayer1 = p1Fighter;
            boundPlayer2 = p2Fighter;

            // Player 1 UI Atamaları
            if (boundPlayer1 != null && boundPlayer1.Data != null)
            {
                if (p1PortraitImage != null)
                {
                    p1PortraitImage.sprite = boundPlayer1.Data.portrait;
                    p1PortraitImage.preserveAspect = true;
                }
                if (p1NameText != null)
                {
                    p1NameText.text = boundPlayer1.Data.displayName.ToUpperInvariant();
                }

                ResetP1BarImmediate(boundPlayer1.CurrentHealth, boundPlayer1.MaxHealth);
                boundPlayer1.OnHealthChanged += UpdateP1HealthUI;
            }

            // Player 2 UI Atamaları
            if (boundPlayer2 != null && boundPlayer2.Data != null)
            {
                if (p2PortraitImage != null)
                {
                    p2PortraitImage.sprite = boundPlayer2.Data.portrait;
                    p2PortraitImage.preserveAspect = true;
                }
                if (p2NameText != null)
                {
                    p2NameText.text = boundPlayer2.Data.displayName.ToUpperInvariant();
                }

                ResetP2BarImmediate(boundPlayer2.CurrentHealth, boundPlayer2.MaxHealth);
                boundPlayer2.OnHealthChanged += UpdateP2HealthUI;
            }
        }

        private void ResetP1BarImmediate(int currentHealth, int maxHealth)
        {
            float ratio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 1f;
            p1TargetRatio = ratio;
            p1CurrentFill = ratio;
            p1TrailFill = ratio;
            p1TrailTimer = 0f;
            p1FlashTimer = 0f;
            ApplyBarVisuals(p1HealthBarFill, p1TrailBarFill, p1CurrentFill, p1TrailFill, 0f);
        }

        private void ResetP2BarImmediate(int currentHealth, int maxHealth)
        {
            float ratio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 1f;
            p2TargetRatio = ratio;
            p2CurrentFill = ratio;
            p2TrailFill = ratio;
            p2TrailTimer = 0f;
            p2FlashTimer = 0f;
            ApplyBarVisuals(p2HealthBarFill, p2TrailBarFill, p2CurrentFill, p2TrailFill, 0f);
        }

        private void UpdateP1HealthUI(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0) return;
            float newRatio = Mathf.Clamp01((float)currentHealth / maxHealth);

            if (newRatio > p1TargetRatio + 0.01f)
            {
                ResetP1BarImmediate(currentHealth, maxHealth);
                return;
            }

            if (newRatio < p1TargetRatio)
            {
                p1TrailTimer = trailDelay;
                p1FlashTimer = 0.14f;
            }

            p1TargetRatio = newRatio;
        }

        private void UpdateP2HealthUI(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0) return;
            float newRatio = Mathf.Clamp01((float)currentHealth / maxHealth);

            if (newRatio > p2TargetRatio + 0.01f)
            {
                ResetP2BarImmediate(currentHealth, maxHealth);
                return;
            }

            if (newRatio < p2TargetRatio)
            {
                p2TrailTimer = trailDelay;
                p2FlashTimer = 0.14f;
            }

            p2TargetRatio = newRatio;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // P1 Health & Trail güncellemesi
            p1CurrentFill = Mathf.MoveTowards(p1CurrentFill, p1TargetRatio, mainBarLerpSpeed * dt * 0.25f);
            p1CurrentFill = Mathf.Lerp(p1CurrentFill, p1TargetRatio, mainBarLerpSpeed * dt);

            if (p1TrailTimer > 0f)
            {
                p1TrailTimer -= dt;
            }
            else if (p1TrailFill > p1CurrentFill)
            {
                p1TrailFill = Mathf.MoveTowards(p1TrailFill, p1CurrentFill, trailLerpSpeed * dt * 0.35f);
                p1TrailFill = Mathf.Lerp(p1TrailFill, p1CurrentFill, trailLerpSpeed * dt);
            }
            else
            {
                p1TrailFill = p1CurrentFill;
            }

            if (p1FlashTimer > 0f) p1FlashTimer = Mathf.Max(0f, p1FlashTimer - dt);
            ApplyBarVisuals(p1HealthBarFill, p1TrailBarFill, p1CurrentFill, p1TrailFill, p1FlashTimer);
            ApplyBarAndPortraitShake(p1HealthBarFill, p1TrailBarFill, p1BgRect, p1BarBasePos, p1PortraitImage, p1PortraitBasePos, p1FlashTimer);

            // P2 Health & Trail güncellemesi
            p2CurrentFill = Mathf.MoveTowards(p2CurrentFill, p2TargetRatio, mainBarLerpSpeed * dt * 0.25f);
            p2CurrentFill = Mathf.Lerp(p2CurrentFill, p2TargetRatio, mainBarLerpSpeed * dt);

            if (p2TrailTimer > 0f)
            {
                p2TrailTimer -= dt;
            }
            else if (p2TrailFill > p2CurrentFill)
            {
                p2TrailFill = Mathf.MoveTowards(p2TrailFill, p2CurrentFill, trailLerpSpeed * dt * 0.35f);
                p2TrailFill = Mathf.Lerp(p2TrailFill, p2CurrentFill, trailLerpSpeed * dt);
            }
            else
            {
                p2TrailFill = p2CurrentFill;
            }

            if (p2FlashTimer > 0f) p2FlashTimer = Mathf.Max(0f, p2FlashTimer - dt);
            ApplyBarVisuals(p2HealthBarFill, p2TrailBarFill, p2CurrentFill, p2TrailFill, p2FlashTimer);
            ApplyBarAndPortraitShake(p2HealthBarFill, p2TrailBarFill, p2BgRect, p2BarBasePos, p2PortraitImage, p2PortraitBasePos, p2FlashTimer);
        }

        private void ApplyBarVisuals(Image mainBar, Image trailBar, float mainFill, float trailFill, float flashTime)
        {
            if (mainBar != null)
            {
                mainBar.fillAmount = mainFill;
                Color targetColor = EvaluateHealthColor(mainFill);

                // Kritik canda hafif nabız efekti
                if (mainFill > 0f && mainFill <= 0.25f)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                    targetColor = Color.Lerp(lowHealthColor, new Color(1f, 0.55f, 0.35f, 1f), pulse * 0.45f);
                }

                // Hasar anında kısa beyaz parlama
                if (flashTime > 0f)
                {
                    float flashFactor = Mathf.Clamp01(flashTime / 0.14f);
                    targetColor = Color.Lerp(targetColor, Color.white, flashFactor * 0.65f);
                }

                mainBar.color = targetColor;
            }

            if (trailBar != null)
            {
                trailBar.fillAmount = trailFill;
                trailBar.color = damageTrailColor;
            }
        }

        private Color EvaluateHealthColor(float ratio)
        {
            if (ratio >= 0.55f)
            {
                float t = Mathf.InverseLerp(0.55f, 1f, ratio);
                return Color.Lerp(midHealthColor, fullHealthColor, t);
            }
            else
            {
                float t = Mathf.InverseLerp(0.2f, 0.55f, ratio);
                return Color.Lerp(lowHealthColor, midHealthColor, t);
            }
        }

        private void ApplyBarAndPortraitShake(
            Image mainFill,
            Image trailFill,
            RectTransform bgRect,
            Vector2 barBasePos,
            Image portrait,
            Vector2 portraitBasePos,
            float flashTime)
        {
            Vector2 offset = Vector2.zero;
            if (flashTime > 0f)
            {
                float intensity = (flashTime / 0.14f) * 5f;
                offset = new Vector2(
                    Mathf.Sin(Time.unscaledTime * 95f) * intensity,
                    Mathf.Cos(Time.unscaledTime * 120f) * intensity * 0.6f);
            }

            if (mainFill != null) mainFill.rectTransform.anchoredPosition = barBasePos + offset;
            if (trailFill != null) trailFill.rectTransform.anchoredPosition = barBasePos + offset;
            if (bgRect != null) bgRect.anchoredPosition = barBasePos + offset;
            if (portrait != null) portrait.rectTransform.anchoredPosition = portraitBasePos + offset * 1.15f;
        }

        private void EnsureHUDStyling()
        {
            if (hudStyled) return;
            hudStyled = true;

            p1TrailBarFill = SetupHealthBarHierarchy(p1HealthBarFill, out p1BgRect, out p1BarBasePos);
            p2TrailBarFill = SetupHealthBarHierarchy(p2HealthBarFill, out p2BgRect, out p2BarBasePos);

            if (p1PortraitImage != null) p1PortraitBasePos = p1PortraitImage.rectTransform.anchoredPosition;
            if (p2PortraitImage != null) p2PortraitBasePos = p2PortraitImage.rectTransform.anchoredPosition;

            StylePortrait(p1PortraitImage, new Color(0.22f, 0.72f, 1f, 0.92f));
            StylePortrait(p2PortraitImage, new Color(1f, 0.28f, 0.34f, 0.92f));

            StyleNameText(p1NameText, TextAlignmentOptions.Left, new Color(0.92f, 0.97f, 1f, 1f));
            StyleNameText(p2NameText, TextAlignmentOptions.Right, new Color(1f, 0.92f, 0.92f, 1f));
        }

        private Image SetupHealthBarHierarchy(Image mainFill, out RectTransform bgRect, out Vector2 basePos)
        {
            bgRect = null;
            basePos = Vector2.zero;
            if (mainFill == null) return null;

            RectTransform mainRect = mainFill.rectTransform;
            basePos = mainRect.anchoredPosition;

            Transform parent = mainFill.transform.parent;
            if (parent != null)
            {
                // P1HealthBarBG veya P2HealthBarBG kardeş objesini bul
                Transform bgTransform = parent.Find(mainFill.name + "BG");
                if (bgTransform != null)
                {
                    bgRect = bgTransform as RectTransform;
                    if (bgTransform.TryGetComponent(out Image bgImage))
                    {
                        bgImage.color = barBackgroundColor;
                    }

                    if (!bgTransform.TryGetComponent(out Outline outline))
                    {
                        outline = bgTransform.gameObject.AddComponent<Outline>();
                    }
                    outline.effectColor = new Color(0.92f, 0.75f, 0.26f, 0.9f);
                    outline.effectDistance = new Vector2(2.5f, -2.5f);

                    if (!bgTransform.TryGetComponent(out Shadow shadow))
                    {
                        shadow = bgTransform.gameObject.AddComponent<Shadow>();
                    }
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                    shadow.effectDistance = new Vector2(4f, -4f);
                }
            }

            // Gecikmeli hasar (chip/trail) barını ana barın hemen arkasına oluştur
            GameObject trailObj = new GameObject(mainFill.name + "_Trail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trailObj.transform.SetParent(mainFill.transform.parent, false);
            trailObj.transform.SetSiblingIndex(mainFill.transform.GetSiblingIndex());

            RectTransform trailRect = trailObj.GetComponent<RectTransform>();
            trailRect.anchorMin = mainRect.anchorMin;
            trailRect.anchorMax = mainRect.anchorMax;
            trailRect.pivot = mainRect.pivot;
            trailRect.anchoredPosition = mainRect.anchoredPosition;
            trailRect.sizeDelta = mainRect.sizeDelta;
            trailRect.localScale = Vector3.one;

            Image trailImage = trailObj.GetComponent<Image>();
            trailImage.sprite = mainFill.sprite;
            trailImage.type = Image.Type.Filled;
            trailImage.fillMethod = mainFill.fillMethod;
            trailImage.fillOrigin = mainFill.fillOrigin;
            trailImage.fillAmount = mainFill.fillAmount;
            trailImage.color = damageTrailColor;
            trailImage.raycastTarget = false;

            return trailImage;
        }

        private void StylePortrait(Image portrait, Color accentColor)
        {
            if (portrait == null) return;

            if (!portrait.TryGetComponent(out Outline outline))
            {
                outline = portrait.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(3f, -3f);

            if (!portrait.TryGetComponent(out Shadow shadow))
            {
                shadow = portrait.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(4.5f, -4.5f);
        }

        private void StyleNameText(TextMeshProUGUI textComp, TextAlignmentOptions alignment, Color tint)
        {
            if (textComp == null) return;
            textComp.color = tint;
            textComp.fontStyle = FontStyles.Bold;
            textComp.alignment = alignment;
            textComp.enableWordWrapping = false;
            textComp.overflowMode = TextOverflowModes.Overflow;

            if (!textComp.TryGetComponent(out Outline outline))
            {
                outline = textComp.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            if (!textComp.TryGetComponent(out Shadow shadow))
            {
                shadow = textComp.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        public void UnbindFighters()
        {
            if (boundPlayer1 != null) boundPlayer1.OnHealthChanged -= UpdateP1HealthUI;
            if (boundPlayer2 != null) boundPlayer2.OnHealthChanged -= UpdateP2HealthUI;
            boundPlayer1 = null;
            boundPlayer2 = null;
        }

        private void OnDestroy()
        {
            UnbindFighters();
        }
    }
}
