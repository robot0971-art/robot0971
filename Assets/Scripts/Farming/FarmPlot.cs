using System;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Farming
{
    public enum PlotState
    {
        Empty, Planted, Growing, Ready, Dead
    }

    public class FarmPlot : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] private SpriteRenderer _plotSprite;
        [SerializeField] private SpriteRenderer _cropSprite;
        [SerializeField] private CropData _cropData; // ScriptableObject 감자 데이터

        [Header("=== Size ===")]
        [SerializeField] private float _plotScale = 0f; // 0 이하면 GameData 기본값 사용

        private PlotState _state = PlotState.Empty;
        private string _cropId;
        private float _growthProgress;
        private bool _isWatered;
        private int _daysPlanted;

        public PlotState State => _state;
        public string CropId => _cropId;
        public float GrowthProgress => _growthProgress;
        public bool IsWatered => _isWatered;
        public bool IsReady => _state == PlotState.Ready;
        public bool IsEmpty => _state == PlotState.Empty;

        public string SaveKey => $"FarmPlot_{gameObject.GetInstanceID()}";

        private void Start()
        {
            if (_cropData == null)
            {
                _state = PlotState.Empty;
            }

            float plotScale = _plotScale;
            if (plotScale <= 0f)
            {
                var gameData = Resources.Load<SunnysideIsland.GameData.GameData>("GameData/GameData");
                if (gameData != null)
                {
                    plotScale = gameData.defaultPlotScale;
                }
                else
                {
                    plotScale = 1f;
                }
            }

            if (_plotSprite != null)
            {
                _plotSprite.transform.localScale = Vector3.one * plotScale;
            }

            UpdateVisuals();
        }

        public bool Plant(string seedItemId, CropData cropData)
        {
            if (_state != PlotState.Empty) return false;
            if (cropData == null) return false;

            _cropId = cropData.cropId;
            _cropData = cropData;

            // 1. 상태를 성장 중으로 변경
            _state = PlotState.Growing;

            // 2. ★ 중요: 성장도를 0으로 맞춰야 첫 번째 씨앗 이미지가 나옵니다!
            _growthProgress = 0f;
            _daysPlanted = 0;
            _isWatered = false;

            // 3. 시각적 업데이트 호출
            UpdateVisuals();

            // 4. 농작물 스케일 적용
            if (_cropSprite != null && _cropData != null)
            {
                _cropSprite.transform.localScale = Vector3.one * _cropData.CropScale;
            }

            Debug.Log($"[FarmPlot] {cropData.name} 심기 완료! 현재 이미지 인덱스 계산 시작");
            return true;
        }
        public bool Water()
        {
            if (_state == PlotState.Empty || _state == PlotState.Dead) return false;
            if (_isWatered) return false;

            _isWatered = true;
            UpdateVisuals(); // 물 주면 색깔 변하게 즉시 반영
            return true;
        }

        public void DayPassed() // 매니저에서 호출하는 함수
        {
            Debug.Log($"[FarmPlot] DayPassed 호출! 위치: {transform.position}, 상태: {_state}, 물 여부: {_isWatered}, 작물: {(_cropId ?? "없음")}");
            // 심었거나 자라는 중일 때만 작동
            if (_state == PlotState.Planted || _state == PlotState.Growing)
            {
                if (_isWatered)
                {
                    _daysPlanted++;

                    if (_cropData != null && _cropData.growthDays > 0)
                    {
                        _growthProgress = (float)_daysPlanted / _cropData.growthDays;
                        _growthProgress = Mathf.Clamp01(_growthProgress);

                        Debug.Log($"[FarmPlot] 성장! {_growthProgress * 100:F0}% 완료 (Day {_daysPlanted}/{_cropData.growthDays})");
                    }

                    // 첫날 물을 줘서 성장이 시작되면 상태를 Growing으로 변경
                    if (_state == PlotState.Planted) _state = PlotState.Growing;

                    if (_growthProgress >= 1f)
                    {
                        _state = PlotState.Ready;
                        Debug.Log($"[FarmPlot] 수확 준비 완료!");
                    }
                }
                else
                {
                    Debug.Log($"[FarmPlot] 물을 안 줘서 성장하지 않음");
                }
            }
            _isWatered = false;
            UpdateVisuals();
        }
        public bool Harvest()
        {
            if (_state != PlotState.Ready) return false;
            if (_cropData == null) return false;

            // cropItemId가 있으면 사용, 없으면 cropId + "_item" 사용
            string itemId = !string.IsNullOrEmpty(_cropData.cropItemId) 
                ? _cropData.cropItemId 
                : _cropId + "_item";

            EventBus.Publish(new CropHarvestedEvent
            {
                PlotId = gameObject.GetInstanceID(),
                CropId = itemId,
                Amount = _cropData.yieldAmount
            });

            Clear();
            return true;
        }

        public void Clear()
        {
            _state = PlotState.Empty;
            //_cropId = null;
            //_cropData = null;
            _growthProgress = 0f;
            _isWatered = false;
            _daysPlanted = 0;

            if (_cropSprite != null)
            {
                _cropSprite.transform.localScale = Vector3.one;
            }

            UpdateVisuals();
        }

        public void Interact()
        {
            // 1. 수확 체크
            if (_state == PlotState.Ready)
            {
                Harvest();
                return;
            }

            // 2. 심기 체크 (인스펙터의 _cropData 활용)
            if (_state == PlotState.Empty)
            {
                if (_cropData != null)
                {
                    Plant(_cropData.cropId, _cropData);
                }
                else
                {
                    Debug.LogError("Crop Data가 연결되지 않았습니다!");
                }
            }
            /*// 3. 물 주기
            else
            {
                Water();
            }*/
        }

        private void UpdateVisuals()
        {
            // 데이터가 없으면 비우기
            if (_cropData == null || _state == PlotState.Empty)
            {
                if (_cropSprite != null) _cropSprite.sprite = null;
            }
            else
            {
                if (_cropData.growthSprites != null && _cropData.growthSprites.Length > 0)
                {
                    // 성장도(0~1)에 따라 이미지를 고릅니다.
                    // _growthProgress가 0일 때 (0 * 2) = 0번 인덱스가 나옵니다.
                    int spriteIndex = Mathf.FloorToInt(_growthProgress * (_cropData.growthSprites.Length - 1));
                    spriteIndex = Mathf.Clamp(spriteIndex, 0, _cropData.growthSprites.Length - 1);

                    _cropSprite.sprite = _cropData.growthSprites[spriteIndex];
                }
            }

            //3. 흙 색깔 변경
            if (_plotSprite != null)
            {
                _plotSprite.color = _isWatered ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            }
        }

        // --- 세이브/로드 기능 (기존 유지) ---
        public object GetSaveData()
        {
            return new FarmPlotSaveData { State = _state, CropId = _cropId, GrowthProgress = _growthProgress, IsWatered = _isWatered, DaysPlanted = _daysPlanted };
        }

        public void LoadSaveData(object state)
        {
            if (state is FarmPlotSaveData data)
            {
                _state = data.State; _cropId = data.CropId; _growthProgress = data.GrowthProgress; _isWatered = data.IsWatered; _daysPlanted = data.DaysPlanted;
                UpdateVisuals();
            }
        }
    }

    [Serializable] public class FarmPlotSaveData { public PlotState State; public string CropId; public float GrowthProgress; public bool IsWatered; public int DaysPlanted; }
    public class CropPlantedEvent { public int PlotId { get; set; } public string CropId { get; set; } }
    public class CropReadyEvent { public int PlotId { get; set; } public string CropId { get; set; } }
    public class CropHarvestedEvent { public int PlotId { get; set; } public string CropId { get; set; } public int Amount { get; set; } }
}
