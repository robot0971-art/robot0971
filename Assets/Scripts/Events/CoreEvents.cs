namespace SunnysideIsland.Events
{
    /// <summary>
    /// 게임이 시작되었을 때 발생하는 이벤트
    /// </summary>
    public class GameStartedEvent
    {
        public bool IsNewGame { get; set; }
        public string SaveName { get; set; }
    }

    /// <summary>
    /// 게임이 일시정지되었을 때 발생하는 이벤트
    /// </summary>
    public class GamePausedEvent
    {
        public bool IsPaused { get; set; }
    }

    /// <summary>
    /// 게임이 저장되었을 때 발생하는 이벤트
    /// </summary>
    public class GameSavedEvent
    {
        public string SaveName { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 게임이 불러와졌을 때 발생하는 이벤트
    /// </summary>
    public class GameLoadedEvent
    {
        public string SaveName { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 메인 메뉴로 돌아갈 때 발생하는 이벤트
    /// </summary>
    public class ReturnToMainMenuEvent
    {
    }

    /// <summary>
    /// 게임 오버되었을 때 발생하는 이벤트
    /// </summary>
    public class GameOverEvent
    {
        public string DeathReason { get; set; }
    }

    /// <summary>
    /// 게임 초기화가 완료되었을 때 발생하는 이벤트
    /// </summary>
    public class InitializationCompleteEvent
    {
        public int TotalSteps { get; set; }
        public float TotalTime { get; set; }
    }

    /// <summary>
    /// 게임 초기화 중 에러가 발생했을 때 발생하는 이벤트
    /// </summary>
    public class InitializationErrorEvent
    {
        public string ErrorMessage { get; set; }
        public string StepName { get; set; }
        public System.Exception Exception { get; set; }
    }
}
