using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunnysideIsland.UI.Shop
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("=== References ===")]
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _stockText;
        [SerializeField] private Button _button;
        
        public string ItemId { get; private set; }
        public int Price { get; private set; }
        public int Stock { get; private set; }
        
        public event Action<string, int> OnClicked;
        
        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }
        
        public void Setup(string itemId, string itemName, int price, int stock)
        {
            ItemId = itemId;
            Price = price;
            Stock = stock;
            
            if (_itemNameText != null)
            {
                _itemNameText.text = itemName;
            }
            
            if (_priceText != null)
            {
                _priceText.text = $"{price:N0} G";
            }
            
            if (_stockText != null)
            {
                _stockText.text = stock > 0 ? $"x{stock}" : "";
                _stockText.gameObject.SetActive(stock > 0);
            }
        }
        
        private void OnButtonClicked()
        {
            OnClicked?.Invoke(ItemId, Price);
        }
    }
}