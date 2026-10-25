using System;
using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// Procedural real-time DSP engine and vehicular audio synthesizer for PlayerCar.
    /// Synthesizes dynamic 4-cylinder engine pitch, combustion harmonics,
    /// gear-shift mechanical clicks, and collision impact thumps without external audio files.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EngineAudioSynthesizer : MonoBehaviour
    {
        public static EngineAudioSynthesizer Instance { get; private set; }

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 0.45f;
        [SerializeField] private bool muteAudio = false;

        [Header("Engine RPM & Pitch")]
        [SerializeField] private float idleFrequency = 55f;    // ~800 RPM
        [SerializeField] private float maxFrequency = 240f;    // ~5500 RPM
        [SerializeField] private float smoothingSpeed = 8f;

        private AudioSource audioSource;
        private PlayerCarController carController;
        private float currentFrequency = 55f;
        private double phase;
        private double phase2;
        private double phase3;
        private float sampleRate = 48000f;

        // Mechanical & Impact Audio Synthesis State
        private VehicleGear previousGear = VehicleGear.P;
        private float shiftClickTimer = 0f;
        private float impactThumpTimer = 0f;
        private float impactThumpIntensity = 0f;
        private double impactPhase;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public bool MuteAudio
        {
            get => muteAudio;
            set => muteAudio = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = true;
            audioSource.loop = true;
            audioSource.spatialBlend = 0.5f; // Semi-spatial
            audioSource.volume = 1f;

            carController = GetComponent<PlayerCarController>();
            sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate < 8000f) sampleRate = 48000f;
        }

        private void OnEnable()
        {
            PlayerCarController.OnVehicleCollision += HandleCollisionImpact;
        }

        private void OnDisable()
        {
            PlayerCarController.OnVehicleCollision -= HandleCollisionImpact;
        }

        private void Start()
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void HandleCollisionImpact(float impactForce)
        {
            impactThumpTimer = 0.22f;
            impactThumpIntensity = Mathf.Clamp01(impactForce / 8.0f);
        }

        private void Update()
        {
            if (carController == null)
            {
                carController = GetComponent<PlayerCarController>();
                return;
            }

            // Detect gear shifts for mechanical notch click
            if (carController.CurrentGear != previousGear)
            {
                shiftClickTimer = 0.05f;
                previousGear = carController.CurrentGear;
            }

            if (shiftClickTimer > 0f) shiftClickTimer -= Time.deltaTime;
            if (impactThumpTimer > 0f) impactThumpTimer -= Time.deltaTime;

            float speedRatio = Mathf.Clamp01(Mathf.Abs(carController.CurrentSpeedKmh) / 60f);
            float throttle = Mathf.Clamp01(Mathf.Abs(carController.ThrottleInput));

            // Target frequency combines forward speed and active throttle load
            float targetFreq = idleFrequency + (maxFrequency - idleFrequency) * (speedRatio * 0.75f + throttle * 0.25f);
            currentFrequency = Mathf.Lerp(currentFrequency, targetFreq, Time.deltaTime * smoothingSpeed);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (muteAudio || masterVolume <= 0.001f)
            {
                for (int i = 0; i < data.Length; i++) data[i] = 0f;
                return;
            }

            double freqStep = currentFrequency / sampleRate;
            double freqStep2 = (currentFrequency * 2.0) / sampleRate;
            double freqStep3 = (currentFrequency * 4.0) / sampleRate;

            float vol = masterVolume * 0.25f;

            for (int i = 0; i < data.Length; i += channels)
            {
                phase += freqStep;
                if (phase > 1.0) phase -= 1.0;

                phase2 += freqStep2;
                if (phase2 > 1.0) phase2 -= 1.0;

                phase3 += freqStep3;
                if (phase3 > 1.0) phase3 -= 1.0;

                // Triangular-sawtooth waveform for deep exhaust rumble
                float wave1 = (float)(2.0 * Math.Abs(2.0 * phase - 1.0) - 1.0);
                float wave2 = (float)Math.Sin(2.0 * Math.PI * phase2) * 0.5f;
                float wave3 = (float)Math.Sin(2.0 * Math.PI * phase3) * 0.25f;

                float sample = (wave1 * 0.6f + wave2 * 0.3f + wave3 * 0.1f) * vol;

                // Shift click acoustic pop
                if (shiftClickTimer > 0f)
                {
                    float clickNoise = ((float)new System.Random().NextDouble() * 2f - 1f) * 0.15f * masterVolume;
                    sample += clickNoise;
                }

                // Collision impact thump (low frequency damped sine boom)
                if (impactThumpTimer > 0f)
                {
                    impactPhase += 65.0 / sampleRate; // 65 Hz bass boom
                    if (impactPhase > 1.0) impactPhase -= 1.0;
                    float thump = (float)Math.Sin(2.0 * Math.PI * impactPhase) * impactThumpIntensity * masterVolume * 0.6f;
                    sample += thump;
                }

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = sample;
                }
            }
        }
    }
}
