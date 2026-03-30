using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using SunnysideIsland.Events;

namespace SunnysideIsland.Building
{
    public class BuildingPlacer : MonoBehaviour
    {
        [Header("=== Tilemap References ===")]
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private Tilemap _sandTilemap;
        [SerializeField] private Tilemap _seaTilemap;
        [SerializeField] private Transform _buildingParent;

        [Header("=== Layers ===")]
        [SerializeField] private LayerMask _treeLayer;
        [SerializeField] private LayerMask _buildingLayer;

        [Header("=== Preview ===")]
        [SerializeField] private GameObject _previewPrefab;
        [SerializeField] private float _previewScale = 1f;

        [Header("=== Building Range ===")]
        [SerializeField] private float _buildRange = 4f;
        [SerializeField] private Transform _playerTransform;

        [Header("=== Input ===")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset _inputActions;

        public bool IsInBuildMode => _currentBuildingData != null;

        private DetailedBuildingData _currentBuildingData;
        private GameObject _previewObject;
        private SpriteRenderer _previewRenderer;
        private Vector3 _previewOriginalScale;
        private Vector3Int _currentGridPosition;
        private bool _canPlace;
        private string _placementFailReason;

        private UnityEngine.InputSystem.InputAction _clickAction;
        private UnityEngine.InputSystem.InputAction _pointAction;
        private UnityEngine.InputSystem.InputAction _cancelAction;

        private BuildingSystem _buildingSystem;
        private BuildingManager _buildingManager;

        public void Initialize(BuildingSystem buildingSystem, BuildingManager buildingManager)
        {
            _buildingSystem = buildingSystem;
            _buildingManager = buildingManager;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            if (_inputActions != null)
            {
                var uiMap = _inputActions.FindActionMap("UI");
                if (uiMap != null)
                {
                    _clickAction = uiMap.FindAction("Click");
                    _pointAction = uiMap.FindAction("Point");
                    _cancelAction = uiMap.FindAction("Cancel");
                }
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

        public void StartPlacement(DetailedBuildingData buildingData)
        {
            if (buildingData == null) return;

            _currentBuildingData = buildingData;
            CreatePreview();

            Debug.Log($"[BuildingPlacer] Started placement for: {buildingData.BuildingName}");
        }

        public void CancelPlacement(bool userCancelled = true)
        {
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }

            _currentBuildingData = null;
            _previewRenderer = null;

            if (userCancelled)
            {
                EventBus.Publish(new BuildingPlacementCancelledEvent());
            }

            Debug.Log("[BuildingPlacer] Placement cancelled");
        }

        private void CreatePreview()
        {
            if (_currentBuildingData == null) return;

            if (_currentBuildingData.BuildingPrefab != null)
            {
                _previewObject = Instantiate(_currentBuildingData.BuildingPrefab);
                _previewObject.name = $"Preview_{_currentBuildingData.BuildingId}";

                _previewRenderer = _previewObject.GetComponent<SpriteRenderer>();
                if (_previewRenderer == null)
                {
                    _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
                }

                foreach (var collider in _previewObject.GetComponentsInChildren<Collider2D>())
                {
                    collider.enabled = false;
                }
            }
            else if (_previewPrefab != null)
            {
                _previewObject = Instantiate(_previewPrefab);
                _previewObject.name = $"Preview_{_currentBuildingData.BuildingId}";

                _previewRenderer = _previewObject.GetComponent<SpriteRenderer>();
                if (_previewRenderer == null)
                {
                    _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
                }
            }

            if (_previewObject == null) return;

            _previewOriginalScale = _previewObject.transform.localScale;
            float scale = _previewScale * _currentBuildingData.PreviewScale;
            _previewObject.transform.localScale = _previewOriginalScale * scale;

            if (_previewRenderer != null)
            {
                _previewRenderer.sortingOrder = 100;
            }

            SetPreviewColor(false);
        }

        private void Update()
        {
            if (!IsInBuildMode || _previewObject == null) return;

            UpdatePreviewPosition();

            if (_cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                CancelPlacement(userCancelled: true);
                return;
            }

            if (_clickAction != null && _clickAction.WasPressedThisFrame())
            {
                TryPlaceBuilding();
            }
        }

        private void UpdatePreviewPosition()
        {
            if (_pointAction == null || _groundTilemap == null || _previewObject == null) return;

            Vector2 mousePosition = _pointAction.ReadValue<Vector2>();
            Vector3 worldPosition = UnityEngine.Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0;

            Vector3Int cellPosition = _groundTilemap.WorldToCell(worldPosition);

            BuildingSize size = _currentBuildingData.Size;
            Vector3Int adjustedPosition = new Vector3Int(cellPosition.x, cellPosition.y, 0);

            Vector3 worldPos = _groundTilemap.GetCellCenterWorld(adjustedPosition);
            _previewObject.transform.position = worldPos;

            _currentGridPosition = adjustedPosition;

            var result = CanPlaceAt(adjustedPosition, size, _currentBuildingData.PlacementType);
            _canPlace = result.CanPlace;
            _placementFailReason = result.FailReason;
            SetPreviewColor(_canPlace);
        }

        private (bool CanPlace, string FailReason) CanPlaceAt(Vector3Int position, BuildingSize size, PlacementType placementType)
        {
            if (_groundTilemap == null) return (false, "타일맵이 없습니다.");

            if (_playerTransform != null)
            {
                Vector3 buildWorldPos = _groundTilemap.GetCellCenterWorld(position);
                float distance = Vector3.Distance(_playerTransform.position, buildWorldPos);
                if (distance > _buildRange)
                {
                    return (false, $"캐릭터 근처에만 건설할 수 있습니다. (거리: {distance:F1})");
                }
            }

            if (placementType == PlacementType.SeaShore)
            {
                return CanPlaceAtSeaShore(position, size);
            }
            else
            {
                return CanPlaceOnGround(position, size);
            }
        }

        private (bool CanPlace, string FailReason) CanPlaceOnGround(Vector3Int position, BuildingSize size)
        {
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    Vector3Int checkPos = new Vector3Int(position.x + x, position.y + y, 0);

                    TileBase groundTile = _groundTilemap.GetTile(checkPos);
                    if (groundTile == null)
                    {
                        return (false, "땅이 아닌 곳에는 건설할 수 없습니다.");
                    }

                    if (_seaTilemap != null)
                    {
                        TileBase seaTile = _seaTilemap.GetTile(checkPos);
                        if (seaTile != null)
                        {
                            return (false, "바다 위에는 건설할 수 없습니다.");
                        }
                    }

                    Vector3 worldCheckPos = _groundTilemap.GetCellCenterWorld(checkPos);

                    Collider2D treeCollider = Physics2D.OverlapPoint(worldCheckPos, _treeLayer);
                    if (treeCollider != null)
                    {
                        return (false, "나무가 있어 건설할 수 없습니다.");
                    }

                    Collider2D buildingCollider = Physics2D.OverlapPoint(worldCheckPos, _buildingLayer);
                    if (buildingCollider != null)
                    {
                        return (false, "이미 건물이 있습니다.");
                    }
                }
            }

            return (true, null);
        }

        private (bool CanPlace, string FailReason) CanPlaceAtSeaShore(Vector3Int position, BuildingSize size)
        {
            bool hasValidSand = false;

            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    Vector3Int checkPos = new Vector3Int(position.x + x, position.y + y, 0);

                    TileBase sandTile = _sandTilemap != null ? _sandTilemap.GetTile(checkPos) : null;
                    TileBase groundTile = _groundTilemap.GetTile(checkPos);

                    // 배는 Sand 위에만 건설 가능 (ground만 있으면 안됨)
                    if (sandTile == null)
                    {
                        return (false, "배는 모래 위에만 건설할 수 있습니다.");
                    }

                    hasValidSand = true;

                    Vector3 worldCheckPos = _groundTilemap.GetCellCenterWorld(checkPos);

                    Collider2D treeCollider = Physics2D.OverlapPoint(worldCheckPos, _treeLayer);
                    if (treeCollider != null)
                    {
                        return (false, "나무가 있어 건설할 수 없습니다.");
                    }

                    Collider2D buildingCollider = Physics2D.OverlapPoint(worldCheckPos, _buildingLayer);
                    if (buildingCollider != null)
                    {
                        return (false, "이미 건물이 있습니다.");
                    }
                }
            }

            if (!hasValidSand)
            {
                return (false, "배는 모래 위에 지어야 합니다.");
            }

            return (true, null);
        }

        private void SetPreviewColor(bool canPlace)
        {
            if (_previewRenderer == null) return;

            Color color = canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            _previewRenderer.color = color;
        }

        private void TryPlaceBuilding()
        {
            if (_currentBuildingData == null) return;

            if (!_canPlace)
            {
                if (!string.IsNullOrEmpty(_placementFailReason))
                {
                    EventBus.Publish(new PlacementFailedEvent
                    {
                        Message = _placementFailReason,
                        Reason = "PlacementRule"
                    });
                }
                return;
            }

            EventBus.Publish(new BuildingPlaceRequestedEvent
            {
                BuildingId = _currentBuildingData.BuildingId,
                GridPosition = _currentGridPosition
            });

            CancelPlacement(userCancelled: false);
        }

        public void ConfirmBuildingPlacement(DetailedBuildingData buildingData, Vector3Int gridPosition)
        {
            if (buildingData != null)
            {
                PlaceBuilding(gridPosition, buildingData);
            }
        }

        private void PlaceBuilding(Vector3Int gridPosition, DetailedBuildingData buildingData)
        {
            if (buildingData.BuildingPrefab == null)
            {
                Debug.LogError($"[BuildingPlacer] BuildingPrefab is null for {buildingData.BuildingId}");
                return;
            }

            Vector3 worldPosition = _groundTilemap.GetCellCenterWorld(gridPosition);

            GameObject buildingGO = Instantiate(buildingData.BuildingPrefab, worldPosition, Quaternion.identity);
            buildingGO.name = $"Building_{buildingData.BuildingId}";

            buildingGO.transform.localScale = buildingGO.transform.localScale * buildingData.PreviewScale;

            if (_buildingParent != null)
            {
                buildingGO.transform.parent = _buildingParent;
            }

            Building building = buildingGO.GetComponent<Building>();
            if (building == null)
            {
                building = buildingGO.AddComponent<Building>();
            }

            building.SetBuildingData(buildingData);
            building.Place(gridPosition);
            building.StartConstruction();

            _buildingManager?.RegisterBuilding(building);

            Debug.Log($"[BuildingPlacer] Building placed: {buildingData.BuildingName} at {gridPosition}");

            _buildingSystem?.OnBuildingPlaced(buildingData.BuildingId, gridPosition);
        }
    }
}