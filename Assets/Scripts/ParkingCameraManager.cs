using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SmartParking
{
    public enum CameraViewMode
    {
        ThirdPerson = 0,
        TopDownOverview = 1,
        EntranceGate = 2,
        NearestSlotGuidance = 3,
        CinematicExterior = 4
    }

    /// <summary>
    /// Manages multi-camera switching:
    /// 1. Third-Person Chase View (with obstacle collision avoidance)
    /// 2. Top-Down Facility Overview (orthogonal top view)
    /// 3. Entrance Gate View (monitoring entry traffic)
    /// 4. Parking Guidance View (targeting the recommended slot)
    /// 5. Cinematic Exterior View (slowly orbiting showcase of the entire 3D facility)
    /// Shortcuts: [C] to cycle, [1]-[5] to select directly.
    /// </summary>
    public class ParkingCameraManager : MonoBehaviour
    {
        public static ParkingCameraManager Instance { get; private set; }

        [Header("Cameras")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera topDownCamera;
        [SerializeField] private Camera entranceCamera;

        [Header("Cinematic Orbit Settings")]
        [SerializeField] private Vector3 orbitCenter = new Vector3(7.3f, 6.0f, -1.9f);
        [SerializeField] private float orbitDistance = 48.0f;
        [SerializeField] private float orbitHeight = 22.0f;
        [SerializeField] private float orbitSpeed = 8.0f; // degrees per second

        [Header("State")]
        [SerializeField] private CameraViewMode currentMode = CameraViewMode.ThirdPerson;

        private CarFollowCamera followScript;
        private float currentOrbitAngle = 35f;

        public CameraViewMode CurrentMode => currentMode;
        public string CurrentModeName => currentMode.ToString();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            FindCameras();
        }

        private void Start()
        {
            FindCameras();
            ApplyCameraMode(currentMode);
        }

        private void FindCameras()
        {
            if (mainCamera == null)
            {
                var camGo = GameObject.Find("Main Camera");
                if (camGo != null) mainCamera = camGo.GetComponent<Camera>();
            }

            if (mainCamera != null)
            {
                followScript = mainCamera.GetComponent<CarFollowCamera>();
            }

            if (topDownCamera == null)
            {
                var topGo = GameObject.Find("Cam_Plan_Top");
                if (topGo == null) topGo = GameObject.Find("Cam_Overview");
                if (topGo != null) topDownCamera = topGo.GetComponent<Camera>();
            }

            if (entranceCamera == null)
            {
                var entGo = GameObject.Find("Cam_Entrance");
                if (entGo != null) entranceCamera = entGo.GetComponent<Camera>();
            }
        }

        private void Update()
        {
            HandleInputs();

            // Animate cinematic exterior orbit if active
            if (currentMode == CameraViewMode.CinematicExterior && mainCamera != null)
            {
                currentOrbitAngle += orbitSpeed * Time.deltaTime;
                float rad = currentOrbitAngle * Mathf.Deg2Rad;
                Vector3 orbitPos = orbitCenter + new Vector3(Mathf.Cos(rad) * orbitDistance, orbitHeight, Mathf.Sin(rad) * orbitDistance);

                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, orbitPos, Time.deltaTime * 3.0f);
                Vector3 lookDir = (orbitCenter - mainCamera.transform.position).normalized;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), Time.deltaTime * 3.0f);
                }
            }
        }

        private void HandleInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.cKey.wasPressedThisFrame)
                {
                    CycleCamera();
                }
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    SetCameraMode(CameraViewMode.ThirdPerson);
                }
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    SetCameraMode(CameraViewMode.TopDownOverview);
                }
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                {
                    SetCameraMode(CameraViewMode.EntranceGate);
                }
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                {
                    SetCameraMode(CameraViewMode.NearestSlotGuidance);
                }
                if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
                {
                    SetCameraMode(CameraViewMode.CinematicExterior);
                }
            }
#endif
        }

        public void CycleCamera()
        {
            int next = ((int)currentMode + 1) % 5;
            SetCameraMode((CameraViewMode)next);
        }

        public void SetCameraMode(CameraViewMode mode)
        {
            currentMode = mode;
            ApplyCameraMode(mode);
        }

        private void ApplyCameraMode(CameraViewMode mode)
        {
            FindCameras();

            // Set camera depths: active camera gets highest depth (100)
            bool mainActive = (mode == CameraViewMode.ThirdPerson || mode == CameraViewMode.NearestSlotGuidance || mode == CameraViewMode.CinematicExterior);
            if (mainCamera != null) mainCamera.depth = mainActive ? 100 : 0;
            if (topDownCamera != null) topDownCamera.depth = (mode == CameraViewMode.TopDownOverview) ? 100 : 0;
            if (entranceCamera != null) entranceCamera.depth = (mode == CameraViewMode.EntranceGate) ? 100 : 0;

            if (followScript != null)
            {
                followScript.enabled = (mode == CameraViewMode.ThirdPerson);
                if (mode == CameraViewMode.ThirdPerson)
                {
                    followScript.SnapToTarget();
                }
            }

            // If Guidance view, position Main Camera facing the nearest available slot
            if (mode == CameraViewMode.NearestSlotGuidance && ParkingManager.Instance != null && mainCamera != null)
            {
                var slot = ParkingManager.Instance.NearestAvailableSlot;
                if (slot != null)
                {
                    Vector3 slotPos = slot.SlotCenter;
                    mainCamera.transform.position = slotPos + new Vector3(0f, 4.0f, -6.0f);
                    mainCamera.transform.rotation = Quaternion.LookRotation((slotPos + Vector3.up * 0.5f - mainCamera.transform.position).normalized, Vector3.up);
                }
            }
            // If Cinematic Exterior view, place camera in orbit position
            else if (mode == CameraViewMode.CinematicExterior && mainCamera != null)
            {
                float rad = currentOrbitAngle * Mathf.Deg2Rad;
                Vector3 orbitPos = orbitCenter + new Vector3(Mathf.Cos(rad) * orbitDistance, orbitHeight, Mathf.Sin(rad) * orbitDistance);
                mainCamera.transform.position = orbitPos;
                mainCamera.transform.rotation = Quaternion.LookRotation((orbitCenter - orbitPos).normalized, Vector3.up);
            }
        }
    }
}
