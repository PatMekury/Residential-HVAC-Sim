// VRManagerInitializer.cs
using System.Collections;
using UnityEngine;

namespace ResidentialHVAC.VR
{
    /// <summary>
    /// Ensures OVRManager is properly initialized with correct settings for VR tracking
    /// This prevents camera rig distortion on scene load
    /// 
    /// Add this to a GameObject in your first scene (like FirstLoad)
    /// It will persist across scenes via DontDestroyOnLoad
    /// </summary>
    public class VRManagerInitializer : MonoBehaviour
    {
        private static VRManagerInitializer _instance;

        [Header("Tracking Settings")]
        [SerializeField]
        [Tooltip("Tracking origin type. FloorLevel is recommended for standing VR experiences.")]
        private OVRManager.TrackingOrigin _trackingOrigin = OVRManager.TrackingOrigin.FloorLevel;

        [SerializeField]
        [Tooltip("Use position tracking (recommended: true)")]
        private bool _usePositionTracking = true;

        [SerializeField]
        [Tooltip("Use IPD in position tracking (recommended: true for comfort)")]
        private bool _useIPDInPositionTracking = true;

        [Header("Initialization")]
        [SerializeField]
        [Tooltip("Recenter tracking on initialization")]
        private bool _recenterOnInit = true;

        [SerializeField]
        [Tooltip("Delay before initial recenter (allows tracking to stabilize)")]
        private float _initRecenterDelay = 1.0f;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLogs = true;

        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeOVRManager();
        }

        private void InitializeOVRManager()
        {
            OVRManager ovrManager = FindFirstObjectByType<OVRManager>();

            if (ovrManager == null)
            {
                if (_showDebugLogs)
                {
                    Debug.LogWarning("[VRManagerInitializer] OVRManager not found! Creating one...");
                }

                GameObject managerObj = new GameObject("OVRManager");
                ovrManager = managerObj.AddComponent<OVRManager>();
                DontDestroyOnLoad(managerObj);
            }

            // Configure tracking settings
            ConfigureTracking(ovrManager);

            // Recenter after delay to ensure tracking is stable
            if (_recenterOnInit)
            {
                StartCoroutine(RecenterAfterInit());
            }
        }

        private void ConfigureTracking(OVRManager manager)
        {
            if (manager == null) return;

            // Set tracking origin
            manager.trackingOriginType = _trackingOrigin;

            // Enable position tracking
            manager.usePositionTracking = _usePositionTracking;

            // Use IPD in position tracking for better comfort
            manager.useIPDInPositionTracking = _useIPDInPositionTracking;

            if (_showDebugLogs)
            {
                Debug.Log($"[VRManagerInitializer] OVRManager configured:");
                Debug.Log($"  - Tracking Origin: {_trackingOrigin}");
                Debug.Log($"  - Position Tracking: {_usePositionTracking}");
                Debug.Log($"  - IPD in Position Tracking: {_useIPDInPositionTracking}");
            }
        }

        private IEnumerator RecenterAfterInit()
        {
            if (_showDebugLogs)
            {
                Debug.Log($"[VRManagerInitializer] Waiting {_initRecenterDelay}s before initial recenter...");
            }

            yield return new WaitForSeconds(_initRecenterDelay);

            if (OVRManager.display != null)
            {
                OVRManager.display.RecenterPose();

                if (_showDebugLogs)
                {
                    Debug.Log("[VRManagerInitializer] Initial VR tracking recentered!");
                }
            }
            else
            {
                Debug.LogWarning("[VRManagerInitializer] OVRManager.display is null during init. Skipping recenter.");
            }
        }

        /// <summary>
        /// Manually recenter tracking (for debugging)
        /// </summary>
        [ContextMenu("Force Recenter")]
        public void ForceRecenter()
        {
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenterPose();
                Debug.Log("[VRManagerInitializer] Manually recentered tracking.");
            }
        }
    }
}