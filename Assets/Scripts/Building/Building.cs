using System;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Building
{
    public enum BuildingState
    {
        Preview,
        Placed,
        Constructing,
        Completed,
        Upgrading
    }

    public interface IBuilding
    {
        string BuildingId { get; }
        BuildingState State { get; }
        int CurrentLevel { get; }

        void Place(Vector3Int gridPosition);
        void StartConstruction();
        void ProgressConstruction();
        void Complete();
        void Demolish();
    }

    public class Building : MonoBehaviour, IBuilding, ISaveable
    {
        [Header("=== Building Data ===")]
        [SerializeField] private DetailedBuildingData _buildingData;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("=== Construction UI ===")]
        [SerializeField] private GameObject _constructionProgressBarPrefab;

        public string BuildingId => _buildingData?.BuildingId ?? "unknown";
        public BuildingState State { get; private set; } = BuildingState.Preview;
        public int CurrentLevel { get; private set; } = 1;
        public int ConstructionProgress => _constructionProgress;
        public int RequiredConstructionDays => _buildingData?.BuildTime ?? 1;
        public DetailedBuildingData BuildingData => _buildingData;

        private int _constructionProgress;
        private Vector3Int _gridPosition;
        private GameObject _progressBarInstance;

        public void SetBuildingData(DetailedBuildingData data)
        {
            _buildingData = data;
        }

        public string SaveKey => $"Building_{BuildingId}_{gameObject.GetInstanceID()}";

        public void Place(Vector3Int gridPosition)
        {
            _gridPosition = gridPosition;
            transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);
            State = BuildingState.Placed;
        }

        public void StartConstruction()
        {
            if (State != BuildingState.Placed) return;

            State = BuildingState.Constructing;
            _constructionProgress = 0;

            SetTransparency(0.5f);

            CreateProgressBar();

            EventBus.Publish(new ConstructionStartedEvent
            {
                BuildingId = BuildingId,
                BuildingName = _buildingData?.BuildingName ?? "Unknown"
            });
        }

        public void ProgressConstruction()
        {
            if (State != BuildingState.Constructing && State != BuildingState.Upgrading) return;

            _constructionProgress++;
            int requiredDays = _buildingData?.BuildTime ?? 1;

            EventBus.Publish(new ConstructionProgressEvent
            {
                BuildingId = BuildingId,
                CurrentProgress = _constructionProgress,
                RequiredProgress = requiredDays
            });

            if (_constructionProgress >= requiredDays)
            {
                if (State == BuildingState.Constructing)
                {
                    Complete();
                }
                else if (State == BuildingState.Upgrading)
                {
                    CurrentLevel++;
                    State = BuildingState.Completed;
                }
            }
        }

        public void Complete()
        {
            State = BuildingState.Completed;

            SetTransparency(1.0f);

            DestroyProgressBar();

            EventBus.Publish(new BuildingCompletedEvent
            {
                BuildingId = BuildingId,
                BuildingName = _buildingData?.BuildingName ?? "Unknown"
            });
        }

        public int CancelConstruction()
        {
            if (State != BuildingState.Constructing) return 0;

            int refundedWood = 0;
            if (_buildingData?.Cost != null)
            {
                for (int i = 0; i < _buildingData.Cost.Materials.Count; i++)
                {
                    if (_buildingData.Cost.Materials[i].ToLower() == "wood")
                    {
                        refundedWood = _buildingData.Cost.Amounts[i];
                        break;
                    }
                }
            }

            EventBus.Publish(new ConstructionCancelledEvent
            {
                BuildingId = BuildingId,
                RefundedWood = refundedWood
            });

            Destroy(gameObject);

            return refundedWood;
        }

        public void Demolish()
        {
            Destroy(gameObject);
        }

        private void SetTransparency(float alpha)
        {
            if (_spriteRenderer != null)
            {
                Color color = _spriteRenderer.color;
                color.a = alpha;
                _spriteRenderer.color = color;
            }
            else
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a = alpha;
                    sr.color = color;
                }
            }
        }

        private void CreateProgressBar()
        {
            if (_constructionProgressBarPrefab != null)
            {
                _progressBarInstance = Instantiate(_constructionProgressBarPrefab, transform);
                _progressBarInstance.transform.localPosition = new Vector3(0, -1f, 0);

                Vector3 parentScale = transform.localScale;
                if (parentScale.x != 0 && parentScale.y != 0)
                {
                    float targetWorldScale = 0.02f;
                    _progressBarInstance.transform.localScale = new Vector3(
                        targetWorldScale / parentScale.x,
                        targetWorldScale / parentScale.y,
                        1f
                    );
                }
            }
        }

        private void DestroyProgressBar()
        {
            if (_progressBarInstance != null)
            {
                Destroy(_progressBarInstance);
                _progressBarInstance = null;
            }
        }

        public object GetSaveData()
        {
            return new BuildingSaveData
            {
                BuildingId = BuildingId,
                State = State,
                CurrentLevel = CurrentLevel,
                ConstructionProgress = _constructionProgress,
                GridPosition = _gridPosition
            };
        }

        public void LoadSaveData(object state)
        {
            if (state is BuildingSaveData data)
            {
                State = data.State;
                CurrentLevel = data.CurrentLevel;
                _constructionProgress = data.ConstructionProgress;
                _gridPosition = data.GridPosition;
                transform.position = new Vector3(_gridPosition.x, _gridPosition.y, 0);

                if (State == BuildingState.Constructing)
                {
                    SetTransparency(0.5f);
                    CreateProgressBar();
                }
            }
        }
    }

    [Serializable]
    public class BuildingSaveData
    {
        public string BuildingId;
        public BuildingState State;
        public int CurrentLevel;
        public int ConstructionProgress;
        public Vector3Int GridPosition;
    }
}
