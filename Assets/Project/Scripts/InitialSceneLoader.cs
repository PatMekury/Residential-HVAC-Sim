// InitialSceneLoader.cs
using UnityEngine;
using System.Threading.Tasks;


    public class InitialSceneLoader : MonoBehaviour
    {
        [Tooltip("Name of the MainMenu level defined in GameLoader.")]
        public string mainMenuLevelName = "MainMenu"; // Ensure this matches a level name in GameLoader

        async void Start()
        {
            if (GameLoader.Instance == null)
            {
                Debug.LogError("GameLoader not found! Ensure GameLoader is in the FirstLoad scene.");
                return;
            }

            // Ensure OVRCameraRig persists if it's in this FirstLoad scene
            OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                DontDestroyOnLoad(cameraRig.gameObject);
            }
            else
            {
                Debug.LogWarning("OVRCameraRig not found in FirstLoad scene. Ensure your VR setup is persistent if needed.");
            }

            // Load and immediately activate the MainMenu level.
            // The ActivateAndUnloadOthers will handle unloading FirstLoad itself.
            await GameLoader.LoadLevel(mainMenuLevelName, true);
        }
    }
