using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DI;
using SunnysideIsland.Events;
using SunnysideIsland.UI.Crafting;

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
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePanels();
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
                        panel.gameObject.SetActive(false);
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_usePanelStack && _panelStack.Count > 0)
                {
                    var topPanel = _panelStack.Peek();
                    topPanel.OnBackButton();
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