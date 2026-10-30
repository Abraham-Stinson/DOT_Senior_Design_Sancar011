using UnityEngine;

namespace GameProject.Runtime.Manager
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ArenaBackground : MonoBehaviour
    {
        [Header("Position & Parallax")]
        [Tooltip("Arka planı yukarı/aşağı kaydırmak için ince ayar")]
        [SerializeField] private float yOffset = 0f;
        [SerializeField] private float zDistance = 15f;
        
        [Range(0f, 1f)]
        [Tooltip("0: Kameradan bağımsız (Uzak), 1: Kameraya tam kilitli (Ön plan)")]
        [SerializeField] private float parallaxFactor = 0.9f; 

        [Header("Auto Scaling (Resolution Fix)")]
        [SerializeField] private bool autoScaleToCamera = true;
        [Tooltip("Ekran kenarlarında siyah boşluk kalmaması için büyüme payı")]
        [SerializeField] private float overscaleMargin = 1.05f;

        private Camera mainCam;
        private Vector3 startPos;
        private float currentResolutionRatio;

        private void Start()
        {
            mainCam = Camera.main;
            startPos = transform.position;
            
            if (autoScaleToCamera) FitToScreen();
        }

        private void LateUpdate()
        {
            if (mainCam == null) return;

            if (autoScaleToCamera && currentResolutionRatio != (float)Screen.width / Screen.height)
            {
                FitToScreen();
            }

            float distX = mainCam.transform.position.x * parallaxFactor;
            transform.position = new Vector3(startPos.x + distX, mainCam.transform.position.y + yOffset, zDistance);
        }

        public void FitToScreen()
        {
            if (mainCam == null) return;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null) return;

            currentResolutionRatio = (float)Screen.width / Screen.height;
            transform.localScale = Vector3.one;
            
            float worldScreenHeight = mainCam.orthographicSize * 2f;
            float worldScreenWidth = worldScreenHeight * currentResolutionRatio;

            float widthRatio = worldScreenWidth / sr.sprite.bounds.size.x;
            float heightRatio = worldScreenHeight / sr.sprite.bounds.size.y;

            float finalScale = Mathf.Max(widthRatio, heightRatio) * overscaleMargin;
            transform.localScale = new Vector3(finalScale, finalScale, 1f);
        }
    }
}