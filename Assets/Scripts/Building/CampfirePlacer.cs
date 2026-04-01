using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using SunnysideIsland.Events;
using SunnysideIsland.Pool;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// Campfire 배치 시스템
    /// Preview → Hammer 애니메이션 2회 → Base 생성
    /// </summary>
    public class CampfirePlacer : MonoBehaviour
    {
        [Header("=== Tilemap References ===")]
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private Tilemap _sandTilemap;
        [SerializeField] private Transform _buildingParent;

        [Header("=== Layers ===")]
        [SerializeField] private LayerMask _treeLayer;
        [SerializeField] private LayerMask _buildingLayer;

        [Header("=== Preview ===")]
        [SerializeField] private Sprite _campfireBaseSprite;
        [Tooltip("미리보기 오브젝트의 스케일")]
        [SerializeField] private Vector3 _previewScale = Vector3.one;

        [Header("=== Campfire Prefab ===")]
        [SerializeField] private GameObject _campfirePrefab;

        [Header("=== Input ===")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("=== Player ===")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Animator _playerAnimator;

        public bool IsInPlacementMode => _isInPlacementMode;

        private bool _isInPlacementMode = false;
        private GameObject _previewObject;
        private SpriteRenderer _previewRenderer;
        private Vector3Int _currentGridPosition;
        private bool _canPlace;
        private string _placementFailReason;

        private InputAction _clickAction;
        private InputAction _pointAction;
        private InputAction _cancelAction;

        private BuildingSystem _buildingSystem;

        private static readonly int AnimHammer = Animator.StringToHash("Hammer");

        public void Initialize(BuildingSystem buildingSystem)
        {
            _buildingSystem = buildingSystem;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerAnimator = player.GetComponent<Animator>();
            }

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (_inputActions == null) return;

            var uiMap = _inputActions.FindActionMap("UI");
            if (uiMap != null)
            {
                _clickAction = uiMap.FindAction("Click");
                _pointAction = uiMap.FindAction("Point");
                _cancelAction = uiMap.FindAction("Cancel");
            }
        }

        private void OnEnable()
        {
            SetupInputActions();
            _clickAction?.Enable();
            _pointAction?.Enable();
            _cancelAction?.Enable();
        }

        private void OnDisable()
        {
            _clickAction?.Disable();
            _pointAction?.Disable();
            _cancelAction?.Disable();
        }

        /// <summary>
        /// Campfire 배치 모드 시작
        /// </summary>
        public void StartPlacement()
        {
            if (_groundTilemap == null)
            {
                Debug.LogError("[CampfirePlacer] Ground tilemap not assigned");
                return;
            }

            _isInPlacementMode = true;
            CreatePreview();

            Debug.Log("[CampfirePlacer] 모닥불을 놓을 위치를 선택하세요");
        }

        private void CreatePreview()
        {
            if (_previewObject != null)
            {
                Destroy(_previewObject);
            }

            _previewObject = new GameObject("CampfirePreview");
            _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();

            if (_campfireBaseSprite != null)
            {
                _previewRenderer.sprite = _campfireBaseSprite;
            }

            _previewRenderer.sortingOrder = 100;
            _previewObject.transform.localScale = _previewScale;

            SetPreviewColor(false);
        }

        private void OnValidate()
        {
            if (_previewObject != null)
            {
                _previewObject.transform.localScale = _previewScale;
            }
        }

        private void Update()
        {
            if (!_isInPlacementMode || _previewObject == null) return;

            UpdatePreviewPosition();

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                CancelPlacement();
                return;
            }

            if (_clickAction != null && _clickAction.WasPressedThisFrame())
            {
                TryPlaceCampfire();
            }
        }

        private void UpdatePreviewPosition()
        {
            if (_pointAction == null || _groundTilemap == null || _previewObject == null) return;

            Vector2 mousePosition = _pointAction.ReadValue<Vector2>();
            Vector3 worldPosition = UnityEngine.Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0;

            Vector3Int cellPosition = _groundTilemap.WorldToCell(worldPosition);
            Vector3 worldPos = _groundTilemap.GetCellCenterWorld(cellPosition);

            _previewObject.transform.position = worldPos;
            _currentGridPosition = cellPosition;

            var result = CanPlaceAt(cellPosition);
            _canPlace = result.CanPlace;
            _placementFailReason = result.FailReason;
            SetPreviewColor(_canPlace);
        }

        private (bool CanPlace, string FailReason) CanPlaceAt(Vector3Int position)
        {
            if (_groundTilemap == null) return (false, "타일맵이 없습니다.");

            // Ground 타일 확인
            TileBase groundTile = _groundTilemap.GetTile(position);
            if (groundTile == null)
            {
                return (false, "땅 위에만 설치할 수 있습니다.");
            }

            // Sand 타일은 불가
            if (_sandTilemap != null)
            {
                TileBase sandTile = _sandTilemap.GetTile(position);
                if (sandTile != null)
                {
                    return (false, "모래 위에는 설치할 수 없습니다.");
                }
            }

            Vector3 worldCheckPos = _groundTilemap.GetCellCenterWorld(position);

            // 나무 확인
            Collider2D treeCollider = Physics2D.OverlapPoint(worldCheckPos, _treeLayer);
            if (treeCollider != null)
            {
                return (false, "나무가 있어 설치할 수 없습니다.");
            }

            // 다른 건물 확인
            Collider2D buildingCollider = Physics2D.OverlapPoint(worldCheckPos, _buildingLayer);
            if (buildingCollider != null)
            {
                return (false, "이미 건물이 있습니다.");
            }

            // Player 거리 확인
            if (_playerTransform != null)
            {
                float distance = Vector3.Distance(_playerTransform.position, worldCheckPos);
                if (distance > 4f)
                {
                    return (false, "너무 멀리 있습니다.");
                }
            }

            return (true, null);
        }

        private void SetPreviewColor(bool canPlace)
        {
            if (_previewRenderer == null) return;

            // BuildingPlacer와 동일한 색상
            Color color = canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            _previewRenderer.color = color;
        }

        private void TryPlaceCampfire()
        {
            Debug.Log($"[CampfirePlacer] TryPlaceCampfire called. _canPlace: {_canPlace}, _placementFailReason: {_placementFailReason}");
            
            if (!_canPlace)
            {
                if (!string.IsNullOrEmpty(_placementFailReason))
                {
                    Debug.Log($"[CampfirePlacer] {_placementFailReason}");
                }
                return;
            }

            // 배치 시작
            Debug.Log("[CampfirePlacer] Starting PlaceCampfireSequence...");
            StartCoroutine(PlaceCampfireSequence());
        }

        private IEnumerator PlaceCampfireSequence()
        {
            _isInPlacementMode = false;

            // Preview 제거
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }

            // Player 위치 이동
            if (_playerTransform != null)
            {
                Vector3 targetPos = _groundTilemap.GetCellCenterWorld(_currentGridPosition);
                // Player를 타겟 위치로 이동 (필요시 NavMesh 사용)
            }

            // Hammer 애니메이션 2회
            if (_playerAnimator != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    _playerAnimator.SetTrigger(AnimHammer);

                    // 애니메이션 완료 대기
                    yield return new WaitForSeconds(0.5f);

                    while (_playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("hamering"))
                    {
                        yield return null;
                    }
                }
            }
            else
            {
                // Animator 없으면 딜레이만
                yield return new WaitForSeconds(1f);
            }

            // Campfire Base 생성
            CreateCampfireBase();

            // UI 닫기
            EventBus.Publish(new BuildingPlacedEvent
            {
                BuildingId = "campfire",
                Position = _groundTilemap.GetCellCenterWorld(_currentGridPosition)
            });
        }

        private void CreateCampfireBase()
        {
            if (_campfirePrefab == null)
            {
                Debug.LogError("[CampfirePlacer] Campfire prefab not assigned");
                return;
            }

            Vector3 worldPosition = _groundTilemap.GetCellCenterWorld(_currentGridPosition);

            GameObject campfireGO = Instantiate(_campfirePrefab, worldPosition, Quaternion.identity);
            campfireGO.name = "Campfire";

            if (_buildingParent != null)
            {
                campfireGO.transform.SetParent(_buildingParent);
            }

            // Campfire 컴포넌트 설정
            Campfire campfire = campfireGO.GetComponent<Campfire>();
            if (campfire == null)
            {
                campfire = campfireGO.AddComponent<Campfire>();
            }

            Debug.Log($"[CampfirePlacer] Campfire base placed at {_currentGridPosition}");

            Debug.Log("[CampfirePlacer] 모닥불 틀을 만들었습니다. 불을 붙이려면 나무 2개가 필요합니다.");
        }

        private void CancelPlacement()
        {
            _isInPlacementMode = false;

            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }

            EventBus.Publish(new BuildingPlacementCancelledEvent());
        }

        public void Cleanup()
        {
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }
            _isInPlacementMode = false;
        }
    }
}
