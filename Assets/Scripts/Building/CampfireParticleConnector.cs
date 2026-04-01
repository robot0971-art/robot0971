using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// Campfire 파티클 커넥터
    /// 메인 불꽃, 불꽃놀이(스파크), 연기를 연결하여 멋진 불 효과 생성
    /// </summary>
    public class CampfireParticleConnector : MonoBehaviour
    {
        [Header("=== Particle Systems ===")]
        [Tooltip("메인 불꽃 파티클")]
        [SerializeField] private ParticleSystem _mainFireParticles;
        
        [Tooltip("불꽃놀이/스파크 파티클")]
        [SerializeField] private ParticleSystem _sparkParticles;
        
        [Tooltip("연기 파티클")]
        [SerializeField] private ParticleSystem _smokeParticles;
        
        [Header("=== Light Effect ===")]
        [Tooltip("불빛 (Light2D)")]
        [SerializeField] private Light2D _campfireLight;
        
        [Tooltip("불빛 깜빡임 범위")]
        [SerializeField] [Range(0.05f, 0.3f)] private float _flickerRange = 0.15f;
        
        [Tooltip("불빛 깜빡임 속도")]
        [SerializeField] [Range(1f, 10f)] private float _flickerSpeed = 4f;
        
        [Header("=== Audio ===")]
        [Tooltip("불 타는 소리")]
        [SerializeField] private AudioSource _fireSound;
        
        private float _baseLightIntensity;
        private bool _isPlaying = false;
        private Coroutine _flickerCoroutine;
        
        private void Awake()
        {
            InitializeParticleSystems();
        }
        
        private void InitializeParticleSystems()
        {
            // 메인 불꽃이 없으면 자식에서 찾기
            if (_mainFireParticles == null)
            {
                _mainFireParticles = GetComponentInChildren<ParticleSystem>();
            }
            
            // Light 설정
            if (_campfireLight != null)
            {
                _baseLightIntensity = _campfireLight.intensity;
            }
            
            // 초기 상태는 정지
            StopAllEffects();
        }
        
        /// <summary>
        /// 모든 효과 시작
        /// </summary>
        public void StartAllEffects()
        {
            if (_isPlaying) return;
            
            _isPlaying = true;
            
            // 메인 불꽃
            if (_mainFireParticles != null)
            {
                _mainFireParticles.Play();
            }
            
            // 스파크 (지연 시작)
            StartCoroutine(StartSparksWithDelay(0.2f));
            
            // 연기 (지연 시작)
            StartCoroutine(StartSmokeWithDelay(0.5f));
            
            // 불빛 깜빡임 시작
            if (_campfireLight != null && _flickerCoroutine == null)
            {
                _flickerCoroutine = StartCoroutine(FlickerLight());
            }
            
            // 소리 재생
            if (_fireSound != null)
            {
                _fireSound.loop = true;
                _fireSound.Play();
            }
            
            Debug.Log("[CampfireParticleConnector] All effects started");
        }
        
        /// <summary>
        /// 모든 효과 정지
        /// </summary>
        public void StopAllEffects()
        {
            if (!_isPlaying) return;
            
            _isPlaying = false;
            
            // 메인 불꽃
            if (_mainFireParticles != null)
            {
                _mainFireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            
            // 스파크
            if (_sparkParticles != null)
            {
                _sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            
            // 연기
            if (_smokeParticles != null)
            {
                _smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            
            // 불빛 깜빡임 정지
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }
            
            // 불빛 끄기
            if (_campfireLight != null)
            {
                _campfireLight.intensity = 0f;
            }
            
            // 소리 정지
            if (_fireSound != null)
            {
                _fireSound.Stop();
            }
            
            Debug.Log("[CampfireParticleConnector] All effects stopped");
        }
        
        /// <summary>
        /// 스파크 지연 시작
        /// </summary>
        private IEnumerator StartSparksWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_isPlaying && _sparkParticles != null)
            {
                _sparkParticles.Play();
            }
        }
        
        /// <summary>
        /// 연기 지연 시작
        /// </summary>
        private IEnumerator StartSmokeWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_isPlaying && _smokeParticles != null)
            {
                _smokeParticles.Play();
            }
        }
        
        /// <summary>
        /// 불빛 깜빡임 효과
        /// </summary>
        private IEnumerator FlickerLight()
        {
            float time = 0f;
            
            while (_isPlaying && _campfireLight != null)
            {
                time += Time.deltaTime * _flickerSpeed;
                
                // Perlin noise로 자연스러운 깜빡임
                float noise = Mathf.PerlinNoise(time, 0f);
                float flicker = Mathf.Lerp(-_flickerRange, _flickerRange, noise);
                
                _campfireLight.intensity = Mathf.Max(0f, _baseLightIntensity + flicker);
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 불꽃 크기 조정 (0~1)
        /// </summary>
        public void SetFireIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            
            if (_mainFireParticles != null)
            {
                var emission = _mainFireParticles.emission;
                emission.rateOverTime = Mathf.Lerp(5f, 35f, intensity);
            }
            
            if (_campfireLight != null)
            {
                _baseLightIntensity = Mathf.Lerp(0.3f, 1f, intensity);
                _campfireLight.intensity = _baseLightIntensity;
            }
        }
        
        /// <summary>
        /// 현재 재생 중인지 확인
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 파티클 시스템 설정
        /// </summary>
        public void SetupParticleSystems(ParticleSystem main, ParticleSystem sparks = null, ParticleSystem smoke = null)
        {
            _mainFireParticles = main;
            _sparkParticles = sparks;
            _smokeParticles = smoke;
        }
        
        private void OnDestroy()
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
            }
        }
    }
}
