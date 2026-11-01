using System.Collections;
using UnityEngine;

namespace GameProject.Runtime.Manager
{
    public class MatchSpawner : MonoBehaviour
    {
        [Header("Managers")]
        [Tooltip("Dinamik Arena Kamerası Scripti (MainCamera üzerinde olmalı)")]
        [SerializeField] private ArenaCamera arenaCamera;
        [SerializeField] private PvPHUDManager pvpHUDManager;
        [SerializeField] private RoundManager roundManager; // YENİ EKLENDİ

        [Header("Match Setup (World Coordinates)")]
        [SerializeField] private Vector3 p1SpawnPosition = new Vector3(-4f, 0f, 0f);
        [SerializeField] private Vector3 p2SpawnPosition = new Vector3(4f, 0f, 0f);

        [Header("Arena Setup")]
        [SerializeField] private GameObject backgroundPrefab;

        private CharacterData selectedP1Character;
        private CharacterData selectedP2Character;
        private MapData currentSelectedMap; // Rematch için hafızada tutuyoruz

        private GameObject spawnedBackgroundInstance;
        private GameObject spawnedArenaInstance;
        private FighterController activePlayer1;
        private FighterController activePlayer2;

        public void SetSelectedCharacters(CharacterData p1Data, CharacterData p2Data)
        {
            selectedP1Character = p1Data;
            selectedP2Character = p2Data;
        }

        public void GenerateSubLevelAndFighters(MapData selectedMap)
        {
            ClearMatch();
            currentSelectedMap = selectedMap;

            if (selectedMap == null) return;

            // Arka Plan
            if (backgroundPrefab != null && selectedMap.backgroundSprite != null)
            {
                spawnedBackgroundInstance = Instantiate(backgroundPrefab, new Vector3(0f, 0f, 15f), Quaternion.identity);
                if (spawnedBackgroundInstance.TryGetComponent(out SpriteRenderer sr)) sr.sprite = selectedMap.backgroundSprite;
            }

            // Zemin / Arena
            if (selectedMap.arenaPrefab != null)
            {
                spawnedArenaInstance = Instantiate(selectedMap.arenaPrefab, Vector3.zero, Quaternion.identity);
            }

            // Karakterler
            activePlayer1 = InstantiateFighter(selectedP1Character, p1SpawnPosition, FighterControlType.Player1, "Player1");
            activePlayer2 = InstantiateFighter(selectedP2Character, p2SpawnPosition, FighterControlType.Player2, "Player2");

            // Karakter Bakışları
            if (activePlayer1 != null && activePlayer1.TryGetComponent(out FighterFacing p1Facing)) p1Facing.RefreshTarget();
            if (activePlayer2 != null && activePlayer2.TryGetComponent(out FighterFacing p2Facing)) p2Facing.RefreshTarget();

            // HUD Bağlantısı
            if (pvpHUDManager != null) pvpHUDManager.BindFightersToHUD(activePlayer1, activePlayer2);

            // Kamera Bağlantısı
            if (arenaCamera != null && activePlayer1 != null && activePlayer2 != null)
            {
                arenaCamera.SetTargets(activePlayer1.transform, activePlayer2.transform);
            }

            // Round Sistemini Başlat
            if (roundManager != null)
            {
                roundManager.Initialize(activePlayer1, activePlayer2, this);
            }
        }

        private FighterController InstantiateFighter(CharacterData data, Vector3 spawnPos, FighterControlType controlType, string instanceName)
        {
            if (data == null || data.fighterPrefab == null) return null;
            GameObject fighterGO = Instantiate(data.fighterPrefab, spawnPos, Quaternion.identity);
            fighterGO.name = $"{instanceName}_{data.displayName}";
            if (fighterGO.TryGetComponent(out FighterController controller))
            {
                controller.Initialize(data);
                controller.SetControlType(controlType);
                return controller;
            }
            return null;
        }

        public void ResetFighterPositions()
        {
            if (activePlayer1 != null)
            {
                activePlayer1.transform.position = p1SpawnPosition;
                activePlayer1.ResetCombatState(); 
            }

            if (activePlayer2 != null)
            {
                activePlayer2.transform.position = p2SpawnPosition;
                activePlayer2.ResetCombatState(); 
            }

            // Yönlerini tekrar ortala
            if (activePlayer1 != null && activePlayer1.TryGetComponent(out FighterFacing p1Facing)) p1Facing.RefreshTarget();
            if (activePlayer2 != null && activePlayer2.TryGetComponent(out FighterFacing p2Facing)) p2Facing.RefreshTarget();
        }

        // Rematch butonu için
        public void RestartCurrentMatch()
        {
            if (currentSelectedMap != null)
            {
                GenerateSubLevelAndFighters(currentSelectedMap);
            }
        }

        public void ClearMatch()
        {
            if (activePlayer1 != null) Destroy(activePlayer1.gameObject);
            if (activePlayer2 != null) Destroy(activePlayer2.gameObject);
            if (spawnedArenaInstance != null) Destroy(spawnedArenaInstance);
            if (spawnedBackgroundInstance != null) Destroy(spawnedBackgroundInstance);
        }
    }
}