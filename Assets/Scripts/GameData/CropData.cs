using System;
using UnityEngine;
using ExcelConverter.Attributes;

namespace SunnysideIsland.GameData
{
    [CreateAssetMenu(fileName = "New Crop Data", menuName = "Farming/Crop Data")]
    public class CropData : ScriptableObject 
    {
        [Header("기본 정보")]
        [Column("CropID")]
        public string cropId;

        [Column("CropName")]
        public string cropName;

        [Header("성장/수확")]
        [Column("GrowthDays")]
        public int growthDays;

        [Column("YieldAmount")]
        public int yieldAmount;

        [Header("경제성")]
        [Column("Seasons")]
        public string seasons;

        [Column("BuyPrice")]
        public int buyPrice;

        [Column("SellPrice")]
        public int sellPrice;

        [Header("아이템 정보")]
        [Column("SeedItemID")]
        public string seedItemId;

        [Column("CropItemID")]
        public string cropItemId;

        [Column("SpecialEffect")]
        public string specialEffect;

        [Header("이미지 (6단계 사진)")]
        public Sprite[] growthSprites;

        [Header("=== Size ===")]
        [SerializeField] private float _cropScale = 0f; // 0 이하면 GameData 기본값 사용
        
        public float CropScale
        {
            get
            {
                if (_cropScale <= 0f)
                {
                    var gameData = Resources.Load<SunnysideIsland.GameData.GameData>("GameData/GameData");
                    if (gameData != null)
                    {
                        return gameData.defaultCropScale;
                    }
                    return 1f;
                }
                return _cropScale;
            }
        }
    }
}