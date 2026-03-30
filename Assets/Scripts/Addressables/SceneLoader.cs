using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using SunnysideIsland.ResourceManagement;

namespace SunnysideIsland.AddressableManagement
{
    /// <summary>
    /// 씬 로딩 및 에셋 관리를 담당하는 클래스
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }
        
        [Header("=== Loading UI ===")]
        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private UnityEngine.UI.Slider _loadingBar;
        
        private bool _isLoading;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 씬을 비동기로 로드합니다.
        /// </summary>
        public async Task LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning("Scene is already loading!");
                return;
            }
            
            _isLoading = true;
            ShowLoadingScreen();
            
            try
            {
                // 1. 현재 씬 에셋 해제
                if (AddressableResourceManager.Instance != null)
                {
                    AddressableResourceManager.Instance.ReleaseAllAssets();
                }
                
                // 2. 다음 씬 필요 에셋 프리로드
                await PreloadSceneAssets(sceneName);
                
                // 3. 씬 로드
                var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                
                while (!handle.IsDone)
                {
                    UpdateLoadingProgress(handle.PercentComplete);
                    await Task.Yield();
                }
                
                await handle.Task;
                
                // 3.5. EventBus 초기화 (씬 전환 시 이전 씬의 이벤트 구독 제거)
                EventBus.Clear();
                
                // 4. 씬 초기화 완료 대기
                await Task.Delay(100); // 한 프레임 대기
            }
            finally
            {
                HideLoadingScreen();
                _isLoading = false;
            }
        }
        
        /// <summary>
        /// Addressable 씬을 로드합니다.
        /// </summary>
        public async Task LoadAddressableScene(AssetReference sceneReference)
        {
            if (_isLoading)
            {
                Debug.LogWarning("Scene is already loading!");
                return;
            }
            
            _isLoading = true;
            ShowLoadingScreen();
            
            try
            {
                if (AddressableResourceManager.Instance != null)
                {
                    AddressableResourceManager.Instance.ReleaseAllAssets();
                }
                
                var handle = sceneReference.LoadSceneAsync(LoadSceneMode.Single);
                
                while (!handle.IsDone)
                {
                    UpdateLoadingProgress(handle.PercentComplete);
                    await Task.Yield();
                }
                
                await handle.Task;
                await Task.Delay(100);
            }
            finally
            {
                HideLoadingScreen();
                _isLoading = false;
            }
        }
        
        /// <summary>
        /// 씬별 필요 에셋을 미리 로드합니다.
        /// </summary>
        private async Task PreloadSceneAssets(string sceneName)
        {
            // 씬별 필요 에셋 라벨
            string label = $"Scene_{sceneName}";
            
            if (AddressableResourceManager.Instance != null)
            {
                await AddressableResourceManager.Instance.PreloadAssetsByLabel(label);
            }
        }
        
        /// <summary>
        /// 로딩 화면을 표시합니다.
        /// </summary>
        private void ShowLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(true);
            }
        }
        
        /// <summary>
        /// 로딩 화면을 숨깁니다.
        /// </summary>
        private void HideLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
        }
        
        /// <summary>
        /// 로딩 진행도를 업데이트합니다.
        /// </summary>
        private void UpdateLoadingProgress(float progress)
        {
            if (_loadingBar != null)
            {
                _loadingBar.value = progress;
            }
        }
        
        /// <summary>
        /// 현재 로딩 중인지 확인합니다.
        /// </summary>
        public bool IsLoading()
        {
            return _isLoading;
        }
    }
}
