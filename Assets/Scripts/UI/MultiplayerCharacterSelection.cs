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
        [SerializeField] private Color unselectedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.2f, 1f, 0.3f, 1f);

        [Header("Match Spawner Reference")]
        [SerializeField] private MatchSpawner matchSpawner;

        [Header("Character Info & Ready Texts")]
        [SerializeField] private TextMeshProUGUI p1NameText;
        [SerializeField] private TextMeshProUGUI p1ReadyText;
        [SerializeField] private TextMeshProUGUI p2NameText;
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        private readonly List<Image> p1GeneratedSlots = new List<Image>();
        private readonly List<Image> p2GeneratedSlots = new List<Image>();

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

            ClearPlaceholderChildren(p1PanelParent);
            ClearPlaceholderChildren(p2PanelParent);

            p1GeneratedSlots.Clear();
            p2GeneratedSlots.Clear();

            for (int i = 0; i < CharacterLists.Length; i++)
            {
                CharacterData data = CharacterLists[i];
                if (data == null) continue;

                if (p1PanelParent != null && characterSlotPrefab != null)
                {
                    Image p1Slot = Instantiate(characterSlotPrefab, p1PanelParent);
                    p1Slot.sprite = data.portrait;
                    p1Slot.preserveAspect = true;
                    p1GeneratedSlots.Add(p1Slot);
                }

                if (p2PanelParent != null && characterSlotPrefab != null)
                {
                    Image p2Slot = Instantiate(characterSlotPrefab, p2PanelParent);
                    p2Slot.sprite = data.portrait;
                    p2Slot.preserveAspect = true;
                    p2GeneratedSlots.Add(p2Slot);
                }
            }

            isUIGenerated = true;
            UpdateSelectionUI();
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

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || CharacterLists == null || CharacterLists.Length == 0)
                return;

            HandlePlayer1Navigation(keyboard);
            HandlePlayer2Navigation(keyboard);
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
                p1GeneratedSlots[i].color = (i == p1Index)
                    ? (p1Locked ? lockedColor : highlightedColor)
                    : unselectedColor;
            }

            if (p1NameText != null && CharacterLists.Length > 0)
                p1NameText.text = CharacterLists[p1Index].displayName;

            if (p1ReadyText != null)
                p1ReadyText.text = p1Locked ? "READY" : "";

            for (int i = 0; i < p2GeneratedSlots.Count; i++)
            {
                if (p2GeneratedSlots[i] == null) continue;
                p2GeneratedSlots[i].color = (i == p2Index)
                    ? (p2Locked ? lockedColor : highlightedColor)
                    : unselectedColor;
            }

            if (p2NameText != null && CharacterLists.Length > 0)
                p2NameText.text = CharacterLists[p2Index].displayName;

            if (p2ReadyText != null)
                p2ReadyText.text = p2Locked ? "READY" : "";
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