using UnityEngine;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 스파크(불꽃놀이) 파티클 생성기
    /// 불에서 튀어나오는 작은 불꽃들
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class SparkParticles : MonoBehaviour
    {
        [Header("=== Spark Settings ===")]
        [Tooltip("스파크 색상")]
        [SerializeField] private Color _sparkColor = new Color(1f, 0.8f, 0.3f, 1f);
        
        [Tooltip("스파크 생성 간격(초)")]
        [SerializeField] [Range(0.1f, 2f)] private float _spawnInterval = 0.5f;
        
        [Tooltip("스파크 크기")]
        [SerializeField] [Range(0.02f, 0.1f)] private float _sparkSize = 0.04f;
        
        [Tooltip("스파크 수명")]
        [SerializeField] [Range(0.2f, 1f)] private float _lifetime = 0.4f;
        
        [Tooltip("스파크 속도")]
        [SerializeField] [Range(0.5f, 3f)] private float _speed = 1.5f;
        
        private ParticleSystem _particleSystem;
        private float _timer = 0f;
        private bool _isEmitting = false;
        
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            ConfigureSparks();
        }
        
        private void ConfigureSparks()
        {
            if (_particleSystem == null) return;
            
            var main = _particleSystem.main;
            var emission = _particleSystem.emission;
            var shape = _particleSystem.shape;
            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            
            // Main
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = _lifetime;
            main.startSpeed = _speed;
            main.startSize = _sparkSize;
            main.startColor = _sparkColor;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f; // 중력 적용 (아래로 떨어짐)
            
            // Emission - 버스트로 생성
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 3, 6) // 0초에 3~6개 생성
            });
            
            // Shape - 불 주변에서 랜덤
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.position = new Vector3(0, 0.2f, 0);
            
            // Velocity Over Lifetime은 사용하지 않음 (문제 방지)
            velocityOverLifetime.enabled = false;
            
            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            
            // 작은 원형 텍스처
            if (renderer.material == null)
            {
                Texture2D tex = ParticleTextureGenerator.CreateCircleTexture(16, false);
                renderer.material = ParticleTextureGenerator.CreateSimpleParticleMaterial(tex);
            }
            
            _particleSystem.Stop();
        }
        
        private void Update()
        {
            if (!_isEmitting) return;
            
            _timer += Time.deltaTime;
            
            if (_timer >= _spawnInterval)
            {
                _timer = 0f;
                EmitSparks();
            }
        }
        
        private void EmitSparks()
        {
            if (_particleSystem == null) return;
            
            // 랜덤하게 2~5개의 스파크 생성
            int count = Random.Range(2, 6);
            
            for (int i = 0; i < count; i++)
            {
                Vector3 position = transform.position + new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(0.15f, 0.25f),
                    0
                );
                
                Vector3 velocity = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(1.5f, 3f),
                    0
                );
                
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = position;
                emitParams.velocity = velocity;
                emitParams.startSize = _sparkSize * Random.Range(0.8f, 1.2f);
                emitParams.startColor = new Color(
                    _sparkColor.r * Random.Range(0.9f, 1f),
                    _sparkColor.g * Random.Range(0.7f, 0.9f),
                    _sparkColor.b * Random.Range(0.3f, 0.5f),
                    1f
                );
                
                _particleSystem.Emit(emitParams, 1);
            }
        }
        
        public void StartSparks()
        {
            _isEmitting = true;
        }
        
        public void StopSparks()
        {
            _isEmitting = false;
            _timer = 0f;
            if (_particleSystem != null)
            {
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
