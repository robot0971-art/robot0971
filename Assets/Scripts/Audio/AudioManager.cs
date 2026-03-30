using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Events;

namespace SunnysideIsland.Audio
{
    public interface IAudioManager
    {
        void PlayBGM(string soundId, float fadeDuration = 1f);
        void StopBGM(float fadeDuration = 1f);
        void PlaySFX(string soundId, Vector3? position = null);
        void StopSFX(string soundId);
        void SetMasterVolume(float volume);
        void SetBGMVolume(float volume);
        void SetSFXVolume(float volume);
        float MasterVolume { get; }
        float BGMVolume { get; }
        float SFXVolume { get; }
    }

    [Serializable]
    public class AudioSaveData
    {
        public float masterVolume = 1f;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
    }

    public class AudioManager : MonoBehaviour, IAudioManager, ISaveable
    {
        public static AudioManager Instance { get; private set; }

        [Header("=== Database ===")]
        [SerializeField] private SoundDatabase _soundDatabase;

        [Header("=== Audio Sources ===")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _bgmSourceSecondary;

        [Header("=== Pool Settings ===")]
        [SerializeField] private int _sfxPoolInitialSize = 10;
        [SerializeField] private int _sfxPoolMaxSize = 30;
        [SerializeField] private Transform _poolParent;

        [Header("=== Default Settings ===")]
        [SerializeField] private float _defaultCrossfadeDuration = 1f;

        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private readonly Dictionary<string, List<AudioSource>> _activeSfxSources = new Dictionary<string, List<AudioSource>>();
        private readonly Queue<AudioSource> _availableSources = new Queue<AudioSource>();

        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        private string _currentBgmId;
        private Coroutine _crossfadeCoroutine;

        public string SaveKey => "AudioManager";
        public float MasterVolume => _masterVolume;
        public float BGMVolume => _bgmVolume;
        public float SFXVolume => _sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            InitializePool();
        }

        private void Start()
        {
            if (_soundDatabase != null)
            {
                _soundDatabase.Initialize();
            }

            LoadVolumeSettings();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            ClearPool();
        }

        private void InitializeAudioSources()
        {
            if (_bgmSource == null)
            {
                var bgmObj = new GameObject("BGMSource_Primary");
                bgmObj.transform.SetParent(transform);
                _bgmSource = bgmObj.AddComponent<AudioSource>();
            }

            if (_bgmSourceSecondary == null)
            {
                var bgmObj2 = new GameObject("BGMSource_Secondary");
                bgmObj2.transform.SetParent(transform);
                _bgmSourceSecondary = bgmObj2.AddComponent<AudioSource>();
            }

            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;

            _bgmSourceSecondary.playOnAwake = false;
            _bgmSourceSecondary.loop = true;
            _bgmSourceSecondary.spatialBlend = 0f;
            _bgmSourceSecondary.volume = 0f;
        }

        private void InitializePool()
        {
            if (_poolParent == null)
            {
                var poolObj = new GameObject("SFXPool");
                poolObj.transform.SetParent(transform);
                _poolParent = poolObj.transform;
            }

            for (int i = 0; i < _sfxPoolInitialSize; i++)
            {
                CreatePooledSource();
            }
        }

        private AudioSource CreatePooledSource()
        {
            var obj = new GameObject($"SFXSource_{_sfxPool.Count}");
            obj.transform.SetParent(_poolParent);
            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxPool.Add(source);
            _availableSources.Enqueue(source);
            return source;
        }

        private AudioSource GetPooledSource()
        {
            if (_availableSources.Count > 0)
            {
                return _availableSources.Dequeue();
            }

            if (_sfxPool.Count < _sfxPoolMaxSize)
            {
                return CreatePooledSource();
            }

            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (!_sfxPool[i].isPlaying)
                {
                    return _sfxPool[i];
                }
            }

            Debug.LogWarning("[AudioManager] SFX pool exhausted");
            return null;
        }

