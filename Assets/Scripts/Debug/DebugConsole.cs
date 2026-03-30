using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Core;
using SunnysideIsland.Inventory;
using SunnysideIsland.Player;
using SunnysideIsland.Survival;

namespace SunnysideIsland.Cheats
{
    public class DebugConsole : MonoBehaviour
    {
        [Header("=== UI References ===")]
        [SerializeField] private GameObject _consolePanel;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TextMeshProUGUI _outputText;
        [SerializeField] private int _maxOutputLines = 100;
        
        [Header("=== Settings ===")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.BackQuote;
        [SerializeField] private bool _showOnStart = false;
        
        private readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        private readonly List<string> _outputHistory = new List<string>();
        private readonly List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        
        [Inject]
        private GameManager _gameManager;
        [Inject]
        private InventorySystem _inventory;
        [Inject]
        private TimeManager _timeManager;
        
        private bool _isVisible = false;
        
        private void Awake()
        {
            RegisterCommands();
            
            if (_consolePanel != null)
            {
                _consolePanel.SetActive(_showOnStart);
                _isVisible = _showOnStart;
            }
        }
        
        private void Start()
        {
            if (_inputField != null)
            {
                _inputField.onSubmit.AddListener(OnCommandSubmitted);
            }
            
            Log("Debug Console initialized. Type 'help' for commands.");
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleConsole();
            }
            
            if (_isVisible)
            {
                HandleHistoryNavigation();
            }
        }
        
        private void RegisterCommands()
        {
            _commands["help"] = CmdHelp;
            _commands["clear"] = CmdClear;
            _commands["additem"] = CmdAddItem;
            _commands["addgold"] = CmdAddGold;
            _commands["settime"] = CmdSetTime;
            _commands["setday"] = CmdSetDay;
            _commands["god"] = CmdGodMode;
            _commands["heal"] = CmdHeal;
            _commands["teleport"] = CmdTeleport;
            _commands["spawn"] = CmdSpawn;
            _commands["timescale"] = CmdTimeScale;
            _commands["save"] = CmdSave;
            _commands["load"] = CmdLoad;
            _commands["clearsave"] = CmdClearSave;
            _commands["stats"] = CmdStats;
            _commands["quest"] = CmdQuest;
            _commands["unlock"] = CmdUnlock;
        }
        
        private void ToggleConsole()
        {
            _isVisible = !_isVisible;
            
            if (_consolePanel != null)
            {
                _consolePanel.SetActive(_isVisible);
            }
            
            if (_isVisible && _inputField != null)
            {
                _inputField.ActivateInputField();
            }
            
            if (_isVisible)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }
        
        private void HandleHistoryNavigation()
        {
            if (_commandHistory.Count == 0) return;
            
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _historyIndex = Mathf.Min(_historyIndex + 1, _commandHistory.Count - 1);
                if (_historyIndex >= 0 && _historyIndex < _commandHistory.Count)
                {
                    _inputField.text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _historyIndex = Mathf.Max(_historyIndex - 1, -1);
                if (_historyIndex >= 0)
                {
                    _inputField.text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                }
                else
                {
                    _inputField.text = "";
                }
            }
        }
        
        private void OnCommandSubmitted(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            
            _commandHistory.Add(input);
            _historyIndex = -1;
            
            Log($"> {input}");
            
            ProcessCommand(input);
            
            _inputField.text = "";
            _inputField.ActivateInputField();
        }
        
        private void ProcessCommand(string input)
        {
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;
            
            string command = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            
            if (_commands.TryGetValue(command, out var cmd))
            {
                try
                {
                    cmd(args);
                }
                catch (Exception e)
                {
                    LogError($"Error executing command: {e.Message}");
                }
            }
            else
            {
                LogError($"Unknown command: {command}. Type 'help' for available commands.");
            }
        }
        
        private void Log(string message)
        {
            _outputHistory.Add(message);
            
            if (_outputHistory.Count > _maxOutputLines)
            {
                _outputHistory.RemoveAt(0);
            }
            
            UpdateOutput();
        }
        
        private void LogError(string message)
        {
            Log($"<color=red>{message}</color>");
        }
        
        private void LogSuccess(string message)
        {
            Log($"<color=green>{message}</color>");
        }
        
        private void UpdateOutput()
        {
            if (_outputText != null)
            {
                _outputText.text = string.Join("\n", _outputHistory);
            }
        }
        
        #region Commands
        
        private void CmdHelp(string[] args)
        {
            Log("Available commands:");
            Log("  help - Show this help");
            Log("  clear - Clear console output");
            Log("  additem <id> <amount> - Add item to inventory");
            Log("  addgold <amount> - Add gold");
            Log("  settime <hour> - Set current time (0-24)");
            Log("  setday <day> - Set current day");
            Log("  god - Toggle god mode");
            Log("  heal - Restore all stats");
            Log("  teleport <x> <y> - Teleport player");
            Log("  spawn <prefab> - Spawn prefab at player");
            Log("  timescale <scale> - Set time scale");
            Log("  save - Save game");
            Log("  load - Load game");
            Log("  clearsave - Clear save data");
            Log("  stats - Show player stats");
            Log("  quest <list|complete> - Quest commands");
            Log("  unlock <all|building> - Unlock items");
        }
        
        private void CmdClear(string[] args)
        {
            _outputHistory.Clear();
            UpdateOutput();
        }
        
        private void CmdAddItem(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: additem <id> <amount>");
                return;
            }
            
            string itemId = args[0];
            if (!int.TryParse(args[1], out int amount))
            {
                LogError("Invalid amount");
                return;
            }
            
            _inventory?.AddItem(itemId, amount);
            LogSuccess($"Added {amount}x {itemId}");
        }
        
        private void CmdAddGold(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: addgold <amount>");
                return;
            }
            
            if (!int.TryParse(args[0], out int amount))
            {
                LogError("Invalid amount");
                return;
            }
            
            LogSuccess("Gold system disabled");
        }
        
        private void CmdSetTime(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: settime <hour>");
                return;
            }
            
            if (!int.TryParse(args[0], out int hour))
            {
                LogError("Invalid hour");
                return;
            }
            
            _timeManager?.Initialize(_timeManager.CurrentDay, hour, 0);
            LogSuccess($"Time set to {hour}:00");
        }
        
        private void CmdSetDay(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: setday <day>");
                return;
            }
            
            if (!int.TryParse(args[0], out int day))
            {
                LogError("Invalid day");
                return;
            }
            
            _timeManager?.Initialize(day, _timeManager.CurrentHour, _timeManager.CurrentMinute);
            LogSuccess($"Day set to {day}");
        }
        
        private void CmdGodMode(string[] args)
        {
            LogSuccess("God mode toggled (not implemented)");
        }
        
        private void CmdHeal(string[] args)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                LogError("Player not found");
                return;
            }
            
            var health = player.GetComponent<HealthSystem>();
            var hunger = player.GetComponent<HungerSystem>();
            var stamina = player.GetComponent<StaminaSystem>();
            
            health?.Heal(999, "Debug");
            hunger?.SetValue(hunger.MaxValue);
            stamina?.Reset();
            
            LogSuccess("Player fully healed");
        }
        
        private void CmdTeleport(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: teleport <x> <y>");
                return;
            }
            
            if (!float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y))
            {
                LogError("Invalid coordinates");
                return;
            }
            
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.transform.position = new Vector3(x, y, 0);
                LogSuccess($"Teleported to ({x}, {y})");
            }
        }
        
        private void CmdSpawn(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: spawn <prefab_name>");
                return;
            }
            
            Log($"Spawning {args[0]}... (not implemented)");
        }
        
        private void CmdTimeScale(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: timescale <scale>");
                return;
            }
            
            if (!float.TryParse(args[0], out float scale))
            {
                LogError("Invalid scale");
                return;
            }
            
            Time.timeScale = scale;
            LogSuccess($"Time scale set to {scale}");
        }
        
        private void CmdSave(string[] args)
        {
            _gameManager?.SaveGame();
            LogSuccess("Game saved");
        }
        
        private void CmdLoad(string[] args)
        {
            _gameManager?.LoadGame("default");
            LogSuccess("Game loaded");
        }
        
        private void CmdClearSave(string[] args)
        {
            PlayerPrefs.DeleteAll();
            LogSuccess("Save data cleared");
        }
        
        private void CmdStats(string[] args)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                LogError("Player not found");
                return;
            }
            
            var health = player.GetComponent<HealthSystem>();
            var hunger = player.GetComponent<HungerSystem>();
            var stamina = player.GetComponent<StaminaSystem>();
            
            Log("=== Player Stats ===");
            Log($"Position: {player.transform.position}");
            Log($"Health: {health?.CurrentValue ?? 0}/{health?.MaxValue ?? 0}");
            Log($"Hunger: {hunger?.CurrentValue ?? 0}/{hunger?.MaxValue ?? 0}");
            Log($"Stamina: {stamina?.CurrentValue ?? 0}/{stamina?.MaxValue ?? 0}");
            Log($"Gold: 0");
        }
        
        private void CmdQuest(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: quest <list|complete>");
                return;
            }
            
            switch (args[0].ToLower())
            {
                case "list":
                    Log("Quest list: (not implemented)");
                    break;
                case "complete":
                    if (args.Length < 2)
                    {
                        LogError("Usage: quest complete <quest_id>");
                        return;
                    }
                    Log($"Completing quest {args[1]}... (not implemented)");
                    break;
            }
        }
        
        private void CmdUnlock(string[] args)
        {
            if (args.Length < 1)
            {
                LogError("Usage: unlock <all|building>");
                return;
            }
            
            Log($"Unlocking {args[0]}... (not implemented)");
        }
        
        #endregion
    }
}