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
        [SerializeField] private Color p1SelectedColor = new Color(0.20f, 0.56f, 0.92f, 1f);
        [SerializeField] private Color p2SelectedColor = new Color(0.86f, 0.22f, 0.24f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.95f, 0.86f, 0.55f, 1f);

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
            public Image p1BadgeBg;
            public GameObject p2BadgeObj;
            public Image p2BadgeBg;
        }

        private class CharacterPreviewRig
        {
            public RectTransform rootRect;
            public Image fullBodyImage;
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
                screenBg.color = new Color(0.05f, 0.05f, 0.07f, 0.96f);
            }

            // Ortadaki Harita Havuzu
            RectTransform poolRect = mapPanelParent as RectTransform;
            if (poolRect != null)
            {
                poolRect.anchorMin = new Vector2(0.5f, 0.5f);
                poolRect.anchorMax = new Vector2(0.5f, 0.5f);
                poolRect.pivot = new Vector2(0.5f, 0.5f);
                poolRect.anchoredPosition = new Vector2(0f, -10f);
                poolRect.sizeDelta = new Vector2(520f, 400f);
            }

            if (mapPanelParent.TryGetComponent(out Mask mask))
            {
                mask.enabled = false;
            }

            if (mapPanelParent.TryGetComponent(out Image panelImg))
            {
                panelImg.color = new Color(0.07f, 0.07f, 0.09f, 0.78f);
            }

            if (!mapPanelParent.TryGetComponent(out Outline outline))
            {
                outline = mapPanelParent.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            outline.effectDistance = new Vector2(1f, -1f);

            if (mapPanelParent.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.cellSize = new Vector2(145f, 145f);
                grid.spacing = new Vector2(16f, 16f);
                grid.padding = new RectOffset(16, 16, 16, 16);
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
                        t.text = "SELECT ARENA";
                        t.enableAutoSizing = false;
                        t.fontSize = 38f;
                        t.fontStyle = FontStyles.Bold;
                        t.rectTransform.anchoredPosition = new Vector2(0f, 450f);
                        t.rectTransform.sizeDelta = new Vector2(600f, 60f);
                        StyleLabel(t, new Color(0.92f, 0.88f, 0.78f, 1f));
                    }
                }

                // Seçili Arena Banner
                Transform existingBanner = screenRoot.Find("CenterArenaBanner");
                if (existingBanner != null) Destroy(existingBanner.gameObject);

                GameObject bannerObj = new GameObject("CenterArenaBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                bannerObj.transform.SetParent(screenRoot, false);
                RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
                bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
                bannerRect.pivot = new Vector2(0.5f, 0.5f);
                bannerRect.anchoredPosition = new Vector2(0f, 240f);
                bannerRect.sizeDelta = new Vector2(520f, 40f);

                Image bannerBg = bannerObj.GetComponent<Image>();
                bannerBg.color = new Color(0.09f, 0.10f, 0.13f, 0.85f);
                bannerBg.raycastTarget = false;

                Outline bannerOutline = bannerObj.GetComponent<Outline>();
                bannerOutline.effectColor = new Color(1f, 1f, 1f, 0.14f);
                bannerOutline.effectDistance = new Vector2(1f, -1f);

                GameObject bannerTxtObj = new GameObject("BannerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                bannerTxtObj.transform.SetParent(bannerRect, false);
                RectTransform bannerTxtRect = bannerTxtObj.GetComponent<RectTransform>();
                bannerTxtRect.anchorMin = Vector2.zero;
                bannerTxtRect.anchorMax = Vector2.one;
                bannerTxtRect.offsetMin = Vector2.zero;
                bannerTxtRect.offsetMax = Vector2.zero;

                centerArenaBannerText = bannerTxtObj.GetComponent<TextMeshProUGUI>();
                if (p1MapNameText != null) centerArenaBannerText.font = p1MapNameText.font;
                centerArenaBannerText.enableAutoSizing = false;
                centerArenaBannerText.fontSize = 22f;
                centerArenaBannerText.alignment = TextAlignmentOptions.Center;
                centerArenaBannerText.raycastTarget = false;
                StyleLabel(centerArenaBannerText, new Color(0.92f, 0.92f, 0.95f, 1f));

                // Sağ ve Sol Seçilen Karakter Önizlemeleri
                float p1X = p1PreviewOnRightSide ? 610f : -610f;
                float p2X = p1PreviewOnRightSide ? -610f : 610f;
                float p1Facing = p1PreviewOnRightSide ? -1f : 1f;
                float p2Facing = p1PreviewOnRightSide ? 1f : -1f;

                p1FighterPreviewRig = CreateCharacterPreviewRig(screenRoot, "P1_LockedFighterPreview", new Vector2(p1X, 45f), p1Facing);
                p2FighterPreviewRig = CreateCharacterPreviewRig(screenRoot, "P2_LockedFighterPreview", new Vector2(p2X, 45f), p2Facing);

                Color offWhite = new Color(0.94f, 0.95f, 0.97f, 1f);
                Color hintGray = new Color(0.62f, 0.65f, 0.70f, 0.9f);

                if (p1MapNameText != null)
                {
                    p1MapNameText.rectTransform.anchoredPosition = new Vector2(p1X, -320f);
                    p1MapNameText.rectTransform.sizeDelta = new Vector2(460f, 44f);
                    p1MapNameText.enableAutoSizing = false;
                    p1MapNameText.fontSize = 28f;
                    StyleLabel(p1MapNameText, offWhite);
                }
                if (p1ReadyText != null)
                {
                    p1ReadyText.rectTransform.anchoredPosition = new Vector2(p1X, -362f);
                    p1ReadyText.rectTransform.sizeDelta = new Vector2(460f, 32f);
                    p1ReadyText.enableAutoSizing = false;
                    p1ReadyText.fontSize = 18f;
                    StyleLabel(p1ReadyText, hintGray);
                }
                if (p2MapNameText != null)
                {
                    p2MapNameText.rectTransform.anchoredPosition = new Vector2(p2X, -320f);
                    p2MapNameText.rectTransform.sizeDelta = new Vector2(460f, 44f);
                    p2MapNameText.enableAutoSizing = false;
                    p2MapNameText.fontSize = 28f;
                    StyleLabel(p2MapNameText, offWhite);
                }
                if (p2ReadyText != null)
                {
                    p2ReadyText.rectTransform.anchoredPosition = new Vector2(p2X, -362f);
                    p2ReadyText.rectTransform.sizeDelta = new Vector2(460f, 32f);
                    p2ReadyText.enableAutoSizing = false;
                    p2ReadyText.fontSize = 18f;
                    StyleLabel(p2ReadyText, hintGray);
                }
            }
        }

        private CharacterPreviewRig CreateCharacterPreviewRig(Transform parent, string name, Vector2 anchoredPos, float facingSign)
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
            rootRect.sizeDelta = new Vector2(480f, 620f);

            GameObject bodyObj = new GameObject("FullBodyFighter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
            bodyObj.transform.SetParent(rootRect, false);
            RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, 10f);
            bodyRect.sizeDelta = new Vector2(460f, 580f);
            bodyRect.localScale = new Vector3(facingSign, 1f, 1f);

            Image bodyImg = bodyObj.GetComponent<Image>();
            bodyImg.preserveAspect = true;
            bodyImg.raycastTarget = false;

            Shadow bodyShadow = bodyObj.GetComponent<Shadow>();
            bodyShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            bodyShadow.effectDistance = new Vector2(facingSign * -8f, -6f);

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
                hiddenSpriteRenderer = sr,
                hiddenAnimator = anim,
                baseAnchoredPos = anchoredPos,
                facingSign = facingSign
            };
        }

        private MapSlotBorderVisual BuildMapSlotBorderAndBadges(Image slotImage, string mapName)
        {
            RectTransform rt = slotImage.rectTransform;
            float borderThickness = 4f;

            // Alt kısımda harita adını gösteren ince koyu şerit
            GameObject nameBarObj = new GameObject("MapNameStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nameBarObj.transform.SetParent(rt, false);
            RectTransform nameBarRect = nameBarObj.GetComponent<RectTransform>();
            nameBarRect.anchorMin = new Vector2(0f, 0f);
            nameBarRect.anchorMax = new Vector2(1f, 0f);
            nameBarRect.pivot = new Vector2(0.5f, 0f);
            nameBarRect.anchoredPosition = new Vector2(0f, borderThickness);
            nameBarRect.sizeDelta = new Vector2(-borderThickness * 2f, 24f);
            Image nameBarImg = nameBarObj.GetComponent<Image>();
            nameBarImg.color = new Color(0.05f, 0.05f, 0.07f, 0.86f);
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
            titleTmp.fontSize = 13f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.93f, 0.94f, 0.96f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.enableWordWrapping = false;
            titleTmp.overflowMode = TextOverflowModes.Ellipsis;
            titleTmp.raycastTarget = false;

            Image top = CreateBorderEdge(rt, "Border_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -borderThickness), Vector2.zero);
            Image bottom = CreateBorderEdge(rt, "Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, borderThickness));
            Image left = CreateBorderEdge(rt, "Border_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(borderThickness, 0f));
            Image right = CreateBorderEdge(rt, "Border_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-borderThickness, 0f), Vector2.zero);

            GameObject p1Badge = CreateCornerBadge(rt, "P1_Badge", new Vector2(0f, 1f), new Vector2(18f, -12f), p1SelectedColor, "P1", out Image p1Bg);
            GameObject p2Badge = CreateCornerBadge(rt, "P2_Badge", new Vector2(1f, 1f), new Vector2(-18f, -12f), p2SelectedColor, "P2", out Image p2Bg);

            if (!slotImage.TryGetComponent(out Shadow shadow))
            {
                shadow = slotImage.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(3f, -3f);

            return new MapSlotBorderVisual
            {
                mapImage = slotImage,
                rootRect = rt,
                topBorder = top,
                bottomBorder = bottom,
                leftBorder = left,
                rightBorder = right,
                p1BadgeObj = p1Badge,
                p1BadgeBg = p1Bg,
                p2BadgeObj = p2Badge,
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
            out Image bgImage)
        {
            GameObject badgeObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObj.transform.SetParent(parent, false);

            RectTransform rt = badgeObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(32f, 20f);

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

            TextMeshProUGUI label = txtObj.GetComponent<TextMeshProUGUI>();
            if (p1MapNameText != null) label.font = p1MapNameText.font;
            label.text = text;
            label.fontSize = 14f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            return badgeObj;
        }

        private void RefreshSelectedCharacterPreviews()
        {
            if (matchSpawner == null) return;

            BindPreviewRigToCharacter(p1FighterPreviewRig, matchSpawner.SelectedP1Character);
            BindPreviewRigToCharacter(p2FighterPreviewRig, matchSpawner.SelectedP2Character);
        }

        private void BindPreviewRigToCharacter(CharacterPreviewRig rig, CharacterData data)
        {
            if (rig == null || data == null) return;

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
            label.richText = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;

            if (label.TryGetComponent(out Outline outline))
            {
                outline.enabled = false;
            }

            if (!label.TryGetComponent(out Shadow shadow))
            {
                shadow = label.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
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
            float dt = Time.unscaledDeltaTime * 14f;

            for (int i = 0; i < mapSlots.Count; i++)
            {
                if (mapSlots[i] == null || mapSlots[i].rootRect == null) continue;
                bool isHovered = (i == p1MapIndex || i == p2MapIndex);
                float targetScale = isHovered ? 1.04f : 1.0f;

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
            Color inactiveBorder = new Color(0.22f, 0.24f, 0.28f, 0.85f);
            Color activeP1Color = p1MapLocked ? Color.Lerp(p1SelectedColor, lockedColor, 0.30f) : p1SelectedColor;
            Color activeP2Color = p2MapLocked ? Color.Lerp(p2SelectedColor, lockedColor, 0.30f) : p2SelectedColor;

            for (int i = 0; i < mapSlots.Count; i++)
            {
                MapSlotBorderVisual slot = mapSlots[i];
                if (slot == null) continue;

                bool isP1Hovered = (i == p1MapIndex);
                bool isP2Hovered = (i == p2MapIndex);

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
                    if (slot.p1BadgeBg != null) slot.p1BadgeBg.color = activeP1Color;
                }

                if (slot.p2BadgeObj != null)
                {
                    slot.p2BadgeObj.SetActive(isP2Hovered);
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
                    centerArenaBannerText.text = (p1MapIndex == p2MapIndex)
                        ? p1MapName
                        : $"{p1MapName}  /  {p2MapName}";
                }

                if (p1MapNameText != null)
                {
                    p1MapNameText.text = p1CharName;
                }

                if (p2MapNameText != null)
                {
                    p2MapNameText.text = p2CharName;
                }
            }

            if (p1ReadyText != null)
            {
                p1ReadyText.text = p1MapLocked ? "READY" : "A / D  •  J";
                p1ReadyText.color = p1MapLocked
                    ? new Color(0.95f, 0.82f, 0.35f, 1f)
                    : new Color(0.62f, 0.65f, 0.70f, 0.9f);
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = p2MapLocked ? "READY" : "← / →  •  ENTER";
                p2ReadyText.color = p2MapLocked
                    ? new Color(0.95f, 0.82f, 0.35f, 1f)
                    : new Color(0.62f, 0.65f, 0.70f, 0.9f);
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
