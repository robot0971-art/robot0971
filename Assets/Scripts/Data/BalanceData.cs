using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Data
{
    /// <summary>
    /// 게임 밸런스 데이터
    /// 모든 수치값을 중앙에서 관리
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceData", menuName = "SunnysideIsland/Data/BalanceData")]
    public class BalanceData : ScriptableObject
    {
        [Header("=== Player Balance ===")]
        public PlayerBalance Player = new PlayerBalance();

        [Header("=== Survival Balance ===")]
        public SurvivalBalance Survival = new SurvivalBalance();

        [Header("=== Economy Balance ===")]
        public EconomyBalance Economy = new EconomyBalance();

        [Header("=== Combat Balance ===")]
        public CombatBalance Combat = new CombatBalance();

        [Header("=== Farming Balance ===")]
        public FarmingBalance Farming = new FarmingBalance();

        [Header("=== Fishing Balance ===")]
        public FishingBalance Fishing = new FishingBalance();

        [Header("=== Gathering Balance ===")]
        public GatheringBalance Gathering = new GatheringBalance();

        [Header("=== Tourism Balance ===")]
        public TourismBalance Tourism = new TourismBalance();
    }

    #region Balance Classes

    /// <summary>
    /// 플레이어 밸런스
    /// </summary>
    [Serializable]
    public class PlayerBalance
    {
        [Header("Movement")]
        public float WalkSpeed = 3f;
        public float SprintSpeed = 6f;
        public float RollSpeed = 8f;
        public float RollDuration = 0.3f;
        public float RollCooldown = 1f;
        
        [Header("Stamina Costs")]
        public float SprintStaminaCost = 10f;       // 초당
        public float RollStaminaCost = 20f;
        public float AttackStaminaCost = 10f;
        
        [Header("Interaction")]
        public float InteractionRange = 2f;
    }

    /// <summary>
    /// 생존 밸런스
    /// </summary>
    [Serializable]
    public class SurvivalBalance
    {
        [Header("Health")]
        public int MaxHealth = 100;
        public int HealthRegenRate = 1;            // 분당
        public int StarvingHealthDamage = 5;       // 일일
        
        [Header("Hunger")]
        public int MaxHunger = 100;
        public int HungerDecayRate = 2;            // 시간당
        public int HungerDecaySprint = 3;          // 스프린트 시
        
        [Header("Stamina")]
        public int MaxStamina = 100;
        public float StaminaRegenRate = 15f;       // 초당
        public float StaminaRegenDelay = 2f;       // 사용 후 재생까지
        
        [Header("States")]
        public int HungerHungryThreshold = 50;
        public int HungerStarvingThreshold = 20;
        public float StarvingDamageMultiplier = 1.5f;
    }

    /// <summary>
    /// 경제 밸런스
    /// </summary>
    [Serializable]
    public class EconomyBalance
    {
        [Header("Starting Resources")]
        public int StartingGold = 100;
        public int StartingFood = 10;
        
        [Header("Prices")]
        public int ItemPriceMultiplier = 100;       // 기본 아이템 가격 계수
        public int BuildingPriceMultiplier = 1;
        public float SellPriceRatio = 0.6f;        // 판매 가격 비율
        public float BuyPriceRatio = 1.0f;         // 구매 가격 비율
        
        [Header("Income")]
        public int BaseDailyIncome = 10;
        public float TouristIncomePerVisitor = 5f;
        public float ResidentTaxRate = 0.1f;
        
        [Header("Price Fluctuation")]
        public float MinPriceModifier = 0.8f;
        public float MaxPriceModifier = 1.5f;
    }

    /// <summary>
    /// 전투 밸런스
    /// </summary>
    [Serializable]
    public class CombatBalance
    {
        [Header("Player Combat")]
        public int BaseDamage = 10;
        public float AttackCooldown = 0.5f;
        public float AttackRange = 1.5f;
        public float InvincibilityTime = 0.5f;
        
        [Header("Damage Formula")]
        public float DefenseReduction = 0.5f;       // 방어력당 데미지 감소 %
        public float CriticalChance = 0.1f;
        public float CriticalMultiplier = 2f;
        
        [Header("Enemy Scaling")]
        public float EnemyHealthPerDay = 5f;       // 일일 증가량
        public float EnemyDamagePerDay = 1f;
        
        [Header("Boss")]
        public int GoblinChiefHP = 500;
        public int GoblinChiefAttack = 30;
        public int GoblinChiefDefense = 20;
        public int GoblinChiefExpReward = 500;
    }

    /// <summary>
    /// 농사 밸런스
    /// </summary>
    [Serializable]
    public class FarmingBalance
    {
        [Header("Growth")]
        public float BaseGrowthTime = 2f;          // 일
        public float RainGrowthBonus = 0.2f;       // 비 올 때 보너스
        public float FertilizerGrowthBonus = 0.3f;
        
        [Header("Quality")]
        public float BaseQuality = 0.5f;
        public float WateredQualityBonus = 0.2f;
        public float FertilizerQualityBonus = 0.3f;
        
        [Header("Yield")]
        public int BaseYieldMin = 1;
        public int BaseYieldMax = 3;
        public float HighQualityYieldBonus = 1f;
        
        [Header("Maintenance")]
        public float WaterDecayRate = 0.5f;        // 일일
        public int WeedChance = 10;                // %
        public int PestChance = 5;                 // %
    }

    /// <summary>
    /// 낚시 밸런스
    /// </summary>
    [Serializable]
    public class FishingBalance
    {
        [Header("Catch Rates")]
        public float BaseCatchChance = 0.3f;
        public float RainCatchBonus = 0.2f;
        public float DawnCatchBonus = 0.15f;
        public float NightCatchBonus = 0.1f;
        
        [Header("Difficulty")]
        public float CommonFishDifficulty = 0.3f;
        public float RareFishDifficulty = 0.6f;
        public float LegendaryFishDifficulty = 0.9f;
        
        [Header("Time")]
        public float MinWaitTime = 2f;
        public float MaxWaitTime = 10f;
        public float ReelingTime = 5f;
        
        [Header("Rewards")]
        public int CommonFishPrice = 20;
        public int RareFishPrice = 100;
        public int LegendaryFishPrice = 500;
    }

    /// <summary>
    /// 채집 밸런스
    /// </summary>
    [Serializable]
    public class GatheringBalance
    {
        [Header("Tree")]
        public int TreeHitPoints = 5;
        public int WoodPerHit = 2;
        public int WoodPerTree = 10;
        public float TreeRespawnTime = 3f;         // 일
        
        [Header("Rock")]
        public int RockHitPoints = 8;
        public int StonePerHit = 1;
        public int StonePerRock = 5;
        public int IronPerRock = 2;
        public float RockRespawnTime = 5f;
        
        [Header("Herb")]
        public int HerbGatherTime = 1;             // 초
        public int HerbsPerNode = 3;
        public float HerbRespawnTime = 1f;
        
        [Header("Tool Durability")]
        public int WoodToolDurability = 50;
        public int IronToolDurability = 200;
        public float DurabilityLossPerHit = 1f;
    }

    /// <summary>
    /// 관광 밸런스
    /// </summary>
    [Serializable]
    public class TourismBalance
    {
        [Header("Tourist Arrival")]
        public int BaseTouristPerDay = 2;
        public float ReputationTouristBonus = 0.1f; // 평판당
        public float BuildingTouristBonus = 0.5f;   // 관광 건물당
        
        [Header("Satisfaction")]
        public float BaseSatisfaction = 50f;
        public float FoodSatisfactionBonus = 10f;
        public float InnSatisfactionBonus = 15f;
        public float AttractionSatisfactionBonus = 5f;
        
        [Header("Reputation")]
        public float BaseReputation = 0f;
        public float ReputationPerSatisfiedTourist = 1f;
        public float ReputationPerUnsatisfiedTourist = -2f;
        public float MaxReputation = 100f;
        
        [Header("Income")]
        public float BaseTouristSpend = 20f;
        public float SatisfactionIncomeMultiplier = 0.5f;
        
        [Header("Milestones")]
        public int TouristMilestone1 = 10;          // 첫 번째 마일스톤
        public int TouristMilestone2 = 50;
        public int TouristMilestone3 = 100;
        public int TouristMilestone4 = 200;
    }

    #endregion

    /// <summary>
    /// 밸런스 데이터 제공자
    /// </summary>
    public static class BalanceProvider
    {
        private static BalanceData _instance;
        
        public static BalanceData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BalanceData>("BalanceData");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[BalanceProvider] BalanceData not found, using defaults");
                        _instance = ScriptableObject.CreateInstance<BalanceData>();
                    }
                }
                return _instance;
            }
        }
        
        public static void SetBalanceData(BalanceData data)
        {
            _instance = data;
        }
    }
}