using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class CraftingRecipeData
    {
        [Column("RecipeID")]
        public string recipeId;
        
        [Column("Category")]
        public CraftingCategory category;
        
        [Column("ResultItemID")]
        public string resultItemId;
        
        [Column("ResultAmount")]
        public int resultAmount;
        
        [Column("Ingredients")]
        public string ingredients;
        
        [Column("RequiredTool")]
        public string requiredTool;
    }

    public enum CraftingCategory
    {
        Tool,
        Equipment,
        Furniture,
        Construction,
        Cooking,
        Processing
    }
}