        private void ReturnPooledSource(AudioSource source)
        {
            if (source == null) return;
            
            source.clip = null;
            source.transform.SetParent(_poolParent);
            source.gameObject.SetActive(false);
            
            if (!_availableSources.Contains(source))
            {
                _availableSources.Enqueue(source);
            }
        }

        private void ClearPool()
        {
            foreach (var source in _sfxPool)
            {
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }
            _sfxPool.Clear();
            _availableSources.Clear();
            _activeSfxSources.Clear();
        }

        private void LoadVolumeSettings()
        {
            if (PlayerPrefs.HasKey("Audio_MasterVolume"))
            {
                _masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume");
            }
            if (PlayerPrefs.HasKey("Audio_BGMVolume"))
            {
                _bgmVolume = PlayerPrefs.GetFloat("Audio_BGMVolume");
            }
            if (PlayerPrefs.HasKey("Audio_SFXVolume"))
            {
                _sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume");
            }

            ApplyVolumes();
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("Audio_MasterVolume", _masterVolume);
            PlayerPrefs.SetFloat("Audio_BGMVolume", _bgmVolume);
            PlayerPrefs.SetFloat("Audio_SFXVolume", _sfxVolume);
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            if (_bgmSource != null)
            {
                _bgmSource.volume = _masterVolume * _bgmVolume;
            }
            if (_bgmSourceSecondary != null)
            {
                _bgmSourceSecondary.volume = 0f;
            }
        }

        public void PlayBGM(string soundId, float fadeDuration = 1f)
        {
            if (_soundDatabase == null)
            {
                Debug.LogError("[AudioManager] SoundDatabase not assigned");
                return;
            }

            var soundData = _soundDatabase.GetSound(soundId);
            if (soundData == null || soundData.clip == null)
            {
                Debug.LogWarning($"[AudioManager] Cannot play BGM: {soundId}");
                return;
            }

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            _crossfadeCoroutine = StartCoroutine(CrossfadeBGM(soundData, fadeDuration));

            EventBus.Publish(new BGMChangedEvent
            {
                SoundId = soundId,
                PreviousSoundId = _currentBgmId
            });

            _currentBgmId = soundId;
        }

        private IEnumerator CrossfadeBGM(SoundData newSound, float duration)
        {
            var fadeOutSource = _bgmSource;
            var fadeInSource = _bgmSourceSecondary;

            fadeInSource.clip = newSound.clip;
            fadeInSource.pitch = newSound.pitch;
            fadeInSource.volume = 0f;
            fadeInSource.Play();

            float elapsed = 0f;
            float startVolumeOut = fadeOutSource.volume;
            float targetVolumeIn = _masterVolume * _bgmVolume * newSound.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, t);
                fadeInSource.volume = Mathf.Lerp(0f, targetVolumeIn, t);

