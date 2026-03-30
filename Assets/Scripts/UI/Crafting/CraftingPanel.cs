using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Crafting;
using SunnysideIsland.Inventory;
using SunnysideIsland.GameData;
using SunnysideIsland.Events;

namespace SunnysideIsland.UI.Crafting
{
    public class CraftingPanel : UIPanel
    {
        [Header("=== Crafting System ===")]
        [SerializeField] private CraftingSystem _craftingSystem;
        [SerializeField] private InventorySystem _inventorySystem;
        [SerializeField] private SunnysideIsland.GameData.GameData _gameData;

        [Header("=== Recipe List ===")]
        [SerializeField] private Transform _recipeListContent;
        [SerializeField] private GameObject _recipeItemPrefab;

        [Header("=== Recipe Detail ===")]
        [SerializeField] private TextMeshProUGUI _recipeNameText;
        [SerializeField] private TextMeshProUGUI _recipeDescriptionText;
        [SerializeField] private Image _resultItemImage;
        [SerializeField] private TextMeshProUGUI _resultAmountText;
        [SerializeField] private Transform _ingredientsListContent;
        [SerializeField] private GameObject _ingredientItemPrefab;

        [Header("=== Crafting Action ===")]
        [SerializeField] private Button _craftButton;
        [SerializeField] private TextMeshProUGUI _craftButtonText;
        [SerializeField] private Slider _craftProgressSlider;

        [Header("=== Navigation ===")]
        [SerializeField] private Button _closeButton;

        private CraftingRecipe _selectedRecipe;
        private List<GameObject> _recipeItems = new List<GameObject>();
        private List<GameObject> _ingredientItems = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            
            if (_craftingSystem == null)
                _craftingSystem = DIContainer.Resolve<CraftingSystem>();
            if (_inventorySystem == null)
                _inventorySystem = DIContainer.Resolve<InventorySystem>();
            if (_gameData == null)
                _gameData = FindObjectOfType<SunnysideIsland.GameData.GameData>();
            
            // UI 참조가 없으면 동적 UI 생성
            if (_recipeListContent == null || _recipeItemPrefab == null || 
                _recipeNameText == null || _craftButton == null || _closeButton == null)
            {
                CreateDynamicUI();
            }
            
            // UIManager에 등록
            UIManager.Instance?.RegisterPanel(this);
        }

        private void OnEnable()
        {
            SubscribeEvents();
            SetupButtons();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            EventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemChanged);
            EventBus.Subscribe<ItemMovedEvent>(OnItemChanged);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemChanged);
            EventBus.Unsubscribe<ItemMovedEvent>(OnItemChanged);
        }

        private void SetupButtons()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
            
            if (_craftButton != null)
                _craftButton.onClick.AddListener(OnCraftButtonClicked);
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            RefreshRecipeList();
            ClearRecipeDetail();
        }

        private void RefreshRecipeList()
        {
            ClearRecipeList();
            
            if (_craftingSystem == null || _recipeListContent == null || _recipeItemPrefab == null)
                return;
            
            var allRecipes = _craftingSystem.GetAllRecipes();
            
            foreach (var recipe in allRecipes)
            {
                var recipeItem = Instantiate(_recipeItemPrefab, _recipeListContent);
                var recipeUI = recipeItem.GetComponent<RecipeItemUI>();
                
                if (recipeUI != null)
                {
                    string recipeName = GetItemName(recipe.ResultItemId);
                    Sprite icon = GetItemIcon(recipe.ResultItemId);
                    bool canCraft = _craftingSystem.CanCraft(recipe.RecipeId);
                    
                    recipeUI.Setup(recipe.RecipeId, recipeName, icon, canCraft);
                    recipeUI.OnClicked += OnRecipeSelected;
                }
                
                _recipeItems.Add(recipeItem);
            }
        }

        private void ClearRecipeList()
        {
            foreach (var item in _recipeItems)
            {
                if (item != null)
                    Destroy(item);
            }
            _recipeItems.Clear();
        }

        private void OnRecipeSelected(string recipeId)
        {
            _selectedRecipe = FindRecipe(recipeId);
            UpdateRecipeDetail();
        }

        private void UpdateRecipeDetail()
        {
            if (_selectedRecipe == null)
            {
                ClearRecipeDetail();
                return;
            }
            
            if (_recipeNameText != null)
                _recipeNameText.text = GetItemName(_selectedRecipe.ResultItemId);
            
            if (_recipeDescriptionText != null)
            {
                var resultItem = GetItemData(_selectedRecipe.ResultItemId);
                _recipeDescriptionText.text = resultItem != null ? resultItem.description : "";
            }
            
            if (_resultItemImage != null)
            {
                _resultItemImage.sprite = GetItemIcon(_selectedRecipe.ResultItemId);
                _resultItemImage.gameObject.SetActive(_resultItemImage.sprite != null);
            }
            
            if (_resultAmountText != null)
                _resultAmountText.text = $"x{_selectedRecipe.ResultAmount}";
            
            UpdateIngredientsList();
            UpdateCraftButton();
        }

        private void UpdateIngredientsList()
        {
            ClearIngredientsList();
            
            if (_selectedRecipe == null || _ingredientsListContent == null || _ingredientItemPrefab == null)
                return;
            
            foreach (var ingredient in _selectedRecipe.Ingredients)
            {
                var ingredientItem = Instantiate(_ingredientItemPrefab, _ingredientsListContent);
                var ingredientUI = ingredientItem.GetComponent<IngredientItemUI>();
                
                if (ingredientUI != null)
                {
                    string itemName = GetItemName(ingredient.Key);
                    Sprite icon = GetItemIcon(ingredient.Key);
                    int owned = _inventorySystem.CountItem(ingredient.Key);
                    int required = ingredient.Value;
                    bool hasEnough = owned >= required;
                    
                    ingredientUI.Setup(ingredient.Key, itemName, icon, owned, required, hasEnough);
                }
                
                _ingredientItems.Add(ingredientItem);
            }
        }

        private void ClearIngredientsList()
        {
            foreach (var item in _ingredientItems)
            {
                if (item != null)
                    Destroy(item);
            }
            _ingredientItems.Clear();
        }

        private void UpdateCraftButton()
        {
            if (_craftButton == null) return;
            
            bool canCraft = _selectedRecipe != null && _craftingSystem.CanCraft(_selectedRecipe.RecipeId);
            _craftButton.interactable = canCraft;
            
            if (_craftButtonText != null)
                _craftButtonText.text = canCraft ? "제작하기" : "재료 부족";
        }

        private void ClearRecipeDetail()
        {
            _selectedRecipe = null;
            
            if (_recipeNameText != null)
                _recipeNameText.text = "";
            if (_recipeDescriptionText != null)
                _recipeDescriptionText.text = "";
            if (_resultItemImage != null)
                _resultItemImage.gameObject.SetActive(false);
            if (_resultAmountText != null)
                _resultAmountText.text = "";
            
            ClearIngredientsList();
            UpdateCraftButton();
        }

        private void OnCraftButtonClicked()
        {
            if (_selectedRecipe == null) return;
            
            bool success = _craftingSystem.Craft(_selectedRecipe.RecipeId);
            
            if (success)
            {
                Debug.Log($"[CraftingPanel] 제작 성공: {_selectedRecipe.ResultItemId}");
                // 제작 성공 시 UI 업데이트는 ItemCraftedEvent에서 처리
            }
            else
            {
                Debug.LogWarning($"[CraftingPanel] 제작 실패: {_selectedRecipe.RecipeId}");
            }
        }

        private void OnItemCrafted(ItemCraftedEvent evt)
        {
            Debug.Log($"[CraftingPanel] 제작 완료 이벤트 수신: {evt.ResultItemId}");
            
            // 레시피 목록 새로고침 (제작 가능 여부 변경)
            RefreshRecipeList();
            
            // 현재 선택된 레시피 업데이트
            if (_selectedRecipe != null && _selectedRecipe.RecipeId == evt.RecipeId)
            {
                UpdateRecipeDetail();
            }
        }

        private void OnItemChanged(ItemPickedUpEvent evt)
        {
            // 아이템 획득/이동 시 재료 목록 업데이트
            if (_selectedRecipe != null)
            {
                UpdateIngredientsList();
                UpdateCraftButton();
            }
        }

        private void OnItemChanged(ItemMovedEvent evt)
        {
            OnItemChanged(new ItemPickedUpEvent { ItemId = evt.ItemId, Quantity = 0 });
        }

        private CraftingRecipe FindRecipe(string recipeId)
        {
            if (_craftingSystem == null) return null;
            
            foreach (var recipe in _craftingSystem.GetAllRecipes())
            {
                if (recipe.RecipeId == recipeId)
                    return recipe;
            }
            return null;
        }

        private string GetItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _gameData == null) return itemId;
            
            var itemData = _gameData.GetItem(itemId);
            return itemData?.itemName ?? itemId;
        }

        private Sprite GetItemIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _gameData == null) return null;
            
            var itemData = _gameData.GetItem(itemId);
            return itemData?.GetIcon();
        }

        private SunnysideIsland.GameData.ItemData GetItemData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _gameData == null) return null;
            return _gameData.GetItem(itemId);
        }
        
        private void CreateDynamicUI()
        {
            // 캔버스 확인 (없으면 생성)
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("CraftingCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // 메인 패널
            GameObject panelGO = new GameObject("CraftingPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.2f);
            panelRect.anchorMax = new Vector2(0.8f, 0.8f);
            panelRect.sizeDelta = Vector2.zero;
            
            // 스크롤뷰 (레시피 목록)
            GameObject scrollGO = new GameObject("RecipeScroll");
            scrollGO.transform.SetParent(panelGO.transform, false);
            ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
            Image scrollImage = scrollGO.AddComponent<Image>();
            scrollImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0.3f);
            scrollRectTransform.anchorMax = new Vector2(0.4f, 1);
            scrollRectTransform.sizeDelta = Vector2.zero;
            
            // 콘텐츠
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 300);
            contentRect.pivot = new Vector2(0, 1);
            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _recipeListContent = contentRect;
            
            // 레시피 아이템 프리팹 생성 (간단한 버튼)
            GameObject recipeItemPrefab = new GameObject("RecipeItemPrefab");
            Button button = recipeItemPrefab.AddComponent<Button>();
            Image image = recipeItemPrefab.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            TextMeshProUGUI text = CreateText(recipeItemPrefab.transform, "RecipeName", 20);
            RectTransform itemRect = recipeItemPrefab.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 40);
            // RecipeItemUI 컴포넌트 추가 (기본 구현)
            RecipeItemUI recipeItemUI = recipeItemPrefab.AddComponent<RecipeItemUI>();
            recipeItemUI.Setup("", "기본 레시피", null, false);
            
            _recipeItemPrefab = recipeItemPrefab;
            
            // 레시피 상세 패널
            GameObject detailPanel = new GameObject("RecipeDetail");
            detailPanel.transform.SetParent(panelGO.transform, false);
            RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.45f, 0.3f);
            detailRect.anchorMax = new Vector2(1, 1);
            detailRect.sizeDelta = Vector2.zero;
            
            _recipeNameText = CreateText(detailPanel.transform, "RecipeName", 24);
            _recipeDescriptionText = CreateText(detailPanel.transform, "RecipeDescription", 18);
            _resultAmountText = CreateText(detailPanel.transform, "ResultAmount", 20);
            
            // 결과 아이템 이미지
            GameObject imageGO = new GameObject("ResultImage");
            imageGO.transform.SetParent(detailPanel.transform, false);
            _resultItemImage = imageGO.AddComponent<Image>();
            RectTransform imageRect = imageGO.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0, 0.6f);
            imageRect.anchorMax = new Vector2(0.3f, 0.9f);
            imageRect.sizeDelta = Vector2.zero;
            
            // 재료 목록 콘텐츠
            GameObject ingredientsGO = new GameObject("IngredientsContent");
            ingredientsGO.transform.SetParent(detailPanel.transform, false);
            RectTransform ingredientsRect = ingredientsGO.AddComponent<RectTransform>();
            ingredientsRect.anchorMin = new Vector2(0.35f, 0);
            ingredientsRect.anchorMax = new Vector2(1, 0.5f);
            ingredientsRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup ingredientsVlg = ingredientsGO.AddComponent<VerticalLayoutGroup>();
            ingredientsVlg.childForceExpandWidth = true;
            ingredientsVlg.childForceExpandHeight = false;
            ContentSizeFitter ingredientsFitter = ingredientsGO.AddComponent<ContentSizeFitter>();
            ingredientsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _ingredientsListContent = ingredientsRect;
            
            // 재료 아이템 프리팹 생성
            GameObject ingredientItemPrefab = new GameObject("IngredientItemPrefab");
            TextMeshProUGUI ingredientText = CreateText(ingredientItemPrefab.transform, "IngredientText", 18);
            IngredientItemUI ingredientUI = ingredientItemPrefab.AddComponent<IngredientItemUI>();
            RectTransform ingredientRect = ingredientItemPrefab.GetComponent<RectTransform>();
            ingredientRect.sizeDelta = new Vector2(0, 30);
            
            _ingredientItemPrefab = ingredientItemPrefab;
            
            // 제작 버튼
            GameObject craftButtonGO = new GameObject("CraftButton");
            craftButtonGO.transform.SetParent(panelGO.transform, false);
            _craftButton = craftButtonGO.AddComponent<Button>();
            Image craftButtonImage = craftButtonGO.AddComponent<Image>();
            craftButtonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            RectTransform craftButtonRect = craftButtonGO.GetComponent<RectTransform>();
            craftButtonRect.anchorMin = new Vector2(0.1f, 0.05f);
            craftButtonRect.anchorMax = new Vector2(0.4f, 0.2f);
            craftButtonRect.sizeDelta = Vector2.zero;
            _craftButtonText = CreateText(craftButtonGO.transform, "ButtonText", 22);
            _craftButtonText.text = "제작하기";
            
            // 닫기 버튼
            GameObject closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            _closeButton = closeButtonGO.AddComponent<Button>();
            Image closeButtonImage = closeButtonGO.AddComponent<Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            RectTransform closeButtonRect = closeButtonGO.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.85f, 0.85f);
            closeButtonRect.anchorMax = new Vector2(0.95f, 0.95f);
            closeButtonRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI closeText = CreateText(closeButtonGO.transform, "X", 20);
            closeText.text = "X";
            
            // 프리팹 비활성화 (Instantiate 시 사용)
            recipeItemPrefab.SetActive(false);
            ingredientItemPrefab.SetActive(false);
        }
        
        private TextMeshProUGUI CreateText(Transform parent, string name, int fontSize)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return tmp;
        }
    }
}