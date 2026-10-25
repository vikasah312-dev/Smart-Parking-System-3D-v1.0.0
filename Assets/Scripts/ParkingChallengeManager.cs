using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartParking
{
    [System.Serializable]
    public class ParkingChallenge
    {
        public string title;
        public string description;
        public string targetSlotId;
        public float parTimeSeconds;
        public Vector3 spawnPosition = new Vector3(45.0f, 0.05f, -1.89f);
        public Vector3 spawnRotationEuler = new Vector3(0f, 90f, 0f);
    }

    public struct ChallengeResult
    {
        public string challengeTitle;
        public string targetSlotId;
        public float timeElapsed;
        public float parTime;
        public int collisionCount;
        public int finalScore;
        public int starsEarned; // 1, 2, or 3
        public string grade;    // S, A, B, C
    }

    /// <summary>
    /// Manages competitive parking challenge missions with precision timers, collision penalties, and score ratings.
    /// </summary>
    public class ParkingChallengeManager : MonoBehaviour
    {
        public static ParkingChallengeManager Instance { get; private set; }

        [Header("Challenges")]
        [SerializeField] private List<ParkingChallenge> challenges = new List<ParkingChallenge>();
        [SerializeField] private int currentChallengeIndex = 0;

        [Header("State")]
        [SerializeField] private bool isChallengeActive = false;
        [SerializeField] private float elapsedTime;
        [SerializeField] private int liveScore = 1000;
        [SerializeField] private ChallengeResult lastResult;

        public bool IsChallengeActive => isChallengeActive;
        public int CurrentChallengeIndex => currentChallengeIndex;
        public float ElapsedTime => elapsedTime;
        public int LiveScore => liveScore;
        public ChallengeResult LastResult => lastResult;
        public IReadOnlyList<ParkingChallenge> AllChallenges => challenges;

        public static event Action<ParkingChallenge> OnChallengeStarted;
        public static event Action<ChallengeResult> OnChallengeCompleted;
        public static event Action<string> OnChallengeFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitDefaultChallenges();
        }

        private void OnEnable()
        {
            ParkingSlot.OnPlayerParked += HandlePlayerParked;
            PlayerCarController.OnVehicleCollision += HandleVehicleCollision;
        }

        private void OnDisable()
        {
            ParkingSlot.OnPlayerParked -= HandlePlayerParked;
            PlayerCarController.OnVehicleCollision -= HandleVehicleCollision;
        }

        private void InitDefaultChallenges()
        {
            if (challenges == null || challenges.Count == 0)
            {
                challenges = new List<ParkingChallenge>
                {
                    new ParkingChallenge
                    {
                        title = "Mission 1: Ground Floor Bay",
                        description = "Pass through the entrance barrier and park securely inside Bay L0-A02.",
                        targetSlotId = "L0-A02",
                        parTimeSeconds = 35f,
                        spawnPosition = new Vector3(45.0f, 0.05f, -1.89f),
                        spawnRotationEuler = new Vector3(0f, 90f, 0f)
                    },
                    new ParkingChallenge
                    {
                        title = "Mission 2: Precision Reverse Park",
                        description = "Navigate past the first bay and back cleanly into Bay L0-A04 in Reverse [R] gear.",
                        targetSlotId = "L0-A04",
                        parTimeSeconds = 48f,
                        spawnPosition = new Vector3(32.0f, 0.05f, -1.89f),
                        spawnRotationEuler = new Vector3(0f, 90f, 0f)
                    },
                    new ParkingChallenge
                    {
                        title = "Mission 3: Level 1 Ramp Ascent",
                        description = "Climb the circular vehicle ramp to Floor 1 and dock perfectly in Bay L1-A01.",
                        targetSlotId = "L1-A01",
                        parTimeSeconds = 70f,
                        spawnPosition = new Vector3(45.0f, 0.05f, -1.89f),
                        spawnRotationEuler = new Vector3(0f, 90f, 0f)
                    }
                };
            }
        }

        private void Update()
        {
            if (isChallengeActive)
            {
                elapsedTime += Time.deltaTime;

                // Live score calculation
                var challenge = challenges[currentChallengeIndex];
                int timePenalty = 0;
                if (elapsedTime > challenge.parTimeSeconds)
                {
                    timePenalty = Mathf.RoundToInt((elapsedTime - challenge.parTimeSeconds) * 12f);
                }

                int collisionPenalty = 0;
                var pc = PlayerCarController.Instance;
                if (pc != null)
                {
                    collisionPenalty = pc.CollisionCount * 150;
                }

                liveScore = Mathf.Max(0, 1000 - timePenalty - collisionPenalty);

                // Timeout failure (3x par time)
                if (elapsedTime >= challenge.parTimeSeconds * 2.5f)
                {
                    FailChallenge("Time Expired! Exceeded maximum challenge duration.");
                }
            }
        }

        public void StartChallenge(int index)
        {
            if (index < 0 || index >= challenges.Count) return;

            currentChallengeIndex = index;
            var challenge = challenges[index];

            elapsedTime = 0f;
            liveScore = 1000;
            isChallengeActive = true;

            // Reset vehicle position and collisions
            var pc = PlayerCarController.Instance;
            if (pc != null)
            {
                pc.ResetCollisionCount();
                var rb = pc.VehicleRigidbody;
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = challenge.spawnPosition;
                    rb.rotation = Quaternion.Euler(challenge.spawnRotationEuler);
                }
                pc.transform.position = challenge.spawnPosition;
                pc.transform.rotation = Quaternion.Euler(challenge.spawnRotationEuler);
            }

            // Snap camera
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CarFollowCamera>();
                if (camFollow != null) camFollow.SnapToTarget();
            }

            var cm = ParkingCameraManager.Instance;
            if (cm != null) cm.SetCameraMode(CameraViewMode.ThirdPerson);

            OnChallengeStarted?.Invoke(challenge);
        }

        public void RetryCurrentChallenge()
        {
            StartChallenge(currentChallengeIndex);
        }

        public void StartNextChallenge()
        {
            int next = (currentChallengeIndex + 1) % challenges.Count;
            StartChallenge(next);
        }

        public void StopChallenge()
        {
            isChallengeActive = false;
        }

        private void HandlePlayerParked(ParkingSlot slot)
        {
            if (!isChallengeActive) return;

            var challenge = challenges[currentChallengeIndex];
            if (slot.SlotId.Equals(challenge.targetSlotId, StringComparison.OrdinalIgnoreCase))
            {
                CompleteChallenge();
            }
        }

        private void HandleVehicleCollision(float impactForce)
        {
            if (!isChallengeActive) return;
            // Collision penalty is tracked via PlayerCarController.CollisionCount
        }

        private void CompleteChallenge()
        {
            isChallengeActive = false;
            var challenge = challenges[currentChallengeIndex];

            int collisions = PlayerCarController.Instance != null ? PlayerCarController.Instance.CollisionCount : 0;
            float timeBonus = (elapsedTime < challenge.parTimeSeconds) ? (challenge.parTimeSeconds - elapsedTime) * 15f : 0f;
            int cleanBonus = (collisions == 0) ? 150 : 0;

            int finalScore = Mathf.Max(0, liveScore + Mathf.RoundToInt(timeBonus) + cleanBonus);

            // Grade & Stars
            int stars = 1;
            string grade = "C";

            if (finalScore >= 950)
            {
                stars = 3;
                grade = "S";
            }
            else if (finalScore >= 750)
            {
                stars = 3;
                grade = "A";
            }
            else if (finalScore >= 550)
            {
                stars = 2;
                grade = "B";
            }

            lastResult = new ChallengeResult
            {
                challengeTitle = challenge.title,
                targetSlotId = challenge.targetSlotId,
                timeElapsed = elapsedTime,
                parTime = challenge.parTimeSeconds,
                collisionCount = collisions,
                finalScore = finalScore,
                starsEarned = stars,
                grade = grade
            };

            OnChallengeCompleted?.Invoke(lastResult);
        }

        public void FailChallenge(string reason)
        {
            isChallengeActive = false;
            OnChallengeFailed?.Invoke(reason);
        }
    }
}
