using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class RecipeData
    {
        [Column("RecipeID")]
        public string recipeId;
        
        [Column("RecipeName")]
        public string recipeName;
        
        [Column("ResultItemID")]
        public string resultItemId;
        
        [Column("Ingredients")]
        public string ingredients;
        
        [Column("HungerRestore")]
        public int hungerRestore;
        
        [Column("AdditionalEffect")]
        public string additionalEffect;
        
        [Column("CookTime")]
        public int cookTime;
    }
}
