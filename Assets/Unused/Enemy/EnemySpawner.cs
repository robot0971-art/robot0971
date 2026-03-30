using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Enemy
{
    /// <summary>
    /// 적 스포너
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("=== Spawn Settings ===")]
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _maxEnemies = 5;
        [SerializeField] private float _spawnInterval = 10f;
        [SerializeField] private float _spawnRadius = 5f;
        
        private List<GameObject> _spawnedEnemies = new List<GameObject>();
        private float _spawnTimer;
        
        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            
            if (_spawnTimer >= _spawnInterval)
            {
                TrySpawn();
                _spawnTimer = 0f;
            }
        }
        
        private void TrySpawn()
        {
            if (_enemyPrefab == null) return;
            
            // 죽은 적 제거
            _spawnedEnemies.RemoveAll(e => e == null);
            
            if (_spawnedEnemies.Count >= _maxEnemies) return;
            
            Vector3 spawnPosition = GetRandomSpawnPosition();
            GameObject enemy = Instantiate(_enemyPrefab, spawnPosition, Quaternion.identity);
            _spawnedEnemies.Add(enemy);
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
            return transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }
    }
}
