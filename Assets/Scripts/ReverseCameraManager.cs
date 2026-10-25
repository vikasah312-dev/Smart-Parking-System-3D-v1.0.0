using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// Automatic Rear-View Backup Camera system.
    /// Spawns an in-vehicle wide-angle backup camera over the rear bumper that renders to a RenderTexture.
    /// Automatically activates when PlayerCar shifts into Reverse [R] gear, conserving GPU resources when not in use.
    /// </summary>
    public class ReverseCameraManager : MonoBehaviour
    {
        public static ReverseCameraManager Instance { get; private set; }

        [Header("Camera Settings")]
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 1.20f, -2.15f);
        [SerializeField] private Vector3 cameraLocalRotation = new Vector3(14f, 180f, 0f);
        [SerializeField] private float fieldOfView = 75f;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 288;

        [Header("State")]
        [SerializeField] private bool autoActivateInReverse = true;
        [SerializeField] private bool forceActivate = false;

        private Camera reverseCamera;
        private RenderTexture backupTexture;
        private PlayerCarController carController;

        public RenderTexture BackupTexture => backupTexture;
        public bool IsActive => reverseCamera != null && reverseCamera.enabled;
        public bool AutoActivateInReverse
        {
            get => autoActivateInReverse;
            set => autoActivateInReverse = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateBackupTexture();
            SetupCamera();
        }

        private void OnDestroy()
        {
            if (backupTexture != null)
            {
                backupTexture.Release();
                Destroy(backupTexture);
            }
            if (Instance == this) Instance = null;
        }

        private void CreateBackupTexture()
        {
            if (backupTexture == null)
            {
                backupTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32)
                {
                    name = "ReverseCamera_RenderTexture",
                    filterMode = FilterMode.Bilinear,
                    antiAliasing = 2
                };
                backupTexture.Create();
            }
        }

        private void SetupCamera()
        {
            var pc = GameObject.Find("PlayerCar");
            if (pc == null) return;

            carController = pc.GetComponent<PlayerCarController>();

            var camGo = pc.transform.Find("ReverseBackupCamera");
            if (camGo == null)
            {
                var newGo = new GameObject("ReverseBackupCamera");
                newGo.transform.SetParent(pc.transform, false);
                newGo.transform.localPosition = cameraLocalPosition;
                newGo.transform.localRotation = Quaternion.Euler(cameraLocalRotation);
                camGo = newGo.transform;
            }

            reverseCamera = camGo.GetComponent<Camera>();
            if (reverseCamera == null) reverseCamera = camGo.gameObject.AddComponent<Camera>();

            reverseCamera.targetTexture = backupTexture;
            reverseCamera.fieldOfView = fieldOfView;
            reverseCamera.nearClipPlane = 0.15f;
            reverseCamera.farClipPlane = 45f;
            reverseCamera.depth = -10; // lower than main camera
            reverseCamera.enabled = false; // Disabled until reverse gear engaged
        }

        private void Update()
        {
            if (carController == null)
            {
                var pc = GameObject.Find("PlayerCar");
                if (pc != null) carController = pc.GetComponent<PlayerCarController>();
            }

            if (reverseCamera == null)
            {
                SetupCamera();
            }

            if (reverseCamera != null && carController != null)
            {
                bool shouldBeActive = forceActivate || (autoActivateInReverse && carController.CurrentGear == VehicleGear.R);
                if (reverseCamera.enabled != shouldBeActive)
                {
                    reverseCamera.enabled = shouldBeActive;
                }
            }
        }

        public void SetForceActivate(bool force)
        {
            forceActivate = force;
            if (reverseCamera != null)
            {
                reverseCamera.enabled = force || (carController != null && carController.CurrentGear == VehicleGear.R);
            }
        }
    }
}
