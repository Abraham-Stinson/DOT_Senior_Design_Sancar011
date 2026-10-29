using UnityEngine;

namespace GameProject.Runtime.Manager
{
    public class MatchSpawner : MonoBehaviour
    {
        [Header("HUD Reference")]
        [SerializeField] private PvPHUDManager pvpHUDManager;

        private CharacterData selectedP1Character;
        private CharacterData selectedP2Character;

        private SubLevelArena activeSubLevel;
        private FighterController activePlayer1;
        private FighterController activePlayer2;

        // 1. Adım: Karakter seçim ekranından gelen verileri hafızaya al
        public void SetSelectedCharacters(CharacterData p1Data, CharacterData p2Data)
        {
            selectedP1Character = p1Data;
            selectedP2Character = p2Data;
        }

        // 2. Adım: Harita seçildikten sonra 3D Arenayı ve karakterleri üret
        public void GenerateSubLevelAndFighters(MapData selectedMap)
        {
            ClearMatch();

            if (selectedMap == null || selectedMap.arenaPrefab == null)
            {
                Debug.LogError("MatchSpawner: Seçilen MapData veya 3D Arena Prefab eksik!", this);
                return;
            }

            // A) 3D Sub-Level / Arena Prefab'ını sahneye bas
            GameObject spawnedArenaGO = Instantiate(selectedMap.arenaPrefab, Vector3.zero, Quaternion.identity);
            
            if (spawnedArenaGO.TryGetComponent(out SubLevelArena arenaComponent))
            {
                activeSubLevel = arenaComponent;
            }
            else
            {
                Debug.LogError("MatchSpawner: Seçilen harita prefabında SubLevelArena bileşeni yok!", this);
                return;
            }

            // B) 3D Sub-Level içindeki Spawn Point'leri kullanarak karakterleri üret
            activePlayer1 = InstantiateFighter(
                selectedP1Character,
                activeSubLevel.Player1SpawnPoint,
                FighterControlType.Player1,
                "Player1_Fighter"
            );

            activePlayer2 = InstantiateFighter(
                selectedP2Character,
                activeSubLevel.Player2SpawnPoint,
                FighterControlType.Player2,
                "Player2_Fighter"
            );

            // C) Karakterlerin birbirine bakmasını sağla
            if (activePlayer1 != null && activePlayer1.TryGetComponent(out FighterFacing p1Facing))
                p1Facing.RefreshTarget();

            if (activePlayer2 != null && activePlayer2.TryGetComponent(out FighterFacing p2Facing))
                p2Facing.RefreshTarget();

            // D) HUD ekranına (can barları ve portreler) karakterleri bağla
            if (pvpHUDManager != null)
            {
                pvpHUDManager.BindFightersToHUD(activePlayer1, activePlayer2);
            }
        }

        private FighterController InstantiateFighter(
            CharacterData data,
            Transform spawnPoint,
            FighterControlType controlType,
            string instanceName)
        {
            if (data == null || data.fighterPrefab == null)
            {
                Debug.LogError($"MatchSpawner: {instanceName} için CharacterData veya fighterPrefab eksik!");
                return null;
            }

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            GameObject fighterGO = Instantiate(data.fighterPrefab, spawnPos, Quaternion.identity);
            fighterGO.name = $"{instanceName}_{data.displayName}";

            if (fighterGO.TryGetComponent(out FighterController controller))
            {
                controller.Initialize(data);
                controller.SetControlType(controlType);
                return controller;
            }

            Debug.LogError($"MatchSpawner: {data.fighterPrefab.name} prefabında FighterController yok!");
            return null;
        }

        public void ClearMatch()
        {
            if (activePlayer1 != null) Destroy(activePlayer1.gameObject);
            if (activePlayer2 != null) Destroy(activePlayer2.gameObject);
            if (activeSubLevel != null) Destroy(activeSubLevel.gameObject);
        }
    }
}