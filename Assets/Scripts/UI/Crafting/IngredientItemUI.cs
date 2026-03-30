using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunnysideIsland.UI.Crafting
{
    public class IngredientItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _amountText;
        [SerializeField] private Color _enoughColor = Color.white;
        [SerializeField] private Color _notEnoughColor = Color.red;
        
        public void Setup(string itemId, string itemName, Sprite icon, int owned, int required, bool hasEnough)
        {
            if (_itemNameText != null)
                _itemNameText.text = itemName;
            
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.gameObject.SetActive(icon != null);
            }
            
            if (_amountText != null)
            {
                _amountText.text = $"{owned}/{required}";
                _amountText.color = hasEnough ? _enoughColor : _notEnoughColor;
            }
        }
    }
}