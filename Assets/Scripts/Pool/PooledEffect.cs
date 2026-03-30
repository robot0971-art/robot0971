using System.Collections;
using UnityEngine;

namespace SunnysideIsland.Pool
{
    public class PooledEffect : PoolableObject
    {
        [Header("=== Sprite Animation ===")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _frameRate = 10f;

        [Header("=== Particle System (Optional) ===")]
        [SerializeField] private ParticleSystem _particleSystem;

        [Header("=== Return Settings ===")]
        [SerializeField] private float _autoReturnDelay = 1f;

        private Coroutine _animationCoroutine;
        private bool _isPlaying;

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();

            _isPlaying = true;

            if (_particleSystem != null)
            {
                _particleSystem.Clear();
                _particleSystem.Play();
            }

            if (_frames != null && _frames.Length > 0 && _spriteRenderer != null)
            {
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }
                _animationCoroutine = StartCoroutine(PlaySpriteAnimation());
            }
            else
            {
                StartCoroutine(AutoReturnAfterDelay());
            }
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();

            _isPlaying = false;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            if (_particleSystem != null)
            {
                _particleSystem.Stop();
                _particleSystem.Clear();
            }
        }

        private IEnumerator PlaySpriteAnimation()
        {
            if (_frames == null || _frames.Length == 0 || _spriteRenderer == null)
            {
                yield break;
            }

            float frameInterval = 1f / _frameRate;

            for (int i = 0; i < _frames.Length; i++)
            {
                if (!_isPlaying) yield break;

                _spriteRenderer.sprite = _frames[i];
                yield return new WaitForSeconds(frameInterval);
            }

            yield return new WaitForSeconds(_autoReturnDelay);

            if (_isPlaying)
            {
                ReturnToPool();
            }
        }

        private IEnumerator AutoReturnAfterDelay()
        {
            yield return new WaitForSeconds(_autoReturnDelay);

            if (_isPlaying)
            {
                ReturnToPool();
            }
        }

        public void SetAutoReturnDelay(float delay)
        {
            _autoReturnDelay = delay;
        }

        public void SetFrames(Sprite[] frames)
        {
            _frames = frames;
        }
    }
}