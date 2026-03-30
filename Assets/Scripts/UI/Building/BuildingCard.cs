using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using SunnysideIsland.Building;

namespace SunnysideIsland.UI.Building
{
    [ExecuteAlways]
    public class BuildingCard : MonoBehaviour
    {
        [Header("=== References ===")]
        [SerializeField] private Transform _iconContainer;
        [SerializeField] private TextMeshProUGUI _buildingNameText;
        [SerializeField] private GameObject _lockedOverlay;
        [SerializeField] private Image _selectionOverlay;
        [SerializeField] private Button _button;
        
        [Header("=== Appearance Settings ===")]
        [SerializeField] private bool _stretchIconToContainer = true;
        [SerializeField] private bool _preserveIconAspect = true;
        
        public DetailedBuildingData Data { get; private set; }
        public bool IsUnlocked { get; private set; }
        
        public event Action<BuildingCard> OnClicked;
        
        private AsyncOperationHandle<GameObject> _iconHandle;
        private GameObject _currentIconInstance;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
            
            if (_iconContainer == null)
            {
                _iconContainer = transform;
            }
            
            SetSelected(false);
        }

        private void OnDestroy()
        {
            CleanupIcon();
            
#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != null)
            {
                var sel = UnityEditor.Selection.activeGameObject;
                if (sel == gameObject || (transform != null && sel.transform.IsChildOf(transform)))
                {
                    UnityEditor.Selection.activeGameObject = null;
                }
            }
#endif
        }

        private void CleanupIcon()
        {
            if (_currentIconInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(_currentIconInstance);
                else
                    DestroyImmediate(_currentIconInstance);
                
                _currentIconInstance = null;
            }

            if (_iconHandle.IsValid())
            {
                Addressables.Release(_iconHandle);
            }
        }
        
        public void Setup(DetailedBuildingData data, AssetReferenceGameObject houseRef, AssetReferenceGameObject bigHouseRef, AssetReferenceGameObject boatRef)
        {
            Data = data;
            
            if (_buildingNameText != null)
            {
                _buildingNameText.text = data.BuildingName;
            }
            
            if (_iconContainer != null)
            {
                AssetReferenceGameObject targetRef = houseRef; // 기본: 집
                
                // 탈출용 배 판별 (최우선)
                if (data.BuildingId.Contains("Boat") || data.BuildingName.Contains("배"))
                {
                    targetRef = boatRef;
                }
                // 큰 집 판별 (다음)
                else if (data.BuildingId.Contains("LargeHouse") || data.BuildingId.Contains("BigHouse") || data.BuildingName.Contains("큰 집"))
                {
                    targetRef = bigHouseRef;
                }

                if (targetRef != null && targetRef.RuntimeKeyIsValid())
                {
                    LoadAndInstantiateIcon(targetRef);
                }
            }
            
            IsUnlocked = data.IsUnlockedDefault;
            
            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(!IsUnlocked);
            }
        }

        private void LoadAndInstantiateIcon(AssetReferenceGameObject assetRef)
        {
            CleanupIcon();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var prefab = assetRef.editorAsset as GameObject;
                
                if (prefab == null && assetRef.RuntimeKeyIsValid())
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetRef.ToString());
                    if (!string.IsNullOrEmpty(path))
                    {
                        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    }
                }
                
                if (prefab != null)
                {
                    _currentIconInstance = Instantiate(prefab, _iconContainer);
                    if (_currentIconInstance != null)
                    {
                        _currentIconInstance.hideFlags = HideFlags.HideAndDontSave;
                        SetupIconInstance();
                    }
                }
                return;
            }
#endif

            _iconHandle = Addressables.InstantiateAsync(assetRef, _iconContainer);
            _iconHandle.Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentIconInstance = handle.Result;
                    SetupIconInstance();
                }
            };
        }

        private void SetupIconInstance()
        {
            if (_currentIconInstance == null) return;
            
            var rect = _currentIconInstance.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = _currentIconInstance.AddComponent<RectTransform>();
            }
            
            if (_stretchIconToContainer)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            
            var sr = _currentIconInstance.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var img = _currentIconInstance.GetComponent<Image>();
                if (img == null)
                {
                    img = _currentIconInstance.AddComponent<Image>();
                }
                img.sprite = sr.sprite;
                img.preserveAspect = _preserveIconAspect;
                img.enabled = true;
                sr.enabled = false;
            }
        }
        
        public void SetSelected(bool selected)
        {
            if (_selectionOverlay != null)
            {
                _selectionOverlay.enabled = selected;
            }
        }
        
        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(!unlocked);
            }
        }
        
        private void OnButtonClicked()
        {
            OnClicked?.Invoke(this);
        }
    }
}