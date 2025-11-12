// Assets/Project/Scripts/Loading/OVRSetup.cs
using ResidentialHVAC.Loading;
using UnityEngine;

namespace ResidentialHVAC.Loading
{
    /// <summary>
    /// Simple OVR initialization script.
    /// Ensures OVRManager exists and is properly configured.
    /// Based on First Hand's OVRSetup implementation.
    /// </summary>
    public class OVRSetup : MonoBehaviour
    {
        private void Awake()
        {
            // Ensure OVRManager exists
            OVRManager ovrManager = FindFirstObjectByType<OVRManager>();
            
            if (ovrManager == null)
            {
                Debug.LogWarning("[OVRSetup] OVRManager not found! Creating one...");
                GameObject managerObj = new GameObject("OVRManager");
                managerObj.AddComponent<OVRManager>();
                DontDestroyOnLoad(managerObj);
            }
            
            // Persist this setup object
            DontDestroyOnLoad(gameObject);
        }
    }
}

