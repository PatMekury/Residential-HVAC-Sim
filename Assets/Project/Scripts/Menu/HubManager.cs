// Assets/Project/Scripts/Menu/HubManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using ResidentialHVAC.Progress;
using ResidentialHVAC.Loading;

namespace ResidentialHVAC.Menu
{
    /// <summary>
    /// Manages the Hub scene - the central navigation point
    /// All training modules are accessed from here
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject _moduleSelectionPanel;
        [SerializeField] private GameObject _progressPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Module Buttons")]
        [SerializeField] private GameObject _trainingModeButton;
        [SerializeField] private GameObject _loadCalculationButton;
        [SerializeField] private GameObject _materialSelectionButton;
        [SerializeField] private GameObject _systemInstallationButton;

        [Header("Progress Display")]
        [SerializeField] private UnityEngine.UI.Text _overallProgressText;
        [SerializeField] private UnityEngine.UI.Slider _progressSlider;
        [SerializeField] private UnityEngine.UI.Text _trainingStatusText;
        [SerializeField] private UnityEngine.UI.Text _loadCalcStatusText;
        [SerializeField] private UnityEngine.UI.Text _materialStatusText;
        [SerializeField] private UnityEngine.UI.Text _installStatusText;

        private void Start()
        {
            // Show module selection by default
            ShowModuleSelection();

            // Update button states based on progress
            UpdateButtonStates();

            // Update progress display
            UpdateProgressDisplay();

            // Play hub music
            if (MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play("hub.main");
            }

            // Subscribe to progress changes
            if (HVACProgressTracker.Instance != null)
            {
                HVACProgressTracker.Instance.OnProgressChanged += UpdateProgressDisplay;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from progress changes
            if (HVACProgressTracker.Instance != null)
            {
                HVACProgressTracker.Instance.OnProgressChanged -= UpdateProgressDisplay;
            }
        }

        /// <summary>
        /// Update which modules are accessible based on progress
        /// </summary>
        private void UpdateButtonStates()
        {
            if (HVACProgressTracker.Instance == null) return;

            var progress = HVACProgressTracker.Instance.ProgressData;

            // Training Mode - always available
            if (_trainingModeButton != null)
                _trainingModeButton.GetComponent<UnityEngine.UI.Button>().interactable = true;

            // Load Calculation - unlocks after Training Mode
            if (_loadCalculationButton != null)
                _loadCalculationButton.GetComponent<UnityEngine.UI.Button>().interactable =
                    progress.trainingModeCompleted;

            // Material Selection - unlocks after Load Calculation
            if (_materialSelectionButton != null)
                _materialSelectionButton.GetComponent<UnityEngine.UI.Button>().interactable =
                    progress.loadCalculationCompleted;

            // System Installation - unlocks after Material Selection
            if (_systemInstallationButton != null)
                _systemInstallationButton.GetComponent<UnityEngine.UI.Button>().interactable =
                    progress.materialSelectionCompleted;
        }

        /// <summary>
        /// Update the progress display
        /// </summary>
        private void UpdateProgressDisplay()
        {
            if (HVACProgressTracker.Instance == null) return;

            var progress = HVACProgressTracker.Instance.ProgressData;

            // Overall progress
            if (_overallProgressText != null)
                _overallProgressText.text = $"{progress.overallProgress}% Complete";

            if (_progressSlider != null)
                _progressSlider.value = progress.overallProgress / 100f;

            // Individual module status
            if (_trainingStatusText != null)
                _trainingStatusText.text = progress.trainingModeCompleted ?
                    "✓ Completed" : "In Progress";

            if (_loadCalcStatusText != null)
                _loadCalcStatusText.text = progress.loadCalculationCompleted ?
                    "✓ Completed" : (progress.trainingModeCompleted ? "Available" : "Locked");

            if (_materialStatusText != null)
                _materialStatusText.text = progress.materialSelectionCompleted ?
                    "✓ Completed" : (progress.loadCalculationCompleted ? "Available" : "Locked");

            if (_installStatusText != null)
                _installStatusText.text = progress.systemInstallationCompleted ?
                    "✓ Completed" : (progress.materialSelectionCompleted ? "Available" : "Locked");
        }

        #region Panel Management

        private void ShowModuleSelection()
        {
            if (_moduleSelectionPanel != null)
                _moduleSelectionPanel.SetActive(true);
            if (_progressPanel != null)
                _progressPanel.SetActive(false);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        private void ShowProgress()
        {
            if (_moduleSelectionPanel != null)
                _moduleSelectionPanel.SetActive(false);
            if (_progressPanel != null)
                _progressPanel.SetActive(true);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);

            UpdateProgressDisplay();
        }

        private void ShowSettings()
        {
            if (_moduleSelectionPanel != null)
                _moduleSelectionPanel.SetActive(false);
            if (_progressPanel != null)
                _progressPanel.SetActive(false);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(true);
        }

        #endregion

        #region Button Handlers - Module Loading

        public void OnTrainingModeClicked()
        {
            Debug.Log("[HubManager] Loading Training Mode");
            LoadModule("TrainingMode");
        }

        public void OnLoadCalculationClicked()
        {
            Debug.Log("[HubManager] Loading Load Calculation");
            LoadModule("LoadCalculation");
        }

        public void OnMaterialSelectionClicked()
        {
            Debug.Log("[HubManager] Loading Material Selection");
            LoadModule("MaterialSelection");
        }

        public void OnSystemInstallationClicked()
        {
            Debug.Log("[HubManager] Loading System Installation");
            LoadModule("SystemInstallation");
        }

        #endregion

        #region Button Handlers - UI Navigation

        public void OnProgressClicked()
        {
            ShowProgress();
        }

        public void OnSettingsClicked()
        {
            ShowSettings();
        }

        public void OnBackToModules()
        {
            ShowModuleSelection();
        }

        public void OnReturnToMainMenu()
        {
            Debug.Log("[HubManager] Returning to Main Menu");

            // Save current state
            HVACProgressTracker.Instance.UpdateProgress(data =>
            {
                data.lastScene = "Hub";
            });

            // Play menu music
            if (MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play("menu");
            }

            // Load Main Menu
            SceneManager.LoadScene("MainMenu");
        }

        #endregion

        /// <summary>
        /// Load a training module scene
        /// </summary>
        private void LoadModule(string sceneName)
        {
            // Update last scene
            HVACProgressTracker.Instance.UpdateProgress(data =>
            {
                data.lastScene = sceneName;
            });

            // Play appropriate music
            if (MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play("main");
            }

            // Load the scene
            SceneManager.LoadScene(sceneName);
        }
    }
}


