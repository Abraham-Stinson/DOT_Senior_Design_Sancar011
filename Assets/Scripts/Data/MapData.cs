using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMap",
    menuName = "Sancar 011/Maps/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Identity")]
    public string mapID;
    public string displayName;

    [Header("UI Presentation")]
    public Sprite mapPortrait;

    [Header("2D Arena Generation")]
    public Sprite backgroundSprite;
    public GameObject arenaPrefab;
}