                yield return null;
            }

            fadeOutSource.Stop();
            fadeOutSource.volume = 0f;
            fadeInSource.volume = targetVolumeIn;

            var temp = _bgmSource;
            _bgmSource = fadeInSource;
            _bgmSourceSecondary = temp;

            _crossfadeCoroutine = null;
        }

        public void StopBGM(float fadeDuration = 1f)
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            _crossfadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));

            EventBus.Publish(new BGMChangedEvent
            {
                SoundId = null,
                PreviousSoundId = _currentBgmId
            });

            _currentBgmId = null;
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startVolume = _bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            _bgmSource.Stop();
            _bgmSource.volume = 0f;
            _crossfadeCoroutine = null;
        }

        public void PlaySFX(string soundId, Vector3? position = null)
        {
            if (_soundDatabase == null)
            {
                Debug.LogError("[AudioManager] SoundDatabase not assigned");
                return;
            }

            var soundData = _soundDatabase.GetSound(soundId);
            if (soundData == null || soundData.clip == null)
            {
                Debug.LogWarning($"[AudioManager] Cannot play SFX: {soundId}");
                return;
            }

            var source = GetPooledSource();
            if (source == null) return;

            source.gameObject.SetActive(true);
            source.clip = soundData.clip;
            source.volume = _masterVolume * _sfxVolume * soundData.volume;
            source.pitch = soundData.pitch;
            source.loop = soundData.loop;
            source.spatialBlend = soundData.spatialBlend;
            source.maxDistance = soundData.maxDistance;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
            }
            else
            {
                source.transform.position = Vector3.zero;
            }

            source.Play();

            if (!_activeSfxSources.ContainsKey(soundId))
            {
                _activeSfxSources[soundId] = new List<AudioSource>();
            }
            _activeSfxSources[soundId].Add(source);

            if (!soundData.loop)
            {
                StartCoroutine(ReturnSourceAfterPlay(source, soundId, soundData.clip.length / soundData.pitch));
            }

            EventBus.Publish(new SFXPlayedEvent
            {
                SoundId = soundId,
                Position = position
            });
        }

        private IEnumerator ReturnSourceAfterPlay(AudioSource source, string soundId, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);

            if (_activeSfxSources.TryGetValue(soundId, out var sources))
            {
                sources.Remove(source);
            }
            
            ReturnPooledSource(source);
        }

        public void StopSFX(string soundId)
        {
            if (!_activeSfxSources.TryGetValue(soundId, out var sources)) return;

            foreach (var source in sources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                    ReturnPooledSource(source);
                }
            }
            sources.Clear();
        }

        public void StopAllSFX()
        {
            foreach (var kvp in _activeSfxSources)
            {
                foreach (var source in kvp.Value)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Stop();
                        ReturnPooledSource(source);
                    }
                }
                kvp.Value.Clear();
            }
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveVolumeSettings();

            EventBus.Publish(new VolumeChangedEvent
            {
                VolumeType = AudioVolumeType.Master,
                Volume = _masterVolume
            });
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveVolumeSettings();

            EventBus.Publish(new VolumeChangedEvent
            {
                VolumeType = AudioVolumeType.BGM,
                Volume = _bgmVolume
            });
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            SaveVolumeSettings();

            foreach (var source in _sfxPool)
            {
                if (source.isPlaying && source.clip != null)
                {
                    var sound = _soundDatabase?.GetSoundByClip(source.clip);
                    if (sound != null)
                    {
                        source.volume = _masterVolume * _sfxVolume * sound.volume;
                    }
                }
            }

            EventBus.Publish(new VolumeChangedEvent
            {
                VolumeType = AudioVolumeType.SFX,
                Volume = _sfxVolume
            });
        }

        public void PauseAll()
        {
            _bgmSource.Pause();
            foreach (var source in _sfxPool)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }
            }
        }

        public void ResumeAll()
        {
            _bgmSource.UnPause();
            foreach (var source in _sfxPool)
            {
                if (source.clip != null && !source.isPlaying)
                {
                    source.UnPause();
                }
            }
        }

        public string GetCurrentBGMId()
        {
            return _currentBgmId;
        }

        #region ISaveable

        public object GetSaveData()
        {
            return new AudioSaveData
            {
                masterVolume = _masterVolume,
                bgmVolume = _bgmVolume,
                sfxVolume = _sfxVolume
            };
        }

        public void LoadSaveData(object data)
        {
            if (data is AudioSaveData saveData)
            {
                _masterVolume = saveData.masterVolume;
                _bgmVolume = saveData.bgmVolume;
                _sfxVolume = saveData.sfxVolume;

                ApplyVolumes();

                Debug.Log($"[AudioManager] Loaded volume settings: Master={_masterVolume}, BGM={_bgmVolume}, SFX={_sfxVolume}");
            }
        }

        #endregion
    }

    public enum AudioVolumeType
    {
        Master,
        BGM,
        SFX
    }

    #region Events

    public class BGMChangedEvent
    {
        public string SoundId { get; set; }
        public string PreviousSoundId { get; set; }
    }

    public class SFXPlayedEvent
    {
        public string SoundId { get; set; }
        public Vector3? Position { get; set; }
    }

    public class VolumeChangedEvent
    {
        public AudioVolumeType VolumeType { get; set; }
        public float Volume { get; set; }
    }

    #endregion
}