using System.Collections;
using UnityEngine;
using TMPro;

namespace GameProject.Runtime.Manager
{
    public class MatchSpawner : MonoBehaviour
    {
        [Header("Camera Reference")]
        [Tooltip("Dinamik Arena Kamerası Scripti (MainCamera üzerinde olmalı)")]
        [SerializeField] private ArenaCamera arenaCamera;
        [Header("HUD Reference")]
        [SerializeField] private PvPHUDManager pvpHUDManager;

        [Header("Match Setup (World Coordinates)")]
        [Tooltip("Player 1'in sahnede doğacağı koordinat")]
        [SerializeField] private Vector3 p1SpawnPosition = new Vector3(-4f, 0f, 0f);
        [Tooltip("Player 2'nin sahnede doğacağı koordinat")]
        [SerializeField] private Vector3 p2SpawnPosition = new Vector3(4f, 0f, 0f);

        [Header("Arena Setup")]
        [Tooltip("Arka plan için kullanılacak boş SpriteRenderer barındıran Prefab")]
        [SerializeField] private GameObject backgroundPrefab;

        [Header("Countdown UI")]
        [SerializeField] private TextMeshProUGUI countdownText;

        private CharacterData selectedP1Character;
        private CharacterData selectedP2Character;

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

            if (selectedMap == null)
            {
                Debug.LogError("MatchSpawner: Seçilen MapData eksik!", this);
                return;
            }

            if (backgroundPrefab != null && selectedMap.backgroundSprite != null)
            {
                spawnedBackgroundInstance = Instantiate(backgroundPrefab, new Vector3(0f, 0f, 15f), Quaternion.identity);
                spawnedBackgroundInstance.name = $"Background_{selectedMap.displayName}";

                if (spawnedBackgroundInstance.TryGetComponent(out SpriteRenderer sr))
                {
                    sr.sprite = selectedMap.backgroundSprite;
                }
            }

            // B) 3D Sub-Level / Platform Prefab'ını Generate Et
            if (selectedMap.arenaPrefab != null)
            {
                spawnedArenaInstance = Instantiate(selectedMap.arenaPrefab, Vector3.zero, Quaternion.identity);
                spawnedArenaInstance.name = $"Arena_{selectedMap.displayName}";
            }

            activePlayer1 = InstantiateFighter(selectedP1Character, p1SpawnPosition, FighterControlType.Player1, "Player1");
            activePlayer2 = InstantiateFighter(selectedP2Character, p2SpawnPosition, FighterControlType.Player2, "Player2");

            if (activePlayer1 != null && activePlayer1.TryGetComponent(out FighterFacing p1Facing))
                p1Facing.RefreshTarget();

            if (activePlayer2 != null && activePlayer2.TryGetComponent(out FighterFacing p2Facing))
                p2Facing.RefreshTarget();

            //E) HUD ekranına karakterleri bağla
            if (pvpHUDManager != null)
            {
                pvpHUDManager.BindFightersToHUD(activePlayer1, activePlayer2);
            }

            // YENİ: F) Dinamik Kameraya Hedefleri Ata
            if (arenaCamera != null && activePlayer1 != null && activePlayer2 != null)
            {
                arenaCamera.SetTargets(activePlayer1.transform, activePlayer2.transform);
            }

            // G) Geri Sayımı Başlat (Kontrolleri kilitler)
            StartCoroutine(MatchStartSequence());
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

        private IEnumerator MatchStartSequence()
        {
            ToggleFighterControls(activePlayer1, false);
            ToggleFighterControls(activePlayer2, false);

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = "3";
                yield return new WaitForSeconds(1f);
                countdownText.text = "2";
                yield return new WaitForSeconds(1f);
                countdownText.text = "1";
                yield return new WaitForSeconds(1f);
                countdownText.text = "FIGHT!";
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            ToggleFighterControls(activePlayer1, true);
            ToggleFighterControls(activePlayer2, true);

            if (countdownText != null)
            {
                yield return new WaitForSeconds(1f);
                countdownText.gameObject.SetActive(false);
            }
        }

        private void ToggleFighterControls(FighterController fighter, bool canControl)
        {
            if (fighter == null) return;
            if (fighter.TryGetComponent(out FighterMotor motor)) motor.enabled = canControl;
            if (fighter.TryGetComponent(out FighterCombat combat)) combat.enabled = canControl;
            if (fighter.TryGetComponent(out FighterDefense defense)) defense.enabled = canControl;
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