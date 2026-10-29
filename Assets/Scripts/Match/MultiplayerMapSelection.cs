using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace GameProject.Runtime.Manager
{
    public class MultiplayerMapSelection : MonoBehaviour
    {
        [Header("Map Database")]
        [SerializeField] private MapData[] mapList;

        [Header("Single Panel UI Generation")]
        [SerializeField] private Transform mapPanelParent; // Tek ortak panelin parent objesi
        [SerializeField] private Image mapSlotPrefab;

        [Header("Selection Visuals")]
        [SerializeField] private Color unselectedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color p1SelectedColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color p2SelectedColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.2f, 1f, 0.3f, 1f);
        [Header("Match Spawner Reference")]
        [SerializeField] private MatchSpawner matchSpawner;

        private readonly List<Image> mapSlots = new List<Image>();

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

            ClearChildren(mapPanelParent);
            mapSlots.Clear();

            for (int i = 0; i < mapList.Length; i++)
            {
                MapData data = mapList[i];
                if (data == null) continue;

                if (mapPanelParent != null && mapSlotPrefab != null)
                {
                    Image slot = Instantiate(mapSlotPrefab, mapPanelParent);
                    slot.sprite = data.mapPortrait;
                    slot.preserveAspect = true;
                    mapSlots.Add(slot);
                }
            }

            isMapUIGenerated = true;
            UpdateMapSelectionUI();
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

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || mapList == null || mapList.Length == 0)
                return;

            HandleP1MapInput(keyboard);
            HandleP2MapInput(keyboard);
        }

        private void HandleP1MapInput(Keyboard keyboard)
        {
            int count = mapList.Length;
            if (count == 0) return;

            if (!p1MapLocked)
            {
                // P1 Kontrolleri: A ve D tuşları
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

                // P1 Onay Tuşu: J veya Space
                if (keyboard.jKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    p1MapLocked = true;
                    UpdateMapSelectionUI();
                    TryGenerateMatch();
                }
            }
            else if (keyboard.kKey.wasPressedThisFrame) // P1 İptal Tuşu: K
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
                // P2 Kontrolleri: Sol ve Sağ Ok tuşları
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

                // P2 Onay Tuşu: Numpad 1 veya Enter
                if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    p2MapLocked = true;
                    UpdateMapSelectionUI();
                    TryGenerateMatch();
                }
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame) // P2 İptal Tuşu: Numpad 2
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

                if (p1MapLocked && p2MapLocked && (isP1Hovered || isP2Hovered))
                {
                    mapSlots[i].color = lockedColor;
                }
                else if (isP1Hovered && isP2Hovered)
                {
                    // İki oyuncu da aynı harita üzerindeyse renkleri harmanlayabilir veya ortak renk verebiliriz
                    mapSlots[i].color = highlightedColor;
                }
                else if (isP1Hovered)
                {
                    mapSlots[i].color = p1SelectedColor;
                }
                else if (isP2Hovered)
                {
                    mapSlots[i].color = p2SelectedColor;
                }
                else
                {
                    mapSlots[i].color = unselectedColor;
                }
            }
        }

        private void TryGenerateMatch()
        {
            // İki oyuncu da haritasını onaylamadan maçı başlatma
            if (!p1MapLocked || !p2MapLocked)
                return;

            // Eğer haritalar aynıysa direkt onu seç, farklıysa rastgele birini seç
            int finalMapIndex = (p1MapIndex == p2MapIndex)
                ? p1MapIndex
                : (Random.Range(0, 2) == 0 ? p1MapIndex : p2MapIndex);

            MapData chosenMap = mapList[finalMapIndex];

            if (matchSpawner != null)
            {
                matchSpawner.GenerateSubLevelAndFighters(chosenMap);
            }

            // Maç sahnesine / oynanış ekranına geç
            GameManager.instance.SetGameState(EGameState.PVPM_PLAYSCREEN);
        }
    }
}