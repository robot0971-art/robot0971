using SunnysideIsland.Localization;

namespace SunnysideIsland.Events
{
    /// <summary>
    /// 언어가 변경되었을 때 발생하는 이벤트
    /// </summary>
    public class LanguageChangedEvent
    {
        public Language PreviousLanguage { get; set; }
        public Language NewLanguage { get; set; }
    }
}