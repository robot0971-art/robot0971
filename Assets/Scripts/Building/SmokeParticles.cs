using UnityEngine;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 연기 파티클 생성기
    /// 불에서 올라가는 부드러운 연기
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class SmokeParticles : MonoBehaviour
    {
        [Header("=== Smoke Settings ===")]
        [Tooltip("연기 색상 (시작)")]
        [SerializeField] private Color _smokeColorStart = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        
        [Tooltip("연기 색상 (끝)")]
        [SerializeField] private Color _smokeColorEnd = new Color(0.5f, 0.5f, 0.5f, 0f);
        
        [Tooltip("연기 생성 간격")]
        [SerializeField] [Range(0.05f, 0.5f)] private float _emissionRate = 0.15f;
        
        [Tooltip("연기 크기")]
        [SerializeField] [Range(0.1f, 0.5f)] private float _smokeSize = 0.25f;
        
        [Tooltip("연기 수명")]
        [SerializeField] [Range(1f, 4f)] private float _lifetime = 2.5f;
        
        [Tooltip("연기 상승 속도")]
        [SerializeField] [Range(0.3f, 1.5f)] private float _riseSpeed = 0.6f;
        
        [Tooltip("바람 영향")]
        [SerializeField] [Range(0f, 0.5f)] private float _windEffect = 0.15f;
        
        private ParticleSystem _particleSystem;
        private bool _isPlaying = false;
        
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            ConfigureSmoke();
        }
        
        private void ConfigureSmoke()
        {
            if (_particleSystem == null) return;
            
            var main = _particleSystem.main;
            var emission = _particleSystem.emission;
            var shape = _particleSystem.shape;
            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            var colorOverLifetime = _particleSystem.colorOverLifetime;
            var sizeOverLifetime = _particleSystem.sizeOverLifetime;
            var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            
            // Main
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(_lifetime * 0.8f, _lifetime * 1.2f);
            main.startSpeed = 0f;
            main.startSize = _smokeSize;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f; // 천천히 위로
            
            // 색상 그라데이션
            Gradient colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(_smokeColorStart, 0f),
                    new GradientColorKey(new Color(0.4f, 0.4f, 0.4f, 0.2f), 0.6f),
                    new GradientColorKey(_smokeColorEnd, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(0.15f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(colorGrad);
            
            // Emission
            emission.enabled = true;
            emission.rateOverTime = _emissionRate;
            
            // Shape - 불 위에서
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            shape.position = new Vector3(0, 0.3f, 0);
            shape.rotation = new Vector3(0, 0, 0);
            
            // Velocity Over Lifetime은 사용하지 않음 (문제 방지)
            velocityOverLifetime.enabled = false;
            
            // Color over lifetime
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGrad);
            
            // Size over lifetime - 커지면서 흩어짐
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.3f, 0.8f);
            sizeCurve.AddKey(0.7f, 1.2f);
            sizeCurve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            renderer.sortingOrder = 5; // 불보다 위에
            
            // 소프트 원형 텍스처
            if (renderer.material == null)
            {
                Texture2D tex = ParticleTextureGenerator.CreateCircleTexture(64, true);
                renderer.material = ParticleTextureGenerator.CreateSimpleParticleMaterial(tex);
            }
            
            _particleSystem.Stop();
        }
        
        public void StartSmoke()
        {
            if (_particleSystem == null) return;
            
            _particleSystem.Play();
            _isPlaying = true;
        }
        
        public void StopSmoke()
        {
            if (_particleSystem == null) return;
            
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _isPlaying = false;
        }
        
        /// <summary>
        /// 바람 방향 설정
        /// </summary>
        public void SetWindDirection(float windX)
        {
            if (_particleSystem == null) return;
            
            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            velocityOverLifetime.x = windX;
        }
    }
}
