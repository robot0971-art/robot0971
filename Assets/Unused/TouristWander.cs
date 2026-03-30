using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.NPC
{
    /// <summary>
    /// 관광객 배회 컴포넌트
    /// </summary>
    public class TouristWander : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _waitTimeMin = 2f;
        [SerializeField] private float _waitTimeMax = 5f;
        
        private List<Transform> _wanderPoints = new List<Transform>();
        private int _currentPointIndex = 0;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;
        
        public void Initialize(List<Transform> wanderPoints)
        {
            _wanderPoints = wanderPoints;
            if (_wanderPoints.Count > 0)
            {
                _currentPointIndex = Random.Range(0, _wanderPoints.Count);
            }
        }
        
        private void Update()
        {
            if (_wanderPoints == null || _wanderPoints.Count == 0)
                return;
            
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    _currentPointIndex = Random.Range(0, _wanderPoints.Count);
                }
                return;
            }
            
            Transform target = _wanderPoints[_currentPointIndex];
            if (target == null) return;
            
            Vector3 direction = (target.position - transform.position);
            direction.y = 0f;
            
            if (direction.magnitude < 0.5f)
            {
                _isWaiting = true;
                _waitTimer = Random.Range(_waitTimeMin, _waitTimeMax);
                return;
            }
            
            transform.position += direction.normalized * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
    }
}