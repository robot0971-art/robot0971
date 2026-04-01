using UnityEngine;
using UnityEngine.Video;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// Campfire Video Player 설정
    /// 비디오 애니메이션용
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CampfireVideoPlayer : MonoBehaviour
    {
        [Header("=== Video Settings ===")]
        [Tooltip("재생할 비디오 파일")]
        [SerializeField] private VideoClip _videoClip;
        
        [Tooltip("Render Texture")]
        [SerializeField] private RenderTexture _renderTexture;
        
        [Tooltip("비디오 재생 속도")]
        [SerializeField] [Range(0.1f, 2f)] private float _playbackSpeed = 1f;
        
        [Tooltip("루프 재생")]
        [SerializeField] private bool _loop = true;
        
        private VideoPlayer _videoPlayer;
        private SpriteRenderer _spriteRenderer;
        
        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            SetupVideoPlayer();
        }
        
        private void SetupVideoPlayer()
        {
            if (_videoPlayer == null) return;
            
            // Video Player 설정
            _videoPlayer.clip = _videoClip;
            _videoPlayer.playbackSpeed = _playbackSpeed;
            _videoPlayer.isLooping = _loop;
            
            // Render Texture 설정
            if (_renderTexture != null)
            {
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                _videoPlayer.targetTexture = _renderTexture;
            }
            else
            {
                // Render Texture가 없으면 Camera Near Plane 사용
                _videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            }
            
            // 오디오 설정 (음소거)
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            
            // 준비 완료 시 재생
            _videoPlayer.prepareCompleted += OnPrepareCompleted;
            _videoPlayer.Prepare();
        }
        
        private void OnPrepareCompleted(VideoPlayer vp)
        {
            _videoPlayer.Play();
            Debug.Log("[CampfireVideoPlayer] Video playback started");
        }
        
        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= OnPrepareCompleted;
            }
        }
        
        /// <summary>
        /// 비디오 재생
        /// </summary>
        public void Play()
        {
            if (_videoPlayer != null && !_videoPlayer.isPlaying)
            {
                _videoPlayer.Play();
            }
        }
        
        /// <summary>
        /// 비디오 정지
        /// </summary>
        public void Stop()
        {
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Stop();
            }
        }
        
        /// <summary>
        /// 비디오 일시정지
        /// </summary>
        public void Pause()
        {
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Pause();
            }
        }
    }
}
