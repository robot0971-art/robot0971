using UnityEngine;

namespace SunnysideIsland.Animal
{
    public class CowAI : AnimalBaseAI
    {
        protected override void Awake()
        {
            base.Awake();
            // 소 특성 설정
            _wanderRadius = 20f;
            _wanderInterval = 4f;
            _moveSpeed = 1.5f;
            _idleTime = 3f;
            _fleeRange = 1.25f;
            _fleeSpeed = 3f;
        }
        
        protected override void Start()
        {
            base.Start();
            // Grass 레이어 설정
            if (_groundLayer == 0)
                _groundLayer = LayerMask.GetMask("Grass", "Ground");
        }
    }
}
