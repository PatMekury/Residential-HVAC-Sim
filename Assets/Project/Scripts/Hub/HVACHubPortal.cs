// Copyright (c) Your Name/Organization

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ResidentialHVAC.Progress;
using ResidentialHVAC.UI;

namespace ResidentialHVAC.Hub
{
    /// <summary>
    /// Portal button that works with YOUR ACTUAL HVACProgressTracker
    /// Uses: ProgressData property (not CurrentProgress)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HVACHubPortal : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string _targetSceneName;
        [Tooltip("Display name shown on the portal")]
        [SerializeField] private string _displayName;
        [Tooltip("Order in sequence (0 = TrainingMode, 1 = HeatTransfer, etc.)")]
        [SerializeField] private int _sceneOrder = 0;

        [Header("UI References")]
        [SerializeField] private Button _portalButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private GameObject _completedIcon;

        [Header("Visual Feedback")]
        [SerializeField] private Color _unlockedColor = Color.white;
        [SerializeField] private Color _lockedColor = Color.gray;
        [SerializeField] private Image _backgroundImage;

        [Header("Transition Settings")]
        [SerializeField] private float _fadeDuration = 1.0f;
        [SerializeField] private bool _useWhiteFade = true;

        private bool _isUnlocked = false;
        private bool _isCompleted = false;
        private bool _isTransitioning = false;

        private HVACProgressTracker ProgressTracker => HVACProgressTracker.Instance;
        private HVACProgressData ProgressData => ProgressTracker?.ProgressData; // YOUR ACTUAL PROPERTY NAME

        private void Start()
        {
            if (_portalButton == null)
                _portalButton = GetComponent<Button>();

            _portalButton.onClick.AddListener(OnPortalClicked);

            UpdatePortalState(false);
        }

        private void OnEnable()
        {
            UpdatePortalState(false);
        }

        public void UpdatePortalState(bool unlockAllForTesting = false)
        {
            if (ProgressTracker == null || ProgressData == null)
            {
                Debug.LogWarning("[HVAC Hub Portal] Progress Tracker not found!");
                return;
            }

#if UNITY_EDITOR
            if (unlockAllForTesting)
            {
                _isUnlocked = true;
                _isCompleted = GetCompletionStatus();
                UpdateVisuals();
                return;
            }
#endif

            _isUnlocked = IsSceneUnlocked();
            _isCompleted = GetCompletionStatus();

            UpdateVisuals();

            Debug.Log($"[HVAC Hub Portal] '{_displayName}' (Order {_sceneOrder}) - Unlocked: {_isUnlocked}, Completed: {_isCompleted}");
        }

        private void UpdateVisuals()
        {
            _portalButton.interactable = _isUnlocked && !_isTransitioning;

            if (_lockedOverlay != null)
                _lockedOverlay.SetActive(!_isUnlocked);

            if (_completedIcon != null)
                _completedIcon.SetActive(_isCompleted);

            if (_backgroundImage != null)
                _backgroundImage.color = _isUnlocked ? _unlockedColor : _lockedColor;

            if (_titleText != null)
                _titleText.text = _displayName;

            if (_statusText != null)
            {
                if (_isCompleted)
                    _statusText.text = "COMPLETED";
                else if (_isUnlocked)
                    _statusText.text = "AVAILABLE";
                else
                    _statusText.text = "LOCKED";
            }
        }

        private bool IsSceneUnlocked()
        {
            if (ProgressData == null) return false;

            // First scene is always unlocked
            if (_sceneOrder == 0) return true;

            // Check if previous scene is completed
            switch (_sceneOrder)
            {
                case 1: // HeatTransfer unlocks after TrainingMode
                    return ProgressData.trainingModeCompleted;

                case 2: // LoadCalculation unlocks after HeatTransfer
                    return ProgressData.heatTransferCompleted;
                    

                case 3: // MaterialSelection unlocks after LoadCalculation
                    return ProgressData.loadCalculationCompleted;

                case 4: // SystemInstallation unlocks after MaterialSelection
                    return ProgressData.materialSelectionCompleted;

                default:
                    return false;
            }
        }

        private bool GetCompletionStatus()
        {
            if (ProgressData == null) return false;

            switch (_targetSceneName)
            {
                case "TrainingMode":
                    return ProgressData.trainingModeCompleted;

                case "HeatTransfer":
                    // NOTE: When you add heatTransferCompleted field, uncomment this:
                    return ProgressData.heatTransferCompleted;
                    

                case "LoadCalculation":
                    return ProgressData.loadCalculationCompleted;

                case "MaterialSelection":
                    return ProgressData.materialSelectionCompleted;

                case "SystemInstallation":
                    return ProgressData.systemInstallationCompleted;

                default:
                    return false;
            }
        }

        private void OnPortalClicked()
        {
            if (!_isUnlocked)
            {
                Debug.LogWarning($"[HVAC Hub Portal] Attempted to access locked scene: {_targetSceneName}");
                return;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning("[HVAC Hub Portal] Transition already in progress");
                return;
            }

            Debug.Log($"[HVAC Hub Portal] Loading scene: {_targetSceneName}");

            // Update last scene using YOUR UpdateProgress method
            if (ProgressTracker != null)
            {
                ProgressTracker.UpdateProgress(data =>
                {
                    data.lastScene = _targetSceneName;
                });
            }

            StartCoroutine(TransitionToScene());
        }

        private IEnumerator TransitionToScene()
        {
            _isTransitioning = true;
            _portalButton.interactable = false;

            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;

                if (_useWhiteFade)
                {
                    FadeTransition.Instance.FadeToWhite(_fadeDuration, () => fadeComplete = true);
                }
                else
                {
                    FadeTransition.Instance.FadeToBlack(_fadeDuration, () => fadeComplete = true);
                }

                while (!fadeComplete)
                {
                    yield return null;
                }
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_targetSceneName);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            _isTransitioning = false;
        }

        public string TargetSceneName
        {
            get => _targetSceneName;
            set => _targetSceneName = value;
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public int SceneOrder
        {
            get => _sceneOrder;
            set => _sceneOrder = value;
        }

        public bool IsUnlocked => _isUnlocked;
        public bool IsCompleted => _isCompleted;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_displayName) && !string.IsNullOrEmpty(_targetSceneName))
            {
                _displayName = _targetSceneName;
            }
        }
#endif
    }
}