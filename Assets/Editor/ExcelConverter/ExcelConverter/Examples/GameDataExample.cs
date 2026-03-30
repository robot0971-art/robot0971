using UnityEngine;
using ExcelConverter.Attributes;

// Example GameData ScriptableObject
// 이 파일은 Unity Editor에서 "Create > Game > GameData" 메뉴로 생성 가능합니다

[CreateAssetMenu(fileName = "GameData", menuName = "Game/GameData", order = 1)]
public class GameData : ScriptableObject
{
    [Header("Character Data")]
    // Characters 시트와 자동 매핑
    public System.Collections.Generic.List<CharacterData> Characters;
    
    [Header("Item Data")]
    // Items 시트와 자동 매핑
    public System.Collections.Generic.List<ItemData> Items;
    
    [Header("Equipment Data")]
    // EquipmentTable 시트와 강제 매핑
    [Sheet("EquipmentTable")]
    public System.Collections.Generic.List<EquipmentData> Equipments;
    
    [Header("Meta")]
    // 변환에서 제외 (런타임 계산용)
    [Ignore]
    public int Version = 1;
}

[System.Serializable]
public class CharacterData
{
    // Id 컬럼과 자동 매핑
    public int Id;
    
    // Name 컬럼과 자동 매핑
    public string Name;
    
    // Level 컬럼과 자동 매핑
    public int Level;
    
    // Hp 컬럼과 자동 매핑
    public float Hp;
    
    // CharacterType 컬럼과 강제 매핑
    [Column("CharacterType")]
    public string Type;
    
    // 변환에서 제외
    [Ignore]
    public float TotalPower;
}

[System.Serializable]
public class ItemData
{
    public int Id;
    public string Name;
    public int Price;
    public bool IsConsumable;
    public ItemRarity Rarity;
}

[System.Serializable]
public class EquipmentData
{
    public int Id;
    public string Name;
    public int Attack;
    public int Defense;
}

public enum ItemRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}
