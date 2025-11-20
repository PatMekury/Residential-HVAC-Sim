// Copyright (c) Your Name/Organization

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using ResidentialHVAC.Progress;
using ResidentialHVAC.UI;

namespace ResidentialHVAC.Hub
{
    /// <summary>
    /// Works with YOUR ACTUAL HVACProgressTracker system
    /// Uses: ProgressData property and UpdateProgress() method
    /// </summary>
    public class HVACSceneCompleter : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [Tooltip("The scene name (should match scene name in build settings)")]
        [SerializeField] private string _sceneName;

        [Header("Completion Behavior")]
        [Tooltip("Automatically complete the scene when this GameObject is destroyed/scene unloads")]
        [SerializeField] private bool _completeOnSceneExit = false;

        [Tooltip("Automatically complete when this script starts (useful for testing)")]
        [SerializeField] private bool _completeOnStart = false;

        [Header("Return to Hub")]
        [Tooltip("Automatically load Hub scene after completion")]
        [SerializeField] private bool _returnToHubAfterCompletion = true;
        [SerializeField] private float _returnDelay = 2f;

        [Header("Fade Settings")]
        [SerializeField] private float _fadeDuration = 1.0f;
        [SerializeField] private bool _useWhiteFade = true;
        [SerializeField] private float _fadeInDelay = 0.2f;

        private bool _hasCompleted = false;
        private bool _isTransitioning = false;

        private HVACProgressTracker ProgressTracker => HVACProgressTracker.Instance;
        private HVACProgressData ProgressData => ProgressTracker?.ProgressData; // YOUR ACTUAL PROPERTY NAME

        private void Start()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                _sceneName = SceneManager.GetActiveScene().name;
                Debug.Log($"[HVAC Scene Completer] Auto-detected scene name: {_sceneName}");
            }

            StartCoroutine(FadeInOnStart());

            if (_completeOnStart)
            {
                CompleteScene();
            }
        }

        private IEnumerator FadeInOnStart()
        {
            yield return new WaitForSeconds(_fadeInDelay);

            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;

                if (_useWhiteFade)
                {
                    FadeTransition.Instance.FadeFromWhite(_fadeDuration, () => fadeComplete = true);
                }
                else
                {
                    FadeTransition.Instance.FadeFromBlack(_fadeDuration, () => fadeComplete = true);
                }

                while (!fadeComplete)
                {
                    yield return null;
                }
            }
        }

        private void OnDestroy()
        {
            if (_completeOnSceneExit && !_hasCompleted)
            {
                CompleteSceneImmediately();
            }
        }

        public void CompleteScene()
        {
            if (_hasCompleted)
            {
                Debug.LogWarning($"[HVAC Scene Completer] Scene '{_sceneName}' already marked as complete");
                return;
            }

            CompleteSceneImmediately();

            if (_returnToHubAfterCompletion)
            {
                StartCoroutine(ReturnToHubWithDelay());
            }
        }

        private void CompleteSceneImmediately()
        {
            if (ProgressTracker == null || ProgressData == null)
            {
                Debug.LogError("[HVAC Scene Completer] Progress Tracker not found!");
                return;
            }

            _hasCompleted = true;

            // Use YOUR UpdateProgress method to mark completion
            ProgressTracker.UpdateProgress(data =>
            {
                switch (_sceneName)
                {
                    case "TrainingMode":
                        data.trainingModeCompleted = true;
                        break;

                    case "HeatTransfer":
                        // NOTE: When you add heatTransferCompleted to HVACProgressData, uncomment:
                        data.heatTransferCompleted = true;
                        break;

                    case "LoadCalculation":
                        data.loadCalculationCompleted = true;
                        break;

                    case "MaterialSelection":
                        data.materialSelectionCompleted = true;
                        break;

                    case "SystemInstallation":
                        data.systemInstallationCompleted = true;
                        break;

                    default:
                        Debug.LogWarning($"[HVAC Scene Completer] Unknown scene '{_sceneName}'");
                        break;
                }

                // Update overall progress
                int completed = 0;
                int total = 5;

                if (data.trainingModeCompleted) completed++;
                if (data.heatTransferCompleted) completed++;
                if (data.loadCalculationCompleted) completed++;
                if (data.materialSelectionCompleted) completed++;
                if (data.systemInstallationCompleted) completed++;

                data.overallProgress = (int)((float)completed / total * 100);
            });

            Debug.Log($"[HVAC Scene Completer] Scene '{_sceneName}' marked as COMPLETED and saved");
        }

        private IEnumerator ReturnToHubWithDelay()
        {
            yield return new WaitForSeconds(_returnDelay);
            StartCoroutine(TransitionToHub());
        }

        private IEnumerator TransitionToHub()
        {
            if (_isTransitioning) yield break;

            _isTransitioning = true;
            Debug.Log("[HVAC Scene Completer] Returning to Hub...");

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

            // Update last scene using YOUR UpdateProgress method
            if (ProgressTracker != null)
            {
                ProgressTracker.UpdateProgress(data =>
                {
                    data.lastScene = "Hub";
                });
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Hub");

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            _isTransitioning = false;
        }

        public void ReturnToHubWithoutCompleting()
        {
            if (_isTransitioning) return;

            Debug.Log("[HVAC Scene Completer] Returning to Hub WITHOUT completing scene...");
            StartCoroutine(TransitionToHub());
        }

        public bool HasCompletedThisSession => _hasCompleted;

        public bool WasCompleted
        {
            get
            {
                if (ProgressData == null) return false;

                switch (_sceneName)
                {
                    case "TrainingMode": return ProgressData.trainingModeCompleted;
                    case "HeatTransfer": return ProgressData.heatTransferCompleted;
                    case "LoadCalculation": return ProgressData.loadCalculationCompleted;
                    case "MaterialSelection": return ProgressData.materialSelectionCompleted;
                    case "SystemInstallation": return ProgressData.systemInstallationCompleted;
                    default: return false;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                _sceneName = SceneManager.GetActiveScene().name;
            }
        }
#endif
    }
}