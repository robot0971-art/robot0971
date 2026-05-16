using DI;
using SunnysideIsland.Building;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.Farming;
using SunnysideIsland.Input;
using SunnysideIsland.UI;
using SunnysideIsland.Pool;
using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;

namespace SunnysideIsland.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInteraction))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerBuildController))]
    [RequireComponent(typeof(PlayerShadowController))]
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
        [SerializeField] private float _buildStandOffDistance = 0.08f;
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
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerBuildController _buildController;

        [Header("=== Swim Settings ===")]
        [SerializeField] private LayerMask _seaLayer;
        [SerializeField] private float _swimSpeed = 2.5f;
        public bool IsSwimming => _movement != null && _movement.IsSwimming;
        public bool CanMove
        {
            get => _movement == null || _movement.CanMove;
            set
            {
                if (_movement != null)
                {
                    _movement.CanMove = value;
                }
            }
        }

        [Header("=== Input ===")]
        [SerializeField] private InputActionAsset _inputActions;
        private InputAction _moveAction;

        [Inject(Optional = true)] private IUIManager _uiManager = default!;
        [Inject(Optional = true)] private BuildingSystem _buildingSystem = default!;
        [Inject(Optional = true)] private ICropSelectionSystem _cropSelectionSystem = default!;
        [Inject(Optional = true)] private FarmingManager _farmingManager = default!;
        [Inject(Optional = true)] private Grid _grid = default!;
        [Inject(Optional = true)] private IPoolManager _poolManager = default!;

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

        private bool _isDead;

        private static readonly int AnimHurt = Animator.StringToHash("Hurt");
        private static readonly int AnimDeath = Animator.StringToHash("Death");
        private static readonly int AnimIsDead = Animator.StringToHash("IsDead");
        private System.Action _onBuildComplete;
        private readonly System.Collections.Generic.HashSet<int> _animatorParameterHashes = new System.Collections.Generic.HashSet<int>();

        [SerializeField] private GameObject _plotPrefab;

        [SerializeField] private LayerMask _groundLayer;
        public string SaveKey => "Player";

        private void Awake()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_movement == null) _movement = GetComponent<PlayerMovement>();
            if (_movement == null) _movement = gameObject.AddComponent<PlayerMovement>();
            if (_interaction == null) _interaction = GetComponent<PlayerInteraction>();
            if (_interaction == null) _interaction = gameObject.AddComponent<PlayerInteraction>();
            if (_combat == null) _combat = GetComponent<PlayerCombat>();
            if (_combat == null) _combat = gameObject.AddComponent<PlayerCombat>();
            if (_buildController == null) _buildController = GetComponent<PlayerBuildController>();
            if (_buildController == null) _buildController = gameObject.AddComponent<PlayerBuildController>();
            _movement.Configure(
                _rb,
                _animator,
                _spriteRenderer,
                _seaLayer,
                _groundLayer,
                _moveSpeed,
                _sprintSpeed,
                _rollSpeed,
                _rollDuration,
                _rollCooldown,
                _swimSpeed);
            ConfigureInteraction();
            ConfigureCombat();
            ConfigureBuildController();
            CacheAnimatorParameters();

            if (GetComponent<PlayerShadowController>() == null)
            {
                gameObject.AddComponent<PlayerShadowController>();
            }

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
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void Start()
        {
            // DI 二쇱엯 ?ㅽ뻾
            DIContainer.Inject(this);

            if (_navMeshAgent != null)
            {
                _navMeshAgent.updateRotation = false;
                _navMeshAgent.updateUpAxis = false;
            }

            ConfigureInteraction();
            ConfigureCombat();
            ConfigureBuildController();

            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void ConfigureInteraction()
        {
            _interaction.Configure(
                _animator,
                _movement,
                _cropSelectionSystem,
                _farmingManager,
                _grid,
                _poolManager,
                _plotPrefab,
                _interactableLayer,
                _farmingLayer,
                _treeLayer,
                _harvestLayer,
                _groundLayer);
        }

        private void ConfigureCombat()
        {
            _combat.Configure(
                _animator,
                _movement,
                _attackRange,
                _attackRadius,
                _attackCooldown,
                _attackHitDelay,
                _attackRecoverTime);
        }

        private void ConfigureBuildController()
        {
            _buildController.Configure(
                _rb,
                _animator,
                _navMeshAgent,
                _movement,
                _buildingSystem,
                _buildStandOffDistance);
        }

        private void Update()
        {
            if (_isDead)
            {
                HandleTimers();
                _movement.UpdateAnimations();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Loading)
            {
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            {
                _movement.Stop();
                CheckCancelAutoMove();
                HandleTimers();
                _movement.UpdateAnimations();
                return;
            }

            if (SunnysideIsland.UI.UIManager.Instance != null
                && SunnysideIsland.UI.UIManager.Instance.GetPanel<SunnysideIsland.UI.Menu.BoatConfirmPanel>()?.IsOpen == true)
            {
                CheckCancelAutoMove();
                HandleTimers();
                _movement.UpdateAnimations();
                return;
            }

            CheckCancelAutoMove();

            if (!_movement.CanMove) return;

            _movement.TickEnvironment();

            HandleInput();
            HandleTimers();
            _movement.UpdateAnimations();
        }

        private void CheckCancelAutoMove()
        {
            if (!_buildController.IsBuilding)
            {
                return;
            }

            Vector2 inputVector = Vector2.zero;
            if (_moveAction != null)
            {
                inputVector = _moveAction.ReadValue<Vector2>();
            }
            else
            {
                inputVector = new Vector2(GameInput.GetAxisRaw("Horizontal"), GameInput.GetAxisRaw("Vertical"));
            }

            _buildController.CancelIfManualInput(inputVector);
        }

        public void PauseMovement(float duration)
        {
            _movement.Pause(duration);
        }

        private void FixedUpdate()
        {
            _movement.FixedTick(_combat.IsAttacking);
        }

        private void HandleInput()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            {
                _movement.Stop();
                return;
            }

            Vector2 inputVector = Vector2.zero;

            if (_moveAction != null)
            {
                inputVector = _moveAction.ReadValue<Vector2>();
            }
            else
            {
                inputVector = new Vector2(GameInput.GetAxisRaw("Horizontal"), GameInput.GetAxisRaw("Vertical"));
            }

            _movement.SetInput(inputVector);
            _movement.SetSprinting(GameInput.GetKey(KeyCode.LeftShift));

            if (GameInput.GetKeyDown(KeyCode.Space))
                _movement.TryRoll();

            if (GameInput.GetKeyDown(_attackKey) && _combat.TryAttack(_buildController.IsBuilding, _isDead))
            {
                return;
            }

            if (GameInput.GetKeyDown(KeyCode.I))
            {
                var uiMgr = UIManager;
                if (uiMgr != null)
                {
                    var inventoryPanel = uiMgr.GetPanel<SunnysideIsland.UI.Inventory.InventoryPanel>();
                    if (inventoryPanel != null)
                    {
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

            if (GameInput.GetKeyDown(KeyCode.B))
            {
                if (_buildingSystem != null)
                {
                    _buildingSystem.ToggleBuildMode();
                }
            }

            if (GameInput.GetKeyDown(KeyCode.E)) // ?섏쁺 以묒뿉???꾩씠?쒖쓣 二쇱슱 ???덈룄濡?!_isSwimming ?쒓굅
            {
                _interaction.HandlePrimaryAction();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (((1 << collision.gameObject.layer) & _seaLayer) != 0)
            {
                _movement.SetSwimming(true);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (((1 << collision.gameObject.layer) & _seaLayer) != 0)
            {
                _movement.SetSwimming(false);
            }
        }

        private void HandleTimers()
        {
            _movement.TickTimers(Time.deltaTime);
            _combat.TickTimers(Time.deltaTime);
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            _combat.CancelAttack();

            _buildController.CancelBuilding();
            StopAllCoroutines();
            _movement.SetDead(true);
            SetAnimatorTriggerIfExists(AnimDeath);
            SetAnimatorBoolIfExists(AnimIsDead, true);
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

        public object GetSaveData()
        {
            return new PlayerSaveData
            {
                Position = transform.position,
                FacingDirectionX = _movement.FacingDirection.x,
                FacingDirectionY = _movement.FacingDirection.y
            };
        }

        public void LoadSaveData(object data)
        {
            var saveData = data as PlayerSaveData ?? (data as JObject)?.ToObject<PlayerSaveData>();
            if (saveData != null)
            {
                transform.position = saveData.Position;
                _movement.SetFacingDirection(new Vector2(saveData.FacingDirectionX, saveData.FacingDirectionY));
                _movement.RefreshEnvironmentState();
            }
        }

        [System.Serializable]
        public class PlayerSaveData { public Vector3 Position; public float FacingDirectionX; public float FacingDirectionY; }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);

            Vector2 direction = _movement.FacingDirection == Vector2.zero
                ? Vector2.down
                : _movement.FacingDirection.normalized;
            Vector3 attackCenter = transform.position + new Vector3(0f, 0.1f, 0f) + (Vector3)(direction * _attackRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, _attackRadius);
        }
    }
}
