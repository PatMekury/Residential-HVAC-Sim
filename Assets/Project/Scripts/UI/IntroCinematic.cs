// Assets/Project/Scripts/UI/IntroCinematic.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using ResidentialHVAC.Core;

namespace ResidentialHVAC.UI
{
    /// <summary>
    /// Plays an intro cinematic when MainMenu loads
    /// Supports both Video Player and AudioSource for SFX
    /// OPTIMIZED: Uses event-based callbacks + VR frame rate independence
    /// </summary>
    public class IntroCinematic : MonoBehaviour
    {
        [Header("Cinematic Settings")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _canSkip = true;
        [SerializeField] private KeyCode _skipKey = KeyCode.Space;

        [Header("Video (Optional)")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private GameObject _videoCanvas;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _introCinematicClip;

        [Header("Timing")]
        [SerializeField] private float _fadeInDuration = 1.0f;
        [SerializeField] private float _fadeOutDuration = 1.0f;
        [SerializeField] private float _delayBeforeMenu = 0.5f;

        [Header("Events")]
        [SerializeField] private UnityEngine.Events.UnityEvent _onCinematicComplete;

        private bool _isPlaying = false;
        private bool _hasPlayed = false;

        private void Awake()
        {
            // CRITICAL: Configure VideoPlayer for VR frame rate independence
            ConfigureVideoPlayerForVR();
        }

        private void Start()
        {
            if (_playOnStart)
            {
                PlayCinematic();
            }
        }

        /// <summary>
        /// Configure VideoPlayer to not lock VR frame rate
        /// </summary>
        private void ConfigureVideoPlayerForVR()
        {
            if (_videoPlayer != null)
            {
                // CRITICAL: Skip video frames if needed to maintain VR frame rate
                // This allows video to drop frames instead of locking VR to 24 FPS
                _videoPlayer.skipOnDrop = true;

                // Wait for first frame to ensure video renders properly
                _videoPlayer.waitForFirstFrame = true;

                // DON'T override renderMode - keep whatever is set in Inspector
                // (Could be RenderTexture, CameraFarPlane, etc.)

                // Ensure playback speed is correct
                _videoPlayer.playbackSpeed = 1.0f;

                Debug.Log($"[IntroCinematic] VideoPlayer configured for VR (renderMode: {_videoPlayer.renderMode}, skipOnDrop: true)");
            }
        }

        private void Update()
        {
            // Allow skipping
            if (_isPlaying && _canSkip && Input.GetKeyDown(_skipKey))
            {
                SkipCinematic();
            }
        }

        /// <summary>
        /// Play the intro cinematic
        /// </summary>
        public void PlayCinematic()
        {
            if (_hasPlayed)
            {
                Debug.Log("[IntroCinematic] Already played, skipping");
                OnCinematicComplete();
                return;
            }

            _hasPlayed = true;
            StartCoroutine(CinematicSequence());
        }

        private IEnumerator CinematicSequence()
        {
            _isPlaying = true;

            // 1. Start with black screen
            if (FadeTransition.Instance != null)
            {
                FadeTransition.Instance.SetFadeOpaque(Color.black);
            }

            // 2. OPTIMIZED: Prepare video using event callback instead of polling
            if (_videoPlayer != null && _videoCanvas != null)
            {
                _videoCanvas.SetActive(true);

                bool prepareComplete = false;
                _videoPlayer.prepareCompleted += (VideoPlayer source) =>
                {
                    prepareComplete = true;
                    Debug.Log("[IntroCinematic] Video prepared");
                };

                _videoPlayer.Prepare();

                // OPTIMIZED: Use WaitUntil with timeout
                float prepareTimeout = 5f; // 5 second timeout
                float prepareTimer = 0f;

                while (!prepareComplete && prepareTimer < prepareTimeout)
                {
                    prepareTimer += Time.deltaTime;
                    yield return null;
                }

                if (!prepareComplete)
                {
                    Debug.LogWarning("[IntroCinematic] Video preparation timed out after 5 seconds");
                }
            }

            // 3. Fade in from black
            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;
                FadeTransition.Instance.FadeFromBlack(_fadeInDuration, () =>
                {
                    fadeComplete = true;
                    Debug.Log("[IntroCinematic] Fade in complete");

                    // OPTIMIZATION: Disable fade canvas during video playback for max performance
                    if (FadeTransition.Instance != null)
                    {
                        Canvas fadeCanvas = FadeTransition.Instance.GetComponent<Canvas>();
                        if (fadeCanvas != null)
                        {
                            fadeCanvas.enabled = false;
                            Debug.Log("[IntroCinematic] Fade canvas disabled during video - performance boost");
                        }
                    }
                });

                // OPTIMIZED: Use WaitUntil instead of tight while loop
                yield return new WaitUntil(() => fadeComplete);
            }

            // 4. CRITICAL: Prepare VR frame rate before video starts
            VRFrameRateManager.PrepareForVideoPlayback();

            // 5. Play video if available
            if (_videoPlayer != null)
            {
                _videoPlayer.Play();
                Debug.Log("[IntroCinematic] Video playing at VR-independent frame rate");
            }

            // 6. OPTIMIZED: Wait for completion using appropriate method
            if (_audioSource != null && _introCinematicClip != null)
            {
                // Play audio (SFX_IntroCinematic)
                _audioSource.clip = _introCinematicClip;
                _audioSource.Play();

                // Wait for audio to finish
                yield return new WaitForSeconds(_introCinematicClip.length);
            }
            else if (_videoPlayer != null)
            {
                // OPTIMIZED: Use event callback with timeout
                bool videoComplete = false;
                _videoPlayer.loopPointReached += (VideoPlayer source) =>
                {
                    videoComplete = true;
                    Debug.Log("[IntroCinematic] Video finished (loopPointReached)");
                };

                // Wait for video to complete with timeout
                float videoTimeout = 300f; // 5 minute max video length
                float videoTimer = 0f;

                while (!videoComplete && videoTimer < videoTimeout && _videoPlayer.isPlaying)
                {
                    videoTimer += Time.deltaTime;
                    yield return null;
                }

                if (videoTimer >= videoTimeout)
                {
                    Debug.LogWarning("[IntroCinematic] Video playback timed out");
                }
                else if (!_videoPlayer.isPlaying && !videoComplete)
                {
                    Debug.Log("[IntroCinematic] Video stopped playing (detected via isPlaying check)");
                }
            }
            else
            {
                // No video or audio, just wait a bit
                yield return new WaitForSeconds(2f);
            }

            // OPTIMIZATION: Re-enable fade canvas before fading out
            if (FadeTransition.Instance != null)
            {
                Canvas fadeCanvas = FadeTransition.Instance.GetComponent<Canvas>();
                if (fadeCanvas != null && !fadeCanvas.enabled)
                {
                    fadeCanvas.enabled = true;
                    Debug.Log("[IntroCinematic] Fade canvas re-enabled for transition");
                }
            }

            // CRITICAL: Restore VR frame rate after video
            VRFrameRateManager.RestoreAfterVideoPlayback();

            // 7. Fade out to black
            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;
                FadeTransition.Instance.FadeToBlack(_fadeOutDuration, () =>
                {
                    fadeComplete = true;
                    Debug.Log("[IntroCinematic] Fade out complete");
                });

                // OPTIMIZED: Use WaitUntil instead of tight while loop
                yield return new WaitUntil(() => fadeComplete);
            }

            // 8. Hide video canvas
            if (_videoCanvas != null)
            {
                _videoCanvas.SetActive(false);
            }

            // 9. Small delay
            yield return new WaitForSeconds(_delayBeforeMenu);

            // 10. Fade in to reveal menu
            if (FadeTransition.Instance != null)
            {
                FadeTransition.Instance.FadeFromBlack(_fadeInDuration);
            }

            _isPlaying = false;

            // 11. Call completion event
            OnCinematicComplete();
        }

        /// <summary>
        /// Skip the cinematic immediately
        /// </summary>
        public void SkipCinematic()
        {
            if (!_isPlaying) return;

            Debug.Log("[IntroCinematic] Skipping cinematic");

            StopAllCoroutines();

            // Stop video
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Stop();
            }

            // Stop audio
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            // Hide video canvas
            if (_videoCanvas != null)
            {
                _videoCanvas.SetActive(false);
            }

            // CRITICAL: Restore VR frame rate after skipping video
            VRFrameRateManager.RestoreAfterVideoPlayback();

            // Fade in to menu
            if (FadeTransition.Instance != null)
            {
                FadeTransition.Instance.FadeFromBlack(_fadeInDuration);
            }

            _isPlaying = false;

            OnCinematicComplete();
        }

        private void OnCinematicComplete()
        {
            Debug.Log("[IntroCinematic] Cinematic complete");
            _onCinematicComplete?.Invoke();
        }

        private void OnDestroy()
        {
            // Clean up video player event listeners
            // Note: Since we use anonymous lambdas, they auto-cleanup when object is destroyed
            // Just stop any playing video to be safe
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Stop();
            }
        }
    }
}