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

    [Header("3D Environment / Arena (Mortal Kombat 2.5D)")]
    [Tooltip("Pivot noktası arenanın ortasında olan 3D sahne/arka plan modeli veya prefab'ı.")]
    public GameObject environment3DPrefab;

    [Tooltip("3D sahne modelinin doğacağı dünya koordinatı (Pivot ortadaysa 0,0,0).")]
    public Vector3 environment3DSpawnPosition = Vector3.zero;

    [Tooltip("3D sahne modelinin doğacağı rotasyon (Euler açıları).")]
    public Vector3 environment3DSpawnRotation = Vector3.zero;

    [Tooltip("3D sahne modelinin ölçeği (Varsayılan: 1,1,1).")]
    public Vector3 environment3DScale = Vector3.one;

    [Tooltip("3D model kendi zeminini ve arka planını içeriyorsa 2D arka planı ve test zemin küpünü gizler.")]
    public bool hide2DElementsWhen3DAssigned = true;

    [Header("2D Arena Generation (Placeholder / Fallback)")]
    public Sprite backgroundSprite;
    public GameObject arenaPrefab;
}
