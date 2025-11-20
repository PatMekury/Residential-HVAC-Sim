// Copyright (c) Your Name/Organization

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ResidentialHVAC.Progress;
using ResidentialHVAC.UI;

namespace ResidentialHVAC.Hub
{
    /// <summary>
    /// Hub Manager that integrates with YOUR ACTUAL HVACProgressTracker system
    /// Uses: ProgressData property (not CurrentProgress)
    /// Uses: UpdateProgress() method
    /// </summary>
    public class HVACHubManager : MonoBehaviour
    {
        [Header("Portal References")]
        [SerializeField] private List<HVACHubPortal> _allPortals = new List<HVACHubPortal>();

        [Header("Progress Display")]
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Slider _progressSlider;

        [Header("Fade Behavior")]
        [Tooltip("How long to wait before fading in when Hub scene starts")]
        [SerializeField] private float _initialFadeDelay = 0.2f;
        [SerializeField] private float _fadeDuration = 1.0f;
        [SerializeField] private bool _useFadeIn = true;

        [Header("Testing Controls (Editor Only)")]
        [SerializeField] private Button _unlockAllButton;
        [SerializeField] private Button _resetProgressButton;
        [SerializeField] private GameObject _testingUI;
        [Tooltip("When enabled in Inspector, all scenes are unlocked for testing (Editor only)")]
        [SerializeField] private bool _unlockAllForTesting = false;

        private HVACProgressTracker ProgressTracker => HVACProgressTracker.Instance;
        private HVACProgressData ProgressData => ProgressTracker?.ProgressData; // YOUR ACTUAL PROPERTY NAME
        private bool _hasInitialized = false;

        private void Start()
        {
            StartCoroutine(InitializeHubSequence());
        }

        private IEnumerator InitializeHubSequence()
        {
            Debug.Log("[HVAC Hub] Starting Hub initialization...");

            yield return new WaitForSeconds(_initialFadeDelay);

            InitializeHub();

            if (_useFadeIn && FadeTransition.Instance != null)
            {
                Debug.Log("[HVAC Hub] Fading in from transition...");
                bool fadeComplete = false;
                FadeTransition.Instance.FadeFromWhite(_fadeDuration, () => fadeComplete = true);

                while (!fadeComplete)
                {
                    yield return null;
                }
            }
            else if (FadeTransition.Instance != null)
            {
                // Just disable the canvas component to hide it
                var canvas = FadeTransition.Instance.GetComponent<Canvas>();
                if (canvas != null) canvas.enabled = false;
            }

            _hasInitialized = true;
            Debug.Log("[HVAC Hub] Hub initialization complete");
        }

        private void InitializeHub()
        {
            Debug.Log("[HVAC Hub] Initializing Hub scene...");

            if (_allPortals == null || _allPortals.Count == 0)
            {
                _allPortals = new List<HVACHubPortal>(FindObjectsByType<HVACHubPortal>(FindObjectsSortMode.None));
                Debug.Log($"[HVAC Hub] Auto-found {_allPortals.Count} portals");
            }

            RefreshAllPortals();
            UpdateProgressDisplay();

#if UNITY_EDITOR
            SetupTestingControls();
#else
            if (_testingUI != null)
                _testingUI.SetActive(false);
#endif
        }

        public void RefreshAllPortals()
        {
            if (_allPortals == null) return;

            Debug.Log($"[HVAC Hub] Refreshing {_allPortals.Count} portals...");

            foreach (var portal in _allPortals)
            {
                if (portal != null)
                {
                    portal.UpdatePortalState(_unlockAllForTesting);
                }
            }
        }

        private void UpdateProgressDisplay()
        {
            if (ProgressData == null) return;

            int totalScenes = 5;
            int completedScenes = 0;

            if (ProgressData.trainingModeCompleted) completedScenes++;
            if (ProgressData.heatTransferCompleted) completedScenes++;
            if (ProgressData.loadCalculationCompleted) completedScenes++;
            if (ProgressData.materialSelectionCompleted) completedScenes++;
            if (ProgressData.systemInstallationCompleted) completedScenes++;

            float progressPercent = (float)completedScenes / totalScenes;

            if (_progressText != null)
            {
                _progressText.text = $"Training Progress: {completedScenes}/{totalScenes} Modules Complete ({progressPercent:P0})";
            }

            if (_progressSlider != null)
            {
                _progressSlider.value = progressPercent;
            }

            Debug.Log($"[HVAC Hub] Progress: {completedScenes}/{totalScenes} ({progressPercent:P0})");
        }

        private void OnEnable()
        {
            if (_hasInitialized)
            {
                Debug.Log("[HVAC Hub] Returning to Hub - refreshing state...");
                RefreshAllPortals();
                UpdateProgressDisplay();

                if (FadeTransition.Instance != null)
                {
                    StartCoroutine(FadeInOnReturn());
                }
            }
        }

        private IEnumerator FadeInOnReturn()
        {
            yield return new WaitForSeconds(0.2f);

            bool fadeComplete = false;
            FadeTransition.Instance.FadeFromWhite(_fadeDuration, () => fadeComplete = true);

            while (!fadeComplete)
            {
                yield return null;
            }
        }

#if UNITY_EDITOR
        private void SetupTestingControls()
        {
            if (_testingUI != null)
                _testingUI.SetActive(true);

            if (_unlockAllButton != null)
            {
                _unlockAllButton.onClick.AddListener(OnUnlockAllClicked);
            }

            if (_resetProgressButton != null)
            {
                _resetProgressButton.onClick.AddListener(OnResetProgressClicked);
            }

            Debug.Log("[HVAC Hub] Testing controls enabled (Editor mode)");
        }

        private void OnUnlockAllClicked()
        {
            _unlockAllForTesting = !_unlockAllForTesting;
            RefreshAllPortals();
            
            string status = _unlockAllForTesting ? "UNLOCKED" : "LOCKED BY PROGRESS";
            Debug.Log($"[HVAC Hub] Testing Mode: All scenes now {status}");
        }

        private void OnResetProgressClicked()
        {
            if (ProgressTracker != null)
            {
                ProgressTracker.ResetProgress();
                RefreshAllPortals();
                UpdateProgressDisplay();
                Debug.Log("[HVAC Hub] All progress reset");
            }
        }
#endif

        public void ManualRefresh()
        {
            RefreshAllPortals();
            UpdateProgressDisplay();
        }

        public HVACHubPortal GetPortal(string sceneName)
        {
            return _allPortals?.Find(p => p.TargetSceneName == sceneName);
        }

        public bool UnlockAllForTesting
        {
            get => _unlockAllForTesting;
            set
            {
                _unlockAllForTesting = value;
                RefreshAllPortals();
            }
        }
    }
}