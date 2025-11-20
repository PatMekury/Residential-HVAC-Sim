// Assets/Project/Scripts/UI/SceneFadeController.cs
using UnityEngine;

namespace ResidentialHVAC.UI
{
    /// <summary>
    /// Add this to any scene to ensure the FadeCanvas is properly hidden on scene start
    /// Useful for scenes that don't manage their own transitions
    /// </summary>
    public class SceneFadeController : MonoBehaviour
    {
        [Header("Fade In Settings")]
        [SerializeField] private bool _fadeInOnStart = false;
        [SerializeField] private float _fadeInDuration = 1.0f;
        [SerializeField] private bool _fadeFromWhite = false;

        [Header("Force Hide Settings")]
        [SerializeField] private bool _forceHideOnStart = true;
        [Tooltip("Delay before hiding (to ensure any ongoing fades complete)")]
        [SerializeField] private float _hideDelay = 0.1f;

        private void Start()
        {
            if (FadeTransition.Instance == null)
            {
                Debug.LogWarning("[SceneFadeController] No FadeTransition found in scene");
                return;
            }

            if (_fadeInOnStart)
            {
                // Perform a fade-in animation
                if (_fadeFromWhite)
                {
                    FadeTransition.Instance.FadeFromWhite(_fadeInDuration, OnFadeComplete);
                }
                else
                {
                    FadeTransition.Instance.FadeFromBlack(_fadeInDuration, OnFadeComplete);
                }
            }
            else if (_forceHideOnStart)
            {
                // Just immediately hide it
                Invoke(nameof(HideFadeCanvas), _hideDelay);
            }
        }

        private void OnFadeComplete()
        {
            if (_forceHideOnStart)
            {
                HideFadeCanvas();
            }
        }

        private void HideFadeCanvas()
        {
            if (FadeTransition.Instance != null)
            {
                // Ensure it's fully transparent
                FadeTransition.Instance.SetFadeTransparent();
                
                // Disable the Canvas component (keeps GameObject active for singleton)
                Canvas fadeCanvas = FadeTransition.Instance.GetComponent<Canvas>();
                if (fadeCanvas != null)
                {
                    fadeCanvas.enabled = false;
                    Debug.Log("[SceneFadeController] Fade canvas hidden");
                }
            }
        }

        /// <summary>
        /// Call this to re-enable the fade canvas (e.g., before transitioning out)
        /// </summary>
        public void ShowFadeCanvas()
        {
            if (FadeTransition.Instance != null)
            {
                Canvas fadeCanvas = FadeTransition.Instance.GetComponent<Canvas>();
                if (fadeCanvas != null)
                {
                    fadeCanvas.enabled = true;
                }
            }
        }
    }
}
