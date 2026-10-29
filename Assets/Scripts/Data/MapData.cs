using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMap",
    menuName = "Sancar 011/Maps/Map Data"
)]
public class MapData : ScriptableObject
{
    [Header("Identity")]
    public string mapID;
    public string displayName;

    [Header("UI Presentation")]
    public Sprite mapPortrait; // Harita seçim ekranında görünecek önizleme

    [Header("3D Arena / Sub-Level Prefab")]
    public GameObject arenaPrefab; // İçinde 3D Ground, Background ve Spawn Point'ler olan Prefab
}