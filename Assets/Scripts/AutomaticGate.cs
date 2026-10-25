using System.Collections;
using UnityEngine;

namespace SmartParking
{
    public enum GateType
    {
        Entry,
        Exit
    }

    /// <summary>
    /// Controls automatic entry and exit barrier gates.
    /// Detects approaching vehicles, smoothly animates the barrier arm up/down,
    /// and ensures secure traffic control.
    /// </summary>
    public class AutomaticGate : MonoBehaviour
    {
        [Header("Gate Configuration")]
        [SerializeField] private GateType gateType = GateType.Entry;
        [SerializeField] private Transform hingeTransform;
        [SerializeField] private float openAngle = -90f; // -90 for entry, +90 for exit
        [SerializeField] private float animationDuration = 0.85f;
        [SerializeField] private float autoCloseDelay = 2.0f;
        [SerializeField] private float detectionRadius = 9.0f;

        [Header("Telemetry")]
        [SerializeField] private bool isOpen;
        [SerializeField] private bool isAnimating;
        [SerializeField] private float currentAngle;
        [SerializeField] private string gateStatus = "CLOSED";

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine activeAnimation;
        private float lastCarSeenTime;
        private Transform trackedVehicle;

        public GateType Type => gateType;
        public bool IsOpen => isOpen;
        public string GateStatus => gateStatus;

        public void Initialize(GateType type, Transform hinge, float angle)
        {
            gateType = type;
            hingeTransform = hinge;
            openAngle = angle;

            if (hingeTransform != null)
            {
                closedRotation = hingeTransform.rotation;
                // Open rotation is closedRotation rotated around world X by openAngle
                openRotation = Quaternion.AngleAxis(openAngle, Vector3.right) * closedRotation;
            }
        }

        private void Start()
        {
            if (hingeTransform == null)
            {
                hingeTransform = transform;
            }
            closedRotation = hingeTransform.rotation;
            openRotation = Quaternion.AngleAxis(openAngle, Vector3.right) * closedRotation;
        }

        private void Update()
        {
            CheckVehicleProximity();
        }

        private void CheckVehicleProximity()
        {
            if (trackedVehicle == null)
            {
                var pc = GameObject.Find("PlayerCar");
                if (pc != null) trackedVehicle = pc.transform;
            }

            if (trackedVehicle == null) return;

            Vector3 hingePos = hingeTransform != null ? hingeTransform.position : transform.position;
            float dist = Vector3.Distance(trackedVehicle.position, hingePos);

            // Check if car is in approach zone
            // For entry gate (hinge at X=36.32, Z=-5.09): approach is from X > 36 towards west
            // For exit gate (hinge at X=36.32, Z=+1.31): approach is from X < 36 towards east
            bool carNearby = dist <= detectionRadius;

            if (carNearby)
            {
                lastCarSeenTime = Time.time;
                if (!isOpen && !isAnimating)
                {
                    OpenGate();
                }
            }
            else
            {
                // Car has left detection zone
                if (isOpen && !isAnimating && (Time.time - lastCarSeenTime) >= autoCloseDelay)
                {
                    CloseGate();
                }
            }
        }

        public void OpenGate()
        {
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(AnimateBarrier(true));
        }

        public void CloseGate()
        {
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(AnimateBarrier(false));
        }

        private IEnumerator AnimateBarrier(bool targetOpen)
        {
            isAnimating = true;
            gateStatus = targetOpen ? "OPENING..." : "CLOSING...";

            Quaternion startRot = hingeTransform.rotation;
            Quaternion targetRot = targetOpen ? openRotation : closedRotation;

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
                hingeTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            hingeTransform.rotation = targetRot;
            isOpen = targetOpen;
            isAnimating = false;
            gateStatus = isOpen ? "OPEN - PROCEED" : "CLOSED";
            activeAnimation = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isOpen ? Color.green : Color.yellow;
            Vector3 pos = hingeTransform != null ? hingeTransform.position : transform.position;
            Gizmos.DrawWireSphere(pos, detectionRadius);
        }
    }
}
