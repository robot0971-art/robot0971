using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.Events;


namespace SunnysideIsland.UI.Dialogue
{
    public class DialogueChoice
    {
        public string Text;
        public string NextDialogueId;
        public System.Action OnSelected;
    }

    public class DialoguePanel : UIPanel
    {
        [Header("=== Speaker ===")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        
        [Header("=== Dialogue ===")]
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private float _typewriterSpeed = 0.03f;
        [SerializeField] private GameObject _continueIndicator;
        
        [Header("=== Choices ===")]
        [SerializeField] private Transform _choiceContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;
        
        [Header("=== Buttons ===")]
        [SerializeField] private Button _skipButton;
        

        
        private string _currentNPCId;
        private string _currentDialogue;
        private int _currentDialogueIndex;
        private List<string> _dialogueLines = new List<string>();
        private List<DialogueChoice> _currentChoices = new List<DialogueChoice>();
        private readonly List<GameObject> _choiceButtons = new List<GameObject>();
        private bool _isTypewriting = false;
        private bool _skipRequested = false;
        private Coroutine _typewriterCoroutine;
        
        public bool IsDialogueActive => _isOpen;
        
        protected override void Awake()
        {
            base.Awake();
            _isModal = true;
            
            _skipButton?.onClick.AddListener(OnSkipClicked);
            HideContinueIndicator();
        }
        
        protected override void OnOpened()
        {
            base.OnOpened();
        }
        
        protected override void OnClosed()
        {
            base.OnClosed();
            ClearChoices();
            HideContinueIndicator();
        }
        
        public void ShowDialogue(string npcId, string dialogue, List<DialogueChoice> choices = null)
        {
            _currentNPCId = npcId;
            _currentDialogue = dialogue;
            _currentChoices = choices ?? new List<DialogueChoice>();
            
            if (_speakerNameText != null)
            {
                _speakerNameText.text = "???";
            }
            
            ParseDialogueLines(dialogue);
            _currentDialogueIndex = 0;
            ShowCurrentLine();
            Open();
        }
        
        public void ShowDialogueWithLines(string npcId, List<string> lines, List<DialogueChoice> choices = null)
        {
            _currentNPCId = npcId;
            _dialogueLines = lines ?? new List<string>();
            _currentChoices = choices ?? new List<DialogueChoice>();
            
            if (_speakerNameText != null)
            {
                _speakerNameText.text = "???";
            }
            
            _currentDialogueIndex = 0;
            ShowCurrentLine();
            Open();
        }
        
        private void ParseDialogueLines(string dialogue)
        {
            _dialogueLines.Clear();
            
            if (string.IsNullOrEmpty(dialogue))
            {
                _dialogueLines.Add("");
                return;
            }
            
            var lines = dialogue.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    _dialogueLines.Add(trimmed);
                }
            }
            
            if (_dialogueLines.Count == 0)
            {
                _dialogueLines.Add(dialogue);
            }
        }
        
        private void ShowCurrentLine()
        {
            if (_currentDialogueIndex < 0 || _currentDialogueIndex >= _dialogueLines.Count)
            {
                ShowChoicesOrClose();
                return;
            }
            
            string line = _dialogueLines[_currentDialogueIndex];
            
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(line));
        }
        
        private System.Collections.IEnumerator TypewriterEffect(string text)
        {
            _isTypewriting = true;
            _skipRequested = false;
            HideContinueIndicator();
            ClearChoices();
            
            if (_dialogueText != null)
            {
                _dialogueText.text = "";
                
                foreach (char c in text)
                {
                    if (_skipRequested)
                    {
                        _dialogueText.text = text;
                        break;
                    }
                    
                    _dialogueText.text += c;
                    yield return new UnityEngine.WaitForSeconds(_typewriterSpeed);
                }
            }
            
            _isTypewriting = false;
            
            if (_currentDialogueIndex < _dialogueLines.Count - 1 || _currentChoices.Count > 0)
            {
                ShowContinueIndicator();
            }
        }
        
        private void ShowContinueIndicator()
        {
            if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(true);
            }
        }
        
        private void HideContinueIndicator()
        {
            if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(false);
            }
        }
        
        private void OnSkipClicked()
        {
            if (_isTypewriting)
            {
                _skipRequested = true;
            }
            else
            {
                AdvanceDialogue();
            }
        }
        
        public void AdvanceDialogue()
        {
            if (_isTypewriting) return;
            
            _currentDialogueIndex++;
            
            if (_currentDialogueIndex >= _dialogueLines.Count)
            {
                ShowChoicesOrClose();
            }
            else
            {
                ShowCurrentLine();
            }
        }
        
        private void ShowChoicesOrClose()
        {
            HideContinueIndicator();
            
            if (_currentChoices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                Close();
            }
        }
        
        private void ShowChoices()
        {
            ClearChoices();
            
            if (_choiceContainer == null || _choiceButtonPrefab == null) return;
            
            for (int i = 0; i < _currentChoices.Count; i++)
            {
                var choice = _currentChoices[i];
                var buttonGO = Instantiate(_choiceButtonPrefab, _choiceContainer);
                
                var buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = choice.Text;
                }
                
                var button = buttonGO.GetComponent<Button>();
                if (button != null)
                {
                    int index = i;
                    button.onClick.AddListener(() => OnChoiceSelected(index));
                }
                
                _choiceButtons.Add(buttonGO);
            }
        }
        
        private void ClearChoices()
        {
            foreach (var button in _choiceButtons)
            {
                if (button != null) Destroy(button);
            }
            _choiceButtons.Clear();
        }
        
        private void OnChoiceSelected(int index)
        {
            if (index < 0 || index >= _currentChoices.Count) return;
            
            var choice = _currentChoices[index];
            
            choice.OnSelected?.Invoke();
            
            if (!string.IsNullOrEmpty(choice.NextDialogueId))
            {
                EventBus.Publish(new DialogueChoiceMadeEvent
                {
                    NPCId = _currentNPCId,
                    ChoiceIndex = index,
                    NextDialogueId = choice.NextDialogueId
                });
            }
            
            ClearChoices();
            Close();
        }
        

    }
    
    public class DialogueChoiceMadeEvent
    {
        public string NPCId { get; set; }
        public int ChoiceIndex { get; set; }
        public string NextDialogueId { get; set; }
    }
}