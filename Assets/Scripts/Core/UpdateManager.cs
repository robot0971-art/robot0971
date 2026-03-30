using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Core
{
    public interface IOptimizedUpdate
    {
        void OptimizedUpdate(float deltaTime);
        int UpdatePriority { get; }
    }
    
    public class UpdateManager : MonoBehaviour
    {
        private static UpdateManager _instance;
        public static UpdateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UpdateManager");
                    _instance = go.AddComponent<UpdateManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private readonly List<IOptimizedUpdate> _updateables = new List<IOptimizedUpdate>();
        private readonly List<IOptimizedUpdate> _pendingAdd = new List<IOptimizedUpdate>();
        private readonly List<IOptimizedUpdate> _pendingRemove = new List<IOptimizedUpdate>();
        
        private bool _isIterating = false;
        private float _fixedAccumulator = 0f;
        
        public void Register(IOptimizedUpdate updateable)
        {
            if (_isIterating)
            {
                _pendingAdd.Add(updateable);
            }
            else
            {
                AddUpdateable(updateable);
            }
        }
        
        public void Unregister(IOptimizedUpdate updateable)
        {
            if (_isIterating)
            {
                _pendingRemove.Add(updateable);
            }
            else
            {
                RemoveUpdateable(updateable);
            }
        }
        
        private void AddUpdateable(IOptimizedUpdate updateable)
        {
            int index = _updateables.BinarySearch(updateable, new UpdatePriorityComparer());
            if (index < 0) index = ~index;
            _updateables.Insert(index, updateable);
        }
        
        private void RemoveUpdateable(IOptimizedUpdate updateable)
        {
            _updateables.Remove(updateable);
        }
        
        private void ProcessPendingChanges()
        {
            foreach (var item in _pendingAdd)
            {
                AddUpdateable(item);
            }
            _pendingAdd.Clear();
            
            foreach (var item in _pendingRemove)
            {
                RemoveUpdateable(item);
            }
            _pendingRemove.Clear();
        }
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            _isIterating = true;
            
            for (int i = 0; i < _updateables.Count; i++)
            {
                _updateables[i].OptimizedUpdate(deltaTime);
            }
            
            _isIterating = false;
            
            ProcessPendingChanges();
        }
        
        public int GetUpdateableCount() => _updateables.Count;
        
        private class UpdatePriorityComparer : IComparer<IOptimizedUpdate>
        {
            public int Compare(IOptimizedUpdate x, IOptimizedUpdate y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;
                return x.UpdatePriority.CompareTo(y.UpdatePriority);
            }
        }
    }
}