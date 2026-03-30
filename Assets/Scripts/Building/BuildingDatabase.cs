using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 건물 카테고리
    /// </summary>
    public enum BuildingCategory
    {
        Residential,
        Commercial,
        Tourist,
        Production,
        Decoration,
        Defense
    }

    /// <summary>
    /// 배치 타입
    /// </summary>
    public enum PlacementType
    {
        GroundOnly,
        SeaShore
    }

    /// <summary>
    /// 건물 크기
    /// </summary>
    [Serializable]
    public struct BuildingSize
    {
        public int Width;
        public int Height;

        public BuildingSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// 건물 타입 (BuildingData.cs의 BuildingType과 매칭)
    /// </summary>
    public enum BuildingTypeExt
    {
        Residential,
        Agriculture,
        Commercial,
        Tourist,
        Production
    }

    /// <summary>
    /// 건설 비용
    /// </summary>
    [Serializable]
    public class ConstructionCost
    {
        public int Gold;
        public List<string> Materials = new List<string>();
        public List<int> Amounts = new List<int>();
    }

    /// <summary>
    /// 건물 효과
    /// </summary>
    [Serializable]
    public class BuildingEffect
    {
        public string EffectType;      // "TouristCap", "ResidentCap", "Income", "Defense" 등
        public float Value;
        public string Description;
    }

    /// <summary>
    /// 상세 건물 데이터
    /// </summary>
    [Serializable]
    public class DetailedBuildingData
    {
        public string BuildingId;
        public string BuildingName;
        public string Description;
        public BuildingCategory Category;
        public BuildingTypeExt Type;
        public BuildingSize Size;
        public int BuildTime;
        public ConstructionCost Cost;
        public PlacementType PlacementType = PlacementType.GroundOnly;
        public float PreviewScale = 1f;
        public List<BuildingEffect> Effects = new List<BuildingEffect>();
        public int MaxResidents;
        public int TouristCapacity;
        public int DailyIncome;
        public string[] UnlocksAtQuest;
        public string UpgradeFrom;
        public string UpgradeTo;
        public bool IsUnlockedDefault;
        public GameObject BuildingPrefab;
    }

    /// <summary>
    /// 모든 건물 데이터를 관리하는 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "SunnysideIsland/Building/Database")]
    public class BuildingDatabase : ScriptableObject
    {
        [Header("=== All Buildings ===")]
        [SerializeField] private List<DetailedBuildingData> _buildings = new List<DetailedBuildingData>();

        public List<DetailedBuildingData> GetAllBuildings() => _buildings;

        public DetailedBuildingData GetBuilding(string buildingId)
        {
            foreach (var building in _buildings)
            {
                if (building.BuildingId == buildingId)
                    return building;
            }
            return null;
        }

        public List<DetailedBuildingData> GetBuildingsByCategory(BuildingCategory category)
        {
            var result = new List<DetailedBuildingData>();
            foreach (var building in _buildings)
            {
                if (building.Category == category)
                    result.Add(building);
            }
            return result;
        }

        #region Static Building Data Generation

        public static List<DetailedBuildingData> GenerateAllBuildings()
        {
            var buildings = new List<DetailedBuildingData>();

            // 주거 시설
            buildings.AddRange(GenerateResidentialBuildings());

            // 탈출용 배
            buildings.Add(GenerateBoat());

            // 상업 시설
            buildings.AddRange(GenerateCommercialBuildings());

            // 관광 시설
            buildings.AddRange(GenerateTouristBuildings());

            // 생산 시설
            buildings.AddRange(GenerateProductionBuildings());

            // 장식물
            buildings.AddRange(GenerateDecorations());

            // 방어 시설
            buildings.AddRange(GenerateDefenseBuildings());

            return buildings;
        }

        private static List<DetailedBuildingData> GenerateResidentialBuildings()
        {
            var buildings = new List<DetailedBuildingData>();

            // 텐트
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Tent",
                BuildingName = "텐트",
                Description = "가장 기본적인 거처. 비바람을 피할 수 있다.",
                Category = BuildingCategory.Residential,
                Type = BuildingTypeExt.Residential,
                Size = new BuildingSize(2, 2),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 0,
                    Materials = new List<string> { "Wood", "Cloth" },
                    Amounts = new List<int> { 10, 5 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Rest", Value = 0.5f, Description = "휴식 효율 50%" }
                },
                MaxResidents = 1,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 오두막
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Hut",
                BuildingName = "오두막",
                Description = "나무로 지은 간단한 집. 텐트보다 편안하다.",
                Category = BuildingCategory.Residential,
                Type = BuildingTypeExt.Residential,
                Size = new BuildingSize(3, 3),
                BuildTime = 1,
                Cost = new ConstructionCost
                {
                    Gold = 200,
                    Materials = new List<string> { "Wood", "Stone" },
                    Amounts = new List<int> { 30, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Rest", Value = 0.75f, Description = "휴식 효율 75%" }
                },
                MaxResidents = 2,
                DailyIncome = 0,
                UpgradeFrom = "Tent",
                UpgradeTo = "House",
                IsUnlockedDefault = false
            });

            // 집
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "House",
                BuildingName = "집",
                Description = "제대로 지은 집. 한 가족이 살기 충분하다.",
                Category = BuildingCategory.Residential,
                Type = BuildingTypeExt.Residential,
                Size = new BuildingSize(4, 4),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 0,
                    Materials = new List<string> { "wood" },
                    Amounts = new List<int> { 10 }
                },
                PlacementType = PlacementType.GroundOnly,
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Rest", Value = 1f, Description = "휴식 효율 100%" },
                    new BuildingEffect { EffectType = "Happiness", Value = 10f, Description = "행복도 +10" }
                },
                MaxResidents = 4,
                DailyIncome = 0,
                UpgradeFrom = "Hut",
                UpgradeTo = "LargeHouse",
                IsUnlockedDefault = true
            });

            // 큰 집
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "LargeHouse",
                BuildingName = "큰 집",
                Description = "넓은 2층 집. 여러 가족이 살 수 있다.",
                Category = BuildingCategory.Residential,
                Type = BuildingTypeExt.Residential,
                Size = new BuildingSize(5, 5),
                BuildTime = 3,
                Cost = new ConstructionCost
                {
                    Gold = 0,
                    Materials = new List<string> { "wood" },
                    Amounts = new List<int> { 30 }
                },
                PlacementType = PlacementType.GroundOnly,
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Rest", Value = 1.2f, Description = "휴식 효율 120%" },
                    new BuildingEffect { EffectType = "Happiness", Value = 20f, Description = "행복도 +20" }
                },
                MaxResidents = 8,
                DailyIncome = 0,
                UpgradeFrom = "House",
                UpgradeTo = "Mansion",
                IsUnlockedDefault = true
            });

            // 저택
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Mansion",
                BuildingName = "저택",
                Description = "호화로운 저택. 섬에서 가장 좋은 집이다.",
                Category = BuildingCategory.Residential,
                Type = BuildingTypeExt.Residential,
                Size = new BuildingSize(6, 6),
                BuildTime = 5,
                Cost = new ConstructionCost
                {
                    Gold = 5000,
                    Materials = new List<string> { "Wood", "Stone", "Iron", "Glass", "Gold" },
                    Amounts = new List<int> { 100, 100, 30, 20, 5 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Rest", Value = 1.5f, Description = "휴식 효율 150%" },
                    new BuildingEffect { EffectType = "Happiness", Value = 50f, Description = "행복도 +50" },
                    new BuildingEffect { EffectType = "TouristAttraction", Value = 1f, Description = "관광 명소" }
                },
                MaxResidents = 12,
                DailyIncome = 10,
                UpgradeFrom = "LargeHouse",
                IsUnlockedDefault = false
            });

            return buildings;
        }

        private static DetailedBuildingData GenerateBoat()
        {
            return new DetailedBuildingData
            {
                BuildingId = "Boat",
                BuildingName = "탈출용 배",
                Description = "섬을 탈출할 수 있는 배. 바닷가에만 지을 수 있다.",
                Category = BuildingCategory.Production,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(3, 2),
                BuildTime = 10,
                Cost = new ConstructionCost
                {
                    Gold = 0,
                    Materials = new List<string> { "wood" },
                    Amounts = new List<int> { 50 }
                },
                PlacementType = PlacementType.SeaShore,
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Escape", Value = 1f, Description = "섬 탈출" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            };
        }

        private static List<DetailedBuildingData> GenerateCommercialBuildings()
        {
            var buildings = new List<DetailedBuildingData>();

            // 노점
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Stall",
                BuildingName = "노점",
                Description = "간단한 가판대. 기본적인 물건을 팔 수 있다.",
                Category = BuildingCategory.Commercial,
                Type = BuildingTypeExt.Commercial,
                Size = new BuildingSize(2, 2),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 100,
                    Materials = new List<string> { "Wood" },
                    Amounts = new List<int> { 15 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "ShopSlots", Value = 4, Description = "판매 슬롯 4개" }
                },
                MaxResidents = 1,
                DailyIncome = 20,
                IsUnlockedDefault = true
            });

            // 식료품점
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "GroceryStore",
                BuildingName = "식료품점",
                Description = "신선한 식품을 파는 가게.",
                Category = BuildingCategory.Commercial,
                Type = BuildingTypeExt.Commercial,
                Size = new BuildingSize(3, 3),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 500,
                    Materials = new List<string> { "Wood", "Stone" },
                    Amounts = new List<int> { 40, 30 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "ShopSlots", Value = 8, Description = "판매 슬롯 8개" },
                    new BuildingEffect { EffectType = "FoodPrice", Value = 1.2f, Description = "식품 가격 +20%" }
                },
                MaxResidents = 2,
                DailyIncome = 50,
                UpgradeFrom = "Stall",
                IsUnlockedDefault = false
            });

            // 대장간
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Blacksmith",
                BuildingName = "대장간",
                Description = "무기와 도구를 만들고 파는 곳.",
                Category = BuildingCategory.Commercial,
                Type = BuildingTypeExt.Commercial,
                Size = new BuildingSize(3, 3),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 600,
                    Materials = new List<string> { "Stone", "Iron" },
                    Amounts = new List<int> { 50, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "ShopSlots", Value = 6, Description = "판매 슬롯 6개" },
                    new BuildingEffect { EffectType = "WeaponPrice", Value = 1.3f, Description = "무기 가격 +30%" },
                    new BuildingEffect { EffectType = "CraftBonus", Value = 0.1f, Description = "제작 품질 +10%" }
                },
                MaxResidents = 2,
                DailyIncome = 40,
                IsUnlockedDefault = false
            });

            // 식당
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Restaurant",
                BuildingName = "식당",
                Description = "맛있는 요리를 제공하는 식당.",
                Category = BuildingCategory.Commercial,
                Type = BuildingTypeExt.Commercial,
                Size = new BuildingSize(4, 3),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 800,
                    Materials = new List<string> { "Wood", "Stone" },
                    Amounts = new List<int> { 50, 40 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "CookingBonus", Value = 1.2f, Description = "요리 효율 +20%" },
                    new BuildingEffect { EffectType = "TouristFood", Value = 1, Description = "관광객 식사 제공" }
                },
                MaxResidents = 3,
                DailyIncome = 80,
                IsUnlockedDefault = false
            });

            // 여관
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Inn",
                BuildingName = "여관",
                Description = "여행자가 묵을 수 있는 숙소.",
                Category = BuildingCategory.Commercial,
                Type = BuildingTypeExt.Commercial,
                Size = new BuildingSize(4, 4),
                BuildTime = 3,
                Cost = new ConstructionCost
                {
                    Gold = 1200,
                    Materials = new List<string> { "Wood", "Stone", "Cloth" },
                    Amounts = new List<int> { 60, 50, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "TouristCapacity", Value = 5, Description = "관광객 수용 +5" },
                    new BuildingEffect { EffectType = "TouristHappiness", Value = 10, Description = "관광객 만족도 +10" }
                },
                MaxResidents = 4,
                TouristCapacity = 5,
                DailyIncome = 100,
                IsUnlockedDefault = false
            });

            // 시장
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Market",
                BuildingName = "시장",
                Description = "다양한 상품을 거래하는 활기찬 시장.",
                Category = BuildingCategory.Commercial,
                Type = BuildingTypeExt.Commercial,
                Size = new BuildingSize(5, 5),
                BuildTime = 4,
                Cost = new ConstructionCost
                {
                    Gold = 2000,
                    Materials = new List<string> { "Wood", "Stone", "Cloth" },
                    Amounts = new List<int> { 100, 80, 30 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "ShopSlots", Value = 20, Description = "판매 슬롯 20개" },
                    new BuildingEffect { EffectType = "PriceBonus", Value = 1.1f, Description = "판매 가격 +10%" }
                },
                MaxResidents = 6,
                DailyIncome = 150,
                IsUnlockedDefault = false
            });

            return buildings;
        }

        private static List<DetailedBuildingData> GenerateTouristBuildings()
        {
            var buildings = new List<DetailedBuildingData>();

            // 부두
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Dock",
                BuildingName = "부두",
                Description = "배가 정박할 수 있는 선착장.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(4, 2),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 800,
                    Materials = new List<string> { "Wood", "Rope" },
                    Amounts = new List<int> { 50, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "TouristCapacity", Value = 10, Description = "관광객 수용 +10" },
                    new BuildingEffect { EffectType = "FishingBonus", Value = 1.2f, Description = "낚시 효율 +20%" }
                },
                MaxResidents = 1,
                TouristCapacity = 10,
                DailyIncome = 50,
                IsUnlockedDefault = false
            });

            // 등대
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Lighthouse",
                BuildingName = "등대",
                Description = "밤에 빛을 비추어 배를 인도한다.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(2, 2),
                BuildTime = 3,
                Cost = new ConstructionCost
                {
                    Gold = 1500,
                    Materials = new List<string> { "Stone", "Glass", "Iron" },
                    Amounts = new List<int> { 60, 20, 15 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "TouristCapacity", Value = 5, Description = "관광객 수용 +5" },
                    new BuildingEffect { EffectType = "Safety", Value = 20, Description = "안전 +20" },
                    new BuildingEffect { EffectType = "TouristAttraction", Value = 1.5f, Description = "관광 명소" }
                },
                MaxResidents = 1,
                TouristCapacity = 5,
                DailyIncome = 30,
                IsUnlockedDefault = false
            });

            // 광장
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Plaza",
                BuildingName = "광장",
                Description = "주민과 관광객이 모이는 열린 공간.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(5, 5),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 500,
                    Materials = new List<string> { "Stone" },
                    Amounts = new List<int> { 30 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Happiness", Value = 15, Description = "행복도 +15" },
                    new BuildingEffect { EffectType = "EventBonus", Value = 1.2f, Description = "이벤트 효과 +20%" }
                },
                MaxResidents = 0,
                DailyIncome = 10,
                IsUnlockedDefault = false
            });

            // 공원
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Park",
                BuildingName = "공원",
                Description = "아름다운 정원과 벤치가 있는 공원.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(4, 4),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 600,
                    Materials = new List<string> { "Wood", "Flower" },
                    Amounts = new List<int> { 20, 30 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Happiness", Value = 20, Description = "행복도 +20" },
                    new BuildingEffect { EffectType = "TouristAttraction", Value = 0.5f, Description = "관광 명소" }
                },
                MaxResidents = 1,
                DailyIncome = 20,
                IsUnlockedDefault = false
            });

            // 축제장
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "FestivalGround",
                BuildingName = "축제장",
                Description = "축제와 이벤트를 열 수 있는 장소.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(6, 6),
                BuildTime = 3,
                Cost = new ConstructionCost
                {
                    Gold = 1500,
                    Materials = new List<string> { "Wood", "Cloth", "Decoration" },
                    Amounts = new List<int> { 40, 30, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "EventBonus", Value = 2f, Description = "이벤트 효과 +100%" },
                    new BuildingEffect { EffectType = "TouristCapacity", Value = 20, Description = "관광객 수용 +20" }
                },
                MaxResidents = 2,
                TouristCapacity = 20,
                DailyIncome = 50,
                IsUnlockedDefault = false
            });

            // 온천 여관
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "HotSpring_Inn",
                BuildingName = "온천 여관",
                Description = "천연 온천을 즐길 수 있는 고급 여관.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(5, 4),
                BuildTime = 4,
                Cost = new ConstructionCost
                {
                    Gold = 3000,
                    Materials = new List<string> { "Wood", "Stone", "Cloth" },
                    Amounts = new List<int> { 80, 60, 40 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "TouristCapacity", Value = 15, Description = "관광객 수용 +15" },
                    new BuildingEffect { EffectType = "TouristHappiness", Value = 30, Description = "관광객 만족도 +30" },
                    new BuildingEffect { EffectType = "Healing", Value = 2f, Description = "체력 회복 +100%" }
                },
                MaxResidents = 4,
                TouristCapacity = 15,
                DailyIncome = 200,
                IsUnlockedDefault = false
            });

            // 리조트 호텔
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Resort_Hotel",
                BuildingName = "리조트 호텔",
                Description = "섬 최고의 럭셔리 리조트 호텔.",
                Category = BuildingCategory.Tourist,
                Type = BuildingTypeExt.Tourist,
                Size = new BuildingSize(6, 6),
                BuildTime = 7,
                Cost = new ConstructionCost
                {
                    Gold = 10000,
                    Materials = new List<string> { "Wood", "Stone", "Iron", "Glass", "Gold" },
                    Amounts = new List<int> { 150, 120, 40, 30, 10 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "TouristCapacity", Value = 50, Description = "관광객 수용 +50" },
                    new BuildingEffect { EffectType = "TouristHappiness", Value = 50, Description = "관광객 만족도 +50" },
                    new BuildingEffect { EffectType = "IslandReputation", Value = 100, Description = "섬 평판 +100" }
                },
                MaxResidents = 10,
                TouristCapacity = 50,
                DailyIncome = 500,
                IsUnlockedDefault = false
            });

            return buildings;
        }

        private static List<DetailedBuildingData> GenerateProductionBuildings()
        {
            var buildings = new List<DetailedBuildingData>();

            // 밭
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "FarmPlot",
                BuildingName = "밭",
                Description = "작물을 재배할 수 있는 밭.",
                Category = BuildingCategory.Production,
                Type = BuildingTypeExt.Agriculture,
                Size = new BuildingSize(1, 1),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 10,
                    Materials = new List<string>(),
                    Amounts = new List<int>()
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "FarmSlots", Value = 1, Description = "농사 슬롯 1개" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 저장고
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Storage",
                BuildingName = "저장고",
                Description = "아이템을 보관할 수 있는 창고.",
                Category = BuildingCategory.Production,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(3, 2),
                BuildTime = 1,
                Cost = new ConstructionCost
                {
                    Gold = 300,
                    Materials = new List<string> { "Wood", "Stone" },
                    Amounts = new List<int> { 30, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "StorageSlots", Value = 50, Description = "저장 슬롯 +50" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = false
            });

            // 낚시터
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "FishingSpot",
                BuildingName = "낚시터",
                Description = "낚시를 할 수 있는 자리.",
                Category = BuildingCategory.Production,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(2, 2),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 0,
                    Materials = new List<string>(),
                    Amounts = new List<int>()
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "FishingBonus", Value = 1.1f, Description = "낚시 효율 +10%" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 체험 농장
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Farm_Tour",
                BuildingName = "체험 농장",
                Description = "관광객이 농사를 체험할 수 있는 농장.",
                Category = BuildingCategory.Production,
                Type = BuildingTypeExt.Agriculture,
                Size = new BuildingSize(4, 4),
                BuildTime = 2,
                Cost = new ConstructionCost
                {
                    Gold = 400,
                    Materials = new List<string> { "Wood", "Fence" },
                    Amounts = new List<int> { 20, 10 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "FarmSlots", Value = 8, Description = "농사 슬롯 8개" },
                    new BuildingEffect { EffectType = "TouristAttraction", Value = 0.3f, Description = "체험 관광" }
                },
                MaxResidents = 1,
                TouristCapacity = 5,
                DailyIncome = 30,
                IsUnlockedDefault = false
            });

            return buildings;
        }

        private static List<DetailedBuildingData> GenerateDecorations()
        {
            var buildings = new List<DetailedBuildingData>();

            // 벤치
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Bench",
                BuildingName = "벤치",
                Description = "쉬어갈 수 있는 벤치.",
                Category = BuildingCategory.Decoration,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(1, 1),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 20,
                    Materials = new List<string> { "Wood" },
                    Amounts = new List<int> { 3 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Happiness", Value = 2, Description = "행복도 +2" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 가로등
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "StreetLight",
                BuildingName = "가로등",
                Description = "밤길을 밝혀주는 가로등.",
                Category = BuildingCategory.Decoration,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(1, 1),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 50,
                    Materials = new List<string> { "Iron", "Glass" },
                    Amounts = new List<int> { 2, 1 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Safety", Value = 5, Description = "안전 +5" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 꽃밭
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "FlowerBed",
                BuildingName = "꽃밭",
                Description = "아름다운 꽃이 피어있는 꽃밭.",
                Category = BuildingCategory.Decoration,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(2, 1),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 30,
                    Materials = new List<string> { "Flower" },
                    Amounts = new List<int> { 10 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Happiness", Value = 5, Description = "행복도 +5" },
                    new BuildingEffect { EffectType = "TouristAttraction", Value = 0.1f, Description = "관광 매력 +0.1" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 분수대
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Fountain",
                BuildingName = "분수대",
                Description = "아름다운 분수대.",
                Category = BuildingCategory.Decoration,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(2, 2),
                BuildTime = 1,
                Cost = new ConstructionCost
                {
                    Gold = 300,
                    Materials = new List<string> { "Stone", "Iron" },
                    Amounts = new List<int> { 20, 5 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Happiness", Value = 15, Description = "행복도 +15" },
                    new BuildingEffect { EffectType = "TouristAttraction", Value = 0.5f, Description = "관광 명소" }
                },
                MaxResidents = 0,
                DailyIncome = 5,
                IsUnlockedDefault = false
            });

            return buildings;
        }

        private static List<DetailedBuildingData> GenerateDefenseBuildings()
        {
            var buildings = new List<DetailedBuildingData>();

            // 감시탑
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Watchtower",
                BuildingName = "감시탑",
                Description = "주변을 감시할 수 있는 탑.",
                Category = BuildingCategory.Defense,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(2, 2),
                BuildTime = 1,
                Cost = new ConstructionCost
                {
                    Gold = 400,
                    Materials = new List<string> { "Wood", "Stone" },
                    Amounts = new List<int> { 30, 20 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Defense", Value = 20, Description = "방어력 +20" },
                    new BuildingEffect { EffectType = "DetectionRange", Value = 10f, Description = "감지 범위 +10" }
                },
                MaxResidents = 1,
                DailyIncome = 0,
                IsUnlockedDefault = false
            });

            // 담장
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "Fence",
                BuildingName = "담장",
                Description = "마을을 보호하는 담장.",
                Category = BuildingCategory.Defense,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(1, 1),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 20,
                    Materials = new List<string> { "Wood" },
                    Amounts = new List<int> { 5 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Defense", Value = 5, Description = "방어력 +5" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                IsUnlockedDefault = true
            });

            // 석벽
            buildings.Add(new DetailedBuildingData
            {
                BuildingId = "StoneWall",
                BuildingName = "석벽",
                Description = "튼튼한 돌로 쌓은 벽.",
                Category = BuildingCategory.Defense,
                Type = BuildingTypeExt.Production,
                Size = new BuildingSize(1, 1),
                BuildTime = 0,
                Cost = new ConstructionCost
                {
                    Gold = 50,
                    Materials = new List<string> { "Stone" },
                    Amounts = new List<int> { 10 }
                },
                Effects = new List<BuildingEffect>
                {
                    new BuildingEffect { EffectType = "Defense", Value = 15, Description = "방어력 +15" }
                },
                MaxResidents = 0,
                DailyIncome = 0,
                UpgradeFrom = "Fence",
                IsUnlockedDefault = false
            });

            return buildings;
        }

        #endregion
    }
}