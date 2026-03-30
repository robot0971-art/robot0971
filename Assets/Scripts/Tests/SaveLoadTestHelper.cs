using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.Player;

namespace SunnysideIsland.Tests
{
    public class SaveLoadTestHelper : MonoBehaviour
    {
        [Header("=== Test Settings ===")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private string _testSaveName = "test_save";
        
        private List<TestResult> _results = new List<TestResult>();
        
        public struct TestResult
        {
            public string TestName;
            public bool Passed;
            public string Message;
        }
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        public void RunAllTests()
        {
            _results.Clear();
            
            TestSaveSystemExists();
            TestPlayerSaveLoad();
            TestInventorySaveLoad();
            TestTimeSaveLoad();
            TestQuestSaveLoad();
            TestBuildingSaveLoad();
            
            PrintResults();
        }
        
        private void TestSaveSystemExists()
        {
            var saveSystem = FindObjectOfType<SaveSystem>();
            if (saveSystem != null)
            {
                Pass("SaveSystemExists", "SaveSystem found in scene");
            }
            else
            {
                Fail("SaveSystemExists", "SaveSystem not found in scene");
            }
        }
        
        private void TestPlayerSaveLoad()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Skip("PlayerSaveLoad", "PlayerController not found");
                return;
            }
            
            if (player is ISaveable saveable)
            {
                try
                {
                    var data = saveable.GetSaveData();
                    saveable.LoadSaveData(data);
                    Pass("PlayerSaveLoad", "Player save/load completed");
                }
                catch (System.Exception e)
                {
                    Fail("PlayerSaveLoad", $"Exception: {e.Message}");
                }
            }
            else
            {
                Fail("PlayerSaveLoad", "PlayerController does not implement ISaveable");
            }
        }
        
        private void TestInventorySaveLoad()
        {
            var inventory = FindObjectOfType<Inventory.InventorySystem>();
            if (inventory == null)
            {
                Skip("InventorySaveLoad", "InventorySystem not found");
                return;
            }
            
            if (inventory is ISaveable saveable)
            {
                try
                {
                    var data = saveable.GetSaveData();
                    saveable.LoadSaveData(data);
                    Pass("InventorySaveLoad", "Inventory save/load completed");
                }
                catch (System.Exception e)
                {
                    Fail("InventorySaveLoad", $"Exception: {e.Message}");
                }
            }
            else
            {
                Fail("InventorySaveLoad", "InventorySystem does not implement ISaveable");
            }
        }
        
        private void TestTimeSaveLoad()
        {
            var timeManager = FindObjectOfType<TimeManager>();
            if (timeManager == null)
            {
                Skip("TimeSaveLoad", "TimeManager not found");
                return;
            }
            
            if (timeManager is ISaveable saveable)
            {
                try
                {
                    var data = saveable.GetSaveData();
                    saveable.LoadSaveData(data);
                    Pass("TimeSaveLoad", "Time save/load completed");
                }
                catch (System.Exception e)
                {
                    Fail("TimeSaveLoad", $"Exception: {e.Message}");
                }
            }
            else
            {
                Fail("TimeSaveLoad", "TimeManager does not implement ISaveable");
            }
        }
        
        private void TestQuestSaveLoad()
        {
            var questSystem = FindObjectOfType<Quest.QuestSystem>();
            if (questSystem == null)
            {
                Skip("QuestSaveLoad", "QuestSystem not found");
                return;
            }
            
            if (questSystem is ISaveable saveable)
            {
                try
                {
                    var data = saveable.GetSaveData();
                    saveable.LoadSaveData(data);
                    Pass("QuestSaveLoad", "Quest save/load completed");
                }
                catch (System.Exception e)
                {
                    Fail("QuestSaveLoad", $"Exception: {e.Message}");
                }
            }
            else
            {
                Fail("QuestSaveLoad", "QuestSystem does not implement ISaveable");
            }
        }
        
        private void TestBuildingSaveLoad()
        {
            var buildingManager = FindObjectOfType<Building.BuildingManager>();
            if (buildingManager == null)
            {
                Skip("BuildingSaveLoad", "BuildingManager not found");
                return;
            }
            
            if (buildingManager is ISaveable saveable)
            {
                try
                {
                    var data = saveable.GetSaveData();
                    saveable.LoadSaveData(data);
                    Pass("BuildingSaveLoad", "Building save/load completed");
                }
                catch (System.Exception e)
                {
                    Fail("BuildingSaveLoad", $"Exception: {e.Message}");
                }
            }
            else
            {
                Fail("BuildingSaveLoad", "BuildingManager does not implement ISaveable");
            }
        }
        
        private void Pass(string testName, string message)
        {
            _results.Add(new TestResult { TestName = testName, Passed = true, Message = message });
        }
        
        private void Fail(string testName, string message)
        {
            _results.Add(new TestResult { TestName = testName, Passed = false, Message = message });
        }
        
        private void Skip(string testName, string message)
        {
            _results.Add(new TestResult { TestName = testName, Passed = true, Message = $"SKIPPED: {message}" });
        }
        
        private void PrintResults()
        {
            int passed = 0;
            int failed = 0;
            
            Debug.Log("=== Save/Load Test Results ===");
            
            foreach (var result in _results)
            {
                string status = result.Passed ? "[PASS]" : "[FAIL]";
                Debug.Log($"{status} {result.TestName}: {result.Message}");
                
                if (result.Passed) passed++;
                else failed++;
            }
            
            Debug.Log($"=== Total: {passed} passed, {failed} failed ===");
        }
        
        public List<TestResult> GetResults()
        {
            return new List<TestResult>(_results);
        }
    }
}