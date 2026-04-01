using UnityEngine;

namespace SunnysideIsland.Weather
{
    /// <summary>
    /// 비 효과가 카메라를 따라다니도록 하는 스크립트
    /// </summary>
    public class RainEffectFollowCamera : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [Tooltip("카메라와의 Y축 오프셋 (비가 위에서 내리도록)")]
        [SerializeField] private float _yOffset = 12f;
        
        [Tooltip("카메라와의 Z축 위치 (2D에서는 0)")]
        [SerializeField] private float _zPosition = 0f;
        
        [Tooltip("위치 보간 속도 (높을수록 빠르게 따라감)")]
        [SerializeField] private float _followSpeed = 10f;
        
        private Transform _targetCamera;
        private Vector3 _velocity;
        
        private void Start()
        {
            FindCamera();
        }
        
        private void Update()
        {
            FollowCamera();
        }
        
        /// <summary>
        /// 메인 카메라 찾기
        /// </summary>
        private void FindCamera()
        {
            if (UnityEngine.Camera.main != null)
            {
                _targetCamera = UnityEngine.Camera.main.transform;
                Debug.Log($"[RainEffectFollowCamera] Camera found: {_targetCamera.name}");
            }
            else
            {
                Debug.LogWarning("[RainEffectFollowCamera] No main camera found!");
            }
        }
        
        /// <summary>
        /// 카메라 따라가기
        /// </summary>
        private void FollowCamera()
        {
            if (_targetCamera == null)
            {
                FindCamera();
                return;
            }
            
            // 카메라 위치 가져오기
            Vector3 targetPosition = _targetCamera.position;
            
            // Y축 오프셋 적용 (비가 위에서 내리도록)
            targetPosition.y += _yOffset;
            
            // Z축 고정 (2D)
            targetPosition.z = _zPosition;
            
            // 부드럽게 따라가기
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref _velocity, 
                1f / _followSpeed
            );
        }
        
        /// <summary>
        /// 카메라 수동 설정 (외부에서 호출 가능)
        /// </summary>
        public void SetTargetCamera(UnityEngine.Camera camera)
        {
            if (camera != null)
            {
                _targetCamera = camera.transform;
            }
        }
    }
}
