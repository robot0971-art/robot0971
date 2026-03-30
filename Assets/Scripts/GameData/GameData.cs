using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.GameData
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Sunnyside Island/Game Data")]
    public class GameData : ScriptableObject
    {
        [Header("=== Items ===")]
        public List<ItemData> items = new List<ItemData>();
        public List<ToolData> tools = new List<ToolData>();

        
        [Header("=== Farming ===")]
        public List<CropData> crops = new List<CropData>();
        
        [Header("=== Farming Settings ===")]
        public float defaultPlotScale = 1f;
        public float defaultCropScale = 1f;
        
        [Header("=== Fishing ===")]
        public List<FishData> fishData = new List<FishData>();
        public List<FishingRodData> fishingRods = new List<FishingRodData>();
        
        [Header("=== Animals ===")]
        public List<AnimalData> animals = new List<AnimalData>();
        
        [Header("=== Buildings ===")]
        public List<BuildingData> buildings = new List<BuildingData>();
        public List<CommercialBuildingData> commercialBuildings = new List<CommercialBuildingData>();
        public List<TouristBuildingData> touristBuildings = new List<TouristBuildingData>();
        
        [Header("=== NPCs ===")]
        public List<ResidentData> residents = new List<ResidentData>();
        public List<TouristTypeData> touristTypes = new List<TouristTypeData>();
        
        [Header("=== Crafting & Recipes ===")]
        public List<RecipeData> recipes = new List<RecipeData>();
        public List<CraftingRecipeData> craftingRecipes = new List<CraftingRecipeData>();
        
        [Header("=== Quests & Skills ===")]
        public List<QuestData> quests = new List<QuestData>();
        public List<SkillData> skills = new List<SkillData>();
        
        [Header("=== Events & Achievements ===")]
        public List<EventData> events = new List<EventData>();
        public List<AchievementData> achievements = new List<AchievementData>();
        
        [Header("=== World ===")]
        public List<TimeOfDayData> timeOfDayData = new List<TimeOfDayData>();
        public List<WeatherData> weatherData = new List<WeatherData>();
        public List<ResourceSpawnData> resourceSpawns = new List<ResourceSpawnData>();
        public List<AreaData> areas = new List<AreaData>();
        
        [Header("=== Shops ===")]
        public List<ShopItemData> shopItems = new List<ShopItemData>();

        // Helper Methods
        public ItemData GetItem(string itemId)
        {
            return items.Find(x => x.itemId == itemId);
        }

        public CropData GetCrop(string cropId)
        {
            return crops.Find(x => x.cropId == cropId);
        }

        public FishData GetFish(string fishId)
        {
            return fishData.Find(x => x.fishId == fishId);
        }



        public BuildingData GetBuilding(string buildingId)
        {
            return buildings.Find(x => x.buildingId == buildingId);
        }

        public QuestData GetQuest(string questId)
        {
            return quests.Find(x => x.questId == questId);
        }

        public RecipeData GetRecipe(string recipeId)
        {
            return recipes.Find(x => x.recipeId == recipeId);
        }

        public SkillData GetSkill(string skillId)
        {
            return skills.Find(x => x.skillId == skillId);
        }
    }
}
