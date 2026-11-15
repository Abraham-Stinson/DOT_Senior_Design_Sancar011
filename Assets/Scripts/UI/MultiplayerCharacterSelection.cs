using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace GameProject.Runtime.Manager
{
    public class MultiplayerCharacterSelection : MonoBehaviour
    {
        [Header("Character Database")]
        [SerializeField] private CharacterData[] CharacterLists;

        [Header("UI Generation (Panels & Slot Prefab)")]
        [Tooltip("Ortadaki ortak karakter havuzu olarak kullanılır")]
        [SerializeField] private Transform p1PanelParent;
        [Tooltip("Artık ortak havuz kullanıldığı için otomatik gizlenir")]
        [SerializeField] private Transform p2PanelParent;
        [SerializeField] private Image characterSlotPrefab;

        [Header("Selection Visuals (Border Colors)")]
        [SerializeField] private Color unselectedColor = new Color(0.68f, 0.70f, 0.76f, 1f);
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color lockedColor = Color.white;
        [SerializeField] private Color p1BorderColor = new Color(0.15f, 0.68f, 1f, 1f);
        [SerializeField] private Color p2BorderColor = new Color(1f, 0.22f, 0.30f, 1f);
        [SerializeField] private Color lockedBorderTint = new Color(1f, 0.88f, 0.32f, 1f);

        [Header("Preview Layout")]
        [Tooltip("True ise P1 önizlemesi sağda, P2 önizlemesi solda gösterilir. False ise MK standardı (P1 Solda, P2 Sağda) kullanılır.")]
        [SerializeField] private bool p1PreviewOnRightSide = false;

        [Header("Match Spawner Reference")]
        [SerializeField] private MatchSpawner matchSpawner;

        [Header("Character Info & Ready Texts")]
        [SerializeField] private TextMeshProUGUI p1NameText;
        [SerializeField] private TextMeshProUGUI p1ReadyText;
        [SerializeField] private TextMeshProUGUI p2NameText;
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        private class SlotBorderVisual
        {
            public Image portraitImage;
            public RectTransform rootRect;
            public Image topBorder;
            public Image bottomBorder;
            public Image leftBorder;
            public Image rightBorder;
            public GameObject p1BadgeObj;
            public TextMeshProUGUI p1BadgeText;
            public Image p1BadgeBg;
            public GameObject p2BadgeObj;
            public TextMeshProUGUI p2BadgeText;
            public Image p2BadgeBg;
        }

        private class CharacterPreviewRig
        {
            public RectTransform rootRect;
            public Image fullBodyImage;
            public Image portraitBadgeImage;
            public Image portraitFrameImage;
            public Image pedestalGlowImage;
            public SpriteRenderer hiddenSpriteRenderer;
            public Animator hiddenAnimator;
            public Vector2 baseAnchoredPos;
            public float facingSign; // +1 sağa bakar, -1 sola bakar
            public float animTimer;
        }

        private readonly List<SlotBorderVisual> poolSlots = new List<SlotBorderVisual>();
        private CharacterPreviewRig p1PreviewRig;
        private CharacterPreviewRig p2PreviewRig;

        private int p1Index = 0;
        private int p2Index = 0;
        private bool p1Locked = false;
        private bool p2Locked = false;
        private bool isUIGenerated = false;

        private void Awake()
        {
            MP_CreatePlayerUIOnSelectionScreen();
        }

        private void OnEnable()
        {
            ResetSelectionState();
        }

        public void MP_CreatePlayerUIOnSelectionScreen()
        {
            if (isUIGenerated || CharacterLists == null || CharacterLists.Length == 0)
                return;

            SetupCenterPoolAndSidePreviews();

            ClearPlaceholderChildren(p1PanelParent);
            ClearPlaceholderChildren(p2PanelParent);

            // İkinci kutuyu tamamen kapatıyoruz; tüm karakterler ortadaki tek havuzda!
            if (p2PanelParent != null)
            {
                p2PanelParent.gameObject.SetActive(false);
            }

            poolSlots.Clear();

            for (int i = 0; i < CharacterLists.Length; i++)
            {
                CharacterData data = CharacterLists[i];
                if (data == null) continue;

                if (p1PanelParent != null && characterSlotPrefab != null)
                {
                    Image slotImg = Instantiate(characterSlotPrefab, p1PanelParent);
                    slotImg.name = $"CharSlot_{i}_{data.displayName}";
                    slotImg.sprite = data.portrait;
                    slotImg.preserveAspect = false;
                    slotImg.color = unselectedColor;

                    SlotBorderVisual visual = BuildSlotBorderAndBadges(slotImg);
                    poolSlots.Add(visual);
                }
            }

            isUIGenerated = true;
            p1Index = 0;
            p2Index = CharacterLists.Length > 1 ? 1 : 0;
            UpdateSelectionUI(triggerP1Anim: true, triggerP2Anim: true);
        }

        private void SetupCenterPoolAndSidePreviews()
        {
            if (p1PanelParent == null) return;

            Transform screenRoot = p1PanelParent.parent;
            if (screenRoot != null && screenRoot.TryGetComponent(out Image screenBg))
            {
                screenBg.color = new Color(0.05f, 0.06f, 0.10f, 0.96f);
            }

            // Ortadaki Karakter Havuzu Ayarları
            RectTransform poolRect = p1PanelParent as RectTransform;
            if (poolRect != null)
            {
                poolRect.anchorMin = new Vector2(0.5f, 0.5f);
                poolRect.anchorMax = new Vector2(0.5f, 0.5f);
                poolRect.pivot = new Vector2(0.5f, 0.5f);
                poolRect.anchoredPosition = new Vector2(0f, 15f);
                poolRect.sizeDelta = new Vector2(560f, 540f);
            }

            if (p1PanelParent.TryGetComponent(out Mask mask))
            {
                // Kenar çerçevelerinin ve büyüme efektinin kesilmemesi için mask'ı devre dışı bırak
                mask.enabled = false;
            }

            if (p1PanelParent.TryGetComponent(out Image poolBg))
            {
                poolBg.color = new Color(0.08f, 0.10f, 0.16f, 0.92f);
            }

            if (!p1PanelParent.TryGetComponent(out Outline poolOutline))
            {
                poolOutline = p1PanelParent.gameObject.AddComponent<Outline>();
            }
            poolOutline.effectColor = new Color(0.88f, 0.72f, 0.28f, 0.65f);
            poolOutline.effectDistance = new Vector2(2.5f, -2.5f);

            if (p1PanelParent.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.cellSize = new Vector2(165f, 165f);
                grid.spacing = new Vector2(24f, 24f);
                grid.padding = new RectOffset(20, 20, 20, 20);
                grid.constraint = GridLayoutGroup.Constraint.Flexible;
            }

            // Başlık yazısını düzenle
            if (screenRoot != null)
            {
                TextMeshProUGUI[] allTexts = screenRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < allTexts.Length; i++)
                {
                    TextMeshProUGUI t = allTexts[i];
                    if (t == p1NameText || t == p2NameText || t == p1ReadyText || t == p2ReadyText)
                        continue;

                    if (t.name.Contains("CharacterSelection") || t.text.Contains("Chamipon") || t.text.Contains("FIGHTER"))
                    {
                        t.text = "SELECT YOUR FIGHTER";
                        t.color = new Color(1f, 0.85f, 0.28f, 1f);
                        t.fontStyle = FontStyles.Bold;
                        t.rectTransform.anchoredPosition = new Vector2(0f, 445f);
                        StyleLabel(t, new Color(1f, 0.85f, 0.28f, 1f));
                    }
                }

                // P1 ve P2 Önizleme Konumları (Birbirine bakan sağ ve sol karakter önizlemeleri)
                float p1X = p1PreviewOnRightSide ? 610f : -610f;
                float p2X = p1PreviewOnRightSide ? -610f : 610f;
                float p1Facing = p1PreviewOnRightSide ? -1f : 1f;
                float p2Facing = p1PreviewOnRightSide ? 1f : -1f;

                p1PreviewRig = CreateCharacterPreviewRig(screenRoot, "P1_CharacterPreview", new Vector2(p1X, 45f), p1Facing, p1BorderColor);
                p2PreviewRig = CreateCharacterPreviewRig(screenRoot, "P2_CharacterPreview", new Vector2(p2X, 45f), p2Facing, p2BorderColor);

                // İsim ve Ready yazılarını kendi önizlemelerinin altına hizala
                if (p1NameText != null)
                {
                    p1NameText.rectTransform.anchoredPosition = new Vector2(p1X, -365f);
                    StyleLabel(p1NameText, p1BorderColor);
                }
                if (p1ReadyText != null)
                {
                    p1ReadyText.rectTransform.anchoredPosition = new Vector2(p1X, -455f);
                    StyleLabel(p1ReadyText, Color.white);
                }
                if (p2NameText != null)
                {
                    p2NameText.rectTransform.anchoredPosition = new Vector2(p2X, -365f);
                    StyleLabel(p2NameText, p2BorderColor);
                }
                if (p2ReadyText != null)
                {
                    p2ReadyText.rectTransform.anchoredPosition = new Vector2(p2X, -455f);
                    StyleLabel(p2ReadyText, Color.white);
                }
            }
        }

        private CharacterPreviewRig CreateCharacterPreviewRig(Transform parent, string name, Vector2 anchoredPos, float facingSign, Color accentColor)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Destroy(existing.gameObject);

            GameObject rootObj = new GameObject(name, typeof(RectTransform));
            rootObj.transform.SetParent(parent, false);
            // Havuzun ve yazıların arkasında/yanında temiz durması için
            rootObj.transform.SetSiblingIndex(1);

            RectTransform rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = anchoredPos;
            rootRect.sizeDelta = new Vector2(520f, 680f);

            // Alt parlama / platform halkası
            GameObject glowObj = new GameObject("PedestalGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObj.transform.SetParent(rootRect, false);
            RectTransform glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0f);
            glowRect.anchorMax = new Vector2(0.5f, 0f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = new Vector2(0f, 45f);
            glowRect.sizeDelta = new Vector2(360f, 26f);
            Image glowImg = glowObj.GetComponent<Image>();
            glowImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f);
            glowImg.raycastTarget = false;

            // Tam Boy Karakter Görseli (Mortal Kombat tarzı duruş)
            GameObject bodyObj = new GameObject("FullBodyFighter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
            bodyObj.transform.SetParent(rootRect, false);
            RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, 20f);
            bodyRect.sizeDelta = new Vector2(500f, 620f);
            bodyRect.localScale = new Vector3(facingSign, 1f, 1f);

            Image bodyImg = bodyObj.GetComponent<Image>();
            bodyImg.preserveAspect = true;
            bodyImg.raycastTarget = false;

            Shadow bodyShadow = bodyObj.GetComponent<Shadow>();
            bodyShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            bodyShadow.effectDistance = new Vector2(facingSign * -10f, -8f);

            // Köşe Portre Madalyonu (Seçilen karakterin portresini de net gösterir)
            GameObject badgeFrameObj = new GameObject("PortraitMedallionFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
            badgeFrameObj.transform.SetParent(rootRect, false);
            RectTransform badgeFrameRect = badgeFrameObj.GetComponent<RectTransform>();
            badgeFrameRect.anchorMin = new Vector2(0.5f, 0f);
            badgeFrameRect.anchorMax = new Vector2(0.5f, 0f);
            badgeFrameRect.pivot = new Vector2(0.5f, 0.5f);
            // Merkeze bakan alt köşede dursun
            badgeFrameRect.anchoredPosition = new Vector2(facingSign * 145f, 95f);
            badgeFrameRect.sizeDelta = new Vector2(116f, 116f);

            Image badgeFrameImg = badgeFrameObj.GetComponent<Image>();
            badgeFrameImg.color = accentColor;
            badgeFrameImg.raycastTarget = false;

            Shadow badgeShadow = badgeFrameObj.GetComponent<Shadow>();
            badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            badgeShadow.effectDistance = new Vector2(4f, -4f);

            GameObject badgePortraitObj = new GameObject("PortraitMedallion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgePortraitObj.transform.SetParent(badgeFrameRect, false);
            RectTransform badgePortraitRect = badgePortraitObj.GetComponent<RectTransform>();
            badgePortraitRect.anchorMin = Vector2.zero;
            badgePortraitRect.anchorMax = Vector2.one;
            badgePortraitRect.offsetMin = new Vector2(5f, 5f);
            badgePortraitRect.offsetMax = new Vector2(-5f, -5f);

            Image badgePortraitImg = badgePortraitObj.GetComponent<Image>();
            badgePortraitImg.preserveAspect = false;
            badgePortraitImg.raycastTarget = false;

            // Idle animasyonunu UI Image'a aktarmak için gizli SpriteRenderer + Animator
            GameObject animDriverObj = new GameObject("PreviewAnimDriver", typeof(SpriteRenderer), typeof(Animator));
            animDriverObj.transform.SetParent(rootRect, false);
            SpriteRenderer sr = animDriverObj.GetComponent<SpriteRenderer>();
            sr.enabled = false;
            Animator anim = animDriverObj.GetComponent<Animator>();
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            return new CharacterPreviewRig
            {
                rootRect = rootRect,
                fullBodyImage = bodyImg,
                portraitBadgeImage = badgePortraitImg,
                portraitFrameImage = badgeFrameImg,
                pedestalGlowImage = glowImg,
                hiddenSpriteRenderer = sr,
                hiddenAnimator = anim,
                baseAnchoredPos = anchoredPos,
                facingSign = facingSign,
                animTimer = 0f
            };
        }

        private SlotBorderVisual BuildSlotBorderAndBadges(Image slotImage)
        {
            RectTransform rt = slotImage.rectTransform;
            float borderThickness = 6f;

            Image top = CreateBorderEdge(rt, "Border_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -borderThickness), Vector2.zero);
            Image bottom = CreateBorderEdge(rt, "Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, borderThickness));
            Image left = CreateBorderEdge(rt, "Border_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(borderThickness, 0f));
            Image right = CreateBorderEdge(rt, "Border_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-borderThickness, 0f), Vector2.zero);

            GameObject p1Badge = CreateCornerBadge(rt, "P1_Badge", new Vector2(0f, 1f), new Vector2(26f, -18f), p1BorderColor, "P1", out TextMeshProUGUI p1Txt, out Image p1Bg);
            GameObject p2Badge = CreateCornerBadge(rt, "P2_Badge", new Vector2(1f, 1f), new Vector2(-26f, -18f), p2BorderColor, "P2", out TextMeshProUGUI p2Txt, out Image p2Bg);

            if (!slotImage.TryGetComponent(out Shadow shadow))
            {
                shadow = slotImage.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(4f, -4f);

            return new SlotBorderVisual
            {
                portraitImage = slotImage,
                rootRect = rt,
                topBorder = top,
                bottomBorder = bottom,
                leftBorder = left,
                rightBorder = right,
                p1BadgeObj = p1Badge,
                p1BadgeText = p1Txt,
                p1BadgeBg = p1Bg,
                p2BadgeObj = p2Badge,
                p2BadgeText = p2Txt,
                p2BadgeBg = p2Bg
            };
        }

        private Image CreateBorderEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image img = obj.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private GameObject CreateCornerBadge(
            RectTransform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Color bgColor,
            string text,
            out TextMeshProUGUI label,
            out Image bgImage)
        {
            GameObject badgeObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObj.transform.SetParent(parent, false);

            RectTransform rt = badgeObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(44f, 26f);

            bgImage = badgeObj.GetComponent<Image>();
            bgImage.color = bgColor;
            bgImage.raycastTarget = false;

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(rt, false);
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            label = txtObj.GetComponent<TextMeshProUGUI>();
            if (p1NameText != null) label.font = p1NameText.font;
            label.text = text;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            return badgeObj;
        }

        private void StyleLabel(TextMeshProUGUI label, Color defaultColor)
        {
            if (label == null) return;
            label.color = defaultColor;
            label.fontStyle = FontStyles.Bold;
            label.richText = true;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;

            if (!label.TryGetComponent(out Outline outline))
            {
                outline = label.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0f, 0f, 0f, 0.94f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (!label.TryGetComponent(out Shadow shadow))
            {
                shadow = label.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.88f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        private void ClearPlaceholderChildren(Transform panel)
        {
            if (panel == null) return;
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                Destroy(panel.GetChild(i).gameObject);
            }
        }

        public void ResetSelectionState()
        {
            p1Index = 0;
            p2Index = (CharacterLists != null && CharacterLists.Length > 1) ? 1 : 0;
            p1Locked = false;
            p2Locked = false;
            UpdateSelectionUI(triggerP1Anim: true, triggerP2Anim: true);
        }

        private void Update()
        {
            if (GameManager.instance == null ||
                GameManager.instance.gameState != EGameState.PVPM_MENU_CHARACTERSELECTION)
                return;

            AnimateSlotsAndPreviews();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || CharacterLists == null || CharacterLists.Length == 0)
                return;

            HandlePlayer1Navigation(keyboard);
            HandlePlayer2Navigation(keyboard);
        }

        private void LateUpdate()
        {
            if (GameManager.instance == null ||
                GameManager.instance.gameState != EGameState.PVPM_MENU_CHARACTERSELECTION)
                return;

            SyncAnimatedPreviewSprite(p1PreviewRig, GetCharacterAt(p1Index));
            SyncAnimatedPreviewSprite(p2PreviewRig, GetCharacterAt(p2Index));
        }

        private CharacterData GetCharacterAt(int idx)
        {
            if (CharacterLists == null || CharacterLists.Length == 0) return null;
            idx = Mathf.Clamp(idx, 0, CharacterLists.Length - 1);
            return CharacterLists[idx];
        }

        private void SyncAnimatedPreviewSprite(CharacterPreviewRig rig, CharacterData data)
        {
            if (rig == null || rig.fullBodyImage == null || data == null) return;

            if (data.previewSprite != null)
            {
                rig.fullBodyImage.sprite = data.previewSprite;
                return;
            }

            if (rig.hiddenSpriteRenderer != null && rig.hiddenSpriteRenderer.sprite != null)
            {
                rig.fullBodyImage.sprite = rig.hiddenSpriteRenderer.sprite;
            }
            else if (rig.fullBodyImage.sprite == null)
            {
                rig.fullBodyImage.sprite = data.portrait;
            }
        }

        private void AnimateSlotsAndPreviews()
        {
            float dt = Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.025f;

            for (int i = 0; i < poolSlots.Count; i++)
            {
                SlotBorderVisual slot = poolSlots[i];
                if (slot == null || slot.rootRect == null) continue;

                bool isP1 = (i == p1Index);
                bool isP2 = (i == p2Index);
                float targetScale = (isP1 && isP2) ? 1.12f * pulse : ((isP1 || isP2) ? 1.08f * pulse : 0.95f);
                slot.rootRect.localScale = Vector3.Lerp(slot.rootRect.localScale, new Vector3(targetScale, targetScale, 1f), dt * 12f);
            }

            AnimatePreviewRig(p1PreviewRig, dt);
            AnimatePreviewRig(p2PreviewRig, dt);
        }

        private void AnimatePreviewRig(CharacterPreviewRig rig, float dt)
        {
            if (rig == null || rig.rootRect == null) return;

            if (rig.animTimer > 0f)
            {
                rig.animTimer = Mathf.Max(0f, rig.animTimer - dt);
                float t = 1f - (rig.animTimer / 0.18f);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float slideX = Mathf.Lerp(-rig.facingSign * 55f, 0f, eased);
                rig.rootRect.anchoredPosition = rig.baseAnchoredPos + new Vector2(slideX, 0f);
                float s = Mathf.Lerp(0.92f, 1f, eased);
                rig.rootRect.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                rig.rootRect.anchoredPosition = rig.baseAnchoredPos;
                rig.rootRect.localScale = Vector3.one;
            }
        }

        private void HandlePlayer1Navigation(Keyboard keyboard)
        {
            int count = poolSlots.Count;
            if (count == 0) return;

            if (!p1Locked)
            {
                if (keyboard.dKey.wasPressedThisFrame)
                {
                    p1Index = (p1Index + 1) % count;
                    UpdateSelectionUI(triggerP1Anim: true, triggerP2Anim: false);
                }
                else if (keyboard.aKey.wasPressedThisFrame)
                {
                    p1Index = (p1Index - 1 + count) % count;
                    UpdateSelectionUI(triggerP1Anim: true, triggerP2Anim: false);
                }

                if (keyboard.jKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    p1Locked = true;
                    UpdateSelectionUI(triggerP1Anim: true, triggerP2Anim: false);
                    ProceedToMapSelection();
                }
            }
            else if (keyboard.kKey.wasPressedThisFrame)
            {
                p1Locked = false;
                UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: false);
            }
        }

        private void HandlePlayer2Navigation(Keyboard keyboard)
        {
            int count = poolSlots.Count;
            if (count == 0) return;

            if (!p2Locked)
            {
                if (keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    p2Index = (p2Index + 1) % count;
                    UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: true);
                }
                else if (keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    p2Index = (p2Index - 1 + count) % count;
                    UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: true);
                }

                if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    p2Locked = true;
                    UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: true);
                    ProceedToMapSelection();
                }
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame)
            {
                p2Locked = false;
                UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: false);
            }
        }

        private void UpdateSelectionUI(bool triggerP1Anim = false, bool triggerP2Anim = false)
        {
            Color inactiveBorder = new Color(0.16f, 0.19f, 0.26f, 0.9f);
            Color activeP1Color = p1Locked ? Color.Lerp(p1BorderColor, lockedBorderTint, 0.35f) : p1BorderColor;
            Color activeP2Color = p2Locked ? Color.Lerp(p2BorderColor, lockedBorderTint, 0.35f) : p2BorderColor;

            for (int i = 0; i < poolSlots.Count; i++)
            {
                SlotBorderVisual slot = poolSlots[i];
                if (slot == null) continue;

                bool isP1 = (i == p1Index);
                bool isP2 = (i == p2Index);

                // Kutunun tamamını boyamak yerine portreyi doğal renginde bırakıyoruz
                slot.portraitImage.color = (isP1 || isP2) ? highlightedColor : unselectedColor;

                if (isP1 && isP2)
                {
                    // İki oyuncu da aynı karakterin üzerindeyse sol-üst P1 (Mavi), sağ-alt P2 (Kırmızı) çerçeve
                    slot.topBorder.color = activeP1Color;
                    slot.leftBorder.color = activeP1Color;
                    slot.rightBorder.color = activeP2Color;
                    slot.bottomBorder.color = activeP2Color;
                }
                else if (isP1)
                {
                    slot.topBorder.color = activeP1Color;
                    slot.bottomBorder.color = activeP1Color;
                    slot.leftBorder.color = activeP1Color;
                    slot.rightBorder.color = activeP1Color;
                }
                else if (isP2)
                {
                    slot.topBorder.color = activeP2Color;
                    slot.bottomBorder.color = activeP2Color;
                    slot.leftBorder.color = activeP2Color;
                    slot.rightBorder.color = activeP2Color;
                }
                else
                {
                    slot.topBorder.color = inactiveBorder;
                    slot.bottomBorder.color = inactiveBorder;
                    slot.leftBorder.color = inactiveBorder;
                    slot.rightBorder.color = inactiveBorder;
                }

                if (slot.p1BadgeObj != null)
                {
                    slot.p1BadgeObj.SetActive(isP1);
                    if (slot.p1BadgeText != null) slot.p1BadgeText.text = p1Locked ? "P1✓" : "P1";
                    if (slot.p1BadgeBg != null) slot.p1BadgeBg.color = activeP1Color;
                }

                if (slot.p2BadgeObj != null)
                {
                    slot.p2BadgeObj.SetActive(isP2);
                    if (slot.p2BadgeText != null) slot.p2BadgeText.text = p2Locked ? "P2✓" : "P2";
                    if (slot.p2BadgeBg != null) slot.p2BadgeBg.color = activeP2Color;
                }
            }

            CharacterData p1Data = GetCharacterAt(p1Index);
            CharacterData p2Data = GetCharacterAt(p2Index);

            BindPreviewRigToCharacter(p1PreviewRig, p1Data, activeP1Color, triggerP1Anim);
            BindPreviewRigToCharacter(p2PreviewRig, p2Data, activeP2Color, triggerP2Anim);

            if (p1NameText != null && p1Data != null)
            {
                p1NameText.text = $"<size=56%><color=#45B6FE>PLAYER 1</color></size>\n{p1Data.displayName.ToUpperInvariant()}";
            }

            if (p1ReadyText != null)
            {
                p1ReadyText.text = p1Locked
                    ? "<color=#38F27B>★ READY! ★</color> <size=52%><color=#B0BEC5>([K] İptal)</color></size>"
                    : "<size=54%><color=#CFD8DC>[A/D] Seç  •  [J / Space] Onayla</color></size>";
            }

            if (p2NameText != null && p2Data != null)
            {
                p2NameText.text = $"<size=56%><color=#FF4757>PLAYER 2</color></size>\n{p2Data.displayName.ToUpperInvariant()}";
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = p2Locked
                    ? "<color=#38F27B>★ READY! ★</color> <size=52%><color=#B0BEC5>([Num2] İptal)</color></size>"
                    : "<size=54%><color=#CFD8DC>[←/→] Seç  •  [Enter / Num1] Onayla</color></size>";
            }
        }

        private void BindPreviewRigToCharacter(CharacterPreviewRig rig, CharacterData data, Color accentColor, bool triggerAnim)
        {
            if (rig == null || data == null) return;

            if (rig.portraitBadgeImage != null)
                rig.portraitBadgeImage.sprite = data.portrait;

            if (rig.portraitFrameImage != null)
                rig.portraitFrameImage.color = accentColor;

            if (rig.pedestalGlowImage != null)
                rig.pedestalGlowImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);

            // Tam boy karakter sprite & animasyon bağlama
            Sprite fallbackSprite = data.previewSprite != null ? data.previewSprite : data.portrait;
            RuntimeAnimatorController controller = data.animatorController;

            if (data.fighterPrefab != null)
            {
                SpriteRenderer prefabSR = data.fighterPrefab.GetComponentInChildren<SpriteRenderer>(true);
                if (prefabSR != null && prefabSR.sprite != null && data.previewSprite == null)
                {
                    fallbackSprite = prefabSR.sprite;
                }

                if (controller == null)
                {
                    Animator prefabAnim = data.fighterPrefab.GetComponentInChildren<Animator>(true);
                    if (prefabAnim != null) controller = prefabAnim.runtimeAnimatorController;
                }
            }

            if (rig.hiddenAnimator != null && controller != null)
            {
                if (rig.hiddenAnimator.runtimeAnimatorController != controller)
                {
                    rig.hiddenAnimator.runtimeAnimatorController = controller;
                }
                string idleState = string.IsNullOrWhiteSpace(data.idleAnimatorStateName) ? "Sancar_Idle" : data.idleAnimatorStateName;
                rig.hiddenAnimator.Play(idleState, 0, 0f);
                rig.hiddenAnimator.Update(0f);
            }

            if (rig.fullBodyImage != null)
            {
                if (data.previewSprite != null)
                    rig.fullBodyImage.sprite = data.previewSprite;
                else if (rig.hiddenSpriteRenderer != null && rig.hiddenSpriteRenderer.sprite != null)
                    rig.fullBodyImage.sprite = rig.hiddenSpriteRenderer.sprite;
                else
                    rig.fullBodyImage.sprite = fallbackSprite;
            }

            if (triggerAnim)
            {
                rig.animTimer = 0.18f;
            }
        }

        private void ProceedToMapSelection()
        {
            if (!p1Locked || !p2Locked)
                return;

            CharacterData p1SelectedData = CharacterLists[p1Index];
            CharacterData p2SelectedData = CharacterLists[p2Index];

            if (matchSpawner != null)
            {
                matchSpawner.SetSelectedCharacters(p1SelectedData, p2SelectedData);
            }
            Debug.Log($"Player 1 select: {p1SelectedData.displayName}");
            Debug.Log($"Player 2 select: {p2SelectedData.displayName}");
            GameManager.instance.SetGameState(EGameState.PVPM_MENU_MAPSELECTION);
        }
    }
}
