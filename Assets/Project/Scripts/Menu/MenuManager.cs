// Assets/Project/Scripts/Menu/MenuManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using ResidentialHVAC.Progress;
using ResidentialHVAC.Loading;
using ResidentialHVAC.UI;

namespace ResidentialHVAC.Menu
{
    /// <summary>
    /// Manages ONLY the main menu (entry point)
    /// Handles intro cinematic and scene transitions
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("UI Buttons")]
        [SerializeField] private GameObject _continueButton;
        [SerializeField] private GameObject _newGameButton;

        [Header("Transition Settings")]
        [SerializeField] private float _transitionDuration = 1.0f;
        [SerializeField] private bool _useWhiteFade = true;

        private MenuState _currentState = MenuState.MainMenu;
        private bool _isTransitioning = false;

        private void Awake()
        {
            // Hide panels initially (cinematic will show them)
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(false);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        private void Start()
        {
            // Menu UI will be shown after intro cinematic completes
            // (IntroCinematic will fade from black automatically)
        }

        /// <summary>
        /// Called after intro cinematic completes (via UnityEvent)
        /// </summary>
        public void OnIntroCinematicComplete()
        {
            Debug.Log("[MenuManager] Intro complete, showing menu");
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(true);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);

            // Check if player has progress
            bool hasProgress = HVACProgressTracker.Instance.HasProgress();

            if (_continueButton != null)
                _continueButton.SetActive(hasProgress);

            if (_newGameButton != null)
                _newGameButton.SetActive(true);

            // Play menu music
            if (MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play("menu");
            }

            _currentState = MenuState.MainMenu;
        }

        private void ShowSettings()
        {
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(false);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(true);

            _currentState = MenuState.Settings;
        }

        #region Button Handlers

        /// <summary>
        /// Start a completely new game
        /// </summary>
        public void OnNewGameClicked()
        {
            if (_isTransitioning) return;

            Debug.Log("[MenuManager] New Game");

            // Reset ALL progress
            HVACProgressTracker.Instance.ResetProgress();

            // Mark hub as accessible
            HVACProgressTracker.Instance.UpdateProgress(data =>
            {
                data.hubUnlocked = true;
                data.lastScene = "Hub";
            });

            // Transition to Hub
            StartCoroutine(TransitionToHub());
        }

        /// <summary>
        /// Continue from last saved position
        /// </summary>
        public void OnContinueClicked()
        {
            if (_isTransitioning) return;

            Debug.Log("[MenuManager] Continue");

            // Transition to Hub
            StartCoroutine(TransitionToHub());
        }

        public void OnSettingsClicked()
        {
            ShowSettings();
        }

        public void OnBackToMainMenu()
        {
            ShowMainMenu();
        }

        public void OnQuitClicked()
        {
            Debug.Log("[MenuManager] Quit");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        #endregion

        /// <summary>
        /// Transition to Hub with fade effect
        /// </summary>
        private IEnumerator TransitionToHub()
        {
            _isTransitioning = true;

            // 1. Fade to white (or black)
            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;

                if (_useWhiteFade)
                {
                    FadeTransition.Instance.FadeToWhite(_transitionDuration, () => fadeComplete = true);
                }
                else
                {
                    FadeTransition.Instance.FadeToBlack(_transitionDuration, () => fadeComplete = true);
                }

                // Wait for fade to complete
                while (!fadeComplete)
                {
                    yield return null;
                }
            }

            // 2. Change music
            if (MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play("hub.main");
            }

            // 3. Load Hub scene
            Debug.Log("[MenuManager] Loading Hub");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Hub");

            // Wait for scene to load
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 4. Fade from white/black in the Hub scene
            // (Hub scene should handle this in its Start method)
        }
    }
}