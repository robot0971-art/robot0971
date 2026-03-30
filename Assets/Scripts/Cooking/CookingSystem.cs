using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;

namespace SunnysideIsland.Cooking
{
    /// <summary>
    /// 요리 레시피
    /// </summary>
    [System.Serializable]
    public class CookingRecipe
    {
        public string RecipeId;
        public string ResultItemId;
        public int ResultAmount;
        public int HungerRestore;
        public Dictionary<string, int> Ingredients;
    }

    /// <summary>
    /// 요리 시스템
    /// </summary>
    public class CookingSystem : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private List<CookingRecipe> _recipes = new List<CookingRecipe>();
        
        private IInventorySystem _inventorySystem;
        
        public string SaveKey => "CookingSystem";
        
        private void Start()
        {
            _inventorySystem = FindObjectOfType<InventorySystem>();
        }
        
        /// <summary>
        /// 요리 가능 여부 확인
        /// </summary>
        public bool CanCook(string recipeId)
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
        /// 요리하기
        /// </summary>
        public bool Cook(string recipeId)
        {
            if (!CanCook(recipeId)) return false;
            
            var recipe = FindRecipe(recipeId);
            
            // 재료 소모
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!_inventorySystem.RemoveItem(ingredient.Key, ingredient.Value))
                {
                    return false;
                }
            }
            
            // 음식 생성
            _inventorySystem.AddItem(recipe.ResultItemId, recipe.ResultAmount);
            
            EventBus.Publish(new FoodCookedEvent
            {
                RecipeId = recipeId,
                ResultItemId = recipe.ResultItemId,
                Amount = recipe.ResultAmount,
                HungerRestore = recipe.HungerRestore
            });
            
            return true;
        }
        
        /// <summary>
        /// 가능한 레시피 목록
        /// </summary>
        public List<CookingRecipe> GetAvailableRecipes()
        {
            var available = new List<CookingRecipe>();
            foreach (var recipe in _recipes)
            {
                if (CanCook(recipe.RecipeId))
                {
                    available.Add(recipe);
                }
            }
            return available;
        }
        
        /// <summary>
        /// 모든 레시피
        /// </summary>
        public List<CookingRecipe> GetAllRecipes()
        {
            return new List<CookingRecipe>(_recipes);
        }
        
        private CookingRecipe FindRecipe(string recipeId)
        {
            foreach (var recipe in _recipes)
            {
                if (recipe.RecipeId == recipeId)
                    return recipe;
            }
            return null;
        }
        
        public void AddRecipe(CookingRecipe recipe)
        {
            if (recipe != null && FindRecipe(recipe.RecipeId) == null)
            {
                _recipes.Add(recipe);
            }
        }
        
        public object GetSaveData()
        {
            return new CookingSaveData
            {
                Recipes = _recipes
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is CookingSaveData data)
            {
                _recipes = data.Recipes ?? new List<CookingRecipe>();
            }
        }
    }
    
    [System.Serializable]
    public class CookingSaveData
    {
        public List<CookingRecipe> Recipes;
    }
    
    /// <summary>
    /// 요리 완료 이벤트
    /// </summary>
    public class FoodCookedEvent
    {
        public string RecipeId { get; set; }
        public string ResultItemId { get; set; }
        public int Amount { get; set; }
        public int HungerRestore { get; set; }
    }
}
