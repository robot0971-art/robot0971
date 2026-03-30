using DI;
using SunnysideIsland.Building;
using SunnysideIsland.Core;
using SunnysideIsland.Environment;
using SunnysideIsland.Events;
using SunnysideIsland.Farming;
using SunnysideIsland.GameData;
using SunnysideIsland.Items;
using SunnysideIsland.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SunnysideIsland.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour, ISaveable
    {
        [Header("=== Movement Settings ===")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 8f;
        [SerializeField] private float _rollSpeed = 10f;
        [SerializeField] private float _rollDuration = 0.3f;
        [SerializeField] private float _rollCooldown = 0.5f;

        [Header("=== Interaction Settings ===")]
        [SerializeField] private float _interactionRadius = 0.15f;
        [SerializeField] private LayerMask _interactableLayer;
        [SerializeField] private LayerMask _farmingLayer;
        [SerializeField] private LayerMask _treeLayer;
        [SerializeField] private LayerMask _harvestLayer;

        [Header("=== Components ===")]
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private UnityEngine.AI.NavMeshAgent _navMeshAgent;

        [Header("=== Swim Settings ===")]
        [SerializeField] private LayerMask _seaLayer;
        [SerializeField] private float _swimSpeed = 2.5f;
        private bool _isSwimming;
        private static readonly int AnimSwimming = Animator.StringToHash("Swimming");

        [Header("=== Input ===")]
        [SerializeField] private InputActionAsset _inputActions;
        private InputAction _moveAction;

        [Inject] private IUIManager _uiManager;
        [Inject(Optional = true)] private BuildingSystem _buildingSystem;

        private Vector2 _moveDirection;
        private Vector2 _facingDirection = Vector2.down;
        private bool _isSprinting;
        private bool _isRolling;
        private bool _isAttacking;
        private bool _isHurt;
        private bool _isDead;
        private float _rollTimer;
        private float _rollCooldownTimer;
        private bool _canMove = true;

        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int AnimRoll = Animator.StringToHash("Roll");
        private static readonly int AnimFacingX = Animator.StringToHash("FacingX");
        private static readonly int AnimFacingY = Animator.StringToHash("FacingY");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimHurt = Animator.StringToHash("Hurt");
        private static readonly int AnimDie = Animator.StringToHash("Die");
        private static readonly int AnimIsDead = Animator.StringToHash("IsDead");
        private static readonly int AnimWater = Animator.StringToHash("Water");
        private static readonly int AnimHammer = Animator.StringToHash("Hammer");

        private bool _isBuilding;
        private Vector3 _buildTargetPosition;
        private DetailedBuildingData _pendingBuildingData;
        private Vector3Int _pendingGridPosition;
        private int _hammerCount;
        private System.Action _onBuildComplete;
        private Coroutine _buildCoroutine;

        [SerializeField] private GameObject _plotPrefab;

        [SerializeField] private LayerMask _groundLayer;

        [SerializeField] private CropData _potatoData;
        public string SaveKey => "Player";

        private void Awake()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            _rb.gravityScale = 0;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (_buildingSystem == null)
            {
                _buildingSystem = FindObjectOfType<SunnysideIsland.Building.BuildingSystem>();
            }

            if (_inputActions != null)
            {
                var actionMap = _inputActions.FindActionMap("Player");

                if (actionMap != null)
                {
                    _moveAction = actionMap.FindAction("Move");
                }
                _moveAction?.Enable();
            }
        }

        private void OnDestroy()
        {
            _moveAction?.Disable();
            EventBus.Unsubscribe<BuildingPlaceRequestedEvent>(OnBuildingPlaceRequested);
        }

        private void Start()
        {
            DIContainer.Inject(this);


            if (_navMeshAgent != null)
            {
                _navMeshAgent.updateRotation = false;
                _navMeshAgent.updateUpAxis = false;
            }


            if (_buildingSystem == null)
            {
                _buildingSystem = FindObjectOfType<SunnysideIsland.Building.BuildingSystem>();
            }

            EventBus.Subscribe<BuildingPlaceRequestedEvent>(OnBuildingPlaceRequested);
        }

        private void Update()
        {
            CheckCancelAutoMove();

            if (!_canMove) return;

            // Poll for swimming since OnTriggerEnter might fail due to collision matrix or tilemap settings
            // OverlapCircle checks a small area around the player for more stability than OverlapPoint

            float checkRadius = 0.2f;
            bool isOverSea = Physics2D.OverlapCircle(transform.position, checkRadius, _seaLayer) != null;
            bool isOverGround = Physics2D.OverlapCircle(transform.position, checkRadius, _groundLayer) != null;

            // Determine if swimming: must be over sea, and not standing on solid ground
            bool shouldSwim = isOverSea && !isOverGround;

            if (shouldSwim && !_isSwimming) SetSwimming(true);
            else if (!shouldSwim && _isSwimming) SetSwimming(false);

            HandleInput();
            HandleTimers();
            UpdateAnimations();
        }

        private void CheckCancelAutoMove()
        {
            if (_isBuilding)
            {
                Vector2 inputVector = Vector2.zero;
                if (_moveAction != null)
                {
                    inputVector = _moveAction.ReadValue<Vector2>();
                }
                else
                {
                    inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                }

                if (inputVector.sqrMagnitude > 0.01f)
                {
                    CancelBuilding();
                }
            }
        }

        private void CancelBuilding()
        {
            if (_isBuilding)
            {
                if (_buildCoroutine != null)
                {
                    StopCoroutine(_buildCoroutine);
                    _buildCoroutine = null;
                }
                _isBuilding = false;
                _canMove = true;
                _pendingBuildingData = null;


                if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.ResetPath();
                }


                _rb.linearVelocity = Vector2.zero;
                _moveDirection = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            if (!_canMove) return;
            Move();
        }

        private void HandleInput()
        {
            Vector2 inputVector = Vector2.zero;

            if (_moveAction != null)
            {
                inputVector = _moveAction.ReadValue<Vector2>();
            }
            else
            {
                inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            _moveDirection = inputVector.normalized;

            if (_moveDirection != Vector2.zero && !_isRolling)
                _facingDirection = _moveDirection;

            _isSprinting = Input.GetKey(KeyCode.LeftShift) && !_isRolling;

            // 수영 중에는 구르기 금지
            if (Input.GetKeyDown(KeyCode.Space) && CanRoll() && !_isSwimming)
                Roll();

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (_uiManager != null)
                {
                    var inventoryPanel = _uiManager.GetPanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                    if (inventoryPanel != null)
                    {
                        if (inventoryPanel.IsOpen)
                            _uiManager.ClosePanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                        else
                            _uiManager.OpenPanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (_buildingSystem != null)
                {
                    _buildingSystem.ToggleBuildMode();
                }
            }

            // 통합 상호작용 (E키) - 수영 중이 아닐 때만 실행
            if (Input.GetKeyDown(KeyCode.E) && !_isSwimming)
            {
                bool workDone = TryInteract();

                if (!workDone)
                {
                    workDone = ExecuteWateringWithResult();
                    if (!workDone)
                    {
                        TryCreatePlot();
                    }
                }
            }
        }

        private void Move()
        {
            if (_isRolling) return;

            float targetSpeed = _moveSpeed;
            if (_isSwimming) targetSpeed = _swimSpeed;
            else if (_isSprinting) targetSpeed = _sprintSpeed;

            Vector2 moveDir = _moveDirection;

            Vector2 velocity = moveDir * targetSpeed;


            if (velocity.sqrMagnitude > 0.01f)
            {
                _rb.MovePosition(_rb.position + velocity * Time.fixedDeltaTime);
            }

            if (_moveDirection != Vector2.zero)
            {
                EventBus.Publish(new PlayerMovedEvent { Position = transform.position, Direction = _moveDirection, IsSprinting = _isSprinting });
            }
        }

        // --- Swim Logic ---
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (((1 << collision.gameObject.layer) & _seaLayer) != 0)
            {
                SetSwimming(true);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (((1 << collision.gameObject.layer) & _seaLayer) != 0)
            {
                SetSwimming(false);
            }
        }

        private void SetSwimming(bool enable)
        {
            _isSwimming = enable;
            if (_animator != null)
                _animator.SetBool(AnimSwimming, enable);
        }

        // --- Interaction Logic (기존 로직 유지) ---
        private void TryCreatePlot()
        {
            // 플레이어가 바라보는 방향 앞에 Plot 생성
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.1f);
            Vector2 targetPos = origin + _facingDirection * 1.0f;

            Collider2D treeCheck = Physics2D.OverlapCircle(targetPos, 0.5f, _treeLayer);
            Collider2D harvestCheck = Physics2D.OverlapCircle(targetPos, 0.5f, _harvestLayer);
            if (treeCheck != null || harvestCheck != null) return;

            Collider2D groundCheck = Physics2D.OverlapPoint(targetPos, _groundLayer);

            if (groundCheck != null)
            {
                Collider2D overlap = Physics2D.OverlapCircle(targetPos, 0.2f, _interactableLayer);
                if (overlap == null)
                {
                    _animator.SetTrigger("Dig");
                    StartCoroutine(DelayedCreatePlot(targetPos, 0.4f));
                }
            }
        }

        private bool ExecuteWateringWithResult()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.2f);
            RaycastHit2D hit = Physics2D.Raycast(origin, _facingDirection, 0.7f, _farmingLayer);

            if (hit.collider != null && hit.collider.TryGetComponent(out FarmPlot plot))
            {
                ExecuteWatering();
                return true;
            }
            return false;
        }

        private bool TryInteract()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.1f);
            float radius = 0.8f;
            float distance = 1.5f;

            RaycastHit2D[] harvestHits = Physics2D.CircleCastAll(origin, radius, _facingDirection, distance, _harvestLayer);
            if (harvestHits.Length > 0)
            {
                if (harvestHits[0].collider.TryGetComponent(out PickableItem item))
                {
                    item.PickUp();
                    _animator.SetTrigger("Harvest");
                    return true;
                }
            }

            RaycastHit2D[] treeHits = Physics2D.CircleCastAll(origin, radius, _facingDirection, distance, _treeLayer);
            if (treeHits.Length > 0)
            {
                if (treeHits[0].collider.TryGetComponent(out SunnysideIsland.Environment.Tree tree))
                {
                    if (!tree.IsChopped)
                    {
                        tree.Chop();
                        _animator.SetTrigger("Axe");
                        return true;
                    }
                }
            }

            RaycastHit2D hit = Physics2D.Raycast(origin, _facingDirection, 1.0f, _interactableLayer);
            if (hit.collider != null && hit.collider.TryGetComponent(out FarmPlot plot))
            {
                if (plot.IsEmpty)
                {
                    plot.Plant("potato", _potatoData);
                }
                else
                {
                    if (plot.IsReady)
                    {
                        plot.Harvest();
                        _animator.SetTrigger("Harvest");
                    }
                    else
                    {
                        ExecuteWatering();
                    }
                }
                return true;
            }
            return false;
        }

        private void ExecuteWatering()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.2f);
            RaycastHit2D hit = Physics2D.Raycast(origin, _facingDirection, 0.7f, _farmingLayer);

            if (hit.collider != null)
            {
                _animator.SetTrigger(AnimWater);
                if (hit.collider.TryGetComponent(out FarmPlot plot))
                {
                    StartCoroutine(DelayedWatering(plot, 0.5f));
                }
            }
        }

        private System.Collections.IEnumerator DelayedWatering(FarmPlot plot, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (plot != null) plot.Water();
        }

        private System.Collections.IEnumerator DelayedCreatePlot(Vector2 spawnPos, float delay)
        {
            for (int i = 0; i < 5; i++)
            {
                _animator.ResetTrigger("Dig");
                _animator.SetTrigger("Dig");
                yield return new WaitForSeconds(delay);
            }

            if (_plotPrefab != null)
            {
                GameObject newPlot = Instantiate(_plotPrefab, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
                if (newPlot.TryGetComponent(out SunnysideIsland.Farming.FarmPlot plotScript))
                {
                    var manager = FindObjectOfType<SunnysideIsland.Farming.FarmingManager>();
                    if (manager != null) manager.RegisterPlot(plotScript);
                }
            }
        }

        private void Roll()
        {
            _isRolling = true;
            _rollTimer = _rollDuration;
            _rollCooldownTimer = _rollCooldown + _rollDuration;
            _rb.linearVelocity = _facingDirection * _rollSpeed;
            _animator.SetTrigger(AnimRoll);
        }

        private bool CanRoll() => _rollCooldownTimer <= 0 && _facingDirection != Vector2.zero;

        private void HandleTimers()
        {
            if (_rollTimer > 0)
            {
                _rollTimer -= Time.deltaTime;
                if (_rollTimer <= 0)
                {
                    _isRolling = false;
                    _rb.linearVelocity = Vector2.zero;
                }
            }
            if (_rollCooldownTimer > 0) _rollCooldownTimer -= Time.deltaTime;
        }

        private void UpdateAnimations()
        {
            // _rb.MovePosition()으로 이동할 경우 _rb.linearVelocity가 0으로 남으므로 _moveDirection 기반으로 변경
            Vector2 animVelocity = _isRolling ? _rb.linearVelocity.normalized : _moveDirection;

            _animator.SetFloat(AnimMoveX, animVelocity.x);
            _animator.SetFloat(AnimMoveY, animVelocity.y);
            _animator.SetFloat(AnimFacingX, _facingDirection.x);
            _animator.SetFloat(AnimFacingY, _facingDirection.y);

            if (_moveDirection.x != 0)
                _spriteRenderer.flipX = _moveDirection.x < 0;

            // 수영 중에는 Walking 애니메이션을 재생하지 않음
            bool isMoving = !_isSwimming && animVelocity.sqrMagnitude > 0.01f;
            _animator.SetBool(AnimIsMoving, isMoving);
            _animator.SetBool(AnimIsSprinting, _isSprinting && !_isSwimming);
            _animator.SetBool(AnimIsDead, _isDead);
        }

        public object GetSaveData()
        {
            return new PlayerSaveData { Position = transform.position, FacingDirectionX = _facingDirection.x, FacingDirectionY = _facingDirection.y };
        }

        public void LoadSaveData(object data)
        {
            if (data is PlayerSaveData saveData)
            {
                transform.position = saveData.Position;
                _facingDirection = new Vector2(saveData.FacingDirectionX, saveData.FacingDirectionY);
            }
        }

        [System.Serializable]
        public class PlayerSaveData { public Vector3 Position; public float FacingDirectionX; public float FacingDirectionY; }

        private void OnBuildingPlaceRequested(BuildingPlaceRequestedEvent evt)
        {
            if (_isBuilding) return;

            _isBuilding = true;
            _canMove = false;
            _pendingBuildingData = _buildingSystem?.GetBuildingData(evt.BuildingId);
            _pendingGridPosition = evt.GridPosition;
            _hammerCount = 0;

            float width = _pendingBuildingData != null ? _pendingBuildingData.Size.Width : 1f;
            float height = _pendingBuildingData != null ? _pendingBuildingData.Size.Height : 1f;

            // 건물의 대략적인 중심(타일맵은 1x1)
            Vector3 buildingCenter = new Vector3(evt.GridPosition.x + width * 0.5f, evt.GridPosition.y + height * 0.5f, 0);

            Vector3 dirToCenter = (buildingCenter - transform.position).normalized;
            if (dirToCenter == Vector3.zero) dirToCenter = Vector3.down; // Fallback

            // 중심에서 직사각형 가장자리까지의 정확한 거리를 계산합니다.
            float rayDistX = (dirToCenter.x != 0) ? (width * 0.5f) / Mathf.Abs(dirToCenter.x) : float.MaxValue;
            float rayDistY = (dirToCenter.y != 0) ? (height * 0.5f) / Mathf.Abs(dirToCenter.y) : float.MaxValue;
            float boxRadius = Mathf.Min(rayDistX, rayDistY);

            //경계 경계에 정확히 서도록 여백을 추가합니다.
            float safeRadius = boxRadius + 0.3f;

            // 스탠드 위치는 건물에서 약간 바깥쪽, 플레이어가 접근하는 쪽입니다.
            _buildTargetPosition = buildingCenter - dirToCenter * safeRadius;

            _facingDirection = dirToCenter;
            _spriteRenderer.flipX = _facingDirection.x < 0;

            bool navMeshReady = _navMeshAgent != null && _navMeshAgent.isOnNavMesh;
            if (navMeshReady)
            {
                _navMeshAgent.SetDestination(_buildTargetPosition);
            }

            if (_buildCoroutine != null) StopCoroutine(_buildCoroutine);
            _buildCoroutine = StartCoroutine(MoveToAndBuildRoutine(navMeshReady));
        }

        private System.Collections.IEnumerator MoveToAndBuildRoutine(bool useNavMesh)
        {
            while (useNavMesh ? (_navMeshAgent.pathPending || _navMeshAgent.remainingDistance > 0.1f) : Vector3.Distance(transform.position, _buildTargetPosition) > 0.1f)
            {
                Vector3 direction = (_buildTargetPosition - transform.position).normalized;


                if (!useNavMesh)
                {
                    _rb.linearVelocity = direction * _moveSpeed;
                }
                // 이동 중에도 클릭 방향을 유지 (flip은 OnBuildingPlaceRequested에서 이미 설정됨)

                yield return null;
            }

            _moveDirection = Vector2.zero;

            if (useNavMesh)
                _navMeshAgent.ResetPath();
            else
                _rb.linearVelocity = Vector2.zero;

            for (int i = 0; i < 4; i++)
            {
                _animator.SetTrigger(AnimHammer);
                _hammerCount++;

                yield return new WaitForSeconds(0.5f);

                while (_animator.GetCurrentAnimatorStateInfo(0).IsName("hamering"))
                {
                    yield return null;
                }
            }

            if (_pendingBuildingData != null)
            {
                EventBus.Publish(new BuildingPlaceConfirmEvent
                {
                    BuildingId = _pendingBuildingData.BuildingId,
                    GridPosition = _pendingGridPosition
                });
            }

            _isBuilding = false;
            _canMove = true;
            _pendingBuildingData = null;
            _buildCoroutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }
    }
}