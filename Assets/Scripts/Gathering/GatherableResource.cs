using System;
using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Events;
using SunnysideIsland.GameData;

namespace SunnysideIsland.Gathering
{
    /// <summary>
    /// 자원 타입
    /// </summary>
    public enum ResourceType
    {
        Tree,
        Rock,
        Herb
    }

    /// <summary>
    /// 채집 가능한 자원 기본 클래스
    /// </summary>
    public abstract class GatherableResource : MonoBehaviour, ISaveable
    {
        [Header("=== Settings ===")]
        [SerializeField] protected string _resourceId;
        [SerializeField] protected ResourceType _type;
        [SerializeField] protected int _health = 100;
        [SerializeField] protected int _gatherAmount = 1;
        [SerializeField] protected string _dropItemId;
        [SerializeField] protected int _regenDays = 3;
        
        public string ResourceId => _resourceId;
        public ResourceType Type => _type;
        public bool IsAvailable => gameObject.activeSelf && _currentHealth > 0;
        
        protected int _currentHealth;
        protected int _daysSinceDepleted;
        protected bool _isDepleted;
        
        protected virtual void Awake()
        {
            _currentHealth = _health;
        }
        
        public abstract bool CanGather(ToolType equippedTool);
        
        public virtual bool Gather(int damage)
        {
            if (!IsAvailable) return false;
            
            _currentHealth -= damage;
            
            if (_currentHealth <= 0)
            {
                OnResourceDepleted();
                return true;
            }
            
            return false;
        }
        
        public virtual void OnResourceDepleted()
        {
            _isDepleted = true;
            _daysSinceDepleted = 0;
            
            EventBus.Publish(new ResourceGatheredEvent
            {
                ResourceId = _resourceId,
                Type = _type,
                Amount = _gatherAmount
            });
            
            gameObject.SetActive(false);
        }
        
        public virtual void Respawn()
        {
            _isDepleted = false;
            _currentHealth = _health;
            gameObject.SetActive(true);
            
            EventBus.Publish(new ResourceRespawnedEvent
            {
                ResourceId = _resourceId
            });
        }
        
        public void OnDayPassed()
        {
            if (_isDepleted)
            {
                _daysSinceDepleted++;
                
                if (_daysSinceDepleted >= _regenDays)
                {
                    Respawn();
                }
            }
        }
        
        public string SaveKey => $"Resource_{_resourceId}_{gameObject.GetInstanceID()}";
        
        public object GetSaveData()
        {
            return new ResourceSaveData
            {
                ResourceId = _resourceId,
                IsDepleted = _isDepleted,
                DaysSinceDepleted = _daysSinceDepleted,
                CurrentHealth = _currentHealth
            };
        }
        
        public void LoadSaveData(object state)
        {
            if (state is ResourceSaveData data)
            {
                _isDepleted = data.IsDepleted;
                _daysSinceDepleted = data.DaysSinceDepleted;
                _currentHealth = data.CurrentHealth;
                
                gameObject.SetActive(!_isDepleted);
            }
        }
    }
    
    /// <summary>
    /// 나무 자원
    /// </summary>
    public class TreeResource : GatherableResource
    {
        [Header("=== Tree Settings ===")]
        [SerializeField] private int _woodAmount = 3;
        [SerializeField] private string _woodItemId = "wood";
        
        protected override void Awake()
        {
            base.Awake();
            _type = ResourceType.Tree;
        }
        
        public override bool CanGather(ToolType equippedTool)
        {
            return equippedTool == ToolType.Axe;
        }
        
        public override void OnResourceDepleted()
        {
            _gatherAmount = _woodAmount;
            _dropItemId = _woodItemId;
            base.OnResourceDepleted();
        }
    }
    
    /// <summary>
    /// 광석 자원
    /// </summary>
    public class RockResource : GatherableResource
    {
        [Header("=== Rock Settings ===")]
        [SerializeField] private int _oreAmount = 2;
        [SerializeField] private string _oreItemId = "ore";
        
        protected override void Awake()
        {
            base.Awake();
            _type = ResourceType.Rock;
        }
        
        public override bool CanGather(ToolType equippedTool)
        {
            return equippedTool == ToolType.Pickaxe;
        }
        
        public override void OnResourceDepleted()
        {
            _gatherAmount = _oreAmount;
            _dropItemId = _oreItemId;
            base.OnResourceDepleted();
        }
    }
    
    /// <summary>
    /// 약초 자원
    /// </summary>
    public class HerbResource : GatherableResource
    {
        [Header("=== Herb Settings ===")]
        [SerializeField] private int _herbAmount = 1;
        [SerializeField] private string _herbItemId = "herb";
        
        protected override void Awake()
        {
            base.Awake();
            _type = ResourceType.Herb;
        }
        
        public override bool CanGather(ToolType equippedTool)
        {
            // 약초는 도구 필요 없음
            return true;
        }
        
        public override void OnResourceDepleted()
        {
            _gatherAmount = _herbAmount;
            _dropItemId = _herbItemId;
            base.OnResourceDepleted();
        }
    }
    
    /// <summary>
    /// 채집 관리자
    /// </summary>
    public class GatheringManager : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private List<GatherableResource> _resources = new List<GatherableResource>();
        
        private void Start()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            
            if (_resources.Count == 0)
            {
                FindAllResources();
            }
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
        }
        
        private void FindAllResources()
        {
            _resources.Clear();
            _resources.AddRange(FindObjectsOfType<GatherableResource>());
        }
        
        private void OnDayStarted(DayStartedEvent evt)
        {
            foreach (var resource in _resources)
            {
                if (resource != null)
                {
                    resource.OnDayPassed();
                }
            }
        }
    }
    
    [Serializable]
    public class ResourceSaveData
    {
        public string ResourceId;
        public bool IsDepleted;
        public int DaysSinceDepleted;
        public int CurrentHealth;
    }
    
    /// <summary>
    /// 자원 채집 이벤트
    /// </summary>
    public class ResourceGatheredEvent
    {
        public string ResourceId { get; set; }
        public ResourceType Type { get; set; }
        public int Amount { get; set; }
    }
    
    /// <summary>
    /// 자원 재생성 이벤트
    /// </summary>
    public class ResourceRespawnedEvent
    {
        public string ResourceId { get; set; }
    }
}
