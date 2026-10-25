using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SmartParking
{
    public enum SimulationUIState
    {
        Running,
        Paused,
        ControlsModal,
        MainMenu,
        ChallengeSelection,
        ChallengeResults,
        SettingsModal
    }

    /// <summary>
    /// Professional simulation UI suite for Smart Parking System 3D.
    /// Features:
    /// - Comprehensive Main Menu, Pause Menu, Challenge Selection & Results screens.
    /// - Live Occupancy & Facility Telemetry.
    /// - Digital Speedometer, Gear Indicator (P, R, N, D), and Headlights indicator.
    /// - Reversing Backup Camera picture-in-picture viewport with dynamic guide lines.
    /// - Ultrasonic Parking Radar proximity distance indicators and stop alerts.
    /// - Top-down Minimap Radar display (toggleable with [M]).
    /// - Challenge Mode HUD (Mission timer, live score, collision counter).
    /// - Animated Parking Success & Vacated toast notifications.
    /// - Resolution-adaptive scaling across 1280x720 and 1920x1080 without element overlap.
    /// </summary>
    public class ParkingDashboardUI : MonoBehaviour
    {
        public static ParkingDashboardUI Instance { get; private set; }

        [Header("State")]
        [SerializeField] private SimulationUIState uiState = SimulationUIState.Running;
        [SerializeField] private bool showDashboard = true;
        [SerializeField] private bool isCollapsed = false;

        [Header("Appearance & Theme")]
        [SerializeField] private Color primaryColor = new Color(0.0f, 0.9f, 1.0f);          // Neon Cyan
        [SerializeField] private Color availableColor = new Color(0.1f, 0.95f, 0.4f);        // Bright Green
        [SerializeField] private Color occupiedColor = new Color(1.0f, 0.25f, 0.25f);        // Coral Red
        [SerializeField] private Color warningColor = new Color(1.0f, 0.85f, 0.15f);        // Yellow
        [SerializeField] private Color panelBgColor = new Color(0.05f, 0.07f, 0.11f, 0.88f); // Translucent Dark Glass
        [SerializeField] private Color chipBgColor = new Color(0.10f, 0.14f, 0.20f, 0.90f);

        [Header("Parking Notification State")]
        [SerializeField] private bool showParkingNotification;
        [SerializeField] private string notificationTitle = "PARKING SUCCESSFUL";
        [SerializeField] private string notificationSubtitle = "Space L0-A02 Secured";
        [SerializeField] private float notificationDuration = 5.0f;
        private float notificationExpiryTime;

        // Challenge Results Cache
        private ChallengeResult currentResult;

        // Settings Cache
        private float masterVolume = 0.5f;
        private bool soundFxEnabled = true;

        // GUI Styles
        private GUIStyle panelBoxStyle;
        private GUIStyle chipBoxStyle;
        private GUIStyle titleStyle;
        private GUIStyle subTitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle cameraButtonStyle;
        private GUIStyle cameraButtonActiveStyle;
        private GUIStyle hintStyle;
        private GUIStyle menuButtonStyle;
        private GUIStyle modalHeaderStyle;
        private GUIStyle toastBoxStyle;
        private GUIStyle speedNumberStyle;
        private GUIStyle sensorWarningStyle;

        private Texture2D panelTex;
        private Texture2D chipTex;
        private Texture2D greenTex;
        private Texture2D redTex;
        private Texture2D yellowTex;
        private Texture2D cyanTex;
        private Texture2D darkBarTex;
        private Texture2D toastTex;
        private Texture2D guideLineTex;

        private int lastScreenW;
        private int lastScreenH;

        public SimulationUIState CurrentUIState => uiState;
        public bool IsCollapsed => isCollapsed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            CreateTextures();
            ParkingSlot.OnPlayerParked += HandlePlayerParked;
            ParkingSlot.OnPlayerExited += HandlePlayerExited;
            ParkingChallengeManager.OnChallengeCompleted += HandleChallengeCompleted;
        }

        private void OnDisable()
        {
            ParkingSlot.OnPlayerParked -= HandlePlayerParked;
            ParkingSlot.OnPlayerExited -= HandlePlayerExited;
            ParkingChallengeManager.OnChallengeCompleted -= HandleChallengeCompleted;
        }

        private void HandlePlayerParked(ParkingSlot slot)
        {
            showParkingNotification = true;
            notificationTitle = "✔ PARKING SUCCESSFUL!";
            notificationSubtitle = string.Format("Space {0} Occupied • Floor {1}", slot.SlotId, slot.FloorLevel);
            notificationExpiryTime = Time.time + notificationDuration;
        }

        private void HandlePlayerExited(ParkingSlot slot)
        {
            showParkingNotification = true;
            notificationTitle = "➔ SPACE VACATED";
            notificationSubtitle = string.Format("Space {0} is now Available • Drive Safely", slot.SlotId);
            notificationExpiryTime = Time.time + 3.5f;
        }

        private void HandleChallengeCompleted(ChallengeResult result)
        {
            currentResult = result;
            uiState = SimulationUIState.ChallengeResults;
            Time.timeScale = 0f;
        }

        private void CreateTextures()
        {
            if (panelTex == null)
            {
                panelTex = MakeColorTex(panelBgColor);
                chipTex = MakeColorTex(chipBgColor);
                greenTex = MakeColorTex(availableColor);
                redTex = MakeColorTex(occupiedColor);
                yellowTex = MakeColorTex(warningColor);
                cyanTex = MakeColorTex(primaryColor);
                darkBarTex = MakeColorTex(new Color(0.08f, 0.10f, 0.14f, 1.0f));
                toastTex = MakeColorTex(new Color(0.04f, 0.18f, 0.12f, 0.94f));
                guideLineTex = MakeColorTex(new Color(0.0f, 0.9f, 1.0f, 0.85f));
            }
        }

        private Texture2D MakeColorTex(Color col)
        {
            Texture2D tex = new Texture2D(2, 2);
            Color[] pix = new Color[4];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            HandleKeyInputs();

            if (showParkingNotification && Time.time >= notificationExpiryTime)
            {
                showParkingNotification = false;
            }
        }

        private void HandleKeyInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (uiState == SimulationUIState.Running)
                    {
                        PauseSimulation();
                    }
                    else if (uiState == SimulationUIState.Paused || uiState == SimulationUIState.ControlsModal || uiState == SimulationUIState.SettingsModal)
                    {
                        ResumeSimulation();
                    }
                }

                if (kb.hKey.wasPressedThisFrame && uiState == SimulationUIState.Running)
                {
                    isCollapsed = !isCollapsed;
                }
            }
#endif
        }

        public void SetUIState(SimulationUIState newState)
        {
            uiState = newState;
            Time.timeScale = (newState == SimulationUIState.Running) ? 1f : 0f;
        }

        public void PauseSimulation() => SetUIState(SimulationUIState.Paused);
        public void ResumeSimulation() => SetUIState(SimulationUIState.Running);

        public void RestartSimulation()
        {
            Time.timeScale = 1f;
            uiState = SimulationUIState.Running;

            var cm = ParkingChallengeManager.Instance;
            if (cm != null && cm.IsChallengeActive)
            {
                cm.RetryCurrentChallenge();
                return;
            }

            var pc = GameObject.Find("PlayerCar");
            if (pc != null)
            {
                var rb = pc.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = new Vector3(45.0f, 0.05f, -1.89f);
                    rb.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                pc.transform.position = new Vector3(45.0f, 0.05f, -1.89f);
                pc.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            var follow = Camera.main != null ? Camera.main.GetComponent<CarFollowCamera>() : null;
            if (follow != null) follow.SnapToTarget();

            var camMgr = ParkingCameraManager.Instance;
            if (camMgr != null) camMgr.SetCameraMode(CameraViewMode.ThirdPerson);

            showParkingNotification = false;
        }

        private void InitStyles(float scale)
        {
            if (panelBoxStyle != null && lastScreenW == Screen.width && lastScreenH == Screen.height)
                return;

            lastScreenW = Screen.width;
            lastScreenH = Screen.height;

            panelBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(Mathf.RoundToInt(8 * scale), Mathf.RoundToInt(8 * scale),
                                         Mathf.RoundToInt(6 * scale), Mathf.RoundToInt(6 * scale))
            };
            panelBoxStyle.normal.background = panelTex;

            toastBoxStyle = new GUIStyle(panelBoxStyle);
            toastBoxStyle.normal.background = toastTex;

            chipBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(Mathf.RoundToInt(5 * scale), Mathf.RoundToInt(5 * scale),
                                         Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale)),
                margin = new RectOffset(Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale),
                                        Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale))
            };
            chipBoxStyle.normal.background = chipTex;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = primaryColor;

            subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(9 * scale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleRight
            };
            subTitleStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(10 * scale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = Color.white;

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(10 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            valueStyle.normal.textColor = primaryColor;

            speedNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(17 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            speedNumberStyle.normal.textColor = primaryColor;

            sensorWarningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(10 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            sensorWarningStyle.normal.textColor = warningColor;

            cameraButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(9 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(Mathf.RoundToInt(4 * scale), Mathf.RoundToInt(4 * scale),
                                         Mathf.RoundToInt(2 * scale), Mathf.RoundToInt(2 * scale))
            };
            cameraButtonStyle.normal.background = chipTex;
            cameraButtonStyle.normal.textColor = Color.white;

            cameraButtonActiveStyle = new GUIStyle(cameraButtonStyle);
            cameraButtonActiveStyle.normal.background = cyanTex;
            cameraButtonActiveStyle.normal.textColor = Color.black;

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(9 * scale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            hintStyle.normal.textColor = new Color(0.85f, 0.92f, 1.0f);

            menuButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(13 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            menuButtonStyle.normal.background = chipTex;
            menuButtonStyle.normal.textColor = Color.white;
            menuButtonStyle.hover.textColor = primaryColor;

            modalHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(17 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            modalHeaderStyle.normal.textColor = primaryColor;
        }

        private void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.height / 720f, 0.70f, 1.30f);
            InitStyles(scale);

            switch (uiState)
            {
                case SimulationUIState.MainMenu:
                    DrawMainMenu(scale);
                    break;
                case SimulationUIState.ChallengeSelection:
                    DrawChallengeSelection(scale);
                    break;
                case SimulationUIState.ChallengeResults:
                    DrawChallengeResults(scale);
                    break;
                case SimulationUIState.Paused:
                    DrawRunningHUD(scale);
                    DrawPauseMenu(scale);
                    break;
                case SimulationUIState.ControlsModal:
                    DrawRunningHUD(scale);
                    DrawControlsModal(scale);
                    break;
                case SimulationUIState.SettingsModal:
                    DrawRunningHUD(scale);
                    DrawSettingsModal(scale);
                    break;
                case SimulationUIState.Running:
                default:
                    DrawRunningHUD(scale);
                    break;
            }
        }

        private void DrawRunningHUD(float scale)
        {
            if (showDashboard && !isCollapsed)
            {
                DrawTelemetryPanel(scale);
                DrawSpeedAndSensorCluster(scale);
                DrawMinimapRadar(scale);
                DrawReverseBackupMonitor(scale);
                DrawChallengeBanner(scale);
            }

            DrawCameraBar(scale);
            DrawControlsHint(scale);

            if (showParkingNotification)
            {
                DrawParkingToast(scale);
            }
        }

        private void DrawTelemetryPanel(float scale)
        {
            var pm = ParkingManager.Instance;
            if (pm == null) return;

            float panelW = Mathf.Min(340f * scale, Screen.width * 0.36f);
            float panelH = 246f * scale;
            float margin = 12f * scale;

            Rect panelRect = new Rect(margin, margin, panelW, panelH);
            GUI.Box(panelRect, GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(margin + 6, margin + 4, panelW - 12, panelH - 8));

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("SMART PARKING 3D", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("URP SIMULATION", subTitleStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // Metrics row
            GUILayout.BeginHorizontal();
            DrawStatChip("TOTAL", pm.TotalSlots.ToString(), Color.white, scale);
            DrawStatChip("OCCUPIED", pm.OccupiedSlots.ToString(), occupiedColor, scale);
            DrawStatChip("AVAILABLE", pm.AvailableSlots.ToString(), availableColor, scale);
            DrawStatChip("RATE", string.Format("{0:F0}%", pm.OccupancyRate * 100f), primaryColor, scale);
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // Floor breakdown
            DrawFloorBar("L0 Ground", pm.GetFloorOccupied(0), pm.GetFloorTotal(0), scale);
            DrawFloorBar("L1 Mid-Deck", pm.GetFloorOccupied(1), pm.GetFloorTotal(1), scale);
            DrawFloorBar("L2 Rooftop", pm.GetFloorOccupied(2), pm.GetFloorTotal(2), scale);

            GUILayout.Space(2f);

            // Guidance Row
            var nearest = pm.NearestAvailableSlot;
            GUILayout.BeginHorizontal(chipBoxStyle);
            if (nearest != null)
            {
                string text = string.Format("GUIDANCE: Bay <color=#00E5FF><b>{0}</b></color> (Floor {1})", nearest.SlotId, nearest.FloorLevel);
                GUILayout.Label(text, labelStyle);
            }
            else
            {
                GUILayout.Label("<color=#FF5252>● FACILITY AT CAPACITY</color>", labelStyle);
            }
            GUILayout.EndHorizontal();

            // Gates
            var entryGate = GameObject.Find("EntryGate")?.GetComponent<AutomaticGate>();
            var exitGate = GameObject.Find("ExitGate")?.GetComponent<AutomaticGate>();
            GUILayout.BeginHorizontal();
            string entryStatus = entryGate != null && entryGate.IsOpen ? "<color=#1DE9B6>OPEN</color>" : "<color=#FF5252>CLOSED</color>";
            string exitStatus = exitGate != null && exitGate.IsOpen ? "<color=#1DE9B6>OPEN</color>" : "<color=#FF5252>CLOSED</color>";
            GUILayout.Label(string.Format("Gates: Entry [{0}] · Exit [{1}]", entryStatus, exitStatus), labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawStatChip(string title, string val, Color col, float scale)
        {
            GUILayout.BeginVertical(chipBoxStyle, GUILayout.MinWidth(50 * scale));
            GUIStyle sTitle = new GUIStyle(labelStyle) { fontSize = Mathf.RoundToInt(8 * scale), alignment = TextAnchor.MiddleCenter };
            GUIStyle sVal = new GUIStyle(valueStyle) { fontSize = Mathf.RoundToInt(13 * scale), alignment = TextAnchor.MiddleCenter };
            sVal.normal.textColor = col;

            GUILayout.Label(title, sTitle);
            GUILayout.Label(val, sVal);
            GUILayout.EndVertical();
        }

        private void DrawFloorBar(string name, int occ, int total, float scale)
        {
            if (total <= 0) return;
            float ratio = Mathf.Clamp01((float)occ / total);

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, labelStyle, GUILayout.Width(75 * scale));

            Rect r = GUILayoutUtility.GetRect(80 * scale, 12 * scale, GUILayout.ExpandWidth(true));
            GUI.Box(r, GUIContent.none, chipBoxStyle);

            Rect fillR = new Rect(r.x, r.y, r.width * ratio, r.height);
            Color barCol = Color.Lerp(availableColor, occupiedColor, ratio);
            GUI.color = barCol;
            GUI.DrawTexture(fillR, redTex);
            GUI.color = Color.white;

            string countStr = string.Format("{0}/{1}", occ, total);
            GUILayout.Label(countStr, valueStyle, GUILayout.Width(45 * scale));
            GUILayout.EndHorizontal();
        }

        private void DrawSpeedAndSensorCluster(float scale)
        {
            var controller = PlayerCarController.Instance;
            var sensors = ParkingSensors.Instance;
            if (controller == null) return;

            float margin = 12f * scale;
            float clusterW = 200f * scale;
            float clusterH = 88f * scale;
            float clusterY = 265f * scale;

            Rect r = new Rect(margin, clusterY, clusterW, clusterH);
            GUI.Box(r, GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(margin + 6, clusterY + 4, clusterW - 12, clusterH - 8));

            // Speedometer & Gear Row
            GUILayout.BeginHorizontal();
            string gearText = string.Format("GEAR: <color=#00E5FF><b>[{0}]</b></color>", controller.CurrentGear);
            GUILayout.Label(gearText, labelStyle);
            GUILayout.FlexibleSpace();
            string speedText = string.Format("<b>{0:F0}</b> <size={1}>KM/H</size>",
                Mathf.Abs(controller.CurrentSpeedKmh), Mathf.RoundToInt(10 * scale));
            GUILayout.Label(speedText, speedNumberStyle);
            GUILayout.EndHorizontal();

            // Headlights & Ground Status
            GUILayout.BeginHorizontal();
            string lightStr = controller.HeadlightsEnabled ? "<color=#00E5FF>ON</color>" : "<color=#888888>OFF</color>";
            GUILayout.Label(string.Format("Lights: [{0}] (L)", lightStr), labelStyle);
            GUILayout.FlexibleSpace();
            string gndStr = controller.IsGrounded ? "<color=#1DE9B6>GND</color>" : "<color=#FF5252>AIR</color>";
            GUILayout.Label(gndStr, labelStyle);
            GUILayout.EndHorizontal();

            // Proximity Radar Sensor Micro-Bar
            if (sensors != null)
            {
                GUILayout.BeginHorizontal();
                float dist = (controller.CurrentGear == VehicleGear.R) ? sensors.RearDistance : sensors.FrontDistance;
                string label = (controller.CurrentGear == VehicleGear.R) ? "REAR" : "FRONT";
                Color sCol = (dist < 0.6f) ? occupiedColor : (dist < 1.5f ? warningColor : availableColor);

                string sensorText = (dist >= 3.0f) ?
                    string.Format("Radar: {0} [CLEAR]", label) :
                    string.Format("Radar: {0} <color=#{1}><b>{2:F1}m</b></color>", label, ColorUtility.ToHtmlStringRGB(sCol), dist);

                GUILayout.Label(sensorText, labelStyle);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void DrawMinimapRadar(float scale)
        {
            var radar = MinimapRadar.Instance;
            if (radar == null || !radar.IsVisible || radar.MinimapTexture == null) return;

            float mapSize = (radar.DisplayMode == MinimapDisplayMode.Expanded) ? 220f * scale : 160f * scale;
            float margin = 12f * scale;
            float mapX = Screen.width - mapSize - margin;
            float mapY = 48f * scale;

            Rect mapRect = new Rect(mapX, mapY, mapSize, mapSize);
            GUI.Box(mapRect, GUIContent.none, panelBoxStyle);

            // Draw Top-down render texture
            Rect innerRect = new Rect(mapX + 4, mapY + 4, mapSize - 8, mapSize - 8);
            GUI.DrawTexture(innerRect, radar.MinimapTexture, ScaleMode.ScaleToFit);

            // Minimap Header / Shortcut Hint
            Rect headerR = new Rect(mapX, mapY - (18f * scale), mapSize, 16f * scale);
            GUI.Label(headerR, "MINIMAP [M]", hintStyle);
        }

        private void DrawReverseBackupMonitor(float scale)
        {
            var controller = PlayerCarController.Instance;
            var revMgr = ReverseCameraManager.Instance;
            if (controller == null || revMgr == null || !revMgr.IsActive || revMgr.BackupTexture == null)
                return;

            float camW = 280f * scale;
            float camH = 158f * scale;
            float camX = (Screen.width - camW) * 0.5f;
            float camY = 16f * scale;

            Rect bgRect = new Rect(camX, camY, camW, camH);
            GUI.Box(bgRect, GUIContent.none, panelBoxStyle);

            Rect texRect = new Rect(camX + 4, camY + 4, camW - 8, camH - 8);
            GUI.DrawTexture(texRect, revMgr.BackupTexture, ScaleMode.StretchToFill);

            // Dynamic Parking Trajectory Guide Overlay Lines
            DrawTrajectoryGuideLines(texRect);

            // Proximity Sensor Overlay on Camera
            var sensors = ParkingSensors.Instance;
            if (sensors != null && sensors.RearDistance < 2.5f)
            {
                Color c = (sensors.RearDistance < 0.6f) ? occupiedColor : warningColor;
                string stopText = sensors.RearDistance < 0.45f ? "● STOP ●" : string.Format("REAR: {0:F1} M", sensors.RearDistance);
                Rect alertRect = new Rect(camX, camY + camH - (24f * scale), camW, 20f * scale);
                GUI.color = c;
                GUI.Label(alertRect, stopText, sensorWarningStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawTrajectoryGuideLines(Rect r)
        {
            // Left track line
            GUI.color = new Color(0.1f, 0.95f, 0.4f, 0.85f);
            GUI.DrawTexture(new Rect(r.x + r.width * 0.22f, r.y + r.height * 0.40f, 3f, r.height * 0.55f), guideLineTex);
            // Right track line
            GUI.DrawTexture(new Rect(r.x + r.width * 0.78f, r.y + r.height * 0.40f, 3f, r.height * 0.55f), guideLineTex);

            // Safe horizontal line (green)
            GUI.DrawTexture(new Rect(r.x + r.width * 0.24f, r.y + r.height * 0.62f, r.width * 0.52f, 2f), greenTex);

            // Caution horizontal line (yellow)
            GUI.color = warningColor;
            GUI.DrawTexture(new Rect(r.x + r.width * 0.26f, r.y + r.height * 0.78f, r.width * 0.48f, 2.5f), yellowTex);

            // Stop line (red)
            GUI.color = occupiedColor;
            GUI.DrawTexture(new Rect(r.x + r.width * 0.28f, r.y + r.height * 0.90f, r.width * 0.44f, 3f), redTex);

            GUI.color = Color.white;
        }

        private void DrawChallengeBanner(float scale)
        {
            var cm = ParkingChallengeManager.Instance;
            if (cm == null || !cm.IsChallengeActive) return;

            float bannerW = 320f * scale;
            float bannerH = 42f * scale;
            float bannerX = (Screen.width - bannerW) * 0.5f;
            float bannerY = Screen.height - bannerH - (38f * scale);

            Rect r = new Rect(bannerX, bannerY, bannerW, bannerH);
            GUI.Box(r, GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(bannerX + 6, bannerY + 4, bannerW - 12, bannerH - 8));
            GUILayout.BeginHorizontal();

            var challenge = cm.AllChallenges[cm.CurrentChallengeIndex];
            string timeStr = string.Format("TIME: <color=#00E5FF><b>{0:F1}s</b></color>", cm.ElapsedTime);
            GUILayout.Label(timeStr, labelStyle);

            GUILayout.FlexibleSpace();

            int col = PlayerCarController.Instance != null ? PlayerCarController.Instance.CollisionCount : 0;
            string colStr = col > 0 ? string.Format("BUMPS: <color=#FF5252><b>{0}</b></color>", col) : "BUMPS: <color=#1DE9B6>0</color>";
            GUILayout.Label(colStr, labelStyle);

            GUILayout.FlexibleSpace();

            string scoreStr = string.Format("SCORE: <color=#00E5FF><b>{0}</b></color>", cm.LiveScore);
            GUILayout.Label(scoreStr, valueStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawCameraBar(float scale)
        {
            var cm = ParkingCameraManager.Instance;
            if (cm == null) cm = FindFirstObjectByType<ParkingCameraManager>();
            if (cm == null) return;

            float barW = Mathf.Clamp(315f * scale, 270f, 390f);
            float barH = 30f * scale;
            float margin = 12f * scale;
            float barX = Screen.width - barW - margin;
            float barY = margin;

            Rect r = new Rect(barX, barY, barW, barH);
            GUI.Box(r, GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(barX + 4, barY + 3, barW - 8, barH - 6));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Chase [1]", cm.CurrentMode == CameraViewMode.ThirdPerson ? cameraButtonActiveStyle : cameraButtonStyle))
                cm.SetCameraMode(CameraViewMode.ThirdPerson);

            if (GUILayout.Button("Top [2]", cm.CurrentMode == CameraViewMode.TopDownOverview ? cameraButtonActiveStyle : cameraButtonStyle))
                cm.SetCameraMode(CameraViewMode.TopDownOverview);

            if (GUILayout.Button("Gate [3]", cm.CurrentMode == CameraViewMode.EntranceGate ? cameraButtonActiveStyle : cameraButtonStyle))
                cm.SetCameraMode(CameraViewMode.EntranceGate);

            if (GUILayout.Button("Slot [4]", cm.CurrentMode == CameraViewMode.NearestSlotGuidance ? cameraButtonActiveStyle : cameraButtonStyle))
                cm.SetCameraMode(CameraViewMode.NearestSlotGuidance);

            if (GUILayout.Button("Orbit [5]", cm.CurrentMode == CameraViewMode.CinematicExterior ? cameraButtonActiveStyle : cameraButtonStyle))
                cm.SetCameraMode(CameraViewMode.CinematicExterior);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawControlsHint(float scale)
        {
            float hintW = Mathf.Clamp(Screen.width * 0.62f, 380f, 680f);
            float hintH = 22f * scale;
            float hintX = (Screen.width - hintW) * 0.5f;
            float hintY = Screen.height - hintH - (8f * scale);

            Rect r = new Rect(hintX, hintY, hintW, hintH);
            GUI.Box(r, GUIContent.none, panelBoxStyle);

            GUI.Label(new Rect(hintX, hintY, hintW, hintH),
                "<b>[W/A/S/D]</b> Drive  •  <b>[Space]</b> Brake  •  <b>[L]</b> Lights  •  <b>[M]</b> Minimap  •  <b>[C / 1-5]</b> Cameras  •  <b>[Esc]</b> Menu  •  <b>[H]</b> HUD",
                hintStyle);
        }

        private void DrawParkingToast(float scale)
        {
            float toastW = Mathf.Clamp(380f * scale, 300f, 480f);
            float toastH = 46f * scale;
            float toastX = (Screen.width - toastW) * 0.5f;
            float toastY = 16f * scale;

            Rect r = new Rect(toastX, toastY, toastW, toastH);
            GUI.Box(r, GUIContent.none, toastBoxStyle);

            GUILayout.BeginArea(new Rect(toastX + 6, toastY + 4, toastW - 12, toastH - 8));
            GUILayout.Label(string.Format("<color=#1DE9B6><b>{0}</b></color>", notificationTitle), modalHeaderStyle);
            GUILayout.Label(string.Format("<color=#E0F2F1>{0}</color>", notificationSubtitle), hintStyle);
            GUILayout.EndArea();
        }

        private void DrawMainMenu(float scale)
        {
            float menuW = Mathf.Clamp(420f * scale, 320f, 520f);
            float menuH = Mathf.Clamp(380f * scale, 300f, 460f);
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBoxStyle); // Dark full overlay
            GUI.Box(new Rect(menuX, menuY, menuW, menuH), GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(menuX + 20, menuY + 16, menuW - 40, menuH - 32));

            GUILayout.Label("SMART PARKING SYSTEM 3D", modalHeaderStyle);
            GUILayout.Label("<color=#A0B0C0>URP Autonomous Parking Facility Simulation</color>", hintStyle);
            GUILayout.Space(14f * scale);

            if (GUILayout.Button("FREE ROAM SIMULATION", menuButtonStyle, GUILayout.Height(40 * scale)))
            {
                SetUIState(SimulationUIState.Running);
            }

            GUILayout.Space(8f * scale);
            if (GUILayout.Button("PARKING CHALLENGES", menuButtonStyle, GUILayout.Height(40 * scale)))
            {
                SetUIState(SimulationUIState.ChallengeSelection);
            }

            GUILayout.Space(8f * scale);
            if (GUILayout.Button("CONTROLS & HOW TO PLAY", menuButtonStyle, GUILayout.Height(40 * scale)))
            {
                SetUIState(SimulationUIState.ControlsModal);
            }

            GUILayout.Space(8f * scale);
            if (GUILayout.Button("SETTINGS", menuButtonStyle, GUILayout.Height(40 * scale)))
            {
                SetUIState(SimulationUIState.SettingsModal);
            }

            GUILayout.Space(8f * scale);
            if (GUILayout.Button("EXIT SIMULATION", menuButtonStyle, GUILayout.Height(36 * scale)))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }

            GUILayout.EndArea();
        }

        private void DrawChallengeSelection(float scale)
        {
            float menuW = Mathf.Clamp(500f * scale, 380f, 620f);
            float menuH = Mathf.Clamp(400f * scale, 320f, 480f);
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBoxStyle);
            GUI.Box(new Rect(menuX, menuY, menuW, menuH), GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(menuX + 18, menuY + 14, menuW - 36, menuH - 28));

            GUILayout.Label("SELECT PARKING MISSION", modalHeaderStyle);
            GUILayout.Space(10f * scale);

            var cm = ParkingChallengeManager.Instance;
            if (cm != null)
            {
                for (int i = 0; i < cm.AllChallenges.Count; i++)
                {
                    var ch = cm.AllChallenges[i];
                    GUILayout.BeginVertical(chipBoxStyle);
                    GUILayout.Label(string.Format("<b>{0}</b> (Par Time: {1:F0}s)", ch.title, ch.parTimeSeconds), titleStyle);
                    GUILayout.Label(ch.description, labelStyle);

                    if (GUILayout.Button("START MISSION", menuButtonStyle, GUILayout.Height(28 * scale)))
                    {
                        cm.StartChallenge(i);
                        SetUIState(SimulationUIState.Running);
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(4f * scale);
                }
            }

            GUILayout.Space(8f * scale);
            if (GUILayout.Button("BACK TO MAIN MENU", menuButtonStyle, GUILayout.Height(32 * scale)))
            {
                SetUIState(SimulationUIState.MainMenu);
            }

            GUILayout.EndArea();
        }

        private void DrawChallengeResults(float scale)
        {
            float menuW = Mathf.Clamp(420f * scale, 320f, 520f);
            float menuH = Mathf.Clamp(360f * scale, 280f, 440f);
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBoxStyle);
            GUI.Box(new Rect(menuX, menuY, menuW, menuH), GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(menuX + 20, menuY + 16, menuW - 40, menuH - 32));

            GUILayout.Label("MISSION COMPLETED!", modalHeaderStyle);

            // Stars
            string starStr = (currentResult.starsEarned == 3) ? "★★★" : (currentResult.starsEarned == 2 ? "★★☆" : "★☆☆");
            GUIStyle starStyle = new GUIStyle(modalHeaderStyle) { fontSize = Mathf.RoundToInt(24 * scale) };
            starStyle.normal.textColor = warningColor;
            GUILayout.Label(starStr, starStyle);

            GUILayout.Space(8f * scale);
            GUILayout.Label(string.Format("Mission: <color=#00E5FF>{0}</color>", currentResult.challengeTitle), labelStyle);
            GUILayout.Label(string.Format("Time: <b>{0:F1}s</b> (Par: {1:F0}s)", currentResult.timeElapsed, currentResult.parTime), labelStyle);
            GUILayout.Label(string.Format("Collisions: <b>{0}</b>", currentResult.collisionCount), labelStyle);
            GUILayout.Label(string.Format("Final Score: <color=#00E5FF><b>{0} pts</b></color> (Grade: {1})", currentResult.finalScore, currentResult.grade), titleStyle);

            GUILayout.Space(12f * scale);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("RETRY", menuButtonStyle, GUILayout.Height(34 * scale)))
            {
                var cm = ParkingChallengeManager.Instance;
                if (cm != null) cm.RetryCurrentChallenge();
                SetUIState(SimulationUIState.Running);
            }
            if (GUILayout.Button("NEXT MISSION", menuButtonStyle, GUILayout.Height(34 * scale)))
            {
                var cm = ParkingChallengeManager.Instance;
                if (cm != null) cm.StartNextChallenge();
                SetUIState(SimulationUIState.Running);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f * scale);
            if (GUILayout.Button("MAIN MENU", menuButtonStyle, GUILayout.Height(32 * scale)))
            {
                SetUIState(SimulationUIState.MainMenu);
            }

            GUILayout.EndArea();
        }

        private void DrawPauseMenu(float scale)
        {
            float menuW = Mathf.Clamp(360f * scale, 300f, 460f);
            float menuH = Mathf.Clamp(320f * scale, 260f, 400f);
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBoxStyle);
            GUI.Box(new Rect(menuX, menuY, menuW, menuH), GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(menuX + 16, menuY + 14, menuW - 32, menuH - 28));

            GUILayout.Label("SIMULATION PAUSED", modalHeaderStyle);
            GUILayout.Space(12f * scale);

            if (GUILayout.Button("RESUME [Esc]", menuButtonStyle, GUILayout.Height(36 * scale)))
                ResumeSimulation();

            GUILayout.Space(6f * scale);
            if (GUILayout.Button("RESTART VEHICLE", menuButtonStyle, GUILayout.Height(36 * scale)))
                RestartSimulation();

            GUILayout.Space(6f * scale);
            if (GUILayout.Button("CONTROLS GUIDE", menuButtonStyle, GUILayout.Height(36 * scale)))
                SetUIState(SimulationUIState.ControlsModal);

            GUILayout.Space(6f * scale);
            if (GUILayout.Button("MAIN MENU", menuButtonStyle, GUILayout.Height(36 * scale)))
                SetUIState(SimulationUIState.MainMenu);

            GUILayout.EndArea();
        }

        private void DrawControlsModal(float scale)
        {
            float menuW = Mathf.Clamp(460f * scale, 340f, 560f);
            float menuH = Mathf.Clamp(360f * scale, 280f, 440f);
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBoxStyle);
            GUI.Box(new Rect(menuX, menuY, menuW, menuH), GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(menuX + 16, menuY + 14, menuW - 32, menuH - 28));

            GUILayout.Label("HOW TO PLAY & CONTROLS", modalHeaderStyle);
            GUILayout.Space(10f * scale);

            GUILayout.Label("<b>W / Up Arrow:</b> Accelerate / Drive (D)", labelStyle);
            GUILayout.Label("<b>S / Down Arrow:</b> Reverse (R) / Footbrake", labelStyle);
            GUILayout.Label("<b>A / D / Left / Right:</b> Steer Front Wheels", labelStyle);
            GUILayout.Label("<b>Spacebar:</b> Handbrake (P)", labelStyle);
            GUILayout.Label("<b>L:</b> Toggle Headlights (Spotlights + Emissive)", labelStyle);
            GUILayout.Label("<b>M:</b> Toggle Minimap Radar (Compact / Expanded / Off)", labelStyle);
            GUILayout.Label("<b>C / [1-5]:</b> Switch Camera View Modes", labelStyle);
            GUILayout.Label("<b>H:</b> Toggle Dashboard Telemetry HUD", labelStyle);
            GUILayout.Label("<b>Esc:</b> Pause / Menu", labelStyle);

            GUILayout.Space(12f * scale);
            if (GUILayout.Button("CLOSE", menuButtonStyle, GUILayout.Height(34 * scale)))
            {
                SetUIState(SimulationUIState.Running);
            }

            GUILayout.EndArea();
        }

        private void DrawSettingsModal(float scale)
        {
            float menuW = Mathf.Clamp(420f * scale, 320f, 520f);
            float menuH = Mathf.Clamp(320f * scale, 260f, 400f);
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBoxStyle);
            GUI.Box(new Rect(menuX, menuY, menuW, menuH), GUIContent.none, panelBoxStyle);

            GUILayout.BeginArea(new Rect(menuX + 16, menuY + 14, menuW - 32, menuH - 28));

            GUILayout.Label("SIMULATION SETTINGS", modalHeaderStyle);
            GUILayout.Space(12f * scale);

            // Volume Slider
            GUILayout.Label(string.Format("Master Audio Volume: <b>{0:F0}%</b>", masterVolume * 100f), labelStyle);
            float newVol = GUILayout.HorizontalSlider(masterVolume, 0f, 1f);
            if (Math.Abs(newVol - masterVolume) > 0.01f)
            {
                masterVolume = newVol;
                if (EngineAudioSynthesizer.Instance != null) EngineAudioSynthesizer.Instance.MasterVolume = masterVolume;
            }

            GUILayout.Space(10f * scale);

            // Proximity Audio Alert Toggle
            var sensors = ParkingSensors.Instance;
            if (sensors != null)
            {
                bool audioToggle = GUILayout.Toggle(sensors.EnableAudio, " Ultrasonic Radar Audio Alerts", labelStyle);
                sensors.EnableAudio = audioToggle;
            }

            GUILayout.Space(10f * scale);

            // Minimap Toggle
            var radar = MinimapRadar.Instance;
            if (radar != null)
            {
                bool miniToggle = GUILayout.Toggle(radar.IsVisible, " Enable Top-Down Navigation Minimap", labelStyle);
                radar.SetDisplayMode(miniToggle ? MinimapDisplayMode.Compact : MinimapDisplayMode.Hidden);
            }

            GUILayout.Space(16f * scale);
            if (GUILayout.Button("APPLY & RETURN", menuButtonStyle, GUILayout.Height(34 * scale)))
            {
                SetUIState(SimulationUIState.MainMenu);
            }

            GUILayout.EndArea();
        }
    }
}
