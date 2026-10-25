using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SmartParking
{
    public enum VehicleGear
    {
        P = 0, // Park
        R = 1, // Reverse
        N = 2, // Neutral
        D = 3  // Drive
    }

    /// <summary>
    /// Professional physics-based vehicle controller for Smart Parking System.
    /// Features:
    /// - Smooth W/A/S/D acceleration, steering, reverse, and Space braking.
    /// - Automatic gear management (P, R, N, D).
    /// - Visual wheel rolling and front-wheel steering without mesh shearing.
    /// - Dynamic brake lights and reverse lights using vehicle meshes.
    /// - Anti-roll bar stabilization and aerodynamic downforce.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCarController : MonoBehaviour
    {
        public static PlayerCarController Instance { get; private set; }

        [Header("Engine & Speed")]
        [Tooltip("Drive force applied in Newtons.")]
        [SerializeField] private float motorForce = 16000f;

        [Tooltip("Top speed forward in km/h.")]
        [SerializeField] private float maxSpeedKmh = 60f;

        [Tooltip("Top speed reverse in km/h.")]
        [SerializeField] private float maxReverseSpeedKmh = 22f;

        [Tooltip("Deceleration force during regular braking.")]
        [SerializeField] private float brakeForce = 32000f;

        [Tooltip("Handbrake force applied when Space is pressed.")]
        [SerializeField] private float handbrakeForce = 50000f;

        [Header("Steering")]
        [Tooltip("Maximum steering angle in degrees.")]
        [SerializeField] private float maxSteerAngle = 32f;

        [Tooltip("Speed of steering response.")]
        [SerializeField] private float steerSpeed = 6f;

        [Header("Handling & Stability")]
        [Tooltip("Grip coefficient counteracting lateral tire slip.")]
        [SerializeField] private float lateralTireGrip = 22f;

        [Tooltip("Aerodynamic downforce coefficient.")]
        [SerializeField] private float downforceCoefficient = 40f;

        [Tooltip("Anti-roll bar stiffness to prevent body roll in sharp turns.")]
        [SerializeField] private float antiRollForce = 4000f;

        [Tooltip("Center of mass offset relative to vehicle root.")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.32f, 0.1f);

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.6f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Telemetry / Testing")]
        [SerializeField] private VehicleGear currentGear = VehicleGear.P;
        [SerializeField] private float throttleInput;
        [SerializeField] private float steerInput;
        [SerializeField] private bool brakeInput;
        [SerializeField] private bool manualInputOverride;
        [SerializeField] private float currentSpeedKmh;
        [SerializeField] private bool isGrounded;

        [Header("Visual Wheels & Lighting")]
        [SerializeField] private Transform wheelFrontLeft;
        [SerializeField] private Transform wheelFrontRight;
        [SerializeField] private Transform wheelRearLeft;
        [SerializeField] private Transform wheelRearRight;
        [SerializeField] private Renderer taillightLeft;
        [SerializeField] private Renderer taillightRight;
        [SerializeField] private Renderer headlightLeft;
        [SerializeField] private Renderer headlightRight;
        [SerializeField] private Light headlightSpotLeft;
        [SerializeField] private Light headlightSpotRight;
        [SerializeField] private bool headlightsEnabled = true;

        private Rigidbody rb;
        private float currentSteerAngle;
        private float currentSpeedMs;
        private float accumulatedRollAngle;
        private const float wheelRadius = 0.33f;
        private readonly Quaternion wheelBaseRot = Quaternion.Euler(89.98f, 0f, 0f);

        private Material taillightMatInstance;
        private Material headlightMatInstance;
        private int collisionCount = 0;
        private static readonly int emissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Color runningLightColor = new Color(0.45f, 0.04f, 0.04f) * 1.5f;
        private readonly Color brakeLightColor = new Color(1.0f, 0.08f, 0.08f) * 5.0f;
        private readonly Color reverseLightColor = new Color(1.0f, 0.98f, 0.88f) * 4.0f;
        private readonly Color headlightOnColor = new Color(0.95f, 0.98f, 1.0f) * 3.5f;

        public static event System.Action<float> OnVehicleCollision;
        public int CollisionCount => collisionCount;
        public bool HeadlightsEnabled => headlightsEnabled;
        public VehicleGear CurrentGear => currentGear;
        public float CurrentSpeedKmh => currentSpeedKmh;
        public float CurrentSpeedMs => currentSpeedMs;
        public bool IsGrounded => isGrounded;
        public float ThrottleInput => throttleInput;
        public float SteerInput => steerInput;
        public bool IsBraking => brakeInput;
        public Rigidbody VehicleRigidbody => rb;

        public void ResetCollisionCount() => collisionCount = 0;

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody>();
            rb.mass = 1300f;
            rb.linearDamping = 0.08f;
            rb.angularDamping = 2.8f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.centerOfMass = centerOfMassOffset;

            FindWheelAndLightReferences();
            SetupTaillightMaterials();
            SetupHeadlightMaterials();
            CreateHeadlightSpotlights();
            SetHeadlights(headlightsEnabled);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > 1.2f)
            {
                collisionCount++;
                OnVehicleCollision?.Invoke(impactForce);
            }
        }

        private void FindWheelAndLightReferences()
        {
            Transform vm = transform.Find("VisualModel");
            if (vm == null) return;

            if (wheelFrontLeft == null) wheelFrontLeft = vm.Find("CAR_L0-A01_w.003");
            if (wheelFrontRight == null) wheelFrontRight = vm.Find("CAR_L0-A01_w.002");
            if (wheelRearLeft == null) wheelRearLeft = vm.Find("CAR_L0-A01_w.001");
            if (wheelRearRight == null) wheelRearRight = vm.Find("CAR_L0-A01_w");

            if (taillightLeft == null)
            {
                var tlGo = vm.Find("CAR_L0-A01_tl");
                if (tlGo != null) taillightLeft = tlGo.GetComponent<Renderer>();
            }
            if (taillightRight == null)
            {
                var tl1Go = vm.Find("CAR_L0-A01_tl.001");
                if (tl1Go != null) taillightRight = tl1Go.GetComponent<Renderer>();
            }

            if (headlightLeft == null)
            {
                var hlGo = vm.Find("CAR_L0-A01_hl");
                if (hlGo != null) headlightLeft = hlGo.GetComponent<Renderer>();
            }
            if (headlightRight == null)
            {
                var hl1Go = vm.Find("CAR_L0-A01_hl.001");
                if (hl1Go != null) headlightRight = hl1Go.GetComponent<Renderer>();
            }
        }

        private void SetupTaillightMaterials()
        {
            if (taillightLeft != null && taillightLeft.sharedMaterial != null)
            {
                taillightMatInstance = new Material(taillightLeft.sharedMaterial) { name = "PlayerCar_Taillight_Instance" };
                taillightMatInstance.EnableKeyword("_EMISSION");
                taillightLeft.material = taillightMatInstance;

                if (taillightRight != null)
                {
                    taillightRight.material = taillightMatInstance;
                }
            }
        }

        private void SetupHeadlightMaterials()
        {
            if (headlightLeft != null && headlightLeft.sharedMaterial != null)
            {
                headlightMatInstance = new Material(headlightLeft.sharedMaterial) { name = "PlayerCar_Headlight_Instance" };
                headlightMatInstance.EnableKeyword("_EMISSION");
                headlightLeft.material = headlightMatInstance;

                if (headlightRight != null)
                {
                    headlightRight.material = headlightMatInstance;
                }
            }
        }

        private void CreateHeadlightSpotlights()
        {
            Transform vm = transform.Find("VisualModel");
            if (vm == null) return;

            if (headlightSpotLeft == null)
            {
                var spotLGo = new GameObject("Headlight_Spot_L");
                spotLGo.transform.SetParent(vm, false);
                spotLGo.transform.localPosition = new Vector3(-0.68f, 0.45f, 1.95f);
                spotLGo.transform.localRotation = Quaternion.Euler(6f, -3f, 0f);
                headlightSpotLeft = spotLGo.AddComponent<Light>();
                ConfigureHeadlightSpot(headlightSpotLeft);
            }

            if (headlightSpotRight == null)
            {
                var spotRGo = new GameObject("Headlight_Spot_R");
                spotRGo.transform.SetParent(vm, false);
                spotRGo.transform.localPosition = new Vector3(0.68f, 0.45f, 1.95f);
                spotRGo.transform.localRotation = Quaternion.Euler(6f, 3f, 0f);
                headlightSpotRight = spotRGo.AddComponent<Light>();
                ConfigureHeadlightSpot(headlightSpotRight);
            }
        }

        private void ConfigureHeadlightSpot(Light spot)
        {
            spot.type = LightType.Spot;
            spot.range = 28f;
            spot.spotAngle = 52f;
            spot.innerSpotAngle = 32f;
            spot.color = new Color(0.96f, 0.98f, 1.0f);
            spot.intensity = 2.2f;
            spot.shadows = LightShadows.Soft;
            spot.shadowBias = 0.05f;
        }

        public void ToggleHeadlights()
        {
            SetHeadlights(!headlightsEnabled);
        }

        public void SetHeadlights(bool enable)
        {
            headlightsEnabled = enable;

            if (headlightSpotLeft != null) headlightSpotLeft.enabled = enable;
            if (headlightSpotRight != null) headlightSpotRight.enabled = enable;

            if (headlightMatInstance != null)
            {
                if (enable)
                {
                    headlightMatInstance.SetColor(emissionColorId, headlightOnColor);
                }
                else
                {
                    headlightMatInstance.SetColor(emissionColorId, Color.black);
                }
            }
        }

        private void Update()
        {
            if (!manualInputOverride)
            {
                ReadInputs();
            }

            UpdateGears();
            UpdateWheelVisuals();
            UpdateTaillights();
        }

        private void ReadInputs()
        {
            float t = 0f;
            float s = 0f;
            bool b = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) t += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) t -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) s += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) s -= 1f;
                if (kb.spaceKey.isPressed) b = true;
                if (kb.lKey.wasPressedThisFrame) ToggleHeadlights();
            }
#endif

            // Smooth inputs for responsive feel
            throttleInput = Mathf.MoveTowards(throttleInput, t, Time.deltaTime * 5f);
            steerInput = Mathf.MoveTowards(steerInput, s, Time.deltaTime * steerSpeed);
            brakeInput = b;
        }

        private void UpdateGears()
        {
            float absSpeed = Mathf.Abs(currentSpeedKmh);

            if (brakeInput && absSpeed < 0.5f)
            {
                currentGear = VehicleGear.P;
            }
            else if (throttleInput < -0.1f || (currentSpeedMs < -0.3f && throttleInput <= 0f))
            {
                currentGear = VehicleGear.R;
            }
            else if (throttleInput > 0.1f || currentSpeedMs > 0.3f)
            {
                currentGear = VehicleGear.D;
            }
            else
            {
                currentGear = VehicleGear.N;
            }
        }

        private void UpdateWheelVisuals()
        {
            // Angular velocity around wheel axis: w = v / r
            float deltaRoll = (currentSpeedMs * Time.deltaTime / wheelRadius) * Mathf.Rad2Deg;
            accumulatedRollAngle = (accumulatedRollAngle + deltaRoll) % 360f;

            // Front wheels: roll around local Z, steer around local Y
            Quaternion frontLeftRot = wheelBaseRot * Quaternion.Euler(0f, currentSteerAngle, accumulatedRollAngle);
            Quaternion frontRightRot = wheelBaseRot * Quaternion.Euler(0f, currentSteerAngle, accumulatedRollAngle);

            // Rear wheels: roll only
            Quaternion rearLeftRot = wheelBaseRot * Quaternion.Euler(0f, 0f, accumulatedRollAngle);
            Quaternion rearRightRot = wheelBaseRot * Quaternion.Euler(0f, 0f, accumulatedRollAngle);

            if (wheelFrontLeft != null) wheelFrontLeft.localRotation = frontLeftRot;
            if (wheelFrontRight != null) wheelFrontRight.localRotation = frontRightRot;
            if (wheelRearLeft != null) wheelRearLeft.localRotation = rearLeftRot;
            if (wheelRearRight != null) wheelRearRight.localRotation = rearRightRot;
        }

        private void UpdateTaillights()
        {
            if (taillightMatInstance == null) return;

            bool isBrakingActive = brakeInput || (currentSpeedMs > 1.0f && throttleInput < -0.1f);
            bool isReversingActive = currentGear == VehicleGear.R;

            if (isBrakingActive)
            {
                taillightMatInstance.SetColor(emissionColorId, brakeLightColor);
            }
            else if (isReversingActive)
            {
                taillightMatInstance.SetColor(emissionColorId, reverseLightColor);
            }
            else
            {
                taillightMatInstance.SetColor(emissionColorId, runningLightColor);
            }
        }

        public void SetManualInput(float throttle, float steer, bool brake)
        {
            manualInputOverride = true;
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            steerInput = Mathf.Clamp(steer, -1f, 1f);
            brakeInput = brake;
        }

        public void ClearManualInput()
        {
            manualInputOverride = false;
        }

        private void FixedUpdate()
        {
            CheckGround();

            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            currentSpeedMs = localVel.z;
            currentSpeedKmh = currentSpeedMs * 3.6f;

            if (isGrounded)
            {
                ApplyDrive(localVel);
                ApplySteering(localVel);
                ApplyLateralFriction(localVel);
                ApplyDownforce();
                ApplyAntiRollStabilizer();
            }
        }

        private void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * 0.4f;
            isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.4f, groundLayerMask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyDrive(Vector3 localVel)
        {
            float maxFwdMs = maxSpeedKmh / 3.6f;
            float maxRevMs = maxReverseSpeedKmh / 3.6f;

            if (brakeInput)
            {
                if (Mathf.Abs(localVel.z) < 0.25f)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
                else
                {
                    float maxStopForce = (rb.mass * Mathf.Abs(localVel.z)) / Time.fixedDeltaTime;
                    float appliedForce = Mathf.Min(handbrakeForce, maxStopForce);
                    Vector3 brakeForceVector = -transform.forward * Mathf.Sign(localVel.z) * appliedForce;
                    rb.AddForce(brakeForceVector, ForceMode.Force);
                }
            }
            else if (Mathf.Abs(throttleInput) > 0.05f)
            {
                if (localVel.z > 0.8f && throttleInput < -0.1f)
                {
                    rb.AddForce(-transform.forward * brakeForce * Mathf.Abs(throttleInput), ForceMode.Force);
                }
                else if (localVel.z < -0.8f && throttleInput > 0.1f)
                {
                    rb.AddForce(transform.forward * brakeForce * throttleInput, ForceMode.Force);
                }
                else
                {
                    bool underMaxSpeed = (throttleInput > 0 && currentSpeedKmh < maxSpeedKmh) ||
                                         (throttleInput < 0 && currentSpeedKmh > -maxReverseSpeedKmh);

                    if (underMaxSpeed)
                    {
                        Vector3 driveDir = transform.forward;
                        rb.AddForce(driveDir * throttleInput * motorForce, ForceMode.Force);
                    }
                }
            }
            else
            {
                if (Mathf.Abs(localVel.z) < 0.10f)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
                else
                {
                    Vector3 rollingResistance = -transform.forward * localVel.z * (rb.mass * 2.5f);
                    rb.AddForce(rollingResistance, ForceMode.Force);
                }
            }

            // Cap speeds smoothly
            if (localVel.z > maxFwdMs)
            {
                rb.AddForce(-transform.forward * (localVel.z - maxFwdMs) * rb.mass * 6f, ForceMode.Force);
            }
            else if (localVel.z < -maxRevMs)
            {
                rb.AddForce(transform.forward * (-maxRevMs - localVel.z) * rb.mass * 6f, ForceMode.Force);
            }
        }

        private void ApplySteering(Vector3 localVel)
        {
            float speedFactor = Mathf.Clamp01(1f - (Mathf.Abs(currentSpeedKmh) / (maxSpeedKmh * 1.5f)));
            float effectiveSteerAngle = maxSteerAngle * (0.35f + 0.65f * speedFactor);
            currentSteerAngle = steerInput * effectiveSteerAngle;

            float moveDirection = Mathf.Sign(localVel.z);
            if (Mathf.Abs(localVel.z) < 0.1f)
            {
                moveDirection = Mathf.Sign(throttleInput);
            }

            float turnRatio = (currentSteerAngle / 30f) * moveDirection;
            float speedMultiplier = Mathf.Clamp(Mathf.Abs(localVel.z) / 2.0f, 0f, 1.2f);
            if (Mathf.Abs(throttleInput) > 0.1f && speedMultiplier < 0.35f)
            {
                speedMultiplier = 0.35f;
            }

            float yawTorque = turnRatio * speedMultiplier * 5200f;
            rb.AddTorque(transform.up * yawTorque, ForceMode.Force);
        }

        private void ApplyLateralFriction(Vector3 localVel)
        {
            Vector3 sideVelocity = transform.right * localVel.x;
            Vector3 counterForce = -sideVelocity * (rb.mass * lateralTireGrip);
            rb.AddForce(counterForce, ForceMode.Force);
        }

        private void ApplyDownforce()
        {
            float downforce = downforceCoefficient * currentSpeedMs * currentSpeedMs;
            rb.AddForce(-transform.up * downforce, ForceMode.Force);
        }

        private void ApplyAntiRollStabilizer()
        {
            // Keeps vehicle level across Z and X axes during sharp cornering
            float rollAngle = transform.localEulerAngles.z;
            if (rollAngle > 180f) rollAngle -= 360f;

            rb.AddTorque(-transform.forward * rollAngle * antiRollForce, ForceMode.Force);
        }
    }
}
