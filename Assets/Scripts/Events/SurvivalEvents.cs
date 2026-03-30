namespace SunnysideIsland.Events
{
    /// <summary>
    /// 허기 수치가 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class HungerChangedEvent
    {
        public float CurrentHunger { get; set; }
        public float MaxHunger { get; set; }
        public HungerState State { get; set; }
    }

    /// <summary>
    /// 체력이 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class HealthChangedEvent
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public float ChangeAmount { get; set; }
    }

    /// <summary>
    /// 스태미나가 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class StaminaChangedEvent
    {
        public float CurrentStamina { get; set; }
        public float MaxStamina { get; set; }
        public float ChangeAmount { get; set; }
    }

    /// <summary>
    /// 허기 상태
    /// </summary>
    public enum HungerState
    {
        Full,       // 80-100
        Normal,     // 40-79
        Hungry,     // 20-39
        Starving    // 0-19
    }
}
