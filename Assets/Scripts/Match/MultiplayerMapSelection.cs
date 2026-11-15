using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace GameProject.Runtime.Manager
{
    public class MultiplayerMapSelection : MonoBehaviour
    {
        [Header("Map Database")]
        [SerializeField] private MapData[] mapList;

        [Header("Single Panel UI Generation")]
        [SerializeField] private Transform mapPanelParent;
        [SerializeField] private Image mapSlotPrefab;

        [Header("Selection Visuals (Border Colors)")]
        [SerializeField] private Color unselectedColor = new Color(0.68f, 0.70f, 0.76f, 1f);
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color p1SelectedColor = new Color(0.15f, 0.68f, 1f, 1f);
        [SerializeField] private Color p2SelectedColor = new Color(1f, 0.22f, 0.30f, 1f);
        [SerializeField] private Color lockedColor = new Color(1f, 0.88f, 0.32f, 1f);

        [Header("Preview Layout")]
        [Tooltip("True ise P1 karakter önizlemesi sağda, P2 solda gösterilir. False ise MK standardı (P1 Solda, P2 Sağda) kullanılır.")]
        [SerializeField] private bool p1PreviewOnRightSide = false;

        [Header("Map Info & Ready Texts")]
        [SerializeField] private TextMeshProUGUI p1MapNameText;
        [SerializeField] private TextMeshProUGUI p1ReadyText;
        [SerializeField] private TextMeshProUGUI p2MapNameText;
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        [Header("Match Spawner Reference")]
        [SerializeField] private MatchSpawner matchSpawner;

        private class MapSlotBorderVisual
        {
            public Image mapImage;
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
            public TextMeshProUGUI mapTitleLabel;
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
            public float facingSign;
        }

        private readonly List<MapSlotBorderVisual> mapSlots = new List<MapSlotBorderVisual>();
        private CharacterPreviewRig p1FighterPreviewRig;
        private CharacterPreviewRig p2FighterPreviewRig;
        private TextMeshProUGUI centerArenaBannerText;

        private int p1MapIndex = 0;
        private int p2MapIndex = 0;

        private bool p1MapLocked = false;
        private bool p2MapLocked = false;

        private bool isMapUIGenerated = false;

        private void Awake()
        {
            CreateMapUIOnSelectionScreen();
        }

        private void OnEnable()
        {
            ResetMapSelectionState();
            RefreshSelectedCharacterPreviews();
        }

        public void CreateMapUIOnSelectionScreen()
        {
            if (isMapUIGenerated || mapList == null || mapList.Length == 0)
                return;

            SetupCenterMapPoolAndFighterPreviews();

            ClearChildren(mapPanelParent);
            mapSlots.Clear();

            for (int i = 0; i < mapList.Length; i++)
            {
                MapData data = mapList[i];
                if (data == null) continue;

                if (mapPanelParent != null && mapSlotPrefab != null)
                {
                    Image slotImg = Instantiate(mapSlotPrefab, mapPanelParent);
                    slotImg.name = $"MapSlot_{i}_{data.displayName}";
                    slotImg.sprite = data.mapPortrait;
                    slotImg.preserveAspect = false;
                    slotImg.color = unselectedColor;

                    MapSlotBorderVisual visual = BuildMapSlotBorderAndBadges(slotImg, data.displayName);
                    mapSlots.Add(visual);
                }
            }

            isMapUIGenerated = true;
            UpdateMapSelectionUI();
            RefreshSelectedCharacterPreviews();
        }

        private void SetupCenterMapPoolAndFighterPreviews()
        {
            if (mapPanelParent == null) return;

            Transform screenRoot = mapPanelParent.parent;
            if (screenRoot != null && screenRoot.TryGetComponent(out Image screenBg))
            {
                screenBg.color = new Color(0.05f, 0.06f, 0.10f, 0.96f);
            }

            // Ortadaki Harita Havuzu
            RectTransform poolRect = mapPanelParent as RectTransform;
            if (poolRect != null)
            {
                poolRect.anchorMin = new Vector2(0.5f, 0.5f);
                poolRect.anchorMax = new Vector2(0.5f, 0.5f);
                poolRect.pivot = new Vector2(0.5f, 0.5f);
                poolRect.anchoredPosition = new Vector2(0f, -10f);
                poolRect.sizeDelta = new Vector2(560f, 520f);
            }

            if (mapPanelParent.TryGetComponent(out Mask mask))
            {
                mask.enabled = false;
            }

            if (mapPanelParent.TryGetComponent(out Image panelImg))
            {
                panelImg.color = new Color(0.08f, 0.10f, 0.16f, 0.92f);
            }

            if (!mapPanelParent.TryGetComponent(out Outline outline))
            {
                outline = mapPanelParent.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0.95f, 0.78f, 0.28f, 0.65f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            if (mapPanelParent.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.cellSize = new Vector2(165f, 165f);
                grid.spacing = new Vector2(24f, 24f);
                grid.padding = new RectOffset(20, 20, 20, 20);
                grid.constraint = GridLayoutGroup.Constraint.Flexible;
            }

            if (screenRoot != null)
            {
                TextMeshProUGUI[] allTexts = screenRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < allTexts.Length; i++)
                {
                    TextMeshProUGUI t = allTexts[i];
                    if (t == p1MapNameText || t == p2MapNameText || t == p1ReadyText || t == p2ReadyText)
                        continue;

                    if (t.name.Contains("CharacterSelection") || t.text.Contains("Arena") || t.text.Contains("ARENA"))
                    {
                        t.text = "SELECT BATTLE ARENA";
                        t.color = new Color(1f, 0.85f, 0.28f, 1f);
                        t.fontStyle = FontStyles.Bold;
                        t.rectTransform.anchoredPosition = new Vector2(0f, 445f);
                        StyleLabel(t, new Color(1f, 0.85f, 0.28f, 1f));
                    }
                }

                // Havuzun hemen üstünde Mortal Kombat tarzı seçili Arena Başlığı ("SEA OF BLOOD" gibi)
                Transform existingBanner = screenRoot.Find("CenterArenaBanner");
                if (existingBanner != null) Destroy(existingBanner.gameObject);

                GameObject bannerObj = new GameObject("CenterArenaBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                bannerObj.transform.SetParent(screenRoot, false);
                RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
                bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
                bannerRect.pivot = new Vector2(0.5f, 0.5f);
                bannerRect.anchoredPosition = new Vector2(0f, 315f);
                bannerRect.sizeDelta = new Vector2(560f, 54f);

                Image bannerBg = bannerObj.GetComponent<Image>();
                bannerBg.color = new Color(0.08f, 0.11f, 0.18f, 0.94f);
                bannerBg.raycastTarget = false;

                Outline bannerOutline = bannerObj.GetComponent<Outline>();
                bannerOutline.effectColor = new Color(0.95f, 0.78f, 0.28f, 0.8f);
                bannerOutline.effectDistance = new Vector2(2f, -2f);

                GameObject bannerTxtObj = new GameObject("BannerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                bannerTxtObj.transform.SetParent(bannerRect, false);
                RectTransform bannerTxtRect = bannerTxtObj.GetComponent<RectTransform>();
                bannerTxtRect.anchorMin = Vector2.zero;
                bannerTxtRect.anchorMax = Vector2.one;
                bannerTxtRect.offsetMin = Vector2.zero;
                bannerTxtRect.offsetMax = Vector2.zero;

                centerArenaBannerText = bannerTxtObj.GetComponent<TextMeshProUGUI>();
                if (p1MapNameText != null) centerArenaBannerText.font = p1MapNameText.font;
                centerArenaBannerText.fontSize = 28f;
                centerArenaBannerText.alignment = TextAlignmentOptions.Center;
                centerArenaBannerText.raycastTarget = false;
                StyleLabel(centerArenaBannerText, new Color(1f, 0.88f, 0.42f, 1f));

                // Sağ ve Sol Seçilen Karakter Önizlemeleri (2. resimdeki gibi birbirine bakan karakterler)
                float p1X = p1PreviewOnRightSide ? 610f : -610f;
                float p2X = p1PreviewOnRightSide ? -610f : 610f;
                float p1Facing = p1PreviewOnRightSide ? -1f : 1f;
                float p2Facing = p1PreviewOnRightSide ? 1f : -1f;

                p1FighterPreviewRig = CreateCharacterPreviewRig(screenRoot, "P1_LockedFighterPreview", new Vector2(p1X, 45f), p1Facing, p1SelectedColor);
                p2FighterPreviewRig = CreateCharacterPreviewRig(screenRoot, "P2_LockedFighterPreview", new Vector2(p2X, 45f), p2Facing, p2SelectedColor);

                if (p1MapNameText != null)
                {
                    p1MapNameText.rectTransform.anchoredPosition = new Vector2(p1X, -365f);
                    StyleLabel(p1MapNameText, p1SelectedColor);
                }
                if (p1ReadyText != null)
                {
                    p1ReadyText.rectTransform.anchoredPosition = new Vector2(p1X, -455f);
                    StyleLabel(p1ReadyText, Color.white);
                }
                if (p2MapNameText != null)
                {
                    p2MapNameText.rectTransform.anchoredPosition = new Vector2(p2X, -365f);
                    StyleLabel(p2MapNameText, p2SelectedColor);
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
            rootObj.transform.SetSiblingIndex(1);

            RectTransform rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = anchoredPos;
            rootRect.sizeDelta = new Vector2(520f, 680f);

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

            GameObject badgeFrameObj = new GameObject("PortraitMedallionFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
            badgeFrameObj.transform.SetParent(rootRect, false);
            RectTransform badgeFrameRect = badgeFrameObj.GetComponent<RectTransform>();
            badgeFrameRect.anchorMin = new Vector2(0.5f, 0f);
            badgeFrameRect.anchorMax = new Vector2(0.5f, 0f);
            badgeFrameRect.pivot = new Vector2(0.5f, 0.5f);
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
                facingSign = facingSign
            };
        }

        private MapSlotBorderVisual BuildMapSlotBorderAndBadges(Image slotImage, string mapName)
        {
            RectTransform rt = slotImage.rectTransform;
            float borderThickness = 6f;

            // Alt kısımda harita adını gösteren ince koyu şerit
            GameObject nameBarObj = new GameObject("MapNameStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nameBarObj.transform.SetParent(rt, false);
            RectTransform nameBarRect = nameBarObj.GetComponent<RectTransform>();
            nameBarRect.anchorMin = new Vector2(0f, 0f);
            nameBarRect.anchorMax = new Vector2(1f, 0f);
            nameBarRect.pivot = new Vector2(0.5f, 0f);
            nameBarRect.anchoredPosition = new Vector2(0f, borderThickness);
            nameBarRect.sizeDelta = new Vector2(-borderThickness * 2f, 28f);
            Image nameBarImg = nameBarObj.GetComponent<Image>();
            nameBarImg.color = new Color(0.04f, 0.05f, 0.09f, 0.86f);
            nameBarImg.raycastTarget = false;

            GameObject nameTxtObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nameTxtObj.transform.SetParent(nameBarRect, false);
            RectTransform nameTxtRect = nameTxtObj.GetComponent<RectTransform>();
            nameTxtRect.anchorMin = Vector2.zero;
            nameTxtRect.anchorMax = Vector2.one;
            nameTxtRect.offsetMin = new Vector2(4f, 0f);
            nameTxtRect.offsetMax = new Vector2(-4f, 0f);
            TextMeshProUGUI titleTmp = nameTxtObj.GetComponent<TextMeshProUGUI>();
            if (p1MapNameText != null) titleTmp.font = p1MapNameText.font;
            titleTmp.text = mapName.ToUpperInvariant();
            titleTmp.fontSize = 15f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.95f, 0.95f, 1f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.enableWordWrapping = false;
            titleTmp.overflowMode = TextOverflowModes.Ellipsis;
            titleTmp.raycastTarget = false;

            Image top = CreateBorderEdge(rt, "Border_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -borderThickness), Vector2.zero);
            Image bottom = CreateBorderEdge(rt, "Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, borderThickness));
            Image left = CreateBorderEdge(rt, "Border_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(borderThickness, 0f));
            Image right = CreateBorderEdge(rt, "Border_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-borderThickness, 0f), Vector2.zero);

            GameObject p1Badge = CreateCornerBadge(rt, "P1_Badge", new Vector2(0f, 1f), new Vector2(26f, -18f), p1SelectedColor, "P1", out TextMeshProUGUI p1Txt, out Image p1Bg);
            GameObject p2Badge = CreateCornerBadge(rt, "P2_Badge", new Vector2(1f, 1f), new Vector2(-26f, -18f), p2SelectedColor, "P2", out TextMeshProUGUI p2Txt, out Image p2Bg);

            if (!slotImage.TryGetComponent(out Shadow shadow))
            {
                shadow = slotImage.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(4f, -4f);

            return new MapSlotBorderVisual
            {
                mapImage = slotImage,
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
                p2BadgeBg = p2Bg,
                mapTitleLabel = titleTmp
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
            if (p1MapNameText != null) label.font = p1MapNameText.font;
            label.text = text;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            return badgeObj;
        }

        private void RefreshSelectedCharacterPreviews()
        {
            if (matchSpawner == null) return;

            BindPreviewRigToCharacter(p1FighterPreviewRig, matchSpawner.SelectedP1Character, p1SelectedColor);
            BindPreviewRigToCharacter(p2FighterPreviewRig, matchSpawner.SelectedP2Character, p2SelectedColor);
        }

        private void BindPreviewRigToCharacter(CharacterPreviewRig rig, CharacterData data, Color accentColor)
        {
            if (rig == null || data == null) return;

            if (rig.portraitBadgeImage != null)
                rig.portraitBadgeImage.sprite = data.portrait;

            if (rig.portraitFrameImage != null)
                rig.portraitFrameImage.color = accentColor;

            if (rig.pedestalGlowImage != null)
                rig.pedestalGlowImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);

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

        private void ClearChildren(Transform panel)
        {
            if (panel == null) return;
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                Destroy(panel.GetChild(i).gameObject);
            }
        }

        public void ResetMapSelectionState()
        {
            p1MapIndex = 0;
            p2MapIndex = 0;
            p1MapLocked = false;
            p2MapLocked = false;
            UpdateMapSelectionUI();
        }

        private void Update()
        {
            if (GameManager.instance == null ||
                GameManager.instance.gameState != EGameState.PVPM_MENU_MAPSELECTION)
                return;

            AnimateSlotScales();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || mapList == null || mapList.Length == 0)
                return;

            HandleP1MapInput(keyboard);
            HandleP2MapInput(keyboard);
        }

        private void LateUpdate()
        {
            if (GameManager.instance == null ||
                GameManager.instance.gameState != EGameState.PVPM_MENU_MAPSELECTION ||
                matchSpawner == null)
                return;

            SyncAnimatedPreviewSprite(p1FighterPreviewRig, matchSpawner.SelectedP1Character);
            SyncAnimatedPreviewSprite(p2FighterPreviewRig, matchSpawner.SelectedP2Character);
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

        private void AnimateSlotScales()
        {
            float dt = Time.unscaledDeltaTime * 12f;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.025f;

            for (int i = 0; i < mapSlots.Count; i++)
            {
                if (mapSlots[i] == null || mapSlots[i].rootRect == null) continue;
                bool isHovered = (i == p1MapIndex || i == p2MapIndex);
                bool isBoth = (i == p1MapIndex && i == p2MapIndex);
                float targetScale = isBoth ? 1.12f * pulse : (isHovered ? 1.08f * pulse : 0.94f);

                RectTransform rt = mapSlots[i].rootRect;
                rt.localScale = Vector3.Lerp(rt.localScale, new Vector3(targetScale, targetScale, 1f), dt);
            }
        }

        private void HandleP1MapInput(Keyboard keyboard)
        {
            int count = mapList.Length;
            if (count == 0) return;

            if (!p1MapLocked)
            {
                if (keyboard.dKey.wasPressedThisFrame)
                {
                    p1MapIndex = (p1MapIndex + 1) % count;
                    UpdateMapSelectionUI();
                }
                else if (keyboard.aKey.wasPressedThisFrame)
                {
                    p1MapIndex = (p1MapIndex - 1 + count) % count;
                    UpdateMapSelectionUI();
                }

                if (keyboard.jKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    p1MapLocked = true;
                    UpdateMapSelectionUI();
                    TryGenerateMatch();
                }
            }
            else if (keyboard.kKey.wasPressedThisFrame)
            {
                p1MapLocked = false;
                UpdateMapSelectionUI();
            }
        }

        private void HandleP2MapInput(Keyboard keyboard)
        {
            int count = mapList.Length;
            if (count == 0) return;

            if (!p2MapLocked)
            {
                if (keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    p2MapIndex = (p2MapIndex + 1) % count;
                    UpdateMapSelectionUI();
                }
                else if (keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    p2MapIndex = (p2MapIndex - 1 + count) % count;
                    UpdateMapSelectionUI();
                }

                if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    p2MapLocked = true;
                    UpdateMapSelectionUI();
                    TryGenerateMatch();
                }
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame)
            {
                p2MapLocked = false;
                UpdateMapSelectionUI();
            }
        }

        private void UpdateMapSelectionUI()
        {
            Color inactiveBorder = new Color(0.16f, 0.19f, 0.26f, 0.9f);
            Color activeP1Color = p1MapLocked ? Color.Lerp(p1SelectedColor, lockedColor, 0.35f) : p1SelectedColor;
            Color activeP2Color = p2MapLocked ? Color.Lerp(p2SelectedColor, lockedColor, 0.35f) : p2SelectedColor;

            for (int i = 0; i < mapSlots.Count; i++)
            {
                MapSlotBorderVisual slot = mapSlots[i];
                if (slot == null) continue;

                bool isP1Hovered = (i == p1MapIndex);
                bool isP2Hovered = (i == p2MapIndex);

                // Harita görselini komple boyamak yerine doğal renginde bırakıyoruz
                slot.mapImage.color = (isP1Hovered || isP2Hovered) ? highlightedColor : unselectedColor;

                if (isP1Hovered && isP2Hovered)
                {
                    slot.topBorder.color = activeP1Color;
                    slot.leftBorder.color = activeP1Color;
                    slot.rightBorder.color = activeP2Color;
                    slot.bottomBorder.color = activeP2Color;
                }
                else if (isP1Hovered)
                {
                    slot.topBorder.color = activeP1Color;
                    slot.bottomBorder.color = activeP1Color;
                    slot.leftBorder.color = activeP1Color;
                    slot.rightBorder.color = activeP1Color;
                }
                else if (isP2Hovered)
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
                    slot.p1BadgeObj.SetActive(isP1Hovered);
                    if (slot.p1BadgeText != null) slot.p1BadgeText.text = p1MapLocked ? "P1✓" : "P1";
                    if (slot.p1BadgeBg != null) slot.p1BadgeBg.color = activeP1Color;
                }

                if (slot.p2BadgeObj != null)
                {
                    slot.p2BadgeObj.SetActive(isP2Hovered);
                    if (slot.p2BadgeText != null) slot.p2BadgeText.text = p2MapLocked ? "P2✓" : "P2";
                    if (slot.p2BadgeBg != null) slot.p2BadgeBg.color = activeP2Color;
                }
            }

            if (mapList != null && mapList.Length > 0)
            {
                string p1MapName = mapList[p1MapIndex].displayName.ToUpperInvariant();
                string p2MapName = mapList[p2MapIndex].displayName.ToUpperInvariant();
                string p1CharName = (matchSpawner != null && matchSpawner.SelectedP1Character != null)
                    ? matchSpawner.SelectedP1Character.displayName.ToUpperInvariant()
                    : "PLAYER 1";
                string p2CharName = (matchSpawner != null && matchSpawner.SelectedP2Character != null)
                    ? matchSpawner.SelectedP2Character.displayName.ToUpperInvariant()
                    : "PLAYER 2";

                if (centerArenaBannerText != null)
                {
                    if (p1MapIndex == p2MapIndex)
                        centerArenaBannerText.text = p1MapName;
                    else
                        centerArenaBannerText.text = $"<color=#45B6FE>{p1MapName}</color>  <size=75%><color=#FFD147>VS</color></size>  <color=#FF4757>{p2MapName}</color>";
                }

                if (p1MapNameText != null)
                {
                    p1MapNameText.text = $"<size=56%><color=#45B6FE>P1 • {p1CharName}</color></size>\n{p1MapName}";
                }

                if (p2MapNameText != null)
                {
                    p2MapNameText.text = $"<size=56%><color=#FF4757>P2 • {p2CharName}</color></size>\n{p2MapName}";
                }
            }

            if (p1ReadyText != null)
            {
                p1ReadyText.text = p1MapLocked
                    ? "<color=#38F27B>★ READY! ★</color> <size=52%><color=#B0BEC5>([K] İptal)</color></size>"
                    : "<size=54%><color=#CFD8DC>[A/D] Seç  •  [J / Space] Onayla</color></size>";
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = p2MapLocked
                    ? "<color=#38F27B>★ READY! ★</color> <size=52%><color=#B0BEC5>([Num2] İptal)</color></size>"
                    : "<size=54%><color=#CFD8DC>[←/→] Seç  •  [Enter / Num1] Onayla</color></size>";
            }
        }

        private void TryGenerateMatch()
        {
            if (!p1MapLocked || !p2MapLocked)
                return;

            int finalMapIndex = p1MapIndex;

            if (p1MapIndex != p2MapIndex)
            {
                finalMapIndex = (Random.value > 0.5f) ? p1MapIndex : p2MapIndex;
            }

            MapData chosenMap = mapList[finalMapIndex];
            Debug.Log($"Final Map Selected: {chosenMap.displayName}");

            if (matchSpawner != null)
            {
                matchSpawner.GenerateSubLevelAndFighters(chosenMap);
            }

            GameManager.instance.SetGameState(EGameState.PVPM_PLAYSCREEN);
        }
    }
}
