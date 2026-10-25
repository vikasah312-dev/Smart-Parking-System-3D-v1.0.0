using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// 3D Animated guidance waypoint that hovers above the nearest recommended parking slot.
    /// Rotates and bobs gently to provide clear visual orientation from anywhere in the facility.
    /// </summary>
    public class GuidanceMarker : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private Vector3 offsetAboveSlot = new Vector3(0f, 2.8f, 0f);

        private Vector3 basePosition;
        private Renderer markerRenderer;

        private void Start()
        {
            markerRenderer = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            var pm = ParkingManager.Instance;
            if (pm == null) return;

            var targetSlot = pm.NearestAvailableSlot;
            if (targetSlot != null)
            {
                if (markerRenderer != null) markerRenderer.enabled = true;

                basePosition = targetSlot.SlotCenter + offsetAboveSlot;
                float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = basePosition + Vector3.up * bobOffset;

                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                if (markerRenderer != null) markerRenderer.enabled = false;
            }
        }
    }
}
