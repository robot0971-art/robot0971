using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SunnysideIsland.AddressableManagement
{
    /// <summary>
    /// Addressable로 인스턴스화된 오브젝트의 카운트를 관리하는 컴포넌트
    /// </summary>
    public class AddressableInstanceTracker : MonoBehaviour
    {
        public string AssetGUID { get; private set; }
        public bool IsAddressableInstance { get; private set; }
        
        public void Initialize(string guid)
        {
            AssetGUID = guid;
            IsAddressableInstance = !string.IsNullOrEmpty(guid);
        }
        
        private void OnDestroy()
        {
            if (IsAddressableInstance && !string.IsNullOrEmpty(AssetGUID))
            {
                // Addressables.ReleaseInstance을 호출하여 카운트 감소
                Addressables.ReleaseInstance(gameObject);
            }
        }
    }
}
