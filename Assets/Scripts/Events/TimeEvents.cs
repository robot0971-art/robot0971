namespace SunnysideIsland.Events
{
    /// <summary>
    /// 시간이 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class TimeChangedEvent
    {
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public float TimeOfDay { get; set; } // 0.0 ~ 1.0 (0 = 00:00, 1 = 24:00)
    }

    /// <summary>
    /// 새로운 하루가 시작되었을 때 발생하는 이벤트
    /// </summary>
    public class DayStartedEvent
    {
        public int Day { get; set; }
        public Season Season { get; set; }
    }

    /// <summary>
    /// 하루가 끝났을 때 발생하는 이벤트
    /// </summary>
    public class DayEndedEvent
    {
        public int Day { get; set; }
    }

    /// <summary>
    /// 시간대가 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class TimePhaseChangedEvent
    {
        public TimePhase PreviousPhase { get; set; }
        public TimePhase CurrentPhase { get; set; }
    }

    /// <summary>
    /// 계절
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    /// <summary>
    /// 시간대
    /// </summary>
    public enum TimePhase
    {
        Dawn,       // 04:00 - 06:00
        Morning,    // 06:00 - 09:00
        Noon,       // 09:00 - 12:00
        Afternoon,  // 12:00 - 14:00
        Evening,    // 14:00 - 18:00
        Dusk,       // 18:00 - 21:00
        Night       // 21:00 - 04:00
    }
}
