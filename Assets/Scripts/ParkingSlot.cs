using System;
using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// Represents an individual smart parking slot with precision parking validation.
    /// Features:
    /// - Strict boundary and alignment verification.
    /// - Stationary timer requirement (>= 1.0s stop) before confirming parking.
    /// - Dynamic parking success confirmation and exit handling.
    /// - Prevents double counting and updates 3D LED beacons.
    /// </summary>
    public class ParkingSlot : MonoBehaviour
    {
        [Header("Slot Identity")]
        [SerializeField] private string slotId;
        [SerializeField] private int floorLevel;
        [SerializeField] private char rowLetter;
        [SerializeField] private int slotNumber;

        [Header("Occupancy State")]
        [SerializeField] private bool isOccupied;
        [SerializeField] private bool isGuidedTarget;
        [SerializeField] private bool isPlayerParkedHere;
        [SerializeField] private GameObject occupyingVehicle;

        [Header("Validation Parameters")]
        [SerializeField] private float stationaryDurationRequired = 1.0f;
        [SerializeField] private float maxSpeedThresholdKmh = 0.85f;

        [Header("Visual Indicator")]
        [SerializeField] private Renderer indicatorRenderer;

        private float stationaryTimer;
        private bool isTrackingPlayerCar;
        private PlayerCarController playerController;

        private static Material availableMat;
        private static Material occupiedMat;
        private static Material guidedMat;

        public string SlotId => slotId;
        public int FloorLevel => floorLevel;
        public char RowLetter => rowLetter;
        public int SlotNumber => slotNumber;
        public bool IsOccupied => isOccupied;
        public bool IsGuidedTarget => isGuidedTarget;
        public bool IsPlayerParkedHere => isPlayerParkedHere;
        public Vector3 SlotCenter => transform.position;

        public static event Action<ParkingSlot> OnPlayerParked;
        public static event Action<ParkingSlot> OnPlayerExited;

        public void Initialize(string id, int floor, char row, int number, Renderer renderer)
        {
            slotId = id;
            floorLevel = floor;
            rowLetter = row;
            slotNumber = number;
            indicatorRenderer = renderer;

            EnsureMaterials();
            CheckOccupancyNow();
        }

        private static void EnsureMaterials()
        {
            if (availableMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                availableMat = new Material(shader) { name = "Indicator_Available" };
                availableMat.color = new Color(0.1f, 1.0f, 0.25f, 1.0f);
                availableMat.EnableKeyword("_EMISSION");
                availableMat.SetColor("_EmissionColor", new Color(0.1f, 1.0f, 0.25f) * 2.5f);

                occupiedMat = new Material(shader) { name = "Indicator_Occupied" };
                occupiedMat.color = new Color(1.0f, 0.15f, 0.15f, 1.0f);
                occupiedMat.EnableKeyword("_EMISSION");
                occupiedMat.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.15f) * 2.5f);

                guidedMat = new Material(shader) { name = "Indicator_Guided" };
                guidedMat.color = new Color(0.0f, 0.9f, 1.0f, 1.0f);
                guidedMat.EnableKeyword("_EMISSION");
                guidedMat.SetColor("_EmissionColor", new Color(0.0f, 0.9f, 1.0f) * 3.5f);
            }
        }

        public void SetGuided(bool guided)
        {
            isGuidedTarget = guided;
            UpdateVisuals();
        }

        private void Update()
        {
            if (playerController == null && PlayerCarController.Instance != null)
            {
                playerController = PlayerCarController.Instance;
            }

            if (playerController != null)
            {
                Vector3 carPos = playerController.transform.position;
                Vector3 localDelta = transform.InverseTransformPoint(carPos);
                bool insideBounds = Mathf.Abs(localDelta.x) <= 1.25f &&
                                    Mathf.Abs(localDelta.z) <= 2.2f &&
                                    Mathf.Abs(localDelta.y) <= 1.0f;

                if (insideBounds)
                {
                    isTrackingPlayerCar = true;
                    ValidatePlayerParkingProgress(insideBounds);
                }
                else if (isTrackingPlayerCar)
                {
                    if (isPlayerParkedHere)
                    {
                        HandlePlayerUnpark();
                    }
                    isTrackingPlayerCar = false;
                    stationaryTimer = 0f;
                }
            }
        }

        private void ValidatePlayerParkingProgress(bool insideBounds)
        {
            float speed = Mathf.Abs(playerController.CurrentSpeedKmh);

            if (insideBounds && speed <= maxSpeedThresholdKmh)
            {
                stationaryTimer += Time.deltaTime;
                if (stationaryTimer >= stationaryDurationRequired && !isPlayerParkedHere)
                {
                    ConfirmPlayerParked();
                }
            }
            else
            {
                stationaryTimer = 0f;

                // If player was parked here and now drives away
                if (isPlayerParkedHere && (speed > 1.0f || !insideBounds))
                {
                    HandlePlayerUnpark();
                }
            }
        }

        private void ConfirmPlayerParked()
        {
            isPlayerParkedHere = true;
            isOccupied = true;
            occupyingVehicle = playerController.gameObject;

            UpdateVisuals();
            OnPlayerParked?.Invoke(this);

            if (ParkingManager.Instance != null)
            {
                ParkingManager.Instance.NotifySlotChanged(this);
            }
        }

        private void HandlePlayerUnpark()
        {
            isPlayerParkedHere = false;
            isOccupied = false;
            occupyingVehicle = null;
            stationaryTimer = 0f;

            UpdateVisuals();
            OnPlayerExited?.Invoke(this);

            if (ParkingManager.Instance != null)
            {
                ParkingManager.Instance.NotifySlotChanged(this);
            }
        }

        public bool CheckOccupancyNow()
        {
            bool foundCar = false;
            occupyingVehicle = null;

            Vector3 center = transform.position + Vector3.up * 0.75f;
            Vector3 halfExtents = new Vector3(1.15f, 0.8f, 2.15f);
            Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation);

            foreach (var hit in hits)
            {
                if (hit.isTrigger) continue;
                if (hit.gameObject.name.Contains("Paving") ||
                    hit.gameObject.name.Contains("Slab") ||
                    hit.gameObject.name.Contains("Kerb") ||
                    hit.gameObject.name.Contains("Barrier")) continue;

                Transform root = hit.transform.root;
                if (root.name.StartsWith("CAR_") || root.name.Contains("PlayerCar") || hit.GetComponentInParent<Rigidbody>() != null)
                {
                    foundCar = true;
                    occupyingVehicle = root.gameObject;
                    if (root.name.Contains("PlayerCar"))
                    {
                        isPlayerParkedHere = true;
                    }
                    break;
                }
            }

            if (!foundCar)
            {
                Vector3 myPos = transform.position;
                foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (go.name.StartsWith("CAR_") && go.transform.parent != null && go.transform.parent.name == "smart_parking_system")
                    {
                        Vector3 carPos = go.transform.position;
                        if (Mathf.Abs(carPos.x - myPos.x) <= 1.25f &&
                            Mathf.Abs(carPos.z - myPos.z) <= 2.2f &&
                            Mathf.Abs(carPos.y - myPos.y) <= 1.5f)
                        {
                            foundCar = true;
                            occupyingVehicle = go;
                            break;
                        }
                    }
                }
            }

            SetOccupied(foundCar);
            return isOccupied;
        }

        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
            UpdateVisuals();

            if (ParkingManager.Instance != null)
            {
                ParkingManager.Instance.NotifySlotChanged(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            var pc = other.GetComponentInParent<PlayerCarController>();
            if (pc != null)
            {
                isTrackingPlayerCar = true;
                playerController = pc;
                stationaryTimer = 0f;
            }
            else
            {
                Transform root = other.transform.root;
                if (root.name.StartsWith("CAR_") && !isOccupied)
                {
                    occupyingVehicle = root.gameObject;
                    SetOccupied(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger) return;
            var pc = other.GetComponentInParent<PlayerCarController>();
            if (pc != null)
            {
                isTrackingPlayerCar = false;
                if (isPlayerParkedHere)
                {
                    HandlePlayerUnpark();
                }
                playerController = null;
                stationaryTimer = 0f;
            }
            else
            {
                Invoke(nameof(CheckOccupancyNow), 0.2f);
            }
        }

        public void UpdateVisuals()
        {
            if (indicatorRenderer == null) return;
            EnsureMaterials();

            if (isOccupied)
            {
                indicatorRenderer.sharedMaterial = occupiedMat;
            }
            else if (isGuidedTarget)
            {
                indicatorRenderer.sharedMaterial = guidedMat;
            }
            else
            {
                indicatorRenderer.sharedMaterial = availableMat;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isOccupied ? Color.red : (isGuidedTarget ? Color.cyan : Color.green);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.75f, new Vector3(2.2f, 1.4f, 4.2f));
        }
    }
}
