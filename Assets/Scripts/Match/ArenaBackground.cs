using UnityEngine;

namespace GameProject.Runtime.Manager
{
    [DefaultExecutionOrder(310)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ArenaBackground : MonoBehaviour
    {
        [Header("Position & Parallax")]
        [Tooltip("Arka planı yukarı/aşağı kaydırmak için ince ayar")]
        [SerializeField] private float yOffset = 0f;
        [SerializeField] private float zDistance = 15f;

        [Range(0f, 1f)]
        [Tooltip("0: Kameradan bağımsız (Uzak), 1: Kameraya tam kilitli (Ön plan)")]
        [SerializeField] private float parallaxFactor = 0.85f;

        [Header("Auto Scaling (Resolution & Perspective Fix)")]
        [SerializeField] private bool autoScaleToCamera = true;
        [Tooltip("Ekran kenarlarında ve parallax kaymasında boşluk kalmaması için büyüme payı")]
        [SerializeField] private float overscaleMargin = 1.25f;

        private Camera mainCam;
        private SpriteRenderer spriteRenderer;
        private Vector3 startPos;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            mainCam = Camera.main;
            startPos = transform.position;

            if (autoScaleToCamera)
                FitToScreen();
        }

        private void LateUpdate()
        {
            if (mainCam == null)
                mainCam = Camera.main;

            if (mainCam == null)
                return;

            float distX = mainCam.transform.position.x * parallaxFactor;
            transform.position = new Vector3(
                startPos.x + distX,
                mainCam.transform.position.y + yOffset,
                zDistance
            );

            // Perspective kamerada Z mesafesi (dolly zoom) değiştikçe veya
            // çözünürlük değiştikçe arka planın ekranı tam kaplamasını sağlar.
            if (autoScaleToCamera)
                FitToScreen();
        }

        public void FitToScreen()
        {
            if (mainCam == null)
                return;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float worldScreenHeight;

            if (mainCam.orthographic)
            {
                worldScreenHeight = mainCam.orthographicSize * 2f;
            }
            else
            {
                float distanceToCamera = Mathf.Abs(zDistance - mainCam.transform.position.z);
                float halfFovRad = mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                worldScreenHeight = distanceToCamera * Mathf.Tan(halfFovRad) * 2f;
            }

            float worldScreenWidth = worldScreenHeight * aspect;

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0.001f || spriteSize.y <= 0.001f)
                return;

            float widthRatio = worldScreenWidth / spriteSize.x;
            float heightRatio = worldScreenHeight / spriteSize.y;

            float finalScale = Mathf.Max(widthRatio, heightRatio) * overscaleMargin;
            transform.localScale = new Vector3(finalScale, finalScale, 1f);
        }
    }
}
