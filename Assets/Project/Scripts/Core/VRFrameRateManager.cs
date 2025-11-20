// Assets/Project/Scripts/Core/VRFrameRateManager.cs
using UnityEngine;
using UnityEngine.XR;

namespace ResidentialHVAC.Core
{
    /// <summary>
    /// Ensures VR maintains high frame rate (72-90 FPS) regardless of video playback
    /// CRITICAL for VR comfort and preventing motion sickness
    /// </summary>
    public class VRFrameRateManager : MonoBehaviour
    {
        [Header("Target Frame Rates")]
        [SerializeField] private int _vrTargetFrameRate = 90; // Quest 2/3 can do 90
        [SerializeField] private int _fallbackFrameRate = 72; // Minimum acceptable for VR

        [Header("Force Settings")]
        [SerializeField] private bool _disableVSync = true;
        [SerializeField] private bool _forceConstantReprojection = false;

        private static VRFrameRateManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeVRFrameRate();
        }

        private void Start()
        {
            // Double-check on Start
            EnforceVRFrameRate();
        }

        private void InitializeVRFrameRate()
        {
            Debug.Log("[VRFrameRate] Initializing VR frame rate settings...");

            // CRITICAL: Disable VSync - VR handles its own timing
            if (_disableVSync)
            {
                QualitySettings.vSyncCount = 0;
                Debug.Log("[VRFrameRate] VSync disabled");
            }

            // Set target frame rate
            EnforceVRFrameRate();

            // Try to set XR display refresh rate if available
            TrySetXRRefreshRate();
        }

        private void EnforceVRFrameRate()
        {
            // For Meta Quest and other VR headsets
            if (XRSettings.enabled)
            {
                // VR is active - remove frame rate limit and let headset use native refresh rate
                // Quest 2/3 will automatically use 72Hz, 90Hz, or 120Hz based on hardware
                Application.targetFrameRate = -1; // -1 = No limit (use native VR refresh rate)
                Debug.Log($"[VRFrameRate] VR detected - frame rate limit removed (native VR refresh rate)");
            }
            else
            {
                // Editor or non-VR fallback - use reasonable target
                Application.targetFrameRate = _fallbackFrameRate;
                Debug.Log($"[VRFrameRate] Non-VR mode - target frame rate set to {_fallbackFrameRate} FPS");
            }
        }

        private void TrySetXRRefreshRate()
        {
            // Note: XR display refresh rate APIs vary greatly between Unity versions
            // For Quest devices, the headset will automatically use its native refresh rate (72/90/120Hz)
            // We just need to ensure Application.targetFrameRate doesn't limit it

            if (XRSettings.enabled)
            {
                Debug.Log($"[VRFrameRate] XR is active - headset will use native refresh rate");

                // Set target frame rate high enough to not limit VR
                // Quest will naturally run at its native rate (72/90/120Hz depending on model)
                Application.targetFrameRate = -1; // Remove frame rate limit for VR

                Debug.Log("[VRFrameRate] Frame rate limit removed - VR will run at native refresh rate");
            }
            else
            {
                Debug.Log("[VRFrameRate] XR not detected - using fallback frame rate");
            }
        }

        /// <summary>
        /// Call this before playing video to ensure frame rate independence
        /// </summary>
        public static void PrepareForVideoPlayback()
        {
            if (_instance == null) return;

            Debug.Log("[VRFrameRate] Preparing for video playback - maintaining VR frame rate");

            // Ensure VSync is OFF (video might try to re-enable it)
            QualitySettings.vSyncCount = 0;

            // Re-enforce target frame rate
            _instance.EnforceVRFrameRate();
        }

        /// <summary>
        /// Call this after video playback completes
        /// </summary>
        public static void RestoreAfterVideoPlayback()
        {
            if (_instance == null) return;

            Debug.Log("[VRFrameRate] Video playback complete - VR frame rate maintained");

            // Re-enforce settings (just in case)
            _instance.EnforceVRFrameRate();
        }

        // Monitor frame rate in editor
        private void Update()
        {
#if UNITY_EDITOR
            // Log severe frame drops in editor
            if (Time.frameCount % 300 == 0) // Every ~5 seconds at 60fps
            {
                float fps = 1.0f / Time.smoothDeltaTime;
                if (fps < 60f)
                {
                    Debug.LogWarning($"[VRFrameRate] Low FPS detected: {fps:F1} (Target: {Application.targetFrameRate})");
                }
            }
#endif
        }
    }
}