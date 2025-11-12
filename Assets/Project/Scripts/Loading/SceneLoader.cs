// Assets/Project/Scripts/Loading/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResidentialHVAC.Loading
{
    /// <summary>
    /// Simple scene loader component.
    /// Based on First Hand's SceneLoader implementation adapted for newer Meta SDK.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Name of the scene to load (must match scene name in Build Settings)")]
        private string _sceneName;

        [SerializeField]
        [Tooltip("Music ID to play when this scene loads (optional)")]
        private string _musicId = "";

        [SerializeField]
        [Tooltip("Automatically load this scene on Start if this GameObject is active")]
        private bool _loadOnStart = true;

        private void Start()
        {
            if (_loadOnStart && gameObject.activeSelf)
            {
                LoadScene();
            }
        }

        /// <summary>
        /// Load the configured scene
        /// </summary>
        public void LoadScene()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene name is empty on {gameObject.name}!");
                return;
            }

            // Play music if specified
            if (!string.IsNullOrEmpty(_musicId) && MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play(_musicId);
            }

            // Load the scene
            Debug.Log($"[SceneLoader] Loading scene: {_sceneName}");
            SceneManager.LoadScene(_sceneName);
        }

        /// <summary>
        /// Load the scene asynchronously
        /// </summary>
        public void LoadSceneAsync()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene name is empty on {gameObject.name}!");
                return;
            }

            // Play music if specified
            if (!string.IsNullOrEmpty(_musicId) && MasterMusic.Instance != null)
            {
                MasterMusic.Instance.Play(_musicId);
            }

            // Load the scene asynchronously
            Debug.Log($"[SceneLoader] Loading scene async: {_sceneName}");
            SceneManager.LoadSceneAsync(_sceneName);
        }
    }
}