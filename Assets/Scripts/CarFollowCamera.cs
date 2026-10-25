using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// Smooth third-person chase camera for the drivable PlayerCar.
    /// Features:
    /// - Damped position and rotation tracking.
    /// - Predictive look-ahead and pitch stabilization.
    /// - Smart obstacle collision avoidance (anti-clipping) via SphereCast against pillars and ceilings.
    /// </summary>
    public class CarFollowCamera : MonoBehaviour
    {
        [Header("Follow Target")]
        [Tooltip("The vehicle root transform to follow.")]
        [SerializeField] private Transform target;

        [Header("Camera Offsets")]
        [Tooltip("Distance behind the target car.")]
        [SerializeField] private float distance = 5.8f;

        [Tooltip("Height above the target car.")]
        [SerializeField] private float height = 2.1f;

        [Tooltip("Target look-at offset above vehicle root.")]
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.85f, 0f);

        [Header("Smoothing")]
        [Tooltip("Positional interpolation speed.")]
        [SerializeField] private float positionDamping = 6.0f;

        [Tooltip("Rotational interpolation speed.")]
        [SerializeField] private float rotationDamping = 4.5f;

        [Header("Collision & Anti-Clipping")]
        [Tooltip("Radius of the collision sphere cast.")]
        [SerializeField] private float collisionRadius = 0.25f;

        [Tooltip("Layers to check for camera obstruction.")]
        [SerializeField] private LayerMask obstacleLayerMask = ~0;

        [Tooltip("Minimum allowable distance when camera is pushed forward by obstacle.")]
        [SerializeField] private float minDistance = 1.8f;

        private Vector3 currentVelocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetLookPoint = target.position + lookAtOffset;
            Vector3 targetForward = target.forward;

            // Compute ideal position behind vehicle
            Vector3 idealPosition = target.position - (targetForward * distance) + (Vector3.up * height);

            // Obstacle collision avoidance: check line of sight from vehicle to camera
            Vector3 fromTargetToCam = idealPosition - targetLookPoint;
            float maxDist = fromTargetToCam.magnitude;

            if (maxDist > 0.01f)
            {
                Vector3 castDir = fromTargetToCam / maxDist;
                if (Physics.SphereCast(targetLookPoint, collisionRadius, castDir, out RaycastHit hit, maxDist, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // Avoid colliding with PlayerCar itself
                    if (!hit.transform.IsChildOf(target) && hit.transform != target)
                    {
                        float adjustedDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, maxDist);
                        idealPosition = targetLookPoint + castDir * adjustedDist;
                    }
                }
            }

            // Smooth position interpolation
            transform.position = Vector3.Lerp(transform.position, idealPosition, Time.deltaTime * positionDamping);

            // Smooth rotation towards look target
            Vector3 lookDir = (targetLookPoint - transform.position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationDamping);
            }
        }

        /// <summary>
        /// Snaps camera immediately to ideal behind-vehicle position without lerp (useful on reset/spawn).
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 targetLookPoint = target.position + lookAtOffset;
            Vector3 idealPosition = target.position - (target.forward * distance) + (Vector3.up * height);

            Vector3 fromTargetToCam = idealPosition - targetLookPoint;
            float maxDist = fromTargetToCam.magnitude;
            if (maxDist > 0.01f)
            {
                Vector3 castDir = fromTargetToCam / maxDist;
                if (Physics.SphereCast(targetLookPoint, collisionRadius, castDir, out RaycastHit hit, maxDist, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(target) && hit.transform != target)
                    {
                        idealPosition = targetLookPoint + castDir * Mathf.Clamp(hit.distance - 0.15f, minDistance, maxDist);
                    }
                }
            }

            transform.position = idealPosition;
            Vector3 lookDir = (targetLookPoint - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }
    }
}
