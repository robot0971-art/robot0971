using UnityEngine;

namespace SunnysideIsland.Animal
{
    public class PigAI : AnimalBaseAI
    {
        protected override void Awake()
        {
            base.Awake();

            if (GetComponent<PigHuntable>() == null)
            {
                gameObject.AddComponent<PigHuntable>();
            }

            _wanderRadius = 15f;
            _wanderInterval = 2.5f;
            _moveSpeed = 2.5f;
            _idleTime = 1.5f;
            _fleeRange = 3f;
            _fleeSpeed = 4f;
        }

        protected override void Start()
        {
            base.Start();

            if (_groundLayer == 0)
            {
                _groundLayer = LayerMask.GetMask("Grass", "Ground");
            }
        }
    }
}
