using UnityEngine;

namespace SunnysideIsland.Animal
{
    public class RabbitAI : AnimalAI
    {
        [Header("=== Rabbit Settings ===")]
        [SerializeField] private float _hideChance = 0.3f;
        [SerializeField] private float _hideDuration = 2f;

        private float _hideTimer;
        private bool _isHiding;

        protected override void UpdateState()
        {
            if (_isHiding)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0f)
                {
                    _isHiding = false;
                    ChangeState(AnimalState.Flee);
                }
                return;
            }

            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    break;
                case AnimalState.Flee:
                    UpdateFlee();
                    break;
            }
        }

        protected override void OnPlayerDetected()
        {
            if (Random.value < _hideChance)
            {
                _isHiding = true;
                _hideTimer = _hideDuration;
                return;
            }

            _moveSpeed = GetSpeedFromData() * 1.2f;
            ChangeState(AnimalState.Flee);
        }
    }

    public class DeerAI : AnimalAI
    {
        [Header("=== Deer Settings ===")]
        [SerializeField] private float _groupRadius = 5f;
        [SerializeField] private int _maxHerdSize = 5;

        protected override void UpdateState()
        {
            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    break;
                case AnimalState.Flee:
                    UpdateFlee();
                    AlertHerd();
                    break;
            }
        }

        private void AlertHerd()
        {
            Collider[] nearbyDeer = Physics.OverlapSphere(transform.position, _groupRadius);
            foreach (var collider in nearbyDeer)
            {
                var deer = collider.GetComponent<DeerAI>();
                if (deer != null && deer != this && deer._currentState != AnimalState.Flee)
                {
                    deer._target = _target;
                    deer.ChangeState(AnimalState.Flee);
                }
            }
        }

        protected override void OnPlayerDetected()
        {
            _moveSpeed = GetSpeedFromData() * 1.3f;
            ChangeState(AnimalState.Flee);
        }
    }

    public class FoxAI : AnimalAI
    {
        [Header("=== Fox Settings ===")]
        [SerializeField] private float _zigzagInterval = 0.5f;
        [SerializeField] private float _zigzagAngle = 45f;

        private float _zigzagTimer;
        private bool _zigzagRight;

        protected override void UpdateState()
        {
            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    break;
                case AnimalState.Flee:
                    UpdateFleeZigzag();
                    break;
            }
        }

        private void UpdateFleeZigzag()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);
            if (distance > _fleeRange)
            {
                _target = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            _zigzagTimer -= Time.deltaTime;
            if (_zigzagTimer <= 0f)
            {
                _zigzagRight = !_zigzagRight;
                _zigzagTimer = _zigzagInterval;
            }

            Vector3 fleeDirection = (transform.position - _target.position).normalized;
            fleeDirection.y = 0f;

            float angle = _zigzagRight ? _zigzagAngle : -_zigzagAngle;
            fleeDirection = Quaternion.Euler(0, angle, 0) * fleeDirection;

            transform.position += fleeDirection * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fleeDirection), Time.deltaTime * 5f);
        }

        protected override void OnPlayerDetected()
        {
            if (Random.value > 0.5f)
                ChangeState(AnimalState.Flee);
            else
                ChangeState(AnimalState.Idle);
        }
    }

    public class BoarAI : AnimalAI
    {
        [Header("=== Boar Settings ===")]
        [SerializeField] private float _chargeSpeed = 8f;
        [SerializeField] private float _chargeDistance = 5f;
        [SerializeField] private float _stunDuration = 1f;

        private bool _isCharging;
        private Vector3 _chargeTarget;
        private float _stunTimer;

        protected override void UpdateState()
        {
            if (_stunTimer > 0)
            {
                _stunTimer -= Time.deltaTime;
                return;
            }

            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    break;
                case AnimalState.Chase:
                    UpdateChase();
                    break;
                case AnimalState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        protected override void UpdateChase()
        {
            if (_isCharging)
            {
                Vector3 direction = (_chargeTarget - transform.position).normalized;
                direction.y = 0f;

                transform.position += direction * _chargeSpeed * Time.deltaTime;

                if (Vector3.Distance(transform.position, _chargeTarget) < 0.5f)
                {
                    _isCharging = false;
                    _stunTimer = _stunDuration;
                }
                return;
            }

            base.UpdateChase();
        }

        protected override void PerformAttack()
        {
            _isCharging = true;
            _chargeTarget = _target.position + (_target.position - transform.position).normalized * _chargeDistance;
            base.PerformAttack();
        }

        protected override void OnPlayerDetected()
        {
            ChangeState(AnimalState.Chase);
        }
    }

    public class WolfAI : AnimalAI
    {
        [Header("=== Wolf Settings ===")]
        [SerializeField] private float _packRadius = 10f;
        [SerializeField] private float _circleDistance = 3f;

        private bool _isCircling;

        protected override void UpdateState()
        {
            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    break;
                case AnimalState.Chase:
                    UpdateChase();
                    break;
                case AnimalState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        protected override void UpdateChase()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);

            if (distance > _detectionRange * 2f)
            {
                _target = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            if (distance <= 1.5f)
            {
                ChangeState(AnimalState.Attack);
                return;
            }

            AlertPack();

            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0f;

            transform.position += direction * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        private void AlertPack()
        {
            Collider[] nearbyWolves = Physics.OverlapSphere(transform.position, _packRadius);
            foreach (var collider in nearbyWolves)
            {
                var wolf = collider.GetComponent<WolfAI>();
                if (wolf != null && wolf != this && wolf._currentState == AnimalState.Idle)
                {
                    wolf._target = _target;
                    wolf.ChangeState(AnimalState.Chase);
                }
            }
        }

        protected override void OnPlayerDetected()
        {
            ChangeState(AnimalState.Chase);
        }
    }

    public class BearAI : AnimalAI
    {
        [Header("=== Bear Settings ===")]
        [SerializeField] private float _territoryRadius = 8f;
        [SerializeField] private float _roarRange = 6f;
        [SerializeField] private float _swipeCooldown = 2f;

        private Vector3 _territoryCenter;
        private float _lastRoarTime;

        protected override void Start()
        {
            base.Start();
            _territoryCenter = transform.position;
        }

        protected override void UpdateState()
        {
            switch (_currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    CheckTerritory();
                    break;
                case AnimalState.Wander:
                    UpdateWander();
                    CheckTerritory();
                    break;
                case AnimalState.Chase:
                    UpdateTerritorialChase();
                    break;
                case AnimalState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void CheckTerritory()
        {
            float distFromTerritory = Vector3.Distance(transform.position, _territoryCenter);
            if (distFromTerritory > _territoryRadius)
            {
                Vector3 direction = (_territoryCenter - transform.position).normalized;
                transform.position += direction * _moveSpeed * 0.5f * Time.deltaTime;
            }
        }

        private void UpdateTerritorialChase()
        {
            if (_target == null)
            {
                ChangeState(AnimalState.Idle);
                return;
            }

            float distanceFromTerritory = Vector3.Distance(_target.position, _territoryCenter);
            if (distanceFromTerritory > _territoryRadius * 1.5f)
            {
                _target = null;
                ChangeState(AnimalState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.position);

            if (distance <= 1.5f)
            {
                ChangeState(AnimalState.Attack);
                return;
            }

            if (Time.time >= _lastRoarTime + 5f && distance < _roarRange)
            {
                Roar();
            }

            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0f;

            transform.position += direction * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        private void Roar()
        {
            _lastRoarTime = Time.time;
            Debug.Log("[BearAI] ROAR!");
        }

        protected override void OnPlayerDetected()
        {
            float distanceFromTerritory = Vector3.Distance(_target.position, _territoryCenter);
            if (distanceFromTerritory <= _territoryRadius)
            {
                ChangeState(AnimalState.Chase);
            }
        }
    }
}