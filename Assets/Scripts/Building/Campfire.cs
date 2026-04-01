using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;
using SunnysideIsland.Inventory;
using SunnysideIsland.Pool;

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 모닥불
    /// 플레이어가 수동으로 점화하고 6시간 후 자동으로 꺼짐
    /// </summary>
    public class Campfire : MonoBehaviour, ISaveable, IInteractable
    {
        [Header("=== Visuals ===")]
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private CampfireParticles _campfireParticles; // 파티클 시스템
        [SerializeField] private CampfireParticleConnector _particleConnector; // 향상된 파티클 커넥터 (선택적)
        
        [Header("=== Scale Settings ===")]
        [Tooltip("전체 모닥불 오브젝트의 스케일")]
        [SerializeField] private Vector3 _campfireScale = Vector3.one;
        
        [Tooltip("베이스(바닥 타일)의 스케일")]
        [SerializeField] private Vector3 _baseScale = Vector3.one;
        
        [Tooltip("불꽃(Campfire_Fire)의 스케일")]
        [SerializeField] private Vector3 _fireScale = Vector3.one;
        
        [Header("=== Fire Pooling ===")]
        [Tooltip("Campfire Fire Pool 이름")]
        [SerializeField] private string _firePoolName = "CampfireFire";
        [Tooltip("Fire를 배치할 위치 ( Campfire _ Fire의 부모 )")]
        [SerializeField] private Transform _fireSpawnPoint;
        
        private GameObject _currentFireObject;
        
        [Header("=== Lighting ===")]
        [SerializeField] private Light2D _campfireLight;
        [Tooltip("조명 세기")]
        [SerializeField] [Range(0.1f, 2f)] private float _lightIntensity = 1.0f;
        [Tooltip("조명 반경")]
        [SerializeField] [Range(1f, 10f)] private float _lightRadius = 5f;
        [Tooltip("깜빡임 속도")]
        [SerializeField] [Range(0.1f, 5f)] private float _flickerSpeed = 2f;
        [Tooltip("깜빡임 세기")]
        [SerializeField] [Range(0f, 0.5f)] private float _flickerAmount = 0.2f;
        
        [Header("=== Fuel ===")]
        [Tooltip("점화에 필요한 나무 개수")]
        [SerializeField] private int _woodCost = 2;
        [Tooltip("지속 시간(게임 시간 - 시간) ")]
        [SerializeField] private float _burnDurationHours = 6f;
        
        public enum CampfireState
        {
            Unlit,      // 꺼짐 (배치된 상태)
            Lit,        // 켜짐 (불타는 중)
            BurnedOut   // 다 타서 꺼짐
        }
        
        public CampfireState State { get; private set; } = CampfireState.Unlit;
        public float RemainingTimeHours { get; private set; }
        
        public string SaveKey => $"Campfire_{gameObject.GetInstanceID()}";
        
        private float _flickerTimer = 0f;
        private float _baseIntensity;
        private bool _isRegistered = false;
        
        private void Awake()
        {
            // 컴포넌트 참조 찾기 (Inspector에서 설정되지 않은 경우)
            if (_baseRenderer == null)
                _baseRenderer = GetComponent<SpriteRenderer>();
            if (_campfireParticles == null)
                _campfireParticles = GetComponentInChildren<CampfireParticles>();
            if (_campfireLight == null)
                _campfireLight = GetComponentInChildren<Light2D>();
            if (_particleConnector == null)
                _particleConnector = GetComponent<CampfireParticleConnector>();
            
            // 초기 상태 설정
            SetState(CampfireState.Unlit);
            
            if (_campfireLight != null)
            {
                _baseIntensity = _lightIntensity;
                _campfireLight.intensity = 0f;
                _campfireLight.pointLightOuterRadius = _lightRadius;
            }
            
            ApplyScales();
        }
        
        /// <summary>
        /// 설정된 스케일 값들을 적용
        /// </summary>
        private void ApplyScales()
        {
            // 전체 오브젝트 스케일
            transform.localScale = _campfireScale;
            
            // 베이스 스케일
            if (_baseRenderer != null)
            {
                _baseRenderer.transform.localScale = _baseScale;
            }
        }
        
        private void OnValidate()
        {
            ApplyScales();
            
            if (_campfireLight != null)
            {
                _campfireLight.intensity = (State == CampfireState.Lit) ? _lightIntensity : 0f;
                _campfireLight.pointLightOuterRadius = _lightRadius;
            }
        }
        
        private void Start()
        {
            // CampfireManager에 등록
            CampfireManager.Instance?.RegisterCampfire(this);
            _isRegistered = true;
        }
        
        private void OnDestroy()
        {
            if (_isRegistered && CampfireManager.Instance != null)
            {
                CampfireManager.Instance.UnregisterCampfire(this);
            }
        }
        
        private void Update()
        {
            if (State == CampfireState.Lit)
            {
                // 조명 깜빡임 (Particle Connector가 없을 때만)
                if (_particleConnector == null)
                {
                    UpdateLightFlicker();
                }
                
                // 시간 체크
                UpdateBurnTime();
            }
        }
        
        /// <summary>
        /// 조명 깜빡임 효과
        /// </summary>
        private void UpdateLightFlicker()
        {
            if (_campfireLight == null) return;
            
            _flickerTimer += Time.deltaTime * _flickerSpeed;
            float flicker = Mathf.Sin(_flickerTimer) * _flickerAmount;
            _campfireLight.intensity = _baseIntensity + flicker;
        }
        
        /// <summary>
        /// 연소 시간 업데이트
        /// </summary>
        private void UpdateBurnTime()
        {
            float deltaHours = Time.deltaTime * (Time.timeScale / 3600f); // 실제 시간을 게임 시간으로 변환
            // 또는 TimeManager 사용
            
            // TimeManager에서 시간 경과 체크
            RemainingTimeHours -= GetTimeDelta();
            
            if (RemainingTimeHours <= 0)
            {
                BurnOut();
            }
        }
        
        /// <summary>
        /// 시간 변화량 가져오기 (TimeManager 연동)
        /// </summary>
        private float GetTimeDelta()
        {
            // CampfireManager에서 시간 업데이트를 받음
            return 0f; // Manager에서 처리
        }
        
        /// <summary>
        /// 외부에서 시간 업데이트 (CampfireManager에서 호출)
        /// </summary>
        public void UpdateTime(float hoursPassed)
        {
            if (State == CampfireState.Lit)
            {
                RemainingTimeHours -= hoursPassed;
                
                if (RemainingTimeHours <= 0)
                {
                    BurnOut();
                }
            }
        }
        
        /// <summary>
        /// 상호작용 - 점화 시도
        /// </summary>
        public void Interact()
        {
            if (State == CampfireState.Unlit || State == CampfireState.BurnedOut)
            {
                TryLight();
            }
        }
        
        /// <summary>
        /// 점화 시도
        /// </summary>
        private void TryLight()
        {
            // 인벤토리에서 나무 2개 체크
            var inventory = FindObjectOfType<InventorySystem>();
            if (inventory == null)
            {
                Debug.Log("[Campfire] 인벤토리를 찾을 수 없습니다.");
                return;
            }
            
            // 나무 아이템 ID 확인 (프로젝트 설정에 따라 조정)
            string woodItemId = "wood";
            
            if (inventory.CountItem(woodItemId) < _woodCost)
            {
                Debug.Log($"[Campfire] 나무 {_woodCost}개가 필요합니다.");
                return;
            }
            
            // 나무 소모
            if (inventory.RemoveItem(woodItemId, _woodCost))
            {
                LightFire();
            }
        }
        
        /// <summary>
        /// 불 켜기
        /// </summary>
        private void LightFire()
        {
            SetState(CampfireState.Lit);
            RemainingTimeHours = _burnDurationHours;
            
            // Pool에서 Fire 생성
            SpawnFireFromPool();
            
            Debug.Log("[Campfire] 모닥불에 불을 붙였습니다!");
            
            Debug.Log($"[Campfire] Fire lit! Duration: {_burnDurationHours} hours");
        }
        
        /// <summary>
        /// 다 타서 꺼짐 - Base는 유지, Fire만 Pool로 반환
        /// </summary>
        private void BurnOut()
        {
            SetState(CampfireState.BurnedOut);
            
            // Pool에서 Fire 반환
            ReturnFireToPool();
            
            Debug.Log("[Campfire] 모닥불이 다 타서 꺼졌습니다. 다시 불을 붙일 수 있습니다.");
            
            Debug.Log("[Campfire] Burned out - Base remains, Fire returned to pool");
        }
        
        /// <summary>
        /// Pool에서 Fire 가져오기
        /// </summary>
        private void SpawnFireFromPool()
        {
            if (PoolManager.Instance == null)
            {
                Debug.LogError("[Campfire] PoolManager not found");
                return;
            }
            
            Vector3 spawnPos = _fireSpawnPoint != null ? _fireSpawnPoint.position : transform.position + Vector3.up * 0.2f;
            _currentFireObject = PoolManager.Instance.Spawn(_firePoolName, spawnPos, Quaternion.identity);
            
            if (_currentFireObject != null)
            {
                _currentFireObject.transform.SetParent(transform);
                // 불꽃 스케일 적용
                _currentFireObject.transform.localScale = _fireScale;
                Debug.Log("[Campfire] Fire spawned from pool and scale applied");
            }
        }
        
        /// <summary>
        /// Fire를 Pool로 반환
        /// </summary>
        private void ReturnFireToPool()
        {
            if (_currentFireObject != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(_firePoolName, _currentFireObject);
                _currentFireObject = null;
                Debug.Log("[Campfire] Fire returned to pool");
            }
        }
        
        /// <summary>
        /// 상태 설정
        /// </summary>
        private void SetState(CampfireState newState)
        {
            State = newState;
            
            // Particle Connector가 있으면 우선 사용, 없으면 기본 파티클 사용
            if (_particleConnector != null)
            {
                switch (State)
                {
                    case CampfireState.Unlit:
                    case CampfireState.BurnedOut:
                        _particleConnector.StopAllEffects();
                        if (_campfireLight != null) _campfireLight.intensity = 0f;
                        break;
                        
                    case CampfireState.Lit:
                        _particleConnector.StartAllEffects();
                        if (_campfireLight != null) _campfireLight.intensity = _lightIntensity;
                        break;
                }
            }
            else
            {
                switch (State)
                {
                    case CampfireState.Unlit:
                        _campfireParticles?.StopFire();
                        if (_campfireLight != null) _campfireLight.intensity = 0f;
                        break;
                        
                    case CampfireState.Lit:
                        _campfireParticles?.StartFire();
                        if (_campfireLight != null) _campfireLight.intensity = _lightIntensity;
                        break;
                        
                    case CampfireState.BurnedOut:
                        _campfireParticles?.StopFire();
                        if (_campfireLight != null) _campfireLight.intensity = 0f;
                        break;
                }
            }
        }
        
        /// <summary>
        /// 저장 데이터
        /// </summary>
        public object GetSaveData()
        {
            return new CampfireSaveData
            {
                State = State,
                RemainingTime = RemainingTimeHours,
                Position = transform.position
            };
        }
        
        /// <summary>
        /// 저장 데이터 로드
        /// </summary>
        public void LoadSaveData(object state)
        {
            if (state is CampfireSaveData data)
            {
                transform.position = data.Position;
                RemainingTimeHours = data.RemainingTime;
                SetState(data.State);
            }
        }
        
        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        public bool CanInteract()
        {
            return State == CampfireState.Unlit || State == CampfireState.BurnedOut;
        }
        
        /// <summary>
        /// 상호작용 힌트 텍스트
        /// </summary>
        public string GetInteractionHint()
        {
            if (State == CampfireState.Unlit || State == CampfireState.BurnedOut)
            {
                return $"모닥불에 불 붙이기 (나무 {_woodCost}개 필요)";
            }
            return "";
        }
    }
    
    /// <summary>
    /// Campfire 저장 데이터
    /// </summary>
    [Serializable]
    public class CampfireSaveData
    {
        public Campfire.CampfireState State;
        public float RemainingTime;
        public Vector3 Position;
    }
}
