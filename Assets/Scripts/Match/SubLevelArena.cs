using UnityEngine;

namespace GameProject.Runtime.Manager
{
    public class SubLevelArena : MonoBehaviour
    {
        [Header("Fighter Spawn Points (Sub-Level İçi)")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;

        public Transform Player1SpawnPoint => player1SpawnPoint;
        public Transform Player2SpawnPoint => player2SpawnPoint;
    }
}