// Assets/Project/Scripts/Core/VRFrameRateManager.cs
using UnityEngine;

namespace ResidentialHVAC.Core
{
    /// <summary>
    /// Ensures VR maintains high frame rate (72-90 FPS) using Meta's OVRPlugin
    /// CRITICAL for VR comfort and preventing motion sickness
    /// FIXED: Now uses Meta's OVRPlugin instead of legacy Unity XR APIs
    /// </summary>
    public class VRFrameRateManager : MonoBehaviour
    {
        [Header("Target Frame Rates")]
        [SerializeField] private int _vrTargetFrameRate = 90; // Quest 2/3 can do 90Hz
        [SerializeField] private int _fallbackFrameRate = 72; // Minimum acceptable for VR

        [Header("Force Settings")]
        [SerializeField] private bool _disableVSync = true;

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
            LogCurrentDisplayFrequency();
        }

        private void InitializeVRFrameRate()
        {
            Debug.Log("[VRFrameRate] Initializing Meta XR frame rate settings...");

            // CRITICAL: Disable VSync - Meta XR handles its own timing
            if (_disableVSync)
            {
                QualitySettings.vSyncCount = 0;
                Debug.Log("[VRFrameRate] VSync disabled for Meta XR");
            }

            // Set target frame rate using Meta SDK
            EnforceVRFrameRate();
        }

        private void EnforceVRFrameRate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // CRITICAL: Use Meta's OVRPlugin to set display frequency
            try
            {
                // Get available display frequencies from Meta SDK
                float[] availableFreqs = OVRPlugin.systemDisplayFrequenciesAvailable;
                
                if (availableFreqs != null && availableFreqs.Length > 0)
                {
                    Debug.Log($"[VRFrameRate] Available Meta XR frequencies: {string.Join(", ", availableFreqs)} Hz");
                    
                    // Try to set to target frequency (90Hz preferred, then 72Hz)
                    float targetFreq = _vrTargetFrameRate;
                    
                    // Check if target is available, otherwise use fallback
                    bool targetAvailable = System.Array.Exists(availableFreqs, freq => Mathf.Approximately(freq, targetFreq));
                    if (!targetAvailable)
                    {
                        targetFreq = _fallbackFrameRate;
                        bool fallbackAvailable = System.Array.Exists(availableFreqs, freq => Mathf.Approximately(freq, targetFreq));
                        
                        if (!fallbackAvailable)
                        {
                            // Use highest available frequency
                            targetFreq = availableFreqs[availableFreqs.Length - 1];
                        }
                    }
                    
                    // CRITICAL: Set display frequency using OVRPlugin (Meta's proper API)
                    OVRPlugin.systemDisplayFrequency = targetFreq;
                    
                    // Also set Unity's target frame rate to match
                    Application.targetFrameRate = (int)targetFreq;
                    
                    Debug.Log($"[VRFrameRate] ✓ Meta XR display frequency set to {targetFreq} Hz");
                }
                else
                {
                    Debug.LogWarning("[VRFrameRate] No display frequencies available from OVRPlugin");
                    Application.targetFrameRate = _vrTargetFrameRate;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VRFrameRate] Error setting Meta XR display frequency: {e.Message}");
                Application.targetFrameRate = _vrTargetFrameRate;
            }
#else
            // Editor mode - just set Unity target frame rate
            Application.targetFrameRate = _fallbackFrameRate;
            Debug.Log($"[VRFrameRate] Editor mode - target frame rate set to {_fallbackFrameRate} FPS");
#endif
        }

        private void LogCurrentDisplayFrequency()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                float currentFreq = OVRPlugin.systemDisplayFrequency;
                Debug.Log($"[VRFrameRate] Current Meta XR display frequency: {currentFreq} Hz");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VRFrameRate] Could not query current display frequency: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Call this before playing video to ensure frame rate independence
        /// </summary>
        public static void PrepareForVideoPlayback()
        {
            if (_instance == null) return;

            Debug.Log("[VRFrameRate] Preparing for video playback - maintaining Meta XR frame rate");

            // CRITICAL: Ensure VSync is OFF (video might try to re-enable it)
            QualitySettings.vSyncCount = 0;

            // Re-enforce target frame rate using Meta SDK
            _instance.EnforceVRFrameRate();
            
            // Log current frequency for debugging
            _instance.LogCurrentDisplayFrequency();
        }

        /// <summary>
        /// Call this after video playback completes
        /// </summary>
        public static void RestoreAfterVideoPlayback()
        {
            if (_instance == null) return;

            Debug.Log("[VRFrameRate] Restoring after video playback - enforcing Meta XR frame rate");

            // CRITICAL: Force re-application of display frequency
            QualitySettings.vSyncCount = 0;
            _instance.EnforceVRFrameRate();
            
            // Log to verify restoration
            _instance.LogCurrentDisplayFrequency();
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Monitor frame rate in editor
            if (Time.frameCount % 300 == 0)
            {
                float fps = 1.0f / Time.smoothDeltaTime;
                if (fps < 60f)
                {
                    Debug.LogWarning($"[VRFrameRate] Low FPS detected: {fps:F1} (Target: {Application.targetFrameRate})");
                }
            }
#else
            // In build: Periodically verify display frequency hasn't changed
            if (Time.frameCount % 600 == 0) // Every ~10 seconds at 60fps
            {
                LogCurrentDisplayFrequency();
            }
#endif
        }
    }
}