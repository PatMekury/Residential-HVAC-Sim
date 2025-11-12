// Assets/Project/Scripts/Progress/HVACProgressTracker.cs
using System;
using UnityEngine;

namespace ResidentialHVAC.Progress
{
    /// <summary>
    /// Manages player progress throughout the HVAC Sim
    /// Singleton that persists across scenes
    /// </summary>
    public class HVACProgressTracker : MonoBehaviour
    {
        private static HVACProgressTracker _instance;
        public static HVACProgressTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HVACProgressTracker>();
                }
                return _instance;
            }
        }

        [SerializeField]
        private HVACProgressData _progressData;

        public HVACProgressData ProgressData => _progressData;

        public event Action OnProgressChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved progress
            _progressData = HVACProgressData.Load();
            Debug.Log($"[HVACProgressTracker] Loaded progress: {_progressData.overallProgress}%");
        }

        /// <summary>
        /// Update progress and save
        /// </summary>
        public void UpdateProgress(Action<HVACProgressData> updateAction)
        {
            updateAction?.Invoke(_progressData);
            _progressData.Save();
            OnProgressChanged?.Invoke();
        }

        /// <summary>
        /// Mark training mode as complete
        /// </summary>
        public void CompleteTrainingMode()
        {
            UpdateProgress(data =>
            {
                data.trainingModeCompleted = true;
                data.overallProgress = Mathf.Max(data.overallProgress, 25);
            });
        }

        /// <summary>
        /// Mark a room's load calculation as complete
        /// </summary>
        public void CompleteRoomCalculation(string roomName)
        {
            UpdateProgress(data =>
            {
                switch (roomName.ToLower())
                {
                    case "livingroom":
                        data.livingRoomCalculated = true;
                        break;
                    case "kitchen":
                        data.kitchenCalculated = true;
                        break;
                    case "bedroom":
                        data.bedroomCalculated = true;
                        break;
                    case "bathroom":
                        data.bathroomCalculated = true;
                        break;
                }

                // Check if all rooms done
                if (data.livingRoomCalculated && data.kitchenCalculated &&
                    data.bedroomCalculated && data.bathroomCalculated)
                {
                    data.loadCalculationCompleted = true;
                    data.overallProgress = Mathf.Max(data.overallProgress, 50);
                }
            });
        }

        /// <summary>
        /// Check if player has existing progress
        /// </summary>
        public bool HasProgress()
        {
            return _progressData.overallProgress > 0;
        }

        /// <summary>
        /// Reset all progress
        /// </summary>
        public void ResetProgress()
        {
            _progressData.Reset();
            OnProgressChanged?.Invoke();
        }
    }
}