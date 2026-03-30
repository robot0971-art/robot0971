using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;

namespace SunnysideIsland.Crafting
{
    /// <summary>
    /// 조합 레시피
    /// </summary>
    [System.Serializable]
    public class CraftingRecipe
    {
        public string RecipeId;
        public string ResultItemId;
        public int ResultAmount;
        public Dictionary<string, int> Ingredients; // 아이템 ID: 수량
        public float CraftTime; // 조합 시간 (초)
    }

    /// <summary>
    /// 조합 시스템
    /// </summary>
    public class CraftingSystem : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
        
        private IInventorySystem _inventorySystem;
        
        public string SaveKey => "CraftingSystem";
        
        private void Start()
        {
            _inventorySystem = FindObjectOfType<InventorySystem>();
            AddDefaultRecipes();
        }

        private void AddDefaultRecipes()
        {
            // 배 레시피 추가 (없을 경우)
            if (!HasRecipe("boat"))
            {
                var boatRecipe = new CraftingRecipe
                {
                    RecipeId = "boat",
                    ResultItemId = "boat",
                    ResultAmount = 1,
                    Ingredients = new Dictionary<string, int> { { "wood", 50 } },
                    CraftTime = 3f
                };
                AddRecipe(boatRecipe);
            }
        }
        
        /// <summary>
        /// 레시피 확인
        /// </summary>
        public bool HasRecipe(string recipeId)
        {
            return FindRecipe(recipeId) != null;
        }
        
        /// <summary>
        /// 조합 가능 여부 확인
        /// </summary>
        public bool CanCraft(string recipeId)
        {
            var recipe = FindRecipe(recipeId);
            if (recipe == null) return false;
            
            // 재료 확인
            foreach (var ingredient in recipe.Ingredients)
            {
                if (_inventorySystem.CountItem(ingredient.Key) < ingredient.Value)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 아이템 조합
        /// </summary>
        public bool Craft(string recipeId)
        {
            if (!CanCraft(recipeId)) return false;
            
            var recipe = FindRecipe(recipeId);
            
            // 재료 소모
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!_inventorySystem.RemoveItem(ingredient.Key, ingredient.Value))
                {
                    // 실패 시 복구 로직 필요
                    return false;
                }
            }
            
            // 결과물 생성
            _inventorySystem.AddItem(recipe.ResultItemId, recipe.ResultAmount);
            
            EventBus.Publish(new ItemCraftedEvent
            {
                RecipeId = recipeId,
                ResultItemId = recipe.ResultItemId,
                Amount = recipe.ResultAmount
            });
            
            return true;
        }
        
        /// <summary>
        /// 다중 조합
        /// </summary>
        public bool CraftMultiple(string recipeId, int count)
        {
            var recipe = FindRecipe(recipeId);
            if (recipe == null) return false;
            
            // 재료 충분한지 확인
            foreach (var ingredient in recipe.Ingredients)
            {
                if (_inventorySystem.CountItem(ingredient.Key) < ingredient.Value * count)
                {
                    return false;
                }
            }
            
            // 조합 실행
            for (int i = 0; i < count; i++)
            {
                if (!Craft(recipeId))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 가능한 레시피 목록
        /// </summary>
        public List<CraftingRecipe> GetAvailableRecipes()
        {
            var available = new List<CraftingRecipe>();
            foreach (var recipe in _recipes)
            {
                if (CanCraft(recipe.RecipeId))
                {
                    available.Add(recipe);
                }
            }
            return available;
        }
        
        /// <summary>
        /// 모든 레시피
        /// </summary>
        public List<CraftingRecipe> GetAllRecipes()
        {
            return new List<CraftingRecipe>(_recipes);
        }
        
        private CraftingRecipe FindRecipe(string recipeId)
        {
            foreach (var recipe in _recipes)
            {
                if (recipe.RecipeId == recipeId)
                    return recipe;
            }
            return null;
        }
        
        public void AddRecipe(CraftingRecipe recipe)
        {
            if (recipe != null && FindRecipe(recipe.RecipeId) == null)
            {
                _recipes.Add(recipe);
            }
        }
        
        public object GetSaveData()
        {
            return new CraftingSaveData
            {
                Recipes = _recipes
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is CraftingSaveData data)
            {
                _recipes = data.Recipes ?? new List<CraftingRecipe>();
            }
        }
    }
    
    [System.Serializable]
    public class CraftingSaveData
    {
        public List<CraftingRecipe> Recipes;
    }
    
    /// <summary>
    /// 조합 완료 이벤트
    /// </summary>
    public class ItemCraftedEvent
    {
        public string RecipeId { get; set; }
        public string ResultItemId { get; set; }
        public int Amount { get; set; }
    }
}
