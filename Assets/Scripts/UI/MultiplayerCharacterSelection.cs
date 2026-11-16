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
        [SerializeField] private Color p1BorderColor = new Color(0.20f, 0.56f, 0.92f, 1f);
        [SerializeField] private Color p2BorderColor = new Color(0.86f, 0.22f, 0.24f, 1f);
        [SerializeField] private Color lockedBorderTint = new Color(0.95f, 0.86f, 0.55f, 1f);

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

            // İkinci kutuyu tamamen kapatıyoruz; tüm karakterler ortadaki tek havuzda
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
                screenBg.color = new Color(0.05f, 0.05f, 0.07f, 0.96f);
            }

            // Ortadaki Karakter Havuzu
            RectTransform poolRect = p1PanelParent as RectTransform;
            if (poolRect != null)
            {
                poolRect.anchorMin = new Vector2(0.5f, 0.5f);
                poolRect.anchorMax = new Vector2(0.5f, 0.5f);
                poolRect.pivot = new Vector2(0.5f, 0.5f);
                poolRect.anchoredPosition = new Vector2(0f, 10f);
                poolRect.sizeDelta = new Vector2(500f, 420f);
            }

            if (p1PanelParent.TryGetComponent(out Mask mask))
            {
                mask.enabled = false;
            }

            if (p1PanelParent.TryGetComponent(out Image poolBg))
            {
                poolBg.color = new Color(0.07f, 0.07f, 0.09f, 0.78f);
            }

            if (!p1PanelParent.TryGetComponent(out Outline poolOutline))
            {
                poolOutline = p1PanelParent.gameObject.AddComponent<Outline>();
            }
            poolOutline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            poolOutline.effectDistance = new Vector2(1f, -1f);

            if (p1PanelParent.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.cellSize = new Vector2(136f, 136f);
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
                    if (t == p1NameText || t == p2NameText || t == p1ReadyText || t == p2ReadyText)
                        continue;

                    if (t.name.Contains("CharacterSelection") || t.text.Contains("Chamipon") || t.text.Contains("FIGHTER") || t.text.Contains("CHARACTER"))
                    {
                        t.text = "CHARACTER SELECT";
                        t.enableAutoSizing = false;
                        t.fontSize = 38f;
                        t.fontStyle = FontStyles.Bold;
                        t.rectTransform.anchoredPosition = new Vector2(0f, 450f);
                        t.rectTransform.sizeDelta = new Vector2(600f, 60f);
                        StyleLabel(t, new Color(0.92f, 0.88f, 0.78f, 1f));
                    }
                }

                // P1 ve P2 Önizleme Konumları (Birbirine bakan sağ ve sol karakter önizlemeleri)
                float p1X = p1PreviewOnRightSide ? 610f : -610f;
                float p2X = p1PreviewOnRightSide ? -610f : 610f;
                float p1Facing = p1PreviewOnRightSide ? -1f : 1f;
                float p2Facing = p1PreviewOnRightSide ? 1f : -1f;

                p1PreviewRig = CreateCharacterPreviewRig(screenRoot, "P1_CharacterPreview", new Vector2(p1X, 45f), p1Facing);
                p2PreviewRig = CreateCharacterPreviewRig(screenRoot, "P2_CharacterPreview", new Vector2(p2X, 45f), p2Facing);

                Color offWhite = new Color(0.94f, 0.95f, 0.97f, 1f);
                Color hintGray = new Color(0.62f, 0.65f, 0.70f, 0.9f);

                if (p1NameText != null)
                {
                    p1NameText.rectTransform.anchoredPosition = new Vector2(p1X, -320f);
                    p1NameText.rectTransform.sizeDelta = new Vector2(460f, 44f);
                    p1NameText.enableAutoSizing = false;
                    p1NameText.fontSize = 32f;
                    StyleLabel(p1NameText, offWhite);
                }
                if (p1ReadyText != null)
                {
                    p1ReadyText.rectTransform.anchoredPosition = new Vector2(p1X, -362f);
                    p1ReadyText.rectTransform.sizeDelta = new Vector2(460f, 32f);
                    p1ReadyText.enableAutoSizing = false;
                    p1ReadyText.fontSize = 18f;
                    StyleLabel(p1ReadyText, hintGray);
                }
                if (p2NameText != null)
                {
                    p2NameText.rectTransform.anchoredPosition = new Vector2(p2X, -320f);
                    p2NameText.rectTransform.sizeDelta = new Vector2(460f, 44f);
                    p2NameText.enableAutoSizing = false;
                    p2NameText.fontSize = 32f;
                    StyleLabel(p2NameText, offWhite);
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

            // Tam Boy Karakter Görseli (Mortal Kombat tarzı duruş)
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
            float borderThickness = 4f;

            Image top = CreateBorderEdge(rt, "Border_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -borderThickness), Vector2.zero);
            Image bottom = CreateBorderEdge(rt, "Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, borderThickness));
            Image left = CreateBorderEdge(rt, "Border_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(borderThickness, 0f));
            Image right = CreateBorderEdge(rt, "Border_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-borderThickness, 0f), Vector2.zero);

            GameObject p1Badge = CreateCornerBadge(rt, "P1_Badge", new Vector2(0f, 1f), new Vector2(18f, -12f), p1BorderColor, "P1", out Image p1Bg);
            GameObject p2Badge = CreateCornerBadge(rt, "P2_Badge", new Vector2(1f, 1f), new Vector2(-18f, -12f), p2BorderColor, "P2", out Image p2Bg);

            if (!slotImage.TryGetComponent(out Shadow shadow))
            {
                shadow = slotImage.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(3f, -3f);

            return new SlotBorderVisual
            {
                portraitImage = slotImage,
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
            if (p1NameText != null) label.font = p1NameText.font;
            label.text = text;
            label.fontSize = 14f;
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

            for (int i = 0; i < poolSlots.Count; i++)
            {
                SlotBorderVisual slot = poolSlots[i];
                if (slot == null || slot.rootRect == null) continue;

                bool isSelected = (i == p1Index || i == p2Index);
                float targetScale = isSelected ? 1.04f : 1.0f;
                slot.rootRect.localScale = Vector3.Lerp(slot.rootRect.localScale, new Vector3(targetScale, targetScale, 1f), dt * 14f);
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
                float t = 1f - (rig.animTimer / 0.16f);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float slideX = Mathf.Lerp(-rig.facingSign * 36f, 0f, eased);
                rig.rootRect.anchoredPosition = rig.baseAnchoredPos + new Vector2(slideX, 0f);
                rig.rootRect.localScale = Vector3.one;
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
                    UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: false);
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
                    UpdateSelectionUI(triggerP1Anim: false, triggerP2Anim: false);
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
            Color inactiveBorder = new Color(0.22f, 0.24f, 0.28f, 0.85f);
            Color activeP1Color = p1Locked ? Color.Lerp(p1BorderColor, lockedBorderTint, 0.30f) : p1BorderColor;
            Color activeP2Color = p2Locked ? Color.Lerp(p2BorderColor, lockedBorderTint, 0.30f) : p2BorderColor;

            for (int i = 0; i < poolSlots.Count; i++)
            {
                SlotBorderVisual slot = poolSlots[i];
                if (slot == null) continue;

                bool isP1 = (i == p1Index);
                bool isP2 = (i == p2Index);

                slot.portraitImage.color = (isP1 || isP2) ? highlightedColor : unselectedColor;

                if (isP1 && isP2)
                {
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
                    if (slot.p1BadgeBg != null) slot.p1BadgeBg.color = activeP1Color;
                }

                if (slot.p2BadgeObj != null)
                {
                    slot.p2BadgeObj.SetActive(isP2);
                    if (slot.p2BadgeBg != null) slot.p2BadgeBg.color = activeP2Color;
                }
            }

            CharacterData p1Data = GetCharacterAt(p1Index);
            CharacterData p2Data = GetCharacterAt(p2Index);

            BindPreviewRigToCharacter(p1PreviewRig, p1Data, triggerP1Anim);
            BindPreviewRigToCharacter(p2PreviewRig, p2Data, triggerP2Anim);

            if (p1NameText != null && p1Data != null)
            {
                p1NameText.text = p1Data.displayName.ToUpperInvariant();
            }

            if (p1ReadyText != null)
            {
                p1ReadyText.text = p1Locked ? "READY" : "A / D  •  J";
                p1ReadyText.color = p1Locked
                    ? new Color(0.95f, 0.82f, 0.35f, 1f)
                    : new Color(0.62f, 0.65f, 0.70f, 0.9f);
            }

            if (p2NameText != null && p2Data != null)
            {
                p2NameText.text = p2Data.displayName.ToUpperInvariant();
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = p2Locked ? "READY" : "← / →  •  ENTER";
                p2ReadyText.color = p2Locked
                    ? new Color(0.95f, 0.82f, 0.35f, 1f)
                    : new Color(0.62f, 0.65f, 0.70f, 0.9f);
            }
        }

        private void BindPreviewRigToCharacter(CharacterPreviewRig rig, CharacterData data, bool triggerAnim)
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

            if (triggerAnim)
            {
                rig.animTimer = 0.16f;
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
