using System;
using UnityEngine;
using UnityEngine.UI;
using DI;
using SunnysideIsland.Farming;
using SunnysideIsland.GameData;

namespace SunnysideIsland.UI.Farming
{
    [ExecuteAlways]
    public class CropQuickSlotUI : MonoBehaviour
    {
        [Inject] private ICropSelectionSystem _selectionSystem;

        [Serializable]
        public class SlotStyle
        {
            public string name = "Slot";
            public Color normalColor = Color.white;
            public Color selectedColor = Color.yellow;
            public Color textColor = Color.black;
            public Color iconColor = Color.white;
        }

        [Header("=== UI Elements ===")]
        [SerializeField] private GameObject[] _slotObjects;
        [SerializeField] private RectTransform _selectionFrame;

        [Header("=== ALL COLORS (SET HERE) ===")]
        [SerializeField] private SlotStyle[] _slotStyles = new SlotStyle[5];

        private Image[] _slotBackgrounds;
        private Image[] _cropIcons;
        private Text[] _slotNumbers;
        private Text[] _cropNames;

        private void Awake()
        {
            FindSlotElements();
            if (Application.isPlaying) DIContainer.Inject(this);
        }

        private void FindSlotElements()
        {
            if (_slotObjects == null || _slotObjects.Length == 0) return;
            int count = _slotObjects.Length;
            _slotBackgrounds = new Image[count];
            _cropIcons = new Image[count];
            _slotNumbers = new Text[count];
            _cropNames = new Text[count];

            for (int i = 0; i < count; i++)
            {
                if (_slotObjects[i] == null) continue;
                _slotBackgrounds[i] = _slotObjects[i].GetComponent<Image>();
                var iconT = _slotObjects[i].transform.Find("Icon");
                if (iconT != null) _cropIcons[i] = iconT.GetComponent<Image>();
                var numT = _slotObjects[i].transform.Find("Number");
                if (numT != null) _slotNumbers[i] = numT.GetComponent<Text>();
                var nameT = _slotObjects[i].transform.Find("Name");
                if (nameT != null) _cropNames[i] = nameT.GetComponent<Text>();
            }
        }
        
        // Fix for the index error in FindSlotElements
        private void UpdateFindSlotElements()
        {
            if (_slotObjects == null || _slotObjects.Length == 0) return;
            int count = _slotObjects.Length;
            _slotBackgrounds = new Image[count];
            _cropIcons = new Image[count];
            _slotNumbers = new Text[count];
            _cropNames = new Text[count];

            for (int i = 0; i < count; i++)
            {
                if (_slotObjects[i] == null) continue;
                _slotBackgrounds[i] = _slotObjects[i].GetComponent<Image>();
                var iconT = _slotObjects[i].transform.Find("Icon");
                if (iconT != null) _cropIcons[i] = iconT.GetComponent<Image>();
                var numT = _slotObjects[i].transform.Find("Number");
                if (numT != null) _slotNumbers[i] = numT.GetComponent<Text>();
                var nameT = _slotObjects[i].transform.Find("Name");
                if (nameT != null) _cropNames[i] = nameT.GetComponent<Text>();
            }
        }

        private void Start()
        {
            UpdateAllSlots();
            if (Application.isPlaying && _selectionSystem != null)
            {
                _selectionSystem.OnSlotUpdated += (i, d, c) => UpdateSlot(i);
                _selectionSystem.OnSelectedIndexChanged += (i) => { UpdateSelectionFrame(i); UpdateAllSlots(); };
                UpdateSelectionFrame(_selectionSystem.SelectedIndex);
            }
        }

        private void UpdateSelectionFrame(int index)
        {
            if (_selectionFrame != null && _slotObjects != null && index >= 0 && index < _slotObjects.Length)
            {
                if (_slotObjects[index] != null)
                {
                    _selectionFrame.SetParent(_slotObjects[index].transform);
                    _selectionFrame.anchoredPosition = Vector2.zero;
                    _selectionFrame.gameObject.SetActive(true);
                }
            }
        }

        public void UpdateAllSlots()
        {
            if (_slotBackgrounds == null || _slotBackgrounds.Length == 0) UpdateFindSlotElements();
            for (int i = 0; i < (_slotObjects?.Length ?? 0); i++) UpdateSlot(i);
        }

        private void UpdateSlot(int index)
        {
            if (_slotObjects == null || index >= _slotObjects.Length || _slotObjects[index] == null) return;
            if (index >= _slotStyles.Length) return;

            var style = _slotStyles[index];
            bool isSelected = (Application.isPlaying && _selectionSystem != null) ? (index == _selectionSystem.SelectedIndex) : false;
            
            CropData data = (Application.isPlaying && _selectionSystem != null) ? _selectionSystem.GetCropData(index) : null;
            int count = (Application.isPlaying && _selectionSystem != null) ? _selectionSystem.GetCount(index) : 0;

            // 1. 배경색
            if (_slotBackgrounds != null && _slotBackgrounds.Length > index && _slotBackgrounds[index] != null)
                _slotBackgrounds[index].color = isSelected ? style.selectedColor : style.normalColor;

            // 2. 텍스트
            if (_slotNumbers != null && _slotNumbers.Length > index && _slotNumbers[index] != null)
                _slotNumbers[index].color = style.textColor;

            if (_cropNames != null && _cropNames.Length > index && _cropNames[index] != null)
            {
                _cropNames[index].color = style.textColor;
                if (Application.isPlaying)
                {
                    if (data != null)
                    {
                        string nameToShow = string.IsNullOrEmpty(data.cropName) ? data.cropId : data.cropName;
                        _cropNames[index].text = $"{nameToShow} x{count}";
                    }
                    else
                    {
                        _cropNames[index].text = "-";
                    }
                }
            }

            // 3. 아이콘
            if (_cropIcons != null && _cropIcons.Length > index && _cropIcons[index] != null)
            {
                _cropIcons[index].color = style.iconColor;
                if (Application.isPlaying && data != null)
                {
                    if (data.growthSprites != null && data.growthSprites.Length > 0)
                    {
                        _cropIcons[index].sprite = data.growthSprites[data.growthSprites.Length - 1];
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate() 
        { 
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall -= RefreshUI;
            UnityEditor.EditorApplication.delayCall += RefreshUI;
        }

        private void RefreshUI() 
        { 
            if (Application.isPlaying) return;
            UpdateFindSlotElements(); 
            UpdateAllSlots(); 
        }

        private void Update() 
        { 
            if (!Application.isPlaying) UpdateAllSlots(); 
        }
#endif
    }
}
