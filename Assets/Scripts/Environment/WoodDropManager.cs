using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Events;
using SunnysideIsland.Inventory;

namespace SunnysideIsland.Environment
{
    public class WoodDropManager : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private GameObject _woodPrefab;
        [SerializeField] private float _dropRadius = 0.5f;
        
        private IInventorySystem _inventorySystem;
        
        private void Start()
        {
            _inventorySystem = GetComponent<IInventorySystem>();
            if (_inventorySystem == null)
            {
                _inventorySystem = FindObjectOfType<InventorySystem>();
            }
            
            EventBus.Subscribe<TreeChoppedEvent>(OnTreeChopped);
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<TreeChoppedEvent>(OnTreeChopped);
        }
        
        private void OnTreeChopped(TreeChoppedEvent evt)
        {
            // 바닥에 Wood 아이템만 생성 (인벤토리 추가는 PickableItem이 처리)
            if (_woodPrefab != null)
            {
                for (int i = 0; i < evt.WoodAmount; i++)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * _dropRadius;
                    Vector3 spawnPos = evt.TreePosition + new Vector3(randomCircle.x, randomCircle.y, 0);
                    Instantiate(_woodPrefab, spawnPos, Quaternion.identity);
                }
                
                Debug.Log($"[WoodDropManager] Dropped {evt.WoodAmount} wood on ground");
            }
        }
    }
}