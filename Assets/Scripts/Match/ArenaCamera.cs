using UnityEngine;
using System.Collections.Generic;

namespace GameProject.Runtime.Manager
{
    [RequireComponent(typeof(Camera))]
    public class ArenaCamera : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private List<Transform> targets = new List<Transform>();

        [Header("Positioning & Limits")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
        [SerializeField] private float minX = -4f;
        [SerializeField] private float maxX = 4f;

        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 7f;
        [SerializeField] private float zoomLimiter = 10f;
        [SerializeField] private float smoothTime = 0.3f;

        private Camera cam;
        private Vector3 velocity;
        private float zoomVelocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (targets.Count == 0 || cam == null)
                return;

            MoveCamera();
            ZoomCamera();
        }

        private void MoveCamera()
        {
            Vector3 centerPoint = GetCenterPoint();
            Vector3 targetPosition = centerPoint + offset;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        private void ZoomCamera()
        {
            // İki oyuncu arasındaki mesafeye göre yeni zoom değerini hesapla
            float newZoom = Mathf.Lerp(minZoom, maxZoom, GetGreatestDistance() / zoomLimiter);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, newZoom, ref zoomVelocity, smoothTime);
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

            Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
            bounds.Encapsulate(targets[1].position);
            return bounds.size.x;
        }

        public void SetTargets(Transform p1, Transform p2)
        {
            targets.Clear();
            if (p1 != null) targets.Add(p1);
            if (p2 != null) targets.Add(p2);
        }
    }
}