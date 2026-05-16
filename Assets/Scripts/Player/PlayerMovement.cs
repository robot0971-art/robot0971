using System.Collections;
using System.Collections.Generic;
using SunnysideIsland.Events;
using UnityEngine;

namespace SunnysideIsland.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int AnimRoll = Animator.StringToHash("Roll");
        private static readonly int AnimFacingX = Animator.StringToHash("FacingX");
        private static readonly int AnimFacingY = Animator.StringToHash("FacingY");
        private static readonly int AnimSwimming = Animator.StringToHash("Swimming");
        private static readonly int AnimIsDead = Animator.StringToHash("IsDead");

        private readonly HashSet<int> _animatorParameterHashes = new();

        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private LayerMask _seaLayer;
        private LayerMask _groundLayer;
        private float _moveSpeed = 5f;
        private float _sprintSpeed = 8f;
        private float _rollSpeed = 10f;
        private float _rollDuration = 0.3f;
        private float _rollCooldown = 0.5f;
        private float _swimSpeed = 2.5f;
        private Vector2 _moveDirection;
        private Vector2 _facingDirection = Vector2.down;
        private bool _isSprinting;
        private bool _isRolling;
        private bool _isSwimming;
        private bool _isDead;
        private bool _canMove = true;
        private float _rollTimer;
        private float _rollCooldownTimer;

        public Vector2 MoveDirection => _moveDirection;
        public Vector2 FacingDirection => _facingDirection;
        public bool IsSprinting => _isSprinting;
        public bool IsRolling => _isRolling;
        public bool IsSwimming => _isSwimming;
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            CacheAnimatorParameters();
        }

        public void Configure(
            Rigidbody2D rb,
            Animator animator,
            SpriteRenderer spriteRenderer,
            LayerMask seaLayer,
            LayerMask groundLayer,
            float moveSpeed,
            float sprintSpeed,
            float rollSpeed,
            float rollDuration,
            float rollCooldown,
            float swimSpeed)
        {
            _rb = rb != null ? rb : GetComponent<Rigidbody2D>();
            _animator = animator != null ? animator : GetComponent<Animator>();
            _spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            _seaLayer = seaLayer;
            _groundLayer = groundLayer;
            _moveSpeed = moveSpeed;
            _sprintSpeed = sprintSpeed;
            _rollSpeed = rollSpeed;
            _rollDuration = rollDuration;
            _rollCooldown = rollCooldown;
            _swimSpeed = swimSpeed;
            CacheAnimatorParameters();
        }

        public void TickEnvironment()
        {
            const float checkRadius = 0.2f;
            bool isOverSea = Physics2D.OverlapCircle(transform.position, checkRadius, _seaLayer) != null;
            bool isOverGround = Physics2D.OverlapCircle(transform.position, checkRadius, _groundLayer) != null;
            bool shouldSwim = isOverSea && !isOverGround;

            if (shouldSwim != _isSwimming)
            {
                SetSwimming(shouldSwim);
            }
        }

        public void RefreshEnvironmentState()
        {
            TickEnvironment();
        }

        public void SetInput(Vector2 inputVector)
        {
            _moveDirection = inputVector.normalized;

            if (_moveDirection != Vector2.zero && !_isRolling)
            {
                SetFacingDirection(_moveDirection);
            }
        }

        public void SetSprinting(bool isSprinting)
        {
            _isSprinting = isSprinting && !_isRolling;
        }

        public bool TryRoll()
        {
            if (!CanRoll())
            {
                return false;
            }

            _isRolling = true;
            _rollTimer = _rollDuration;
            _rollCooldownTimer = _rollCooldown + _rollDuration;
            _rb.linearVelocity = _facingDirection * _rollSpeed;
            SetAnimatorTriggerIfExists(AnimRoll);
            return true;
        }

        public void TickTimers(float deltaTime)
        {
            if (_rollTimer > 0f)
            {
                _rollTimer -= deltaTime;
                if (_rollTimer <= 0f)
                {
                    _isRolling = false;
                    _rb.linearVelocity = Vector2.zero;
                }
            }

            if (_rollCooldownTimer > 0f)
            {
                _rollCooldownTimer -= deltaTime;
            }
        }

        public void FixedTick(bool isAttacking)
        {
            if (_isDead || !_canMove || _isRolling || isAttacking)
            {
                return;
            }

            float targetSpeed = _isSwimming ? _swimSpeed : (_isSprinting ? _sprintSpeed : _moveSpeed);
            Vector2 velocity = _moveDirection * targetSpeed;

            if (velocity.sqrMagnitude > 0.01f)
            {
                _rb.MovePosition(_rb.position + velocity * Time.fixedDeltaTime);
            }

            if (_moveDirection != Vector2.zero)
            {
                EventBus.Publish(new PlayerMovedEvent
                {
                    Position = transform.position,
                    Direction = _moveDirection,
                    IsSprinting = _isSprinting
                });
            }
        }

        public void UpdateAnimations()
        {
            if (_animator == null)
            {
                return;
            }

            Vector2 animVelocity = _isRolling ? _rb.linearVelocity.normalized : _moveDirection;
            SetAnimatorFloatIfExists(AnimMoveX, animVelocity.x);
            SetAnimatorFloatIfExists(AnimMoveY, animVelocity.y);
            SetAnimatorFloatIfExists(AnimFacingX, _facingDirection.x);
            SetAnimatorFloatIfExists(AnimFacingY, _facingDirection.y);

            if (_moveDirection.x != 0f && _spriteRenderer != null)
            {
                _spriteRenderer.flipX = _moveDirection.x < 0f;
            }

            bool isMoving = !_isSwimming && animVelocity.sqrMagnitude > 0.01f;
            SetAnimatorBoolIfExists(AnimIsMoving, isMoving);
            SetAnimatorBoolIfExists(AnimIsSprinting, _isSprinting && !_isSwimming);
            SetAnimatorBoolIfExists(AnimIsDead, _isDead);
        }

        public void Stop()
        {
            _moveDirection = Vector2.zero;
            _isSprinting = false;
            _rb.linearVelocity = Vector2.zero;
        }

        public void StopRoll()
        {
            _isRolling = false;
            _rollTimer = 0f;
            _rb.linearVelocity = Vector2.zero;
        }

        public void SetDead(bool isDead)
        {
            _isDead = isDead;
            if (isDead)
            {
                _canMove = false;
                _isSprinting = false;
                _isRolling = false;
                Stop();
            }

            SetAnimatorBoolIfExists(AnimIsDead, isDead);
        }

        public void SetFacingDirection(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                return;
            }

            _facingDirection = direction.normalized;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = _facingDirection.x < 0f;
            }
        }

        public void SetSwimming(bool enable)
        {
            _isSwimming = enable;
            SetAnimatorBoolIfExists(AnimSwimming, enable);
        }

        public void Pause(float duration)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PauseRoutine(duration));
            }
        }

        public float GetMoveSpeed()
        {
            return _moveSpeed;
        }

        private bool CanRoll()
        {
            return _rollCooldownTimer <= 0f && _facingDirection != Vector2.zero && !_isSwimming;
        }

        private IEnumerator PauseRoutine(float duration)
        {
            _canMove = false;
            Stop();

            yield return new WaitForSeconds(duration);

            _canMove = true;
        }

        private void CacheAnimatorParameters()
        {
            _animatorParameterHashes.Clear();

            if (_animator == null)
            {
                return;
            }

            foreach (var parameter in _animator.parameters)
            {
                _animatorParameterHashes.Add(parameter.nameHash);
            }
        }

        private void SetAnimatorFloatIfExists(int parameterHash, float value)
        {
            if (_animator != null && _animatorParameterHashes.Contains(parameterHash))
            {
                _animator.SetFloat(parameterHash, value);
            }
        }

        private void SetAnimatorBoolIfExists(int parameterHash, bool value)
        {
            if (_animator != null && _animatorParameterHashes.Contains(parameterHash))
            {
                _animator.SetBool(parameterHash, value);
            }
        }

        private void SetAnimatorTriggerIfExists(int parameterHash)
        {
            if (_animator != null && _animatorParameterHashes.Contains(parameterHash))
            {
                _animator.SetTrigger(parameterHash);
            }
        }
    }
}
