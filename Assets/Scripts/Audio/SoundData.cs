using System;
using UnityEngine;

namespace SunnysideIsland.Audio
{
    [Serializable]
    public class SoundData
    {
        public string soundId;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;
        [Range(0f, 1f)] public float spatialBlend;
        [Range(0f, 500f)] public float maxDistance = 100f;
    }

    [CreateAssetMenu(fileName = "SoundDataAsset", menuName = "SunnysideIsland/Audio/SoundData")]
    public class SoundDataAsset : ScriptableObject
    {
        [Header("=== Sound Data ===")]
        public SoundData soundData;
    }
}