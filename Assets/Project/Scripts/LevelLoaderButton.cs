// LevelLoaderButton.cs
using UnityEngine;
using UnityEngine.UI;


    [RequireComponent(typeof(Button))]
    public class LevelLoaderButton : MonoBehaviour
    {
        [Tooltip("The name of the Level to load (must be defined in GameLoader's Level list).")]
        public string levelName;

        private Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(LoadTargetLevel);
        }

        async void LoadTargetLevel()
        {
            if (string.IsNullOrEmpty(levelName))
            {
                Debug.LogError("LevelName not set on LevelLoaderButton.", this);
                return;
            }

            if (GameLoader.Instance != null)
            {
                // This will load the new level and activate it,
                // and GameLoader's ActivateAndUnloadOthers will handle deactivating/unloading the current MainMenu level.
                await GameLoader.ActivateAndUnloadOthers(levelName);
            }
            else
            {
                Debug.LogError("GameLoader instance not found!");
            }
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(LoadTargetLevel);
            }
        }
    }
