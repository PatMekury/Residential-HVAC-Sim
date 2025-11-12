// Assets/Project/Scripts/UI/IntroCinematic.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace ResidentialHVAC.UI
{
    /// <summary>
    /// Plays an intro cinematic when MainMenu loads
    /// Supports both Video Player and AudioSource for SFX
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

        private void Start()
        {
            if (_playOnStart)
            {
                PlayCinematic();
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

            // 2. Prepare video if available
            if (_videoPlayer != null && _videoCanvas != null)
            {
                _videoCanvas.SetActive(true);
                _videoPlayer.Prepare();
                while (!_videoPlayer.isPrepared)
                {
                    yield return null;
                }
            }

            // 3. Fade in from black
            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;
                FadeTransition.Instance.FadeFromBlack(_fadeInDuration, () => fadeComplete = true);
                while (!fadeComplete)
                {
                    yield return null;
                }
            }

            // 4. Play video if available
            if (_videoPlayer != null)
            {
                _videoPlayer.Play();
            }

            // 5. Play audio (SFX_IntroCinematic)
            if (_audioSource != null && _introCinematicClip != null)
            {
                _audioSource.clip = _introCinematicClip;
                _audioSource.Play();

                // Wait for audio to finish
                yield return new WaitForSeconds(_introCinematicClip.length);
            }
            else if (_videoPlayer != null)
            {
                // Wait for video to finish if no audio
                while (_videoPlayer.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                // No video or audio, just wait a bit
                yield return new WaitForSeconds(2f);
            }

            // 6. Fade out to black
            if (FadeTransition.Instance != null)
            {
                bool fadeComplete = false;
                FadeTransition.Instance.FadeToBlack(_fadeOutDuration, () => fadeComplete = true);
                while (!fadeComplete)
                {
                    yield return null;
                }
            }

            // 7. Hide video canvas
            if (_videoCanvas != null)
            {
                _videoCanvas.SetActive(false);
            }

            // 8. Small delay
            yield return new WaitForSeconds(_delayBeforeMenu);

            // 9. Fade in to reveal menu
            if (FadeTransition.Instance != null)
            {
                FadeTransition.Instance.FadeFromBlack(_fadeInDuration);
            }

            _isPlaying = false;

            // 10. Call completion event
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
    }
}