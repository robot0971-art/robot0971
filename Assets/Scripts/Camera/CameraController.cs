using UnityEngine;

namespace SunnysideIsland.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Header("=== Target ===")]
        [SerializeField] private Transform _target;
        
        [Header("=== Follow Settings ===")]
        [SerializeField] private float _followSpeed = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10);
        [SerializeField] private float _lookAheadFactor = 2f;
        [SerializeField] private float _lookAheadReturnSpeed = 2f;
        [SerializeField] private float _lookAheadMoveThreshold = 0.1f;
        
        [Header("=== Bounds ===")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-100, -100);
        [SerializeField] private Vector2 _maxBounds = new Vector2(100, 100);
        
        [Header("=== Dead Zone ===")]
        [SerializeField] private bool _useDeadZone = false;
        [SerializeField] private float _deadZoneRadius = 0.5f;
        
        [Header("=== Shake ===")]
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeIntensity = 0.5f;
        [SerializeField] private float _shakeDecreaseFactor = 1f;
        
        private Vector3 _lastTargetPosition;
        private Vector3 _currentVelocity;
        private Vector3 _lookAheadPos;
        private float _shakeTimer;
        private Vector3 _shakeOffset;
        
        private UnityEngine.Camera _camera;
        
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }
        
        public bool IsShaking => _shakeTimer > 0;
        
        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }
        
        private void Start()
        {
            if (_target != null)
            {
                _lastTargetPosition = _target.position;
                transform.position = GetTargetPosition();
            }
        }
        
        private void LateUpdate()
        {
            if (_target == null) return;
            
            UpdateFollow();
            UpdateShake();
        }
        
        private void UpdateFollow()
        {
            Vector3 targetPosition = GetTargetPosition();
            
            if (_useDeadZone)
            {
                float distance = Vector3.Distance(transform.position, targetPosition);
                if (distance < _deadZoneRadius)
                {
                    return;
                }
            }
            
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                1f / _followSpeed
            );
            
            if (_useBounds)
            {
                ApplyBounds();
            }
        }
        
        private Vector3 GetTargetPosition()
        {
            if (_target == null) return transform.position;
            
            float xMoveDelta = _target.position.x - _lastTargetPosition.x;
            
            bool updateLookAheadTarget = Mathf.Abs(xMoveDelta) > _lookAheadMoveThreshold;
            
            if (updateLookAheadTarget)
            {
                _lookAheadPos = _lookAheadFactor * Vector3.right * Mathf.Sign(xMoveDelta);
            }
            else
            {
                _lookAheadPos = Vector3.MoveTowards(
                    _lookAheadPos,
                    Vector3.zero,
                    Time.deltaTime * _lookAheadReturnSpeed
                );
            }
            
            _lastTargetPosition = _target.position;
            
            Vector3 targetPos = _target.position + _offset + _lookAheadPos;
            targetPos.z = _offset.z;
            
            return targetPos;
        }
        
        private void ApplyBounds()
        {
            Vector3 pos = transform.position;
            
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            
            pos.x = Mathf.Clamp(pos.x, _minBounds.x + halfWidth, _maxBounds.x - halfWidth);
            pos.y = Mathf.Clamp(pos.y, _minBounds.y + halfHeight, _maxBounds.y - halfHeight);
            
            transform.position = pos;
        }
        
        private void UpdateShake()
        {
            if (_shakeTimer > 0)
            {
                _shakeTimer -= Time.deltaTime * _shakeDecreaseFactor;
                
                if (_shakeTimer > 0)
                {
                    float intensity = _shakeIntensity * (_shakeTimer / _shakeDuration);
                    _shakeOffset = Random.insideUnitSphere * intensity;
                    _shakeOffset.z = 0;
                    
                    transform.position += _shakeOffset;
                }
                else
                {
                    _shakeTimer = 0;
                    _shakeOffset = Vector3.zero;
                }
            }
        }
        
        public void Shake(float duration = -1, float intensity = -1)
        {
            _shakeTimer = duration > 0 ? duration : _shakeDuration;
            _shakeIntensity = intensity > 0 ? intensity : _shakeIntensity;
        }
        
        public void Shake(float duration, float intensity, float decreaseFactor)
        {
            _shakeDuration = duration;
            _shakeIntensity = intensity;
            _shakeDecreaseFactor = decreaseFactor;
            _shakeTimer = duration;
        }
        
        public void SetBounds(Vector2 min, Vector2 max)
        {
            _minBounds = min;
            _maxBounds = max;
            _useBounds = true;
        }
        
        public void ClearBounds()
        {
            _useBounds = false;
        }
        
        public void SnapToTarget()
        {
            if (_target == null) return;
            
            transform.position = GetTargetPosition();
            _currentVelocity = Vector3.zero;
        }
    }
}