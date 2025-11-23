// VRTrackingRecenter.cs
using System.Collections;
using UnityEngine;

namespace ResidentialHVAC.VR
{
    /// <summary>
    /// Automatically recenters VR tracking when a scene loads
    /// This fixes the issue where OVRCameraRig starts in a distorted position
    /// until the user manually presses the Meta button to recenter
    /// 
    /// Attach this to the OVRCameraRig GameObject or any GameObject in the scene
    /// </summary>
    public class VRTrackingRecenter : MonoBehaviour
    {
        [Header("Recenter Settings")]
        [SerializeField]
        [Tooltip("Delay before recentering (seconds). Allows VR tracking to fully initialize.")]
        private float _recenterDelay = 0.5f;

        [SerializeField]
        [Tooltip("Force recenter on every scene load")]
        private bool _recenterOnSceneLoad = true;

        [SerializeField]
        [Tooltip("Recenter on Start (first load)")]
        private bool _recenterOnStart = true;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLogs = true;

        private void Start()
        {
            if (_recenterOnStart)
            {
                StartCoroutine(RecenterAfterDelay());
            }
        }

        private void OnEnable()
        {
            if (_recenterOnSceneLoad)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Recenter tracking when a new scene loads
            StartCoroutine(RecenterAfterDelay());
        }

        private IEnumerator RecenterAfterDelay()
        {
            if (_showDebugLogs)
            {
                Debug.Log($"[VRTrackingRecenter] Waiting {_recenterDelay}s before recentering...");
            }

            // Wait for VR tracking system to fully initialize
            yield return new WaitForSeconds(_recenterDelay);

            RecenterTracking();
        }

        /// <summary>
        /// Recenters the VR tracking origin
        /// This is equivalent to pressing the Meta button to "reset view"
        /// </summary>
        public void RecenterTracking()
        {
            if (OVRManager.display != null)
            {
                OVRManager.display.RecenterPose();

                if (_showDebugLogs)
                {
                    Debug.Log("[VRTrackingRecenter] VR tracking recentered successfully!");
                }
            }
            else
            {
                Debug.LogWarning("[VRTrackingRecenter] OVRManager.display is null. Cannot recenter.");
            }
        }

        /// <summary>
        /// Call this manually to recenter immediately (for debugging)
        /// </summary>
        [ContextMenu("Recenter Now")]
        public void RecenterNow()
        {
            RecenterTracking();
        }
    }
}