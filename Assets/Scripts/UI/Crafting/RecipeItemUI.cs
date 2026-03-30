using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunnysideIsland.UI.Crafting
{
    public class RecipeItemUI : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _recipeNameText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameObject _canCraftIndicator;
        
        private string _recipeId;
        
        public event Action<string> OnClicked;
        
        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);
            
            if (_recipeNameText == null)
                _recipeNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (_iconImage == null)
                _iconImage = GetComponent<Image>();
        }
        
        public void Setup(string recipeId, string recipeName, Sprite icon, bool canCraft)
        {
            _recipeId = recipeId;
            
            if (_recipeNameText != null)
                _recipeNameText.text = recipeName;
            
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.gameObject.SetActive(icon != null);
            }
            
            if (_canCraftIndicator != null)
                _canCraftIndicator.SetActive(canCraft);
        }
        
        private void OnButtonClicked()
        {
            OnClicked?.Invoke(_recipeId);
        }
    }
}