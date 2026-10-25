using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SmartParking
{
    public enum MinimapDisplayMode
    {
        Compact = 0,
        Expanded = 1,
        Hidden = 2
    }

    /// <summary>
    /// Real-time top-down minimap and navigation radar system.
    /// Captures a crisp orthographic feed above PlayerCar to a RenderTexture.
    /// Tracks vacant slots (green), occupied slots (red), and recommended parking target (cyan).
    /// Keyboard shortcut [M] cycles between Compact, Expanded, and Hidden.
    /// </summary>
    public class MinimapRadar : MonoBehaviour
    {
        public static MinimapRadar Instance { get; private set; }

        [Header("Minimap Display")]
        [SerializeField] private MinimapDisplayMode displayMode = MinimapDisplayMode.Compact;
        [SerializeField] private float orthographicSize = 22f;
        [SerializeField] private float cameraHeight = 35f;
        [SerializeField] private int resolution = 256;

        private Camera minimapCamera;
        private RenderTexture minimapTexture;
        private Transform playerCarTransform;

        public RenderTexture MinimapTexture => minimapTexture;
        public MinimapDisplayMode DisplayMode => displayMode;
        public bool IsVisible => displayMode != MinimapDisplayMode.Hidden;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateMinimapTexture();
            SetupMinimapCamera();
        }

        private void OnDestroy()
        {
            if (minimapTexture != null)
            {
                minimapTexture.Release();
                Destroy(minimapTexture);
            }
            if (Instance == this) Instance = null;
        }

        private void CreateMinimapTexture()
        {
            if (minimapTexture == null)
            {
                minimapTexture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32)
                {
                    name = "Minimap_RenderTexture",
                    filterMode = FilterMode.Bilinear
                };
                minimapTexture.Create();
            }
        }

        private void SetupMinimapCamera()
        {
            var go = GameObject.Find("Minimap_Camera");
            if (go == null)
            {
                go = new GameObject("Minimap_Camera");
                go.transform.SetParent(transform, false);
            }

            minimapCamera = go.GetComponent<Camera>();
            if (minimapCamera == null) minimapCamera = go.AddComponent<Camera>();

            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthographicSize;
            minimapCamera.nearClipPlane = 1.0f;
            minimapCamera.farClipPlane = 60f;
            minimapCamera.targetTexture = minimapTexture;
            minimapCamera.depth = -20;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Top-down
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.04f, 0.06f, 0.08f, 1.0f);
        }

        private void Update()
        {
            HandleInput();

            if (playerCarTransform == null)
            {
                var pc = GameObject.Find("PlayerCar");
                if (pc != null) playerCarTransform = pc.transform;
            }

            if (minimapCamera != null)
            {
                minimapCamera.enabled = (displayMode != MinimapDisplayMode.Hidden);

                if (playerCarTransform != null && minimapCamera.enabled)
                {
                    // Center orthographic view on PlayerCar horizontal coordinates
                    Vector3 carPos = playerCarTransform.position;
                    minimapCamera.transform.position = new Vector3(carPos.x, carPos.y + cameraHeight, carPos.z);
                    minimapCamera.orthographicSize = (displayMode == MinimapDisplayMode.Expanded) ? 38f : orthographicSize;
                }
            }
        }

        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame)
            {
                CycleDisplayMode();
            }
#endif
        }

        public void CycleDisplayMode()
        {
            int next = ((int)displayMode + 1) % 3;
            displayMode = (MinimapDisplayMode)next;
        }

        public void SetDisplayMode(MinimapDisplayMode mode)
        {
            displayMode = mode;
        }
    }
}
