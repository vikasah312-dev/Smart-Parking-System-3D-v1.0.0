using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartParking
{
    /// <summary>
    /// Central manager for the entire smart parking facility.
    /// Tracks all 84 slots across 3 levels (L0, L1, L2), calculates real-time occupancy,
    /// and provides intelligent parking guidance to the nearest available space.
    /// </summary>
    public class ParkingManager : MonoBehaviour
    {
        public static ParkingManager Instance { get; private set; }

        [Header("Parking Slots")]
        [SerializeField] private List<ParkingSlot> allSlots = new List<ParkingSlot>();

        [Header("Guidance System")]
        [SerializeField] private Transform playerCarTransform;
        [SerializeField] private ParkingSlot currentNearestSlot;
        [SerializeField] private float guidanceUpdateInterval = 0.5f;

        [Header("Real-Time Telemetry")]
        [SerializeField] private int totalSlots;
        [SerializeField] private int occupiedSlots;
        [SerializeField] private int availableSlots;
        [SerializeField] private float occupancyRate;

        // Per-floor metrics: [0] = L0, [1] = L1, [2] = L2
        private int[] floorTotal = new int[3];
        private int[] floorOccupied = new int[3];
        private int[] floorAvailable = new int[3];

        private float nextGuidanceTime;

        public int TotalSlots => totalSlots;
        public int OccupiedSlots => occupiedSlots;
        public int AvailableSlots => availableSlots;
        public float OccupancyRate => occupancyRate;
        public ParkingSlot NearestAvailableSlot => currentNearestSlot;
        public IReadOnlyList<ParkingSlot> AllSlots => allSlots;

        public int GetFloorTotal(int floor) => (floor >= 0 && floor < floorTotal.Length) ? floorTotal[floor] : 0;
        public int GetFloorOccupied(int floor) => (floor >= 0 && floor < floorOccupied.Length) ? floorOccupied[floor] : 0;
        public int GetFloorAvailable(int floor) => (floor >= 0 && floor < floorAvailable.Length) ? floorAvailable[floor] : 0;

        public event Action OnOccupancyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playerCarTransform == null)
            {
                var pc = GameObject.Find("PlayerCar");
                if (pc != null) playerCarTransform = pc.transform;
            }
        }

        private void Start()
        {
            RefreshSlotsList();
            RecalculateMetrics();
            UpdateGuidance();
        }

        private void Update()
        {
            if (Time.time >= nextGuidanceTime)
            {
                nextGuidanceTime = Time.time + guidanceUpdateInterval;
                UpdateGuidance();
            }
        }

        public void RegisterSlot(ParkingSlot slot)
        {
            if (!allSlots.Contains(slot))
            {
                allSlots.Add(slot);
                RecalculateMetrics();
            }
        }

        public void RefreshSlotsList()
        {
            allSlots.Clear();
            allSlots.AddRange(GetComponentsInChildren<ParkingSlot>(true));
            if (allSlots.Count == 0)
            {
                allSlots.AddRange(FindObjectsByType<ParkingSlot>(FindObjectsSortMode.None));
            }
            RecalculateMetrics();
        }

        public void NotifySlotChanged(ParkingSlot slot)
        {
            RecalculateMetrics();
            UpdateGuidance();
            OnOccupancyChanged?.Invoke();
        }

        public void RecalculateMetrics()
        {
            totalSlots = allSlots.Count;
            occupiedSlots = 0;

            for (int i = 0; i < 3; i++)
            {
                floorTotal[i] = 0;
                floorOccupied[i] = 0;
                floorAvailable[i] = 0;
            }

            foreach (var slot in allSlots)
            {
                int floor = Mathf.Clamp(slot.FloorLevel, 0, 2);
                floorTotal[floor]++;

                if (slot.IsOccupied)
                {
                    occupiedSlots++;
                    floorOccupied[floor]++;
                }
            }

            availableSlots = totalSlots - occupiedSlots;
            occupancyRate = totalSlots > 0 ? (float)occupiedSlots / totalSlots * 100f : 0f;

            for (int i = 0; i < 3; i++)
            {
                floorAvailable[i] = floorTotal[i] - floorOccupied[i];
            }
        }

        public (int total, int occupied, int available) GetFloorStats(int floorLevel)
        {
            int f = Mathf.Clamp(floorLevel, 0, 2);
            return (floorTotal[f], floorOccupied[f], floorAvailable[f]);
        }

        public void UpdateGuidance()
        {
            if (playerCarTransform == null)
            {
                var pc = GameObject.Find("PlayerCar");
                if (pc != null) playerCarTransform = pc.transform;
            }

            Vector3 carPos = playerCarTransform != null ? playerCarTransform.position : Vector3.zero;
            ParkingSlot newNearest = FindNearestAvailableSlot(carPos);

            if (currentNearestSlot != newNearest)
            {
                if (currentNearestSlot != null)
                {
                    currentNearestSlot.SetGuided(false);
                }

                currentNearestSlot = newNearest;

                if (currentNearestSlot != null)
                {
                    currentNearestSlot.SetGuided(true);
                }
            }
        }

        public ParkingSlot FindNearestAvailableSlot(Vector3 fromPos, int preferredFloor = -1)
        {
            ParkingSlot nearest = null;
            float minDistance = float.MaxValue;

            foreach (var slot in allSlots)
            {
                if (slot.IsOccupied) continue;
                if (preferredFloor >= 0 && slot.FloorLevel != preferredFloor) continue;

                // Distance metric: prioritize same floor by penalizing floor difference height
                float verticalPenalty = Mathf.Abs(slot.SlotCenter.y - fromPos.y) * 8.0f;
                float planarDist = Vector2.Distance(
                    new Vector2(slot.SlotCenter.x, slot.SlotCenter.z),
                    new Vector2(fromPos.x, fromPos.z)
                );
                float totalDist = planarDist + verticalPenalty;

                if (totalDist < minDistance)
                {
                    minDistance = totalDist;
                    nearest = slot;
                }
            }

            return nearest;
        }
    }
}
