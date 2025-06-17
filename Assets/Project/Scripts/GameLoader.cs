// Copyright (c) Reality Collab, HCC.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;


    public class GameLoader : MonoBehaviour
    {
        private static GameLoader _instance;
        public static GameLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameLoader>();
#if UNITY_EDITOR
                    if (!_instance && Application.isEditor && !Application.isPlaying)
                    {
                        Debug.LogWarning("GameLoader instance not found. Play from 'FirstLoad' scene for proper initialization.");
                    }
                    else if (!_instance && Application.isPlaying)
                    {
                         Debug.LogError("GameLoader instance is null and couldn't be found. Ensure it's present in your 'FirstLoad' scene and properly initialized.");
                    }
#endif
                }
                return _instance;
            }
        }

        private static bool _triedFindObjectsInEditor;
        public static bool ExistsInEditor
        {
            get
            {
                if (_instance == null && !_triedFindObjectsInEditor)
                {
                    _triedFindObjectsInEditor = true;
                    _instance = FindObjectOfType<GameLoader>();
                }
                return _instance != null;
            }
        }

        private static List<Level> _loadedLevels = new List<Level>();
        private static Coroutine _tetrahedralizationProbesRoutine;

        [Tooltip("Define all game levels here. Each level can consist of one or more scenes.")]
        [SerializeField]
        private List<Level> _levels = new List<Level>();

        public List<Level> Levels => _levels;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate GameLoader instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LightProbes.needsRetetrahedralization += MarkProbesDirty;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                LightProbes.needsRetetrahedralization -= MarkProbesDirty;
                _instance = null;
                _loadedLevels.Clear();
                _triedFindObjectsInEditor = false;
            }
        }

        public static Level GetLevel(string name)
        {
            if (NoInstance($"getting level '{name}'")) return null;
            Level level = Instance._levels.Find(x => x._name == name);
            if (level == null) Debug.LogError($"Couldn't find level scriptable object for '{name}' in GameLoader's list!");
            return level;
        }

        public static Level FindLevel(Predicate<Level> predicate)
        {
            if (NoInstance("finding level with predicate")) return null;
            return Instance._levels.Find(predicate);
        }

        public static async Task LoadLevel(string name, bool activateOnLoad = false)
        {
            if (NoInstance($"loading level '{name}'")) return;

            var levelToLoad = GetLevel(name);
            if (levelToLoad == null)
            {
                Debug.LogError($"Level '{name}' not found in GameLoader configuration.");
                return;
            }

            if (_loadedLevels.Contains(levelToLoad) || levelToLoad.state != Level.State.None) {
                Debug.LogWarning($"Level '{name}' is already loaded or in an active state ({levelToLoad.state}). Skipping load.");
                if (activateOnLoad && levelToLoad.state == Level.State.Loaded) {
                     await ActivateAndUnloadOthers(name);
                }
                return;
            }

            await levelToLoad.Load();
            _loadedLevels.Add(levelToLoad);

            if(activateOnLoad)
            {
                await ActivateAndUnloadOthers(name);
            }
        }

        private static bool NoInstance(string contextMessage)
        {
            bool noInstance = Instance == null;
            if (noInstance) Debug.LogError($"No GameLoader instance available while {contextMessage}. Please ensure GameLoader is in the 'FirstLoad' scene and the game starts from there.");
            return noInstance;
        }

        public static async Task ActivateAndUnloadOthers(string name)
        {
            if (NoInstance($"activating level '{name}' and unloading others")) return;

            var levelToActivate = GetLevel(name);
            if (levelToActivate == null)
            {
                Debug.LogError($"Cannot activate level '{name}': Not found in GameLoader configuration.");
                return;
            }
             if (levelToActivate.state == Level.State.Active)
            {
                Debug.Log($"Level '{name}' is already active.");
                return;
            }

            if (levelToActivate.state == Level.State.Loading)
            {
                await levelToActivate.CurrentTask();
            }
            
            if (levelToActivate.state != Level.State.Loaded && levelToActivate.state != Level.State.None)
            {
                 Debug.LogWarning($"Level '{name}' is in state {levelToActivate.state}, cannot activate directly. Attempting to load first.");
                 if(levelToActivate.state == Level.State.None)
                 {
                    await LoadLevel(name, false);
                 }
                 else {
                    Debug.LogError($"Level '{name}' is in an unexpected state ({levelToActivate.state}) for activation. Please ensure it's loaded first or handle this state.");
                    return;
                 }
            }

            for (int i = _loadedLevels.Count - 1; i >= 0; i--)
            {
                Level loadedLevel = _loadedLevels[i];
                if (loadedLevel == levelToActivate) continue;

                if (loadedLevel.state == Level.State.Loading) await loadedLevel.CurrentTask();
                if (loadedLevel.state == Level.State.Loaded) await loadedLevel.Activate();
                if (loadedLevel.state == Level.State.Activating) await loadedLevel.CurrentTask();
                if (loadedLevel.state == Level.State.Active) await loadedLevel.Deactivate();
            }
            
            if (levelToActivate.state == Level.State.Loaded)
            {
                 await levelToActivate.Activate();
            }
            else
            {
                Debug.LogError($"Level '{name}' could not be activated as it was not in a 'Loaded' state. Current state: {levelToActivate.state}");
                if (levelToActivate.state == Level.State.None) {
                    Debug.Log($"Attempting to load and activate '{name}'...");
                    await LoadLevel(name, true);
                }
                return;
            }

            for (int i = _loadedLevels.Count - 1; i >= 0; i--)
            {
                Level loadedLevel = _loadedLevels[i];
                if (loadedLevel == levelToActivate) continue;

                if (loadedLevel.state == Level.State.Deactivated)
                {
                    await loadedLevel.Unload();
                    _loadedLevels.RemoveAt(i);
                }
                else if(loadedLevel.state == Level.State.Loaded)
                {
                     await loadedLevel.Unload();
                    _loadedLevels.RemoveAt(i);
                }
            }

            try
            {
                Scene firstLoadScene = SceneManager.GetSceneByName("FirstLoad");
                if (firstLoadScene.IsValid() && firstLoadScene.isLoaded)
                {
                    bool isFirstLoadPartOfLevel = false;
                    foreach(string sceneNameInLevel in levelToActivate._scenes) { // Renamed variable for clarity
                        if(sceneNameInLevel == "FirstLoad") {
                            isFirstLoadPartOfLevel = true;
                            break;
                        }
                    }
                    if(!isFirstLoadPartOfLevel) {
                         SceneManager.UnloadSceneAsync(firstLoadScene);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not unload FirstLoad scene: {ex.Message}");
            }
        }

        private static void MarkProbesDirty()
        {
            if (Instance == null || _tetrahedralizationProbesRoutine != null) return;
            _tetrahedralizationProbesRoutine = Instance.StartCoroutine(UpdateProbesRoutine());
        }

        private static IEnumerator UpdateProbesRoutine()
        {
            yield return new WaitWhile(() => _loadedLevels.Exists(x => x.state == Level.State.Activating));
            yield return null;

            bool complete = false;
            Action setComplete = () => complete = true;

            LightProbes.tetrahedralizationCompleted += setComplete;
            LightProbes.TetrahedralizeAsync();

            yield return new WaitUntil(() => complete);

            LightProbes.tetrahedralizationCompleted -= setComplete;
            _tetrahedralizationProbesRoutine = null;
        }

        public static async Task UnloadAll()
        {
            if (NoInstance("unloading all levels")) return;

            for (int i = _loadedLevels.Count - 1; i >= 0; i--)
            {
                Level level = _loadedLevels[i];
                if (level.state == Level.State.Active) await level.Deactivate();
                if (level.state == Level.State.Deactivated || level.state == Level.State.Loaded) await level.Unload();
            }
            _loadedLevels.Clear();
        }

        [Serializable]
        public class Level
        {
            public static readonly State StateNone = 0;

            [SerializeField]
            public string _name;

            [Tooltip("Index of the scene within this level to set as active (e.g., for lighting). -1 for none.")]
            [SerializeField]
            public int _activeIndex = -1;

            [SerializeField]
            public List<string> _scenes = new List<string>();

            public State state { get; private set; }

            private List<AsyncOperation> _ops = new List<AsyncOperation>();
            private TaskCompletionSource<bool> _currentTaskCompletionSource;

            public Task Load()
            {
                if (state != StateNone) {
                    Debug.LogWarning($"Cant Load level '{_name}' while it's in state {state}. Returning current/completed task.");
                    return _currentTaskCompletionSource?.Task ?? Task.CompletedTask;
                }

                state = State.Loading;
                _ops.Clear();
                for (int i = 0; i < _scenes.Count; i++)
                {
                    Scene existingScene = SceneManager.GetSceneByName(_scenes[i]);
                    if (existingScene.isLoaded)
                    {
                        Debug.Log($"Scene '{_scenes[i]}' is already loaded, skipping additive load for it.");
                        continue;
                    }
                    var op = SceneManager.LoadSceneAsync(_scenes[i], LoadSceneMode.Additive);
                    if (op == null) {
                        Debug.LogError($"Failed to start loading scene: {_scenes[i]} for level {_name}. Check build settings.");
                        continue;
                    }
                    op.allowSceneActivation = false;
                    _ops.Add(op);
                }

                _currentTaskCompletionSource = new TaskCompletionSource<bool>();
                CoroutineRunner.Run(LoadRoutine());
                return _currentTaskCompletionSource.Task;
            }
            IEnumerator LoadRoutine()
            {
                yield return null;
                while (_ops.Exists(op => op != null && op.progress < 0.9f))
                {
                    yield return null;
                }
                state = State.Loaded;
                _currentTaskCompletionSource?.SetResult(true);
            }

            public Task Activate()
            {
                 if (state != State.Loaded) {
                    Debug.LogWarning($"Cant Activate level '{_name}' while it's in state {state}. Returning current/completed task.");
                    return _currentTaskCompletionSource?.Task ?? Task.CompletedTask;
                }

                state = State.Activating;
                for (int i = 0; i < _ops.Count; i++)
                {
                    if(_ops[i] != null) _ops[i].allowSceneActivation = true;
                }

                _currentTaskCompletionSource = new TaskCompletionSource<bool>();
                CoroutineRunner.Run(ActivateRoutine());
                return _currentTaskCompletionSource.Task;
            }

            IEnumerator ActivateRoutine()
            {
                yield return null;
                while (_ops.Exists(op => op != null && !op.isDone))
                {
                    yield return null;
                }

                if (_activeIndex >= 0 && _activeIndex < _scenes.Count)
                {
                    Scene sceneToMakeActive = SceneManager.GetSceneByName(_scenes[_activeIndex]);
                    if (sceneToMakeActive.IsValid() && sceneToMakeActive.isLoaded)
                    {
                        bool success = SceneManager.SetActiveScene(sceneToMakeActive);
                        if (!success)
                        {
                            Debug.LogError($"Could not set '{sceneToMakeActive.name}' as the Active scene for level '{_name}'!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Scene '{_scenes[_activeIndex]}' for level '{_name}' not valid or not loaded, cannot set as active.");
                    }
                }

                state = State.Active;
                _ops.Clear();
                _currentTaskCompletionSource?.SetResult(true);
            }

            public Task Deactivate()
            {
                if (state != State.Active) {
                     Debug.LogWarning($"Cant Deactivate level '{_name}' while it's in state {state}.");
                     return Task.CompletedTask;
                }

                for (int i = _scenes.Count - 1; i >= 0; i--)
                {
                    if (_scenes[i] == Instance.gameObject.scene.name) continue;

                    Scene scene = SceneManager.GetSceneByName(_scenes[i]);
                    if (!scene.IsValid() || !scene.isLoaded) continue;

                    var roots = scene.GetRootGameObjects();
                    foreach (var go in roots)
                    {
                        if (go == Instance.gameObject) continue;
                        // CORRECTED CHECK: Check if the GameObject's scene is the "DontDestroyOnLoad" scene
                        if (go.GetComponent<OVRCameraRig>() != null && go.scene.name == "DontDestroyOnLoad") continue;

                        UnityEngine.Object.Destroy(go);
                    }
                }
                state = State.Deactivated;
                return Task.CompletedTask;
            }

            public Task Unload()
            {
                if (!(state == State.Deactivated || state == State.Loaded)) {
                     Debug.LogWarning($"Cant Unload level '{_name}' while it's in state {state}.");
                     return _currentTaskCompletionSource?.Task ?? Task.CompletedTask;
                }

                state = State.Unloading;
                _ops.Clear();
                for (int i = 0; i < _scenes.Count; i++)
                {
                    if (_scenes[i] == Instance.gameObject.scene.name) continue;
                    Scene sceneToUnload = SceneManager.GetSceneByName(_scenes[i]);
                    if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
                    {
                        bool isPersistentRigScene = false;
                        GameObject[] rootObjects = sceneToUnload.GetRootGameObjects();
                        foreach(var rootObj in rootObjects)
                        {
                            // CORRECTED CHECK: Check if the GameObject's scene is the "DontDestroyOnLoad" scene
                            if(rootObj.GetComponent<OVRCameraRig>() != null && rootObj.scene.name == "DontDestroyOnLoad")
                            {
                                isPersistentRigScene = true;
                                break;
                            }
                        }
                        if(isPersistentRigScene)
                        {
                            Debug.Log($"Scene '{_scenes[i]}' contains persistent OVRCameraRig, skipping unload for it.");
                            continue;
                        }

                        var op = SceneManager.UnloadSceneAsync(_scenes[i]);
                        if (op != null) _ops.Add(op);
                    }
                }

                _currentTaskCompletionSource = new TaskCompletionSource<bool>();
                CoroutineRunner.Run(UnloadRoutine());
                return _currentTaskCompletionSource.Task;
            }
            IEnumerator UnloadRoutine()
            {
                yield return null;
                while (_ops.Exists(op => op != null && !op.isDone))
                {
                    yield return null;
                }
                state = StateNone;
                _ops.Clear();
                _currentTaskCompletionSource?.SetResult(true);
            }

            public Task CurrentTask()
            {
                return _currentTaskCompletionSource?.Task;
            }

            [Flags]
            public enum State
            {
                None = 0,
                Loading = 1 << 0,
                Loaded = 1 << 1,
                Activating = 1 << 2,
                Active = 1 << 3,
                Deactivated = 1 << 4,
                Unloading = 1 << 5
            }

#if UNITY_EDITOR
            public static Level FromLoadedScenes()
            {
                var result = new Level();
                result.state = State.Loaded;
                result._name = "EditorLoadedScenes";
                result._scenes = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    result._scenes.Add(SceneManager.GetSceneAt(i).name);
                }
                return result;
            }
#endif
        }

        private class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner _runnerInstance;
            private static void EnsureExists()
            {
                if (_runnerInstance || !Application.isPlaying) { return; }
                _runnerInstance = new GameObject($"_{nameof(GameLoader)}{nameof(CoroutineRunner)}").AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(_runnerInstance.gameObject);
            }
            public static Coroutine Run(IEnumerator routine)
            {
                EnsureExists();
                return _runnerInstance.StartCoroutine(routine);
            }
        }
    }

    public static class ListExtensions
    {
        public static bool Exists<T>(this List<T> list, Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i])) return true;
            }
            return false;
        }
         public static bool TrueForAll<T>(this List<T> list, Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!match(list[i])) return false;
            }
            return true;
        }
    }
