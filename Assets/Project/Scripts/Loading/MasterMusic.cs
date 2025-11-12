// Assets/Project/Scripts/Loading/MasterMusic.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResidentialHVAC.Loading
{
    /// <summary>
    /// Singleton that handles the currently playing audio track.
    /// Based on First Hand's MasterMusic implementation.
    /// Works with AudioSource components.
    /// </summary>
    public class MasterMusic : MonoBehaviour
    {
        private static MasterMusic _instance;
        public static MasterMusic Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MasterMusic>();
                }
                return _instance;
            }
        }

        [SerializeField]
        private List<MusicPair> _music = new List<MusicPair>();

        private AudioSource _currentMusic;

        private void Awake()
        {
            // Persist across scene loads
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Play music by ID string
        /// </summary>
        public void Play(string id)
        {
            var pair = _music.Find(x => x.ID == id);
            if (pair.Music == null)
            {
                Debug.LogWarning($"[MasterMusic] Music ID '{id}' not found!");
                return;
            }
            SetCurrentMusic(pair.Music);
        }

        private void SetCurrentMusic(AudioSource music)
        {
            if (_currentMusic == music) { return; }

            var previous = _currentMusic;
            if (previous != null && previous.isPlaying)
            {
                previous.Stop();
            }

            _currentMusic = music;
            if (music != null)
            {
                music.Play();
            }
        }

        [Serializable]
        public struct MusicPair
        {
            [SerializeField]
            private string _id;
            [SerializeField]
            private AudioSource _music;

            public string ID => _id;
            public AudioSource Music => _music;
        }
    }
}