using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Audio
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "SunnysideIsland/Audio/Database")]
    public class SoundDatabase : ScriptableObject
    {
        [Header("=== Sound Data List ===")]
        [SerializeField] private List<SoundData> _sounds = new List<SoundData>();

        private Dictionary<string, SoundData> _soundDictionary;

        public IReadOnlyList<SoundData> AllSounds => _sounds;

        public void Initialize()
        {
            if (_soundDictionary != null) return;
            
            _soundDictionary = new Dictionary<string, SoundData>();
            
            foreach (var sound in _sounds)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.soundId))
                {
                    if (!_soundDictionary.ContainsKey(sound.soundId))
                    {
                        _soundDictionary[sound.soundId] = sound;
                    }
                    else
                    {
                        Debug.LogWarning($"[SoundDatabase] Duplicate sound ID: {sound.soundId}");
                    }
                }
            }
        }

        public SoundData GetSound(string soundId)
        {
            if (_soundDictionary == null)
            {
                Initialize();
            }

            if (_soundDictionary.TryGetValue(soundId, out var sound))
            {
                return sound;
            }

            Debug.LogWarning($"[SoundDatabase] Sound not found: {soundId}");
            return null;
        }

        public bool HasSound(string soundId)
        {
            if (_soundDictionary == null)
            {
                Initialize();
            }

            return _soundDictionary.ContainsKey(soundId);
        }

        public void AddSound(SoundData sound)
        {
            if (sound == null || string.IsNullOrEmpty(sound.soundId)) return;

            if (_soundDictionary == null)
            {
                Initialize();
            }

            if (!_soundDictionary.ContainsKey(sound.soundId))
            {
                _soundDictionary[sound.soundId] = sound;
                _sounds.Add(sound);
            }
        }

        public SoundData GetSoundByClip(AudioClip clip)
        {
            if (clip == null) return null;
            
            if (_soundDictionary == null)
            {
                Initialize();
            }

            foreach (var sound in _sounds)
            {
                if (sound != null && sound.clip == clip)
                {
                    return sound;
                }
            }

            return null;
        }
    }
}