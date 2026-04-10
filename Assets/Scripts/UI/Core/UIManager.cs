using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.UI.Crafting;
using SunnysideIsland.UI.Menu;
using UnityEventSystem = UnityEngine.EventSystems.EventSystem;

namespace SunnysideIsland.UI
{
    public interface IUIManager
    {
        T GetPanel<T>() where T : UIPanel;
        void OpenPanel<T>() where T : UIPanel;
        void ClosePanel<T>() where T : UIPanel;
        void CloseAllPanels();
        void CloseTopPanel();
        bool HasOpenPanels { get; }
    }

    [DefaultExecutionOrder(-50)]
    public class UIManager : MonoBehaviour, IUIManager
    {
        public static UIManager Instance { get; private set; }
        
        [Header("=== Panels ===")]
        [SerializeField] private List<UIPanel> _panels = new List<UIPanel>();
        
        [Header("=== Settings ===")]
        [SerializeField] private bool _usePanelStack = true;
        [SerializeField] private bool _closeAllOnSceneChange = true;
        
        private readonly Stack<UIPanel> _panelStack = new Stack<UIPanel>();
        private readonly Dictionary<Type, UIPanel> _panelDictionary = new Dictionary<Type, UIPanel>();
        
        public bool HasOpenPanels => _panelStack.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("[UIManager]");
                Instance = go.AddComponent<UIManager>();
                DontDestroyOnLoad(go);
            }
        }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // DI Container가 초기화되지 않았으면 초기화
            if (DIContainer.Global == null)
            {
                DIContainer.InitializeGlobal();
            }
            DIContainer.Global.RegisterInstance<IUIManager>(this);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            InitializePanels();
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[UIManager] Scene loaded: {scene.name} - Reinitializing panels");

            if (_closeAllOnSceneChange)
            {
                _panelStack.Clear();
                _panels.RemoveAll(panel => panel == null);
            }

            _panelDictionary.Clear();
            InitializePanels();

            var scenePanels = FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var panel in scenePanels)
            {
                if (panel == null)
                {
                    continue;
                }

                // 씬 시작 시 기본으로 열려야 하는 패널 (MainMenuPanel 등)은 숨기지 않음
                if (panel.IsOpen)
                {
                    Debug.Log($"[UIManager] Keeping panel open: {panel.GetType().Name}");
                    var type = panel.GetType();
                    if (!_panelDictionary.ContainsKey(type))
                    {
                        _panelDictionary[type] = panel;
                    }
                    if (!_panels.Contains(panel))
                    {
                        _panels.Add(panel);
                    }
                    continue;
                }

                if (panel.IsModal)
                {
                    panel.ForceHide();
                }

                var panelType = panel.GetType();
                if (!_panelDictionary.ContainsKey(panelType))
                {
                    _panelDictionary[panelType] = panel;
                }
                if (!_panels.Contains(panel))
                {
                    _panels.Add(panel);
                }
            }

            if (UnityEventSystem.current != null)
            {
                UnityEventSystem.current.SetSelectedGameObject(null);
            }
        }
        
        private void InitializePanels()
        {
            _panelDictionary.Clear();
            
            foreach (var panel in _panels)
            {
                if (panel != null)
                {
                    var type = panel.GetType();
                    if (!_panelDictionary.ContainsKey(type))
                    {
                        _panelDictionary[type] = panel;
                        // 이미 열려있는 패널은 비활성화하지 않음
                        if (!panel.IsOpen)
                        {
                            panel.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
        
        public T GetPanel<T>() where T : UIPanel
        {
            var type = typeof(T);
            if (_panelDictionary.TryGetValue(type, out var panel))
            {
                return panel as T;
            }
            
            var found = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (found != null)
            {
                _panelDictionary[type] = found;
                if (!_panels.Contains(found))
                    _panels.Add(found);
                return found;
            }
            
            Debug.LogWarning($"[UIManager] Panel {typeof(T).Name} not found!");
            return null;
        }
        
        public void OpenPanel<T>() where T : UIPanel
        {
            var panel = GetPanel<T>();
            if (panel != null)
            {
                OpenPanel(panel);
            }
        }
        
        public void OpenPanel(UIPanel panel)
        {
            if (panel == null || panel.IsOpen) return;
            
            if (_usePanelStack)
            {
                if (panel.IsModal && _panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
                    if (topPanel != panel)
                    {
                        topPanel.Close();
                    }
                }
                
                _panelStack.Push(panel);
            }

            panel.transform.SetAsLastSibling();
            
            panel.Open();
            
            EventBus.Publish(new UIPanelOpenedEvent
            {
                PanelType = panel.GetType().Name,
                IsModal = panel.IsModal
            });
        }
        
        public void ClosePanel<T>() where T : UIPanel
        {
            var panel = GetPanel<T>();
            if (panel != null)
            {
                ClosePanel(panel);
            }
        }
        
        public void ClosePanel(UIPanel panel)
        {
            if (panel == null || !panel.IsOpen) return;
            
            if (_usePanelStack && _panelStack.Count > 0)
            {
                var tempStack = new Stack<UIPanel>();
                while (_panelStack.Count > 0)
                {
                    var current = _panelStack.Pop();
                    if (current != panel)
                    {
                        tempStack.Push(current);
                    }
                    else
                    {
                        break;
                    }
                }
                
                while (tempStack.Count > 0)
                {
                    _panelStack.Push(tempStack.Pop());
                }
            }
            
            panel.Close();
            
            EventBus.Publish(new UIPanelClosedEvent
            {
                PanelType = panel.GetType().Name
            });
        }
        
        public void CloseTopPanel()
        {
            if (_usePanelStack && _panelStack.Count > 0)
            {
                var panel = _panelStack.Pop();
                panel.Close();
                
                EventBus.Publish(new UIPanelClosedEvent
                {
                    PanelType = panel.GetType().Name
                });
            }
        }
        
        public void CloseAllPanels()
        {
            if (_usePanelStack)
            {
                while (_panelStack.Count > 0)
                {
                    var panel = _panelStack.Pop();
                    panel.Close();
                }
            }
            else
            {
                foreach (var panel in _panels)
                {
                    if (panel != null && panel.IsOpen)
                    {
                        panel.Close();
                    }
                }
            }
        }
        
        private void Update()
        {
            if (SunnysideIsland.Core.GameManager.Instance != null)
            {
                var gameState = SunnysideIsland.Core.GameManager.Instance.CurrentState;
                if (gameState == SunnysideIsland.Core.GameState.Loading
                    || gameState == SunnysideIsland.Core.GameState.GameOver)
                {
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_usePanelStack && _panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
                    topPanel.OnBackButton();
                }
                else
                {
                    var saveLoadPanel = GetPanel<SunnysideIsland.UI.Menu.SaveLoadPanel>();
                    if (saveLoadPanel != null)
                    {
                        OpenPanel(saveLoadPanel);
                    }
                }
            }
        }
        
        public void RegisterPanel(UIPanel panel)
        {
            if (panel == null) return;
            
            var type = panel.GetType();
            if (!_panelDictionary.ContainsKey(type))
            {
                _panelDictionary[type] = panel;
                _panels.Add(panel);
            }
        }
        
        public void OpenCraftingPanel()
        {
            var craftingPanel = GetPanel<SunnysideIsland.UI.Crafting.CraftingPanel>();
            if (craftingPanel != null)
            {
                OpenPanel(craftingPanel);
            }
        }
        
        public void UnregisterPanel(UIPanel panel)
        {
            if (panel == null) return;
            
            var type = panel.GetType();
            _panelDictionary.Remove(type);
            _panels.Remove(panel);
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public class UIPanelOpenedEvent
    {
        public string PanelType { get; set; }
        public bool IsModal { get; set; }
    }

    public class UIPanelClosedEvent
    {
        public string PanelType { get; set; }
    }
}
