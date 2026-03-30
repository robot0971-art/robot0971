using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

namespace SunnysideIsland.UI.Menu
{
    public class SettingsPanel : UIPanel
    {
        [Header("=== Audio ===")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _bgmVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        
        [Header("=== Graphics ===")]
        [SerializeField] private TMP_Dropdown _qualityDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        
        [Header("=== Gameplay ===")]
        [SerializeField] private Slider _autoSaveIntervalSlider;
        [SerializeField] private TextMeshProUGUI _autoSaveIntervalText;
        [SerializeField] private Toggle _showTutorialToggle;
        
        [Header("=== Buttons ===")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _closeButton;
        
        private Resolution[] _resolutions;
        
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string QUALITY_KEY = "QualityLevel";
        private const string FULLSCREEN_KEY = "Fullscreen";
        private const string RESOLUTION_KEY = "ResolutionIndex";
        private const string AUTO_SAVE_INTERVAL_KEY = "AutoSaveInterval";
        private const string SHOW_TUTORIAL_KEY = "ShowTutorial";
        
        protected override void Awake()
        {
            base.Awake();
            _isModal = true;
        }
        
        private void OnEnable()
        {
            _masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            _bgmVolumeSlider?.onValueChanged.AddListener(OnBGMVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            _qualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
            _fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
            _resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
            _autoSaveIntervalSlider?.onValueChanged.AddListener(OnAutoSaveIntervalChanged);
            _applyButton?.onClick.AddListener(OnApplyClicked);
            _resetButton?.onClick.AddListener(OnResetClicked);
            _closeButton?.onClick.AddListener(OnCloseClicked);
        }
        
        private void OnDisable()
        {
            _masterVolumeSlider?.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            _bgmVolumeSlider?.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            _qualityDropdown?.onValueChanged.RemoveListener(OnQualityChanged);
            _fullscreenToggle?.onValueChanged.RemoveListener(OnFullscreenChanged);
            _resolutionDropdown?.onValueChanged.RemoveListener(OnResolutionChanged);
            _autoSaveIntervalSlider?.onValueChanged.RemoveListener(OnAutoSaveIntervalChanged);
            _applyButton?.onClick.RemoveListener(OnApplyClicked);
            _resetButton?.onClick.RemoveListener(OnResetClicked);
            _closeButton?.onClick.RemoveListener(OnCloseClicked);
        }
        
        protected override void OnOpened()
        {
            base.OnOpened();
            InitializeResolutions();
            LoadSettings();
        }
        
        private void InitializeResolutions()
        {
            _resolutions = Screen.resolutions;
            
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>();
                int currentResolutionIndex = 0;
                
                for (int i = 0; i < _resolutions.Length; i++)
                {
                    string option = $"{_resolutions[i].width} x {_resolutions[i].height}";
                    options.Add(option);
                    
                    if (_resolutions[i].width == Screen.currentResolution.width &&
                        _resolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResolutionIndex = i;
                    }
                }
                
                _resolutionDropdown.AddOptions(options);
                _resolutionDropdown.value = currentResolutionIndex;
                _resolutionDropdown.RefreshShownValue();
            }
        }
        
        private void LoadSettings()
        {
            if (_masterVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
                _masterVolumeSlider.value = volume;
            }
            
            if (_bgmVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
                _bgmVolumeSlider.value = volume;
            }
            
            if (_sfxVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
                _sfxVolumeSlider.value = volume;
            }
            
            if (_qualityDropdown != null)
            {
                int quality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
                _qualityDropdown.value = quality;
            }
            
            if (_fullscreenToggle != null)
            {
                bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
                _fullscreenToggle.isOn = fullscreen;
            }
            
            if (_resolutionDropdown != null)
            {
                int index = PlayerPrefs.GetInt(RESOLUTION_KEY, 0);
                _resolutionDropdown.value = index;
            }
            
            if (_autoSaveIntervalSlider != null)
            {
                float interval = PlayerPrefs.GetFloat(AUTO_SAVE_INTERVAL_KEY, 300f);
                _autoSaveIntervalSlider.value = interval;
                UpdateAutoSaveIntervalText(interval);
            }
            
            if (_showTutorialToggle != null)
            {
                bool show = PlayerPrefs.GetInt(SHOW_TUTORIAL_KEY, 1) == 1;
                _showTutorialToggle.isOn = show;
            }
        }
        
        private void OnMasterVolumeChanged(float value)
        {
            SetVolume(MASTER_VOLUME_KEY, value);
        }
        
        private void OnBGMVolumeChanged(float value)
        {
            SetVolume(BGM_VOLUME_KEY, value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            SetVolume(SFX_VOLUME_KEY, value);
        }
        
        private void SetVolume(string key, float value)
        {
            if (_audioMixer != null)
            {
                float db = value > 0 ? Mathf.Log10(value) * 20 : -80;
                _audioMixer.SetFloat(key, db);
            }
            PlayerPrefs.SetFloat(key, value);
        }
        
        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index);
            PlayerPrefs.SetInt(QUALITY_KEY, index);
        }
        
        private void OnFullscreenChanged(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0);
        }
        
        private void OnResolutionChanged(int index)
        {
            if (_resolutions != null && index < _resolutions.Length)
            {
                Resolution resolution = _resolutions[index];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
                PlayerPrefs.SetInt(RESOLUTION_KEY, index);
            }
        }
        
        private void OnAutoSaveIntervalChanged(float value)
        {
            UpdateAutoSaveIntervalText(value);
            PlayerPrefs.SetFloat(AUTO_SAVE_INTERVAL_KEY, value);
        }
        
        private void UpdateAutoSaveIntervalText(float seconds)
        {
            if (_autoSaveIntervalText != null)
            {
                int minutes = Mathf.RoundToInt(seconds / 60f);
                _autoSaveIntervalText.text = $"{minutes}분";
            }
        }
        
        private void OnApplyClicked()
        {
            PlayerPrefs.Save();
            Close();
        }
        
        private void OnResetClicked()
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.value = 1f;
            if (_bgmVolumeSlider != null) _bgmVolumeSlider.value = 1f;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = 1f;
            if (_qualityDropdown != null) _qualityDropdown.value = QualitySettings.GetQualityLevel();
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = true;
            if (_autoSaveIntervalSlider != null) _autoSaveIntervalSlider.value = 300f;
            if (_showTutorialToggle != null) _showTutorialToggle.isOn = true;
        }
        
        private void OnCloseClicked()
        {
            Close();
        }
    }
}