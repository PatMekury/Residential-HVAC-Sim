// Assets/Project/Scripts/UI/FadeTransition.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ResidentialHVAC.UI
{
    /// <summary>
    /// Handles fade to/from black or white transitions
    /// </summary>
    public class FadeTransition : MonoBehaviour
    {
        private static FadeTransition _instance;
        public static FadeTransition Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FadeTransition>();
                }
                return _instance;
            }
        }

        [Header("Fade Settings")]
        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _defaultFadeDuration = 1.0f;
        [SerializeField] private Color _fadeToBlackColor = Color.black;
        [SerializeField] private Color _fadeToWhiteColor = Color.white;

        [Header("Canvas Settings")]
        [SerializeField] private Canvas _fadeCanvas;

        private Coroutine _currentFade;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure canvas is set up correctly
            if (_fadeCanvas == null)
                _fadeCanvas = GetComponent<Canvas>();

            if (_fadeCanvas != null)
            {
                _fadeCanvas.sortingOrder = 9999; // Always on top
                _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Start with transparent
            if (_fadeImage != null)
            {
                Color c = _fadeImage.color;
                c.a = 0f;
                _fadeImage.color = c;
                _fadeImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// Fade from transparent to black
        /// </summary>
        public void FadeToBlack(float duration = -1, Action onComplete = null)
        {
            FadeToColor(_fadeToBlackColor, duration, onComplete);
        }

        /// <summary>
        /// Fade from black to transparent
        /// </summary>
        public void FadeFromBlack(float duration = -1, Action onComplete = null)
        {
            FadeFromColor(_fadeToBlackColor, duration, onComplete);
        }

        /// <summary>
        /// Fade from transparent to white
        /// </summary>
        public void FadeToWhite(float duration = -1, Action onComplete = null)
        {
            FadeToColor(_fadeToWhiteColor, duration, onComplete);
        }

        /// <summary>
        /// Fade from white to transparent
        /// </summary>
        public void FadeFromWhite(float duration = -1, Action onComplete = null)
        {
            FadeFromColor(_fadeToWhiteColor, duration, onComplete);
        }

        /// <summary>
        /// Fade to a specific color
        /// </summary>
        public void FadeToColor(Color color, float duration = -1, Action onComplete = null)
        {
            if (_currentFade != null)
                StopCoroutine(_currentFade);

            if (duration < 0)
                duration = _defaultFadeDuration;

            _currentFade = StartCoroutine(FadeRoutine(color, 1f, duration, onComplete));
        }

        /// <summary>
        /// Fade from a specific color to transparent
        /// </summary>
        public void FadeFromColor(Color color, float duration = -1, Action onComplete = null)
        {
            if (_currentFade != null)
                StopCoroutine(_currentFade);

            if (duration < 0)
                duration = _defaultFadeDuration;

            // Set the color first
            _fadeImage.color = color;

            _currentFade = StartCoroutine(FadeRoutine(color, 0f, duration, onComplete));
        }

        /// <summary>
        /// Set fade to fully opaque immediately (no animation)
        /// </summary>
        public void SetFadeOpaque(Color color)
        {
            if (_currentFade != null)
                StopCoroutine(_currentFade);

            if (_fadeImage != null)
            {
                _fadeImage.color = color;
                _fadeImage.raycastTarget = true;
            }
        }

        /// <summary>
        /// Set fade to fully transparent immediately (no animation)
        /// </summary>
        public void SetFadeTransparent()
        {
            if (_currentFade != null)
                StopCoroutine(_currentFade);

            if (_fadeImage != null)
            {
                Color c = _fadeImage.color;
                c.a = 0f;
                _fadeImage.color = c;
                _fadeImage.raycastTarget = false;
            }
        }

        private IEnumerator FadeRoutine(Color targetColor, float targetAlpha, float duration, Action onComplete)
        {
            if (_fadeImage == null)
            {
                Debug.LogError("[FadeTransition] Fade image is null!");
                onComplete?.Invoke();
                yield break;
            }

            // Block raycasts during fade
            _fadeImage.raycastTarget = true;

            Color startColor = _fadeImage.color;
            float startAlpha = startColor.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Color newColor = targetColor;
                newColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                _fadeImage.color = newColor;

                yield return null;
            }

            // Ensure final value
            Color finalColor = targetColor;
            finalColor.a = targetAlpha;
            _fadeImage.color = finalColor;

            // Only block raycasts if fully opaque
            _fadeImage.raycastTarget = (targetAlpha >= 0.99f);

            _currentFade = null;
            onComplete?.Invoke();
        }
    }
}