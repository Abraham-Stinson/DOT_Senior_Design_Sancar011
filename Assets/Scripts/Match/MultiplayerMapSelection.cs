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

        [Header("Selection Visuals")]
        [SerializeField] private Color unselectedColor = new Color(0.38f, 0.40f, 0.46f, 0.92f);
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color p1SelectedColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color p2SelectedColor = new Color(1f, 0.58f, 0.62f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.55f, 1f, 0.68f, 1f);

        [Header("Map Info & Ready Texts")]
        [SerializeField] private TextMeshProUGUI p1MapNameText;
        [SerializeField] private TextMeshProUGUI p1ReadyText;
        [SerializeField] private TextMeshProUGUI p2MapNameText;
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        [Header("Match Spawner Reference")]
        [SerializeField] private MatchSpawner matchSpawner;

        private readonly List<Image> mapSlots = new List<Image>();
        private readonly List<Outline> mapSlotOutlines = new List<Outline>();

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
        }

        public void CreateMapUIOnSelectionScreen()
        {
            if (isMapUIGenerated || mapList == null || mapList.Length == 0)
                return;

            StyleScreenAndPanels();

            ClearChildren(mapPanelParent);
            mapSlots.Clear();
            mapSlotOutlines.Clear();

            for (int i = 0; i < mapList.Length; i++)
            {
                MapData data = mapList[i];
                if (data == null) continue;

                if (mapPanelParent != null && mapSlotPrefab != null)
                {
                    Image slot = Instantiate(mapSlotPrefab, mapPanelParent);
                    slot.sprite = data.mapPortrait;
                    slot.preserveAspect = true;
                    Outline outline = DecorateSlot(slot);
                    mapSlots.Add(slot);
                    mapSlotOutlines.Add(outline);
                }
            }

            isMapUIGenerated = true;
            UpdateMapSelectionUI();
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
            if (mapPanelParent != null)
            {
                if (mapPanelParent.TryGetComponent(out Image panelImg))
                {
                    panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.88f);
                }
                if (!mapPanelParent.TryGetComponent(out Outline outline))
                {
                    outline = mapPanelParent.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0.95f, 0.78f, 0.28f, 0.75f);
                outline.effectDistance = new Vector2(2.5f, -2.5f);

                Transform screenRoot = mapPanelParent.parent;
                if (screenRoot != null)
                {
                    if (screenRoot.TryGetComponent(out Image screenBg))
                    {
                        screenBg.color = new Color(0.05f, 0.06f, 0.10f, 0.95f);
                    }

                    TextMeshProUGUI[] allTexts = screenRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                    for (int i = 0; i < allTexts.Length; i++)
                    {
                        TextMeshProUGUI t = allTexts[i];
                        if (t == p1MapNameText || t == p2MapNameText || t == p1ReadyText || t == p2ReadyText)
                            continue;

                        if (t.text.Contains("Map") || t.name.Contains("CharacterSelection"))
                        {
                            t.text = "SELECT BATTLE ARENA";
                        }

                        StyleLabel(t, new Color(1f, 0.84f, 0.28f, 1f));
                    }
                }
            }

            StyleLabel(p1MapNameText, new Color(0.28f, 0.75f, 1f, 1f));
            StyleLabel(p2MapNameText, new Color(1f, 0.32f, 0.36f, 1f));
            StyleLabel(p1ReadyText, new Color(0.22f, 0.95f, 0.48f, 1f));
            StyleLabel(p2ReadyText, new Color(0.22f, 0.95f, 0.48f, 1f));
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

        private void AnimateSlotScales()
        {
            float dt = Time.unscaledDeltaTime * 12f;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.025f;

            for (int i = 0; i < mapSlots.Count; i++)
            {
                if (mapSlots[i] == null) continue;
                bool isHovered = (i == p1MapIndex || i == p2MapIndex);
                bool isBoth = (i == p1MapIndex && i == p2MapIndex);
                float targetScale = isBoth ? 1.13f * pulse : (isHovered ? 1.08f * pulse : 0.92f);

                RectTransform rt = mapSlots[i].rectTransform;
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
            for (int i = 0; i < mapSlots.Count; i++)
            {
                if (mapSlots[i] == null) continue;

                bool isP1Hovered = (i == p1MapIndex);
                bool isP2Hovered = (i == p2MapIndex);
                Color borderColor = new Color(0.1f, 0.12f, 0.18f, 0.85f);

                if (p1MapLocked && p2MapLocked && (isP1Hovered || isP2Hovered))
                {
                    mapSlots[i].color = lockedColor;
                    borderColor = new Color(0.22f, 0.95f, 0.48f, 1f);
                }
                else if (isP1Hovered && isP2Hovered)
                {
                    mapSlots[i].color = highlightedColor;
                    borderColor = new Color(1f, 0.84f, 0.25f, 1f);
                }
                else if (isP1Hovered)
                {
                    mapSlots[i].color = p1SelectedColor;
                    borderColor = new Color(0.22f, 0.72f, 1f, 1f);
                }
                else if (isP2Hovered)
                {
                    mapSlots[i].color = p2SelectedColor;
                    borderColor = new Color(1f, 0.26f, 0.32f, 1f);
                }
                else
                {
                    mapSlots[i].color = unselectedColor;
                }

                if (i < mapSlotOutlines.Count && mapSlotOutlines[i] != null)
                {
                    mapSlotOutlines[i].effectColor = borderColor;
                    mapSlotOutlines[i].effectDistance = (isP1Hovered || isP2Hovered) ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
                }
            }

            if (mapList != null && mapList.Length > 0)
            {
                if (p1MapNameText != null)
                {
                    p1MapNameText.text = $"<size=58%><color=#45B6FE>P1 VOTE</color></size>\n{mapList[p1MapIndex].displayName.ToUpperInvariant()}";
                }

                if (p2MapNameText != null)
                {
                    p2MapNameText.text = $"<size=58%><color=#FF4757>P2 VOTE</color></size>\n{mapList[p2MapIndex].displayName.ToUpperInvariant()}";
                }
            }

            if (p1ReadyText != null)
            {
                p1ReadyText.text = p1MapLocked
                    ? "<color=#38F27B>★ READY! ★</color>\n<size=52%><color=#B0BEC5>[K] İptal</color></size>"
                    : "<size=58%><color=#CFD8DC>[A/D] Seç  •  [J / Space] Onayla</color></size>";
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = p2MapLocked
                    ? "<color=#38F27B>★ READY! ★</color>\n<size=52%><color=#B0BEC5>[Num2] İptal</color></size>"
                    : "<size=58%><color=#CFD8DC>[←/→] Seç  •  [Enter / Num1] Onayla</color></size>";
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
