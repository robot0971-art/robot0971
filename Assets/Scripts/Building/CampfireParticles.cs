using UnityEngine;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// Campfire 파티클 시스템
    /// 작고 섬세한 불꽃 효과
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class CampfireParticles : MonoBehaviour
    {
        [Header("=== Particle Settings ===")]
        [Tooltip("불꽃 색상 (시작)")]
        [SerializeField] private Color _startColorMin = new Color(1f, 0.6f, 0.1f, 1f); // 노랑-오렌지
        
        [Tooltip("불꽃 색상 (끝)")]
        [SerializeField] private Color _startColorMax = new Color(1f, 0.3f, 0.05f, 1f); // 오렌지-빨강
        
        [Tooltip("불꽃 크기")]
        [SerializeField] [Range(0.05f, 0.5f)] private float _particleSize = 0.15f;
        
        [Tooltip("불꽃 속도")]
        [SerializeField] [Range(0.3f, 2f)] private float _particleSpeed = 0.8f;
        
        [Tooltip("불꽃 수명(초)")]
        [SerializeField] [Range(0.3f, 2f)] private float _particleLifetime = 0.8f;
        
        [Tooltip("생성 속도(개/초)")]
        [SerializeField] [Range(10f, 60f)] private float _emissionRate = 25f;
        
        [Tooltip("최대 파티클 수")]
        [SerializeField] [Range(20, 100)] private int _maxParticles = 40;
        
        [Header("=== Shape Settings ===")]
        [Tooltip("파티클 모양 사용 (false = 원형, true = 불꽃 모양)")]
        [SerializeField] private bool _useFlameShape = false;
        
        [Tooltip("파티클 범위(각도)")]
        [SerializeField] [Range(10f, 60f)] private float _coneAngle = 25f;
        
        [Tooltip("파티클 높이")]
        [SerializeField] [Range(0.3f, 2f)] private float _fireHeight = 0.8f;
        
        private ParticleSystem _particleSystem;
        private bool _isPlaying = false;
        
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null)
            {
                _particleSystem = gameObject.AddComponent<ParticleSystem>();
            }
            
            ConfigureParticles();
        }
        
        /// <summary>
        /// 파티클 설정 적용
        /// </summary>
        private void ConfigureParticles()
        {
            if (_particleSystem == null) return;
            
            var main = _particleSystem.main;
            var emission = _particleSystem.emission;
            var shape = _particleSystem.shape;
            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            var colorOverLifetime = _particleSystem.colorOverLifetime;
            var sizeOverLifetime = _particleSystem.sizeOverLifetime;
            var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            
            // Main 설정
            main.startLifetime = new ParticleSystem.MinMaxCurve(_particleLifetime * 0.7f, _particleLifetime * 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_particleSpeed * 0.5f, _particleSpeed * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(_particleSize * 0.6f, _particleSize * 1.2f);
            main.maxParticles = _maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.2f; // 위로 떠오르는 효과
            
            // 색상 그라데이션 (시간에 따라 변화)
            var colorGradient = new ParticleSystem.MinMaxGradient();
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(_startColorMin, 0f),      // 시작: 노랑
                    new GradientColorKey(_startColorMax, 0.4f),    // 중간: 주황
                    new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 0.8f), // 끝: 붉은색
                    new GradientColorKey(new Color(0.3f, 0.05f, 0.02f), 1f)   // 완전히 어두워짐
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0.3f, 0.9f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorGradient.mode = ParticleSystemGradientMode.Gradient;
            colorGradient.gradient = grad;
            main.startColor = colorGradient;
            
            // Emission 설정
            emission.enabled = true;
            emission.rateOverTime = _emissionRate;
            
            // Shape 설정 (Cone - 아래에서 위로)
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = _coneAngle;
            shape.radius = 0.12f; // 시작 지점 (모닥불 중심)
            shape.position = new Vector3(0, 0.1f, 0); // Base 약간 위에서 시작
            shape.rotation = new Vector3(-90f, 0, 0); // 위쪽을 향하도록
            
            // Velocity Over Lifetime은 사용하지 않음 (Shape과 StartSpeed로 대체)
            velocityOverLifetime.enabled = false;
            
            // 대신 StartSpeed로 위로 올라가는 효과
            
            // Color Over Lifetime - 시간에 따라 색상 변화
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
            
            // Size Over Lifetime - 작아지는 효과
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.5f, 0.8f);
            sizeCurve.AddKey(0.8f, 0.4f);
            sizeCurve.AddKey(1f, 0.1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Renderer 설정
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            
            // 자동 텍스처 및 머티리얼 생성
            if (renderer.material == null || renderer.material.name == "Default-Material")
            {
                Texture2D particleTexture;
                if (_useFlameShape)
                {
                    particleTexture = ParticleTextureGenerator.CreateFlameTexture(64);
                }
                else
                {
                    particleTexture = ParticleTextureGenerator.CreateCircleTexture(64, true);
                }
                renderer.material = ParticleTextureGenerator.CreateSimpleParticleMaterial(particleTexture);
            }
            
            // 초기 상태는 정지
            StopFire();
            
            Debug.Log("[CampfireParticles] Particles configured with " + (_useFlameShape ? "flame" : "circle") + " shape");
        }
        
        /// <summary>
        /// 불 켜기
        /// </summary>
        public void StartFire()
        {
            if (_particleSystem == null) return;
            
            _particleSystem.Play();
            _isPlaying = true;
            
            Debug.Log("[CampfireParticles] Fire started");
        }
        
        /// <summary>
        /// 불 끄기
        /// </summary>
        public void StopFire()
        {
            if (_particleSystem == null) return;
            
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _isPlaying = false;
            
            Debug.Log("[CampfireParticles] Fire stopped");
        }
        
        /// <summary>
        /// 현재 재생 중인지 확인
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 파티클 수 확인
        /// </summary>
        public int ParticleCount => _particleSystem?.particleCount ?? 0;
        
        /// <summary>
        /// 파티클 설정을 다시 적용 (Inspector에서 호출 가능)
        /// </summary>
        [ContextMenu("Reconfigure Particles")]
        public void Reconfigure()
        {
            ConfigureParticles();
            if (_isPlaying && _particleSystem != null)
            {
                _particleSystem.Play();
            }
        }
        
        /// <summary>
        /// 테스트용 - 불 켜기/끄기 토글
        /// </summary>
        [ContextMenu("Toggle Fire")]
        public void ToggleFire()
        {
            if (_isPlaying)
                StopFire();
            else
                StartFire();
        }
    }
}
