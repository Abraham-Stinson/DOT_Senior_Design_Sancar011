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
        [SerializeField] private Transform p1PanelParent;
        [SerializeField] private Transform p2PanelParent;
        [SerializeField] private Image characterSlotPrefab;

        [Header("Selection Visuals")]
        [SerializeField] private Color unselectedColor = new Color(0.38f, 0.40f, 0.46f, 0.92f);
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(1f, 0.92f, 0.55f, 1f);
        [SerializeField] private Color p1AccentColor = new Color(0.22f, 0.72f, 1f, 1f);
        [SerializeField] private Color p2AccentColor = new Color(1f, 0.26f, 0.32f, 1f);
        [SerializeField] private Color readyAccentColor = new Color(0.22f, 0.95f, 0.48f, 1f);

        [Header("Match Spawner Reference")]
        [SerializeField] private MatchSpawner matchSpawner;

        [Header("Character Info & Ready Texts")]
        [SerializeField] private TextMeshProUGUI p1NameText;
        [SerializeField] private TextMeshProUGUI p1ReadyText;
        [SerializeField] private TextMeshProUGUI p2NameText;
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        private readonly List<Image> p1GeneratedSlots = new List<Image>();
        private readonly List<Image> p2GeneratedSlots = new List<Image>();
        private readonly List<Outline> p1SlotOutlines = new List<Outline>();
        private readonly List<Outline> p2SlotOutlines = new List<Outline>();

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

            StyleScreenAndPanels();

            ClearPlaceholderChildren(p1PanelParent);
            ClearPlaceholderChildren(p2PanelParent);

            p1GeneratedSlots.Clear();
            p2GeneratedSlots.Clear();
            p1SlotOutlines.Clear();
            p2SlotOutlines.Clear();

            for (int i = 0; i < CharacterLists.Length; i++)
            {
                CharacterData data = CharacterLists[i];
                if (data == null) continue;

                if (p1PanelParent != null && characterSlotPrefab != null)
                {
                    Image p1Slot = Instantiate(characterSlotPrefab, p1PanelParent);
                    p1Slot.sprite = data.portrait;
                    p1Slot.preserveAspect = true;
                    Outline p1Outline = DecorateSlot(p1Slot);
                    p1GeneratedSlots.Add(p1Slot);
                    p1SlotOutlines.Add(p1Outline);
                }

                if (p2PanelParent != null && characterSlotPrefab != null)
                {
                    Image p2Slot = Instantiate(characterSlotPrefab, p2PanelParent);
                    p2Slot.sprite = data.portrait;
                    p2Slot.preserveAspect = true;
                    Outline p2Outline = DecorateSlot(p2Slot);
                    p2GeneratedSlots.Add(p2Slot);
                    p2SlotOutlines.Add(p2Outline);
                }
            }

            isUIGenerated = true;
            UpdateSelectionUI();
        }

        private Outline DecorateSlot(Image slot)
        {
            if (slot == null) return null;

            if (!slot.TryGetComponent(out Outline outline))
            {
                outline = slot.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0.12f, 0.14f, 0.2f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            if (!slot.TryGetComponent(out Shadow shadow))
            {
                shadow = slot.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(5f, -5f);

            return outline;
        }

        private void StyleScreenAndPanels()
        {
            StyleGridPanel(p1PanelParent, new Color(0.06f, 0.09f, 0.15f, 0.88f), p1AccentColor);
            StyleGridPanel(p2PanelParent, new Color(0.14f, 0.06f, 0.09f, 0.88f), p2AccentColor);

            StyleLabel(p1NameText, p1AccentColor);
            StyleLabel(p2NameText, p2AccentColor);
            StyleLabel(p1ReadyText, readyAccentColor);
            StyleLabel(p2ReadyText, readyAccentColor);

            // Başlık yazısını ve arka planı düzenle
            if (p1PanelParent != null && p1PanelParent.parent != null)
            {
                Transform screenRoot = p1PanelParent.parent;
                if (screenRoot.TryGetComponent(out Image screenBg))
                {
                    screenBg.color = new Color(0.05f, 0.06f, 0.10f, 0.95f);
                }

                TextMeshProUGUI[] allTexts = screenRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < allTexts.Length; i++)
                {
                    TextMeshProUGUI t = allTexts[i];
                    if (t == p1NameText || t == p2NameText || t == p1ReadyText || t == p2ReadyText)
                        continue;

                    if (t.text.Contains("Chamipon") || t.text.Contains("Champion") || t.name.Contains("CharacterSelection"))
                    {
                        t.text = "SELECT YOUR FIGHTER";
                    }

                    t.color = new Color(1f, 0.84f, 0.28f, 1f);
                    t.fontStyle = FontStyles.Bold;
                    StyleLabel(t, new Color(1f, 0.84f, 0.28f, 1f));
                }
            }
        }

        private void StyleGridPanel(Transform panel, Color bgColor, Color borderColor)
        {
            if (panel == null) return;
            if (panel.TryGetComponent(out Image panelImg))
            {
                panelImg.color = bgColor;
            }
            if (!panel.TryGetComponent(out Outline outline))
            {
                outline = panel.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(borderColor.r, borderColor.g, borderColor.b, 0.7f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
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
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (!label.TryGetComponent(out Shadow shadow))
            {
                shadow = label.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
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
            p2Index = 0;
            p1Locked = false;
            p2Locked = false;
            UpdateSelectionUI();
        }

        private void Update()
        {
            if (GameManager.instance == null ||
                GameManager.instance.gameState != EGameState.PVPM_MENU_CHARACTERSELECTION)
                return;

            AnimateSlotScales();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || CharacterLists == null || CharacterLists.Length == 0)
                return;

            HandlePlayer1Navigation(keyboard);
            HandlePlayer2Navigation(keyboard);
        }

        private void AnimateSlotScales()
        {
            float dt = Time.unscaledDeltaTime * 12f;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.025f;

            for (int i = 0; i < p1GeneratedSlots.Count; i++)
            {
                if (p1GeneratedSlots[i] == null) continue;
                float targetScale = (i == p1Index) ? (p1Locked ? 1.14f : 1.09f * pulse) : 0.92f;
                RectTransform rt = p1GeneratedSlots[i].rectTransform;
                rt.localScale = Vector3.Lerp(rt.localScale, new Vector3(targetScale, targetScale, 1f), dt);
            }

            for (int i = 0; i < p2GeneratedSlots.Count; i++)
            {
                if (p2GeneratedSlots[i] == null) continue;
                float targetScale = (i == p2Index) ? (p2Locked ? 1.14f : 1.09f * pulse) : 0.92f;
                RectTransform rt = p2GeneratedSlots[i].rectTransform;
                rt.localScale = Vector3.Lerp(rt.localScale, new Vector3(targetScale, targetScale, 1f), dt);
            }
        }

        private void HandlePlayer1Navigation(Keyboard keyboard)
        {
            int count = p1GeneratedSlots.Count;
            if (count == 0) return;

            if (!p1Locked)
            {
                if (keyboard.dKey.wasPressedThisFrame)
                {
                    p1Index = (p1Index + 1) % count;
                    UpdateSelectionUI();
                }
                else if (keyboard.aKey.wasPressedThisFrame)
                {
                    p1Index = (p1Index - 1 + count) % count;
                    UpdateSelectionUI();
                }

                if (keyboard.jKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    p1Locked = true;
                    UpdateSelectionUI();
                    ProceedToMapSelection();
                }
            }
            else if (keyboard.kKey.wasPressedThisFrame)
            {
                p1Locked = false;
                UpdateSelectionUI();
            }
        }

        private void HandlePlayer2Navigation(Keyboard keyboard)
        {
            int count = p2GeneratedSlots.Count;
            if (count == 0) return;

            if (!p2Locked)
            {
                if (keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    p2Index = (p2Index + 1) % count;
                    UpdateSelectionUI();
                }
                else if (keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    p2Index = (p2Index - 1 + count) % count;
                    UpdateSelectionUI();
                }

                if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    p2Locked = true;
                    UpdateSelectionUI();
                    ProceedToMapSelection();
                }
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame)
            {
                p2Locked = false;
                UpdateSelectionUI();
            }
        }

        private void UpdateSelectionUI()
        {
            for (int i = 0; i < p1GeneratedSlots.Count; i++)
            {
                if (p1GeneratedSlots[i] == null) continue;
                bool selected = (i == p1Index);
                p1GeneratedSlots[i].color = selected
                    ? (p1Locked ? lockedColor : highlightedColor)
                    : unselectedColor;

                if (i < p1SlotOutlines.Count && p1SlotOutlines[i] != null)
                {
                    p1SlotOutlines[i].effectColor = selected
                        ? (p1Locked ? readyAccentColor : p1AccentColor)
                        : new Color(0.1f, 0.12f, 0.18f, 0.85f);
                    p1SlotOutlines[i].effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
                }
            }

            if (p1NameText != null && CharacterLists.Length > 0)
            {
                p1NameText.text = $"<size=60%><color=#45B6FE>PLAYER 1</color></size>\n{CharacterLists[p1Index].displayName.ToUpperInvariant()}";
            }

            if (p1ReadyText != null)
            {
                p1ReadyText.text = p1Locked
                    ? "<color=#38F27B>★ READY! ★</color>\n<size=52%><color=#B0BEC5>[K] İptal</color></size>"
                    : "<size=58%><color=#CFD8DC>[A/D] Seç  •  [J / Space] Onayla</color></size>";
            }

            for (int i = 0; i < p2GeneratedSlots.Count; i++)
            {
                if (p2GeneratedSlots[i] == null) continue;
                bool selected = (i == p2Index);
                p2GeneratedSlots[i].color = selected
                    ? (p2Locked ? lockedColor : highlightedColor)
                    : unselectedColor;

                if (i < p2SlotOutlines.Count && p2SlotOutlines[i] != null)
                {
                    p2SlotOutlines[i].effectColor = selected
                        ? (p2Locked ? readyAccentColor : p2AccentColor)
                        : new Color(0.1f, 0.12f, 0.18f, 0.85f);
                    p2SlotOutlines[i].effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
                }
            }

            if (p2NameText != null && CharacterLists.Length > 0)
            {
                p2NameText.text = $"<size=60%><color=#FF4757>PLAYER 2</color></size>\n{CharacterLists[p2Index].displayName.ToUpperInvariant()}";
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = p2Locked
                    ? "<color=#38F27B>★ READY! ★</color>\n<size=52%><color=#B0BEC5>[Num2] İptal</color></size>"
                    : "<size=58%><color=#CFD8DC>[←/→] Seç  •  [Enter / Num1] Onayla</color></size>";
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
