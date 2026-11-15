using System.Collections.Generic;
using UnityEngine;

namespace GameProject.Runtime.Manager
{
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(Camera))]
    public class ArenaCamera : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private List<Transform> targets = new List<Transform>();

        [Header("Mortal Kombat 2.5D Perspective")]
        [Tooltip("Perspective (3D derinlikli) kamera modunu aktif eder.")]
        [SerializeField] private bool usePerspective = true;

        [Tooltip("Dar açılı dövüş kamerası FOV değeri (MK tarzı için 30-40 arası idealdir).")]
        [Range(20f, 65f)]
        [SerializeField] private float fieldOfView = 35f;

        [Tooltip("Kameranın zemine ve karakterlere hafif aşağı doğru bakış açısı (Pitch).")]
        [Range(-10f, 20f)]
        [SerializeField] private float pitchAngle = 3f;

        [Header("Positioning & Arena Limits")]
        [Tooltip("Y: Karakterlerin yerden kamera yüksekliği (göğüs/baş hizası).")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
        [SerializeField] private float minX = -4.5f;
        [SerializeField] private float maxX = 4.5f;

        [Header("Distance / Zoom Settings (MK Framing)")]
        [Tooltip("Karakterler yakınken kameranın görüntülediği minimum dikey alan (yarı-yükseklik).")]
        [Min(1.5f)]
        [SerializeField] private float minZoom = 2.6f;

        [Tooltip("Karakterler uzaklaştığında kameranın açılacağı maksimum dikey alan (yarı-yükseklik).")]
        [Min(2f)]
        [SerializeField] private float maxZoom = 3.8f;

        [Tooltip("Zoom'un maksimuma ulaşacağı karakterler arası mesafe.")]
        [Min(1f)]
        [SerializeField] private float zoomLimiter = 9f;

        [Tooltip("Kamera takip yumuşaklığı (düşük değer = daha sıkı ve keskin takip).")]
        [Min(0.01f)]
        [SerializeField] private float smoothTime = 0.14f;

        [Header("Screen Edge Constraint (MK Tether)")]
        [Tooltip("Karakterlerin kamera kadrajından dışarı yürümesini engeller.")]
        [SerializeField] private bool constrainTargetsToScreen = true;

        [Tooltip("Karakterlerin ekran kenarından bırakacağı güvenli pay (dünya birimi).")]
        [Min(0.2f)]
        [SerializeField] private float screenEdgePadding = 1.15f;

        private Camera cam;
        private Vector3 velocity;
        private float zoomVelocity;
        private float currentViewHalfHeight;

        public float CurrentViewHalfHeight =>
            currentViewHalfHeight > 0.01f ? currentViewHalfHeight : minZoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            currentViewHalfHeight = minZoom;
            ApplyProjectionSettings();
        }

        private void OnValidate()
        {
            if (maxZoom < minZoom)
                maxZoom = minZoom;

            if (maxX < minX)
                maxX = minX;

            if (cam == null)
                cam = GetComponent<Camera>();

            if (cam != null)
                ApplyProjectionSettings();
        }

        private void ApplyProjectionSettings()
        {
            if (cam == null)
                return;

            cam.orthographic = !usePerspective;

            if (usePerspective)
                cam.fieldOfView = fieldOfView;
        }

        private void LateUpdate()
        {
            // Yok edilmiş (Destroy) hedefleri listeden ayıkla
            targets.RemoveAll(t => t == null);

            if (targets.Count == 0 || cam == null)
                return;

            ApplyProjectionSettings();

            UpdateCameraTransform();

            if (constrainTargetsToScreen && targets.Count >= 2)
                ConstrainTargetsInsideView();
        }

        private void UpdateCameraTransform()
        {
            float distance = GetGreatestDistance();

            float targetHalfHeight = Mathf.Lerp(
                minZoom,
                maxZoom,
                distance / Mathf.Max(0.01f, zoomLimiter)
            );

            currentViewHalfHeight = Mathf.SmoothDamp(
                currentViewHalfHeight,
                targetHalfHeight,
                ref zoomVelocity,
                smoothTime
            );

            Vector3 centerPoint = GetCenterPoint();

            float targetX = Mathf.Clamp(
                centerPoint.x + offset.x,
                minX,
                maxX
            );

            float targetY = centerPoint.y + offset.y;
            float targetZ;

            if (usePerspective)
            {
                float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float depth = currentViewHalfHeight / Mathf.Max(0.01f, Mathf.Tan(halfFovRad));

                // Kameranın hafif eğiminden (pitch) dolayı odak noktasını kaçırmaması için Y telafisi
                float pitchRad = pitchAngle * Mathf.Deg2Rad;
                targetY += Mathf.Sin(pitchRad) * depth * 0.35f;
                targetZ = centerPoint.z - depth;
            }
            else
            {
                cam.orthographicSize = currentViewHalfHeight;
                targetZ = centerPoint.z + offset.z;
            }

            Vector3 targetPosition = new Vector3(targetX, targetY, targetZ);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );

            Quaternion targetRotation = usePerspective
                ? Quaternion.Euler(pitchAngle, 0f, 0f)
                : Quaternion.identity;

            transform.rotation = targetRotation;
        }

        private void ConstrainTargetsInsideView()
        {
            float halfWidth = GetVisibleHalfWidthAtZ(0f) - screenEdgePadding;

            if (halfWidth <= 0.5f)
                return;

            float camX = transform.position.x;
            float leftLimit = camX - halfWidth;
            float rightLimit = camX + halfWidth;

            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null)
                    continue;

                Vector3 pos = target.position;
                float clampedX = Mathf.Clamp(pos.x, leftLimit, rightLimit);

                if (Mathf.Abs(clampedX - pos.x) > 0.0001f)
                {
                    pos.x = clampedX;
                    target.position = pos;
                }
            }
        }

        public float GetVisibleHalfHeightAtZ(float worldZ)
        {
            if (cam == null)
                return CurrentViewHalfHeight;

            if (cam.orthographic)
                return cam.orthographicSize;

            float distance = Mathf.Abs(worldZ - transform.position.z);
            float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            return distance * Mathf.Tan(halfFovRad);
        }

        public float GetVisibleHalfWidthAtZ(float worldZ)
        {
            float aspect = cam != null ? cam.aspect : (float)Screen.width / Mathf.Max(1, Screen.height);
            return GetVisibleHalfHeightAtZ(worldZ) * aspect;
        }

        private Vector3 GetCenterPoint()
        {
            if (targets.Count == 1)
                return targets[0].position;

            Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
            for (int i = 1; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    bounds.Encapsulate(targets[i].position);
                }
            }
            return bounds.center;
        }

        private float GetGreatestDistance()
        {
            if (targets.Count < 2 || targets[0] == null || targets[1] == null)
                return 0f;

            return Mathf.Abs(targets[0].position.x - targets[1].position.x);
        }

        public void SnapToTargets()
        {
            if (targets.Count == 0 || cam == null)
                return;

            velocity = Vector3.zero;
            zoomVelocity = 0f;

            float distance = GetGreatestDistance();
            currentViewHalfHeight = Mathf.Lerp(
                minZoom,
                maxZoom,
                distance / Mathf.Max(0.01f, zoomLimiter)
            );

            Vector3 centerPoint = GetCenterPoint();
            float targetX = Mathf.Clamp(centerPoint.x + offset.x, minX, maxX);
            float targetY = centerPoint.y + offset.y;
            float targetZ;

            if (usePerspective)
            {
                float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float depth = currentViewHalfHeight / Mathf.Max(0.01f, Mathf.Tan(halfFovRad));
                float pitchRad = pitchAngle * Mathf.Deg2Rad;
                targetY += Mathf.Sin(pitchRad) * depth * 0.35f;
                targetZ = centerPoint.z - depth;
            }
            else
            {
                cam.orthographicSize = currentViewHalfHeight;
                targetZ = centerPoint.z + offset.z;
            }

            transform.position = new Vector3(targetX, targetY, targetZ);
            transform.rotation = usePerspective
                ? Quaternion.Euler(pitchAngle, 0f, 0f)
                : Quaternion.identity;
        }

        public void ClearTargets()
        {
            targets.Clear();
            velocity = Vector3.zero;
            zoomVelocity = 0f;
        }

        public void SetTargets(Transform p1, Transform p2)
        {
            targets.Clear();
            if (p1 != null) targets.Add(p1);
            if (p2 != null) targets.Add(p2);

            SnapToTargets();
        }
    }
}
