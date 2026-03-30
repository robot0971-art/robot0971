using System;
using UnityEngine;

namespace DI.Examples
{
    // 인터페이스 정의
    public interface IGameManager
    {
        void StartGame();
        void EndGame();
    }
    
    public interface IScoreService
    {
        int Score { get; }
        void AddScore(int points);
        void Reset();
    }
    
    public interface IAudioService
    {
        void PlaySound(string soundName);
        void StopAllSounds();
    }
    
    // 구현체
    public class GameManager : IGameManager
    {
        private IScoreService _scoreService;
        private IAudioService _audioService;
        
        public GameManager(IScoreService scoreService, IAudioService audioService)
        {
            _scoreService = scoreService;
            _audioService = audioService;
        }
        
        public void StartGame()
        {
            Debug.Log("Game Started!");
            _scoreService.Reset();
            _audioService.PlaySound("game_start");
        }
        
        public void EndGame()
        {
            Debug.Log($"Game Over! Final Score: {_scoreService.Score}");
            _audioService.PlaySound("game_over");
        }
    }
    
    public class ScoreService : IScoreService
    {
        public int Score { get; private set; }
        
        public void AddScore(int points)
        {
            Score += points;
            Debug.Log($"Score added: {points}, Total: {Score}");
        }
        
        public void Reset()
        {
            Score = 0;
        }
    }
    
    public class AudioService : IAudioService
    {
        public void PlaySound(string soundName)
        {
            Debug.Log($"Playing sound: {soundName}");
        }
        
        public void StopAllSounds()
        {
            Debug.Log("All sounds stopped");
        }
    }
    
    // 전역 Installer 예시 - MonoBehavior 없이 자동 실행
    public class ExampleGlobalInstaller : GlobalInstaller
    {
        protected override void InstallGlobalBindings()
        {
            // 싱글톤 서비스 등록
            Bind<IAudioService, AudioService>();
            Bind<IScoreService, ScoreService>();
            
            Debug.Log("Global bindings installed!");
        }
    }
    
    // 씬별 Installer 예시
    public class ExampleSceneInstaller : SceneInstaller
    {
        protected override void InstallSceneBindings()
        {
            // 씬별 서비스 등록 (글로벌 서비스 오버라이드 가능)
            Bind<IGameManager, GameManager>();
            
            Debug.Log("Scene bindings installed!");
        }
    }
    
    // 사용 예시
    public class ExamplePlayer : MonoBehaviour
    {
        [Inject] private IGameManager _gameManager;
        [Inject] private IScoreService _scoreService;
        [Inject("SFX")] private IAudioService _sfxAudio; // 키가 있는 경우
        
        private void Start()
        {
            _gameManager?.StartGame();
        }
        
        private void OnDestroy()
        {
            _gameManager?.EndGame();
        }
        
        public void CollectCoin()
        {
            _scoreService?.AddScore(10);
            _sfxAudio?.PlaySound("coin_collect");
        }
    }
    
    // 수동 주입 예시
    public class ExampleManualInject : MonoBehaviour
    {
        private IScoreService _scoreService;
        
        private void Awake()
        {
            // Awake에서 DIContainer.Inject(this) 호출 필요
            DIContainer.Inject(this);
        }
        
        //[Inject]
        private void InjectDependencies(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }
    }
    
    // 키가 있는 바인딩 예시
    public class ExampleKeyInstaller : SceneInstaller
    {
        protected override void InstallSceneBindings()
        {
            // 동일 타입, 다른 키로 여러 인스턴스 등록
            BindInstance<IAudioService>(new AudioService(), "BGM");
            BindInstance<IAudioService>(new AudioService(), "SFX");
        }
    }
    
    // 직접 Resolve 예시
    public class ExampleDirectResolve : MonoBehaviour
    {
        private void Start()
        {
            // 직접 Resolve
            var gameManager = DIContainer.Resolve<IGameManager>();
            gameManager.StartGame();
            
            // TryResolve - 안전하게 조회
            if (DIContainer.TryResolve<IAudioService>(out var audio))
            {
                audio.PlaySound("direct_resolve");
            }
        }
    }
}
