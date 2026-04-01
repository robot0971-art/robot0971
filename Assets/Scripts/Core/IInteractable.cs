namespace SunnysideIsland.Core
{
    /// <summary>
    /// 상호작용 가능한 오브젝트 인터페이스
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 상호작용 실행
        /// </summary>
        void Interact();
        
        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        bool CanInteract();
        
        /// <summary>
        /// 상호작용 힌트 텍스트
        /// </summary>
        string GetInteractionHint();
    }
}
