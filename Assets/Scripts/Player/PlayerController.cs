using DI;
using SunnysideIsland.Building;
using SunnysideIsland.Core;
using SunnysideIsland.Environment;
using SunnysideIsland.Events;
using SunnysideIsland.Farming;
using SunnysideIsland.GameData;
using SunnysideIsland.Items;
using SunnysideIsland.UI;
using SunnysideIsland.Pool;
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

        [Header("=== Attack Settings ===")]
        [SerializeField] private KeyCode _attackKey = KeyCode.F;
        [SerializeField] private float _attackRange = 1.1f;
        [SerializeField] private float _attackRadius = 0.45f;
        [SerializeField] private float _attackCooldown = 0.45f;
        [SerializeField] private float _attackHitDelay = 0.18f;
        [SerializeField] private float _attackRecoverTime = 0.12f;

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
        public bool IsSwimming => _isSwimming;
        public bool CanMove { get => _canMove; set => _canMove = value; }

        [Header("=== Input ===")]
        [SerializeField] private InputActionAsset _inputActions;
        private InputAction _moveAction;

        [Inject(Optional = true)] private IUIManager _uiManager;
        [Inject(Optional = true)] private BuildingSystem _buildingSystem;
        [Inject(Optional = true)] private ICropSelectionSystem _cropSelectionSystem;
        [Inject(Optional = true)] private FarmingManager _farmingManager;
        [Inject(Optional = true)] private Grid _grid;

        private IUIManager UIManager
        {
            get
            {
                if (_uiManager == null)
                {
                    DIContainer.TryResolve(out _uiManager);
                }
                return _uiManager;
            }
        }

        private Vector2 _moveDirection;
        private Vector2 _facingDirection = Vector2.down;
        private bool _isSprinting;
        private bool _isRolling;
        private bool _isAttacking;
        private bool _isHurt;
        private bool _isDead;
        private float _rollTimer;
        private float _rollCooldownTimer;
        private float _attackCooldownTimer;
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
        public string SaveKey => "Player";

        private void Awake()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            if (GetComponent<SeaDiscoveryTracker>() == null)
            {
                gameObject.AddComponent<SeaDiscoveryTracker>();
            }

            _rb.gravityScale = 0;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

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
            // DI 주입 실행
            DIContainer.Inject(this);

            if (_navMeshAgent != null)
            {
                _navMeshAgent.updateRotation = false;
                _navMeshAgent.updateUpAxis = false;
            }

            EventBus.Subscribe<BuildingPlaceRequestedEvent>(OnBuildingPlaceRequested);
        }

        private void Update()
        {
            CheckCancelAutoMove();

            if (!_canMove) return;

            float checkRadius = 0.2f;
            bool isOverSea = Physics2D.OverlapCircle(transform.position, checkRadius, _seaLayer) != null;
            bool isOverGround = Physics2D.OverlapCircle(transform.position, checkRadius, _groundLayer) != null;

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

        public void PauseMovement(float duration)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PauseMovementRoutine(duration));
            }
        }

        private System.Collections.IEnumerator PauseMovementRoutine(float duration)
        {
            _canMove = false;
            _rb.linearVelocity = Vector2.zero;

            // 획득 애니메이션이 있다면 여기서 트리거 (예: "PickUp")
            // _animator.SetTrigger("PickUp");

            yield return new WaitForSeconds(duration);

            _canMove = true;
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

            if (Input.GetKeyDown(KeyCode.Space) && CanRoll() && !_isSwimming)
                Roll();

            if (Input.GetKeyDown(_attackKey) && CanAttack())
            {
                StartCoroutine(AttackRoutine());
                return;
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log($"[PlayerController] I key pressed. UIManager resolved: {UIManager != null}");
                var uiMgr = UIManager;
                if (uiMgr != null)
                {
                    var inventoryPanel = uiMgr.GetPanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                    Debug.Log($"[PlayerController] InventoryPanel is null: {inventoryPanel == null}");
                    if (inventoryPanel != null)
                    {
                        Debug.Log($"[PlayerController] InventoryPanel.IsOpen: {inventoryPanel.IsOpen}");
                        if (inventoryPanel.IsOpen)
                            uiMgr.ClosePanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                        else
                            uiMgr.OpenPanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                    }
                    else
                    {
                        Debug.LogError("[PlayerController] InventoryPanel not found in UIManager!");
                    }
                }
                else
                {
                    Debug.LogError("[PlayerController] UIManager is null! DI resolution failed.");
                }
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (_buildingSystem != null)
                {
                    _buildingSystem.ToggleBuildMode();
                }
            }

            if (Input.GetKeyDown(KeyCode.E)) // 수영 중에도 아이템을 주울 수 있도록 !_isSwimming 제거
            {
                bool workDone = TryInteract();

                if (!workDone && !_isSwimming) // 땅에서만 가능한 행동들 (물주기, 구멍파기)
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
            if (_isRolling || _isAttacking) return;

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

        private void TryCreatePlot()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0, 0.1f);
            Vector2 rawTargetPos = origin + _facingDirection * 1.0f;
            Vector2 targetPos = rawTargetPos;

            // Grid Snapping
            if (_grid != null)
            {
                Vector3Int cellPos = _grid.WorldToCell(rawTargetPos);
                targetPos = _grid.GetCellCenterWorld(cellPos);
            }

            Collider2D treeCheck = Physics2D.OverlapCircle(targetPos, 0.4f, _treeLayer);
            Collider2D harvestCheck = Physics2D.OverlapCircle(targetPos, 0.4f, _harvestLayer);
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

            Collider2D[] nearbyInteractables = Physics2D.OverlapCircleAll(origin, 2.0f, _interactableLayer);
            Campfire nearestCampfire = null;
            foreach (var col in nearbyInteractables)
            {
                if (col.TryGetComponent(out Campfire cf))
                {
                    nearestCampfire = cf;
                    break;
                }
            }
            bool isNearCampfire = nearestCampfire != null;

            RaycastHit2D hit = Physics2D.CircleCast(origin, 0.4f, _facingDirection, 2.0f, _interactableLayer);
            IInteractable targetInteractable = null;

            if (hit.collider != null)
            {
                hit.collider.TryGetComponent(out targetInteractable);
                if (targetInteractable == null)
                {
                    targetInteractable = hit.collider.GetComponentInParent<IInteractable>();
                }
            }
            else if (isNearCampfire)
            {
                targetInteractable = nearestCampfire;
            }

            if (targetInteractable != null)
            {
                if (targetInteractable.CanInteract())
                {
                    targetInteractable.Interact();
                    return true;
                }
                if (isNearCampfire) return true;
            }

            if (isNearCampfire) return true;

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
            
            RaycastHit2D farmHit = Physics2D.Raycast(origin, _facingDirection, 1.0f, _interactableLayer);
            if (farmHit.collider != null && farmHit.collider.TryGetComponent(out FarmPlot plot))
            {
                if (plot.IsEmpty)
                {
                    var selectedCrop = _cropSelectionSystem?.SelectedCrop;
                    if (selectedCrop != null)
                    {
                        if (_cropSelectionSystem.TryConsume(_cropSelectionSystem.SelectedIndex, 1))
                        {
                            plot.Plant(selectedCrop.seedItemId, selectedCrop);
                        }
                        else
                        {
                            Debug.LogWarning($"[PlayerController] {_cropSelectionSystem.SelectedCrop?.cropName} 수량이 부족합니다 (x{_cropSelectionSystem.GetCount(_cropSelectionSystem.SelectedIndex)})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerController] No crop selected");
                    }
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

            string poolName = "FarmPlot";
            GameObject newPlot = null;

            if (PoolManager.Instance != null)
            {
                if (PoolManager.Instance.GetPool(poolName) == null)
                {
                    PoolManager.Instance.CreatePool(poolName, _plotPrefab, 20, 100);
                }
                newPlot = PoolManager.Instance.Spawn(poolName, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
            }
            else
            {
                newPlot = Instantiate(_plotPrefab, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
            }

            if (newPlot != null && newPlot.TryGetComponent(out FarmPlot plotScript))
            {
                plotScript.Clear();
                if (_farmingManager != null) _farmingManager.RegisterPlot(plotScript);
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
            if (_attackCooldownTimer > 0) _attackCooldownTimer -= Time.deltaTime;
        }

        private bool CanAttack()
        {
            return _attackCooldownTimer <= 0f && !_isRolling && !_isSwimming && !_isAttacking && !_isBuilding;
        }

        private System.Collections.IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _attackCooldownTimer = _attackCooldown;
            _moveDirection = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _animator.SetTrigger(AnimAttack);

            yield return new WaitForSeconds(_attackHitDelay);
            PerformAttackHit();

            yield return new WaitForSeconds(_attackRecoverTime);
            _isAttacking = false;
        }

        private void PerformAttackHit()
        {
            Vector2 direction = _facingDirection == Vector2.zero ? Vector2.down : _facingDirection.normalized;
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, 0.1f);
            Vector2 hitCenter = origin + direction * _attackRange;
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, _attackRadius);

            float nearestDistance = float.MaxValue;
            Animal.PigHuntable nearestPig = null;

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                Animal.PigHuntable pigHuntable = hit.GetComponentInParent<Animal.PigHuntable>();
                if (pigHuntable == null || !pigHuntable.IsAlive)
                {
                    continue;
                }

                float distance = Vector2.Distance(origin, hit.ClosestPoint(origin));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPig = pigHuntable;
                }
            }

            nearestPig?.TryHit();
        }

        private void UpdateAnimations()
        {
            Vector2 animVelocity = _isRolling ? _rb.linearVelocity.normalized : _moveDirection;

            _animator.SetFloat(AnimMoveX, animVelocity.x);
            _animator.SetFloat(AnimMoveY, animVelocity.y);
            _animator.SetFloat(AnimFacingX, _facingDirection.x);
            _animator.SetFloat(AnimFacingY, _facingDirection.y);

            if (_moveDirection.x != 0)
                _spriteRenderer.flipX = _moveDirection.x < 0;

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

            Vector3 buildingCenter = new Vector3(evt.GridPosition.x + width * 0.5f, evt.GridPosition.y + height * 0.5f, 0);

            Vector3 dirToCenter = (buildingCenter - transform.position).normalized;
            if (dirToCenter == Vector3.zero) dirToCenter = Vector3.down;

            float rayDistX = (dirToCenter.x != 0) ? (width * 0.5f) / Mathf.Abs(dirToCenter.x) : float.MaxValue;
            float rayDistY = (dirToCenter.y != 0) ? (height * 0.5f) / Mathf.Abs(dirToCenter.y) : float.MaxValue;
            float boxRadius = Mathf.Min(rayDistX, rayDistY);

            float safeRadius = boxRadius + 0.3f;
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
                yield return null;
            }

            _moveDirection = Vector2.zero;

            if (useNavMesh)
                _navMeshAgent.ResetPath();
            else
                _rb.linearVelocity = Vector2.zero;

            int maxHammerCount = (_pendingBuildingData != null && _pendingBuildingData.BuildingId == "campfire") ? 2 : 4;

            for (int i = 0; i < maxHammerCount; i++)
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

            Vector2 direction = _facingDirection == Vector2.zero ? Vector2.down : _facingDirection.normalized;
            Vector3 attackCenter = transform.position + new Vector3(0f, 0.1f, 0f) + (Vector3)(direction * _attackRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, _attackRadius);
        }
    }
}
