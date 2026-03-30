using UnityEngine;

namespace SunnysideIsland.Events
{
    /// <summary>
    /// 플레이어가 이동했을 때 발생하는 이벤트
    /// </summary>
    public class PlayerMovedEvent
    {
        public Vector3 Position { get; set; }
        public Vector2 Direction { get; set; }
        public bool IsSprinting { get; set; }
    }

    /// <summary>
    /// 플레이어가 데미지를 입었을 때 발생하는 이벤트
    /// </summary>
    public class PlayerDamagedEvent
    {
        public int Damage { get; set; }
        public int RemainingHealth { get; set; }
        public string DamageSource { get; set; }
    }

    /// <summary>
    /// 플레이어가 사망했을 때 발생하는 이벤트
    /// </summary>
    public class PlayerDiedEvent
    {
        public string DeathReason { get; set; }
    }

    /// <summary>
    /// 플레이어가 부활했을 때 발생하는 이벤트
    /// </summary>
    public class PlayerRespawnedEvent
    {
        public Vector3 RespawnPosition { get; set; }
    }

    /// <summary>
    /// 플레이어가 아이템을 줍었을 때 발생하는 이벤트
    /// </summary>
    public class ItemPickedUpEvent
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int TotalQuantity { get; set; }
    }

    /// <summary>
    /// 플레이어가 아이템을 떨어뜨렸을 때 발생하는 이벤트
    /// </summary>
    public class ItemDroppedEvent
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public Vector3 DropPosition { get; set; }
    }

    /// <summary>
    /// 플레이어 경험치가 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class PlayerExperienceChangedEvent
    {
        public int CurrentExperience { get; set; }
        public int RequiredExperience { get; set; }
        public int Level { get; set; }
    }

    /// <summary>
    /// 플레이어가 레벨업했을 때 발생하는 이벤트
    /// </summary>
    public class PlayerLevelUpEvent
    {
        public int NewLevel { get; set; }
        public int MaxHealth { get; set; }
        public int MaxStamina { get; set; }
    }

    /// <summary>
    /// 인벤토리에서 아이템이 이동했을 때 발생하는 이벤트
    /// </summary>
    public class ItemMovedEvent
    {
        public int FromSlot { get; set; }
        public int ToSlot { get; set; }
        public string ItemId { get; set; }
    }
}
