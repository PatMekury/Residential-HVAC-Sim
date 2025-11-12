// Assets/Project/Scripts/Progress/HVACProgressData.cs
using System;
using UnityEngine;

namespace ResidentialHVAC.Progress
{
    /// <summary>
    /// Stores player progress data for HVAC Sim
    /// </summary>
    [Serializable]
    public class HVACProgressData
    {
        public int overallProgress = 0;         // 0-100 overall completion
        public bool hubUnlocked = false;
        public bool trainingModeCompleted = false;
        public bool loadCalculationCompleted = false;
        public bool materialSelectionCompleted = false;
        public bool systemInstallationCompleted = false;

        // Room-specific progress for Load Calculation
        public bool livingRoomCalculated = false;
        public bool kitchenCalculated = false;
        public bool bedroomCalculated = false;
        public bool bathroomCalculated = false;

        // Last scene the player was in
        public string lastScene = "Hub";

        /// <summary>
        /// Save progress to PlayerPrefs
        /// </summary>
        public void Save()
        {
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString("HVACProgress", json);
            PlayerPrefs.Save();
            Debug.Log("[HVACProgress] Progress saved");
        }

        /// <summary>
        /// Load progress from PlayerPrefs
        /// </summary>
        public static HVACProgressData Load()
        {
            if (PlayerPrefs.HasKey("HVACProgress"))
            {
                string json = PlayerPrefs.GetString("HVACProgress");
                return JsonUtility.FromJson<HVACProgressData>(json);
            }
            return new HVACProgressData();
        }

        /// <summary>
        /// Reset all progress
        /// </summary>
        public void Reset()
        {
            overallProgress = 0;
            hubUnlocked = false;
            trainingModeCompleted = false;
            loadCalculationCompleted = false;
            materialSelectionCompleted = false;
            systemInstallationCompleted = false;
            livingRoomCalculated = false;
            kitchenCalculated = false;
            bedroomCalculated = false;
            bathroomCalculated = false;
            lastScene = "Hub";
            Save();
        }
    }
}