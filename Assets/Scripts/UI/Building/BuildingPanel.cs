using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;
using SunnysideIsland.Events;
using SunnysideIsland.Building;

namespace SunnysideIsland.UI.Building
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasGroup))]
    public class BuildingPanel : MonoBehaviour
    {
        [Header("=== UI References ===")]
        [SerializeField] private RectTransform _categoryTabContainer;
        [SerializeField] private RectTransform _buildingGrid;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        
        [Header("=== Data Settings ===")]
        [SerializeField] private BuildingDatabase _buildingDatabase;
        
        [Header("=== Icon Prefabs (Addressables) ===")]
        [SerializeField] private AssetReferenceGameObject _defaultHousePrefab;
        [SerializeField] private AssetReferenceGameObject _bigHousePrefab;
        [SerializeField] private AssetReferenceGameObject _escapeBoatPrefab;
        
        [Header("=== Appearance ===")]
        [SerializeField] private Color _selectedTabColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _unselectedTabColor = new Color(0.3f, 0.3f, 0.4f);
        [SerializeField] private Color _cardNormalColor = new Color(0.2f, 0.2f, 0.3f);
        
        private BuildingCategory _currentCategory = BuildingCategory.Residential;
        private List<GameObject> _buildingCards = new List<GameObject>();
        private Dictionary<GameObject, DetailedBuildingData> _cardDataMap = new Dictionary<GameObject, DetailedBuildingData>();
        
        private TMP_FontAsset _koreanFont;
        private bool _isOpen = false;
        
        public bool IsOpen => _isOpen;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall -= RefreshEditorUI;
                UnityEditor.EditorApplication.delayCall += RefreshEditorUI;
            }
        }
        
        private void OnEnable()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private void OnDisable()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        
        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode || 
                state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                if (UnityEditor.Selection.activeGameObject != null)
                {
                    var sel = UnityEditor.Selection.activeGameObject;
                    if (sel == gameObject || (transform != null && sel.transform.IsChildOf(transform)))
                    {
                        UnityEditor.Selection.activeGameObject = null;
                    }
                }
            }
        }
        
        private void RefreshEditorUI()
        {
            if (this == null || gameObject == null) return;
            
            if (_categoryTabContainer != null && _categoryTabContainer.childCount == 0)
            {
                CreateTabs();
            }
            
            if (_buildingGrid != null && _buildingGrid.childCount == 0)
            {
                CreateCards();
            }
        }
        
        private void CreateTabs()
        {
            /* [스크립트만으로 UI를 생성하는 로직 주석 처리]
            string[] tabNames = { "주거", "상업", "관광", "생산", "장식", "방어" };
            
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/HSSaemaeul-1.asset");
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                var tab = new GameObject($"Tab_{tabNames[i]}");
                tab.transform.SetParent(_categoryTabContainer, false);
                tab.hideFlags = HideFlags.None;
                
                var rect = tab.AddComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(i * 85 + 10, 0);
                rect.sizeDelta = new Vector2(80, 40);
                
                var img = tab.AddComponent<Image>();
                img.color = i == 0 ? _selectedTabColor : _unselectedTabColor;
                
                tab.AddComponent<Button>();
                
                var txtGO = new GameObject("Text");
                txtGO.transform.SetParent(tab.transform, false);
                var txtRect = txtGO.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
                
                var txt = txtGO.AddComponent<TextMeshProUGUI>();
                txt.text = tabNames[i];
                txt.fontSize = 16;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
                if (font != null) txt.font = font;
            }
            */
        }
        
        private void CreateCards()
        {
            /* [스크립트만으로 UI를 생성하는 로직 주석 처리]
            if (_buildingGrid == null) return;
            
            ClearCards();
            
            if (_buildingDatabase == null)
                _buildingDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingDatabase>("Assets/ScriptableObjects/Building/BuildingDatabase.asset");
            
            if (_buildingDatabase == null) return;
            
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/HSSaemaeul-1.asset");
            var buildings = _buildingDatabase.GetBuildingsByCategory(_currentCategory);
            
            int col = 0;
            int row = 0;
            float cardWidth = 130;
            float cardHeight = 150;
            
            foreach (var b in buildings)
            {
                var card = new GameObject($"Card_{b.BuildingId}");
                card.transform.SetParent(_buildingGrid, false);
                card.hideFlags = HideFlags.None;
                
                var rect = card.AddComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(col * cardWidth + 10, -row * cardHeight - 10);
                rect.sizeDelta = new Vector2(120, 140);
                
                var img = card.AddComponent<Image>();
                img.color = _cardNormalColor;
                card.AddComponent<Button>();
                
                var txtGO = new GameObject("Text");
                txtGO.transform.SetParent(card.transform, false);
                var txtRect = txtGO.AddComponent<RectTransform>();
                txtRect.anchorMin = new Vector2(0, 0);
                txtRect.anchorMax = new Vector2(1, 0.3f);
                txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
                
                var txt = txtGO.AddComponent<TextMeshProUGUI>();
                txt.text = b.BuildingName;
                txt.fontSize = 14;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
                if (font != null) txt.font = font;
                
                _buildingCards.Add(card);
                
                col++;
                if (col >= 5)
                {
                    col = 0;
                    row++;
                }
            }
            
            UpdateTabColors();
            */
        }
        
        private void ClearCards()
        {
            foreach (var c in _buildingCards)
            {
                if (c != null) DestroyImmediate(c);
            }
            _buildingCards.Clear();
            _cardDataMap.Clear();
        }
        
        private void UpdateTabColors()
        {
            if (_categoryTabContainer == null) return;
            
            for (int i = 0; i < _categoryTabContainer.childCount; i++)
            {
                var tab = _categoryTabContainer.GetChild(i);
                if (tab == null) continue;
                
                var img = tab.GetComponent<Image>();
                if (img != null) img.color = (int)_currentCategory == i ? _selectedTabColor : _unselectedTabColor;
            }
        }
#endif

        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RefreshEditorUI;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
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
        
        private void Awake()
        {
            _koreanFont = Resources.Load<TMP_FontAsset>("Fonts/HSSaemaeul-1");
            
            // Ensure AssetReference fields are initialized
            if (_defaultHousePrefab == null || !_defaultHousePrefab.RuntimeKeyIsValid())
            {
                _defaultHousePrefab = new AssetReferenceGameObject("House");
            }
            if (_bigHousePrefab == null || !_bigHousePrefab.RuntimeKeyIsValid())
            {
                _bigHousePrefab = new AssetReferenceGameObject("BigHouse");
            }
            // Boat may not exist, but we can still assign if needed
            if (_escapeBoatPrefab == null || !_escapeBoatPrefab.RuntimeKeyIsValid())
            {
                // Try to assign "Boat" if exists, otherwise leave as null
                _escapeBoatPrefab = new AssetReferenceGameObject("Boat");
            }
            
            if (_buildingDatabase == null)
                _buildingDatabase = Resources.Load<BuildingDatabase>("BuildingDatabase");
            
#if UNITY_EDITOR
            if (_buildingDatabase == null)
                _buildingDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingDatabase>("Assets/ScriptableObjects/Building/BuildingDatabase.asset");
#endif
            
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
            
            SetupExistingCards();
        }
        
        private void SetupExistingCards()
        {
            if (_buildingGrid == null || _buildingDatabase == null) return;
            
            foreach (Transform child in _buildingGrid)
            {
                if (!child.name.StartsWith("Card_")) continue;
                
                var buildingId = child.name.Substring(5);
                var buildingData = _buildingDatabase.GetBuilding(buildingId);
                if (buildingData == null) continue;
                
                // BuildingCard 컴포넌트를 가져와서 설정
                var card = child.GetComponent<BuildingCard>();
                if (card != null)
                {
                    card.Setup(buildingData, _defaultHousePrefab, _bigHousePrefab, _escapeBoatPrefab);
                    card.OnClicked += OnCardClicked;
                }
                else
                {
                    // 컴포넌트가 없는 경우 버튼 이벤트만이라도 연결 (하위 호환성)
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        var capturedData = buildingData;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnCardClickedRuntime(capturedData));
                    }
                }
                
                _buildingCards.Add(child.gameObject);
                _cardDataMap[child.gameObject] = buildingData;
            }
        }
        
        private void OnCardClickedRuntime(DetailedBuildingData data)
        {
            if (data == null) return;
            
            EventBus.Publish(new BuildingPlacementStartedEvent { BuildingId = data.BuildingId });
            Close();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                Close();
            }
        }

        public void Open()
        {
            _isOpen = true;
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        public void Close()
        {
            _isOpen = false;
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        public void SelectCategory(BuildingCategory category)
        {
            _currentCategory = category;
            UpdateTabColorsRuntime();
            RefreshBuildingList();
        }

        private void UpdateTabColorsRuntime()
        {
            if (_categoryTabContainer == null) return;
            
            for (int i = 0; i < _categoryTabContainer.childCount; i++)
            {
                var tab = _categoryTabContainer.GetChild(i);
                if (tab == null) continue;
                
                var img = tab.GetComponent<Image>();
                if (img != null)
                {
                    img.color = (int)_currentCategory == i ? _selectedTabColor : _unselectedTabColor;
                }
                
                var btn = tab.GetComponent<Button>();
                if (btn != null)
                {
                    var cat = (BuildingCategory)i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectCategory(cat));
                }
            }
        }

        private void RefreshBuildingList()
        {
            ClearBuildingCardsRuntime();
            
            if (_buildingDatabase == null || _buildingGrid == null) return;

            var buildings = _buildingDatabase.GetBuildingsByCategory(_currentCategory);
            foreach (var building in buildings)
            {
                CreateBuildingCard(building);
            }
        }

        private void CreateBuildingCard(DetailedBuildingData data)
        {
            /* [스크립트만으로 UI를 생성하는 로직 주석 처리]
            var card = new GameObject($"Card_{data.BuildingId}");
            card.transform.SetParent(_buildingGrid, false);
            
            var rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 140);
            
            var img = card.AddComponent<Image>();
            img.color = _cardNormalColor;
            
            var btn = card.AddComponent<Button>();
            
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(card.transform, false);
            var txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0, 0);
            txtRect.anchorMax = new Vector2(1, 0.3f);
            txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
            
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text = data.BuildingName;
            txt.fontSize = 14;
            txt.alignment = TextAlignmentOptions.Center;
            if (_koreanFont != null) txt.font = _koreanFont;

            btn.onClick.AddListener(() => OnCardClicked(card));
            
            _buildingCards.Add(card);
            _cardDataMap[card] = data;
            */
        }

        private void ClearBuildingCardsRuntime()
        {
            foreach (var card in _buildingCards)
            {
                if (card != null)
                {
                    if (Application.isPlaying)
                        Destroy(card);
                    else
                        DestroyImmediate(card);
                }
            }
            _buildingCards.Clear();
            _cardDataMap.Clear();

            if (_buildingGrid != null)
            {
                for (int i = _buildingGrid.childCount - 1; i >= 0; i--)
                {
                    var child = _buildingGrid.GetChild(i);
                    if (child != null && child.name.StartsWith("Card_"))
                    {
                        if (Application.isPlaying)
                            Destroy(child.gameObject);
                        else
                            DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private void OnCardClicked(BuildingCard card)
        {
            if (card == null || card.Data == null) return;
            
            if (Application.isPlaying)
            {
                EventBus.Publish(new BuildingPlacementStartedEvent { BuildingId = card.Data.BuildingId });
                Close();
            }
        }
    }
}