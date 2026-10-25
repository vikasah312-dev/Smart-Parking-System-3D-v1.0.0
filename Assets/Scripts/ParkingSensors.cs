using System;
using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// Ultrasonic parking radar and proximity sensor system.
    /// Employs 6 sensor raycasts (3 front, 3 rear) to detect obstacles (pillars, walls, curbs, parked cars).
    /// Provides real-time distance telemetry and generates procedural audio proximity warning beeps.
    /// </summary>
    [RequireComponent(typeof(PlayerCarController))]
    public class ParkingSensors : MonoBehaviour
    {
        public static ParkingSensors Instance { get; private set; }

        [Header("Sensor Configuration")]
        [SerializeField] private float maxDetectionRange = 3.5f;
        [SerializeField] private float sensorHeight = 0.45f;
        [SerializeField] private float bumperOffset = 2.15f;
        [SerializeField] private float sideOffset = 0.75f;
        [SerializeField] private LayerMask obstacleLayerMask = ~0;

        [Header("Real-Time Telemetry")]
        [SerializeField] private float frontDistance = 3.5f;
        [SerializeField] private float rearDistance = 3.5f;
        [SerializeField] private float minDistance = 3.5f;
        [SerializeField] private string closestObstacleName = "None";

        [Header("Audio Alert Settings")]
        [SerializeField] private bool enableAudioBeeps = true;
        [Range(0f, 1f)]
        [SerializeField] private float beepVolume = 0.35f;

        private PlayerCarController carController;
        private AudioSource audioSource;
        private float nextBeepTime;
        private bool isBeeping;
        private float continuousToneTimer;

        // DSP synthesis state for clean procedural sonar beeps
        private float sampleRate = 48000f;
        private double beepPhase;
        private float currentToneFrequency = 1200f; // 1.2 kHz clean piezo transducer tone

        public float FrontDistance => frontDistance;
        public float RearDistance => rearDistance;
        public float MinDistance => minDistance;
        public string ClosestObstacleName => closestObstacleName;
        public bool IsBeeping => isBeeping;

        public bool EnableAudio
        {
            get => enableAudioBeeps;
            set => enableAudioBeeps = value;
        }

        private void Awake()
        {
            Instance = this;
            carController = GetComponent<PlayerCarController>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate < 8000f) sampleRate = 48000f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            ScanSensors();
            UpdateAudioAlerts();
        }

        private void ScanSensors()
        {
            Transform tr = transform;
            Vector3 fwd = tr.forward;
            Vector3 right = tr.right;
            Vector3 up = tr.up * sensorHeight;
            Vector3 rootPos = tr.position;

            // 3 Front Sensor origins
            Vector3 frontCenter = rootPos + fwd * bumperOffset + up;
            Vector3 frontLeft = frontCenter - right * sideOffset;
            Vector3 frontRight = frontCenter + right * sideOffset;

            // 3 Rear Sensor origins
            Vector3 rearCenter = rootPos - fwd * bumperOffset + up;
            Vector3 rearLeft = rearCenter - right * sideOffset;
            Vector3 rearRight = rearCenter + right * sideOffset;

            frontDistance = maxDetectionRange;
            rearDistance = maxDetectionRange;
            closestObstacleName = "Clear";

            // Raycast Front
            CastSensor(frontCenter, fwd, ref frontDistance);
            CastSensor(frontLeft, (fwd - right * 0.25f).normalized, ref frontDistance);
            CastSensor(frontRight, (fwd + right * 0.25f).normalized, ref frontDistance);

            // Raycast Rear
            CastSensor(rearCenter, -fwd, ref rearDistance);
            CastSensor(rearLeft, (-fwd - right * 0.25f).normalized, ref rearDistance);
            CastSensor(rearRight, (-fwd + right * 0.25f).normalized, ref rearDistance);

            minDistance = Mathf.Min(frontDistance, rearDistance);
        }

        private void CastSensor(Vector3 origin, Vector3 direction, ref float currentMinDist)
        {
            if (Physics.SphereCast(origin, 0.12f, direction, out RaycastHit hit, maxDetectionRange, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root != transform.root)
                {
                    if (hit.distance < currentMinDist)
                    {
                        currentMinDist = hit.distance;
                        closestObstacleName = hit.transform.name;
                    }
                }
            }
        }

        private void UpdateAudioAlerts()
        {
            if (!enableAudioBeeps || carController == null)
            {
                isBeeping = false;
                return;
            }

            // Determine relevant distance based on travel direction
            float relevantDist = maxDetectionRange;
            if (carController.CurrentGear == VehicleGear.R)
            {
                relevantDist = rearDistance;
            }
            else if (carController.CurrentSpeedKmh > 0.5f || carController.ThrottleInput > 0.05f)
            {
                relevantDist = frontDistance;
            }

            // Thresholds: Warning begins at 2.0m
            if (relevantDist <= 2.0f)
            {
                if (relevantDist <= 0.38f)
                {
                    // Continuous warning tone
                    isBeeping = true;
                    currentToneFrequency = 1450f;
                }
                else
                {
                    // Intermittent beep: interval scales from 0.55s down to 0.10s
                    float t = Mathf.InverseLerp(0.38f, 2.0f, relevantDist);
                    float interval = Mathf.Lerp(0.09f, 0.55f, t);
                    currentToneFrequency = Mathf.Lerp(1300f, 950f, t);

                    if (Time.time >= nextBeepTime)
                    {
                        isBeeping = true;
                        continuousToneTimer = 0.065f; // beep pulse length
                        nextBeepTime = Time.time + interval;
                    }
                    else if (continuousToneTimer > 0f)
                    {
                        continuousToneTimer -= Time.deltaTime;
                        if (continuousToneTimer <= 0f)
                        {
                            isBeeping = false;
                        }
                    }
                }
            }
            else
            {
                isBeeping = false;
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!enableAudioBeeps || !isBeeping || beepVolume <= 0.001f)
            {
                return; // pass through
            }

            double freqStep = currentToneFrequency / sampleRate;
            float vol = beepVolume * 0.35f;

            for (int i = 0; i < data.Length; i += channels)
            {
                beepPhase += freqStep;
                if (beepPhase > 1.0) beepPhase -= 1.0;

                // Soft sine tone pulse
                float sample = (float)Math.Sin(2.0 * Math.PI * beepPhase) * vol;

                for (int c = 0; c < channels; c++)
                {
                    // Additive mix with engine sound
                    data[i + c] += sample;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform tr = transform;
            Vector3 fwd = tr.forward;
            Vector3 right = tr.right;
            Vector3 up = tr.up * sensorHeight;
            Vector3 rootPos = tr.position;

            Gizmos.color = frontDistance < 1.0f ? Color.red : (frontDistance < 2.0f ? Color.yellow : Color.green);
            Gizmos.DrawRay(rootPos + fwd * bumperOffset + up, fwd * frontDistance);

            Gizmos.color = rearDistance < 1.0f ? Color.red : (rearDistance < 2.0f ? Color.yellow : Color.green);
            Gizmos.DrawRay(rootPos - fwd * bumperOffset + up, -fwd * rearDistance);
        }
    }
}
