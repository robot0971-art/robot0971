using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DI;
using SunnysideIsland.UI;

namespace SunnysideIsland.Achievement
{
    /// <summary>
    /// 업적 UI 패널
    /// </summary>
    public class AchievementUI : UIPanel
    {
        [Header("=== Achievement List ===")]
        [SerializeField] private Transform _achievementListContainer;
        [SerializeField] private GameObject _achievementItemPrefab;

        [Header("=== Tabs ===")]
        [SerializeField] private Button _allTabButton;
        [SerializeField] private Button _progressTabButton;
        [SerializeField] private Button _collectionTabButton;
        [SerializeField] private Button _combatTabButton;
        [SerializeField] private Button _economyTabButton;
        [SerializeField] private Color _selectedTabColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _unselectedTabColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("=== Info ===")]
        [SerializeField] private TextMeshProUGUI _achievementCountText;
        [SerializeField] private TextMeshProUGUI _totalProgressText;

        [Header("=== Detail Popup ===")]
        [SerializeField] private GameObject _detailPopup;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailTitle;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private Slider _detailProgressBar;
        [SerializeField] private TextMeshProUGUI _detailProgressText;
        [SerializeField] private Transform _detailRewardContainer;
        [SerializeField] private GameObject _rewardItemPrefab;
        [SerializeField] private Button _detailClaimButton;
        [SerializeField] private Button _detailCloseButton;

        [Header("=== Buttons ===")]
        [SerializeField] private Button _closeButton;

        [Inject]
        private AchievementManager _achievementManager;

        private AchievementType? _currentFilter = null;
        private string _selectedAchievementId;
        private readonly List<GameObject> _achievementItems = new List<GameObject>();
        private readonly List<GameObject> _rewardItems = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            _isModal = true;

            SetupTabButtons();
            SetupDetailPopup();
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            RefreshAchievementList();
            UpdateTotalProgress();
            SubscribeEvents();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            UnsubscribeEvents();
            ClearAchievementList();
            HideDetailPopup();
        }

        /// <summary>
        /// 탭 버튼 설정
        /// </summary>
        private void SetupTabButtons()
        {
            if (_allTabButton != null)
            {
                _allTabButton.onClick.AddListener(() => SetFilter(null));
            }

            if (_progressTabButton != null)
            {
                _progressTabButton.onClick.AddListener(() => SetFilter(AchievementType.Progress));
            }

            if (_collectionTabButton != null)
            {
                _collectionTabButton.onClick.AddListener(() => SetFilter(AchievementType.Collection));
            }

            if (_combatTabButton != null)
            {
                _combatTabButton.onClick.AddListener(() => SetFilter(AchievementType.Combat));
            }

            if (_economyTabButton != null)
            {
                _economyTabButton.onClick.AddListener(() => SetFilter(AchievementType.Economy));
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        /// <summary>
        /// 상세 팝업 설정
        /// </summary>
        private void SetupDetailPopup()
        {
            if (_detailClaimButton != null)
            {
                _detailClaimButton.onClick.AddListener(OnClaimButtonClicked);
            }

            if (_detailCloseButton != null)
            {
                _detailCloseButton.onClick.AddListener(HideDetailPopup);
            }

            if (_detailPopup != null)
            {
                _detailPopup.SetActive(false);
            }
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            EventBus.Subscribe<AchievementProgressEvent>(OnAchievementProgress);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
            EventBus.Subscribe<AchievementClaimedEvent>(OnAchievementClaimed);
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<AchievementProgressEvent>(OnAchievementProgress);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
            EventBus.Unsubscribe<AchievementClaimedEvent>(OnAchievementClaimed);
        }

        /// <summary>
        /// 필터 설정
        /// </summary>
        private void SetFilter(AchievementType? type)
        {
            _currentFilter = type;
            _selectedAchievementId = null;

            UpdateTabColors();
            RefreshAchievementList();
            HideDetailPopup();
        }

        /// <summary>
        /// 탭 색상 업데이트
        /// </summary>
        private void UpdateTabColors()
        {
            SetTabButtonColor(_allTabButton, _currentFilter == null);
            SetTabButtonColor(_progressTabButton, _currentFilter == AchievementType.Progress);
            SetTabButtonColor(_collectionTabButton, _currentFilter == AchievementType.Collection);
            SetTabButtonColor(_combatTabButton, _currentFilter == AchievementType.Combat);
            SetTabButtonColor(_economyTabButton, _currentFilter == AchievementType.Economy);
        }

        /// <summary>
        /// 탭 버튼 색상 설정
        /// </summary>
        private void SetTabButtonColor(Button button, bool isSelected)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = isSelected ? _selectedTabColor : _unselectedTabColor;
            button.colors = colors;
        }

        /// <summary>
        /// 업적 목록 새로고침
        /// </summary>
        private void RefreshAchievementList()
        {
            ClearAchievementList();

            if (_achievementManager == null) return;

            var achievements = GetFilteredAchievements();

            foreach (var achievement in achievements)
            {
                CreateAchievementItem(achievement);
            }

            UpdateAchievementCount();
        }

        /// <summary>
        /// 필터링된 업적 목록 조회
        /// </summary>
        private List<AchievementData> GetFilteredAchievements()
        {
            if (_currentFilter.HasValue)
            {
                return _achievementManager.GetAchievementsByType(_currentFilter.Value);
            }

            return _achievementManager.GetAllAchievements();
        }

        /// <summary>
        /// 업적 아이템 생성
        /// </summary>
        private void CreateAchievementItem(AchievementData achievement)
        {
            if (_achievementItemPrefab == null || _achievementListContainer == null) return;

            var itemGO = Instantiate(_achievementItemPrefab, _achievementListContainer);
            var progress = _achievementManager.GetProgress(achievement.AchievementId);
            var isUnlocked = _achievementManager.IsUnlocked(achievement.AchievementId);
            var isClaimed = _achievementManager.IsClaimed(achievement.AchievementId);

            var iconImage = itemGO.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = achievement.Icon;

                if (!isUnlocked && achievement.Hidden)
                {
                    iconImage.color = Color.black;
                }
            }

            var titleText = itemGO.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                if (!isUnlocked && achievement.Hidden)
                {
                    titleText.text = "???";
                }
                else
                {
                    titleText.text = achievement.Title;
                }
            }

            var progressBar = itemGO.transform.Find("ProgressBar")?.GetComponent<Slider>();
            if (progressBar != null && progress != null)
            {
                progressBar.maxValue = achievement.TargetValue;
                progressBar.value = progress.CurrentValue;
            }

            var progressText = itemGO.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            if (progressText != null && progress != null)
            {
                progressText.text = $"{progress.CurrentValue}/{achievement.TargetValue}";
            }

            var lockedOverlay = itemGO.transform.Find("LockedOverlay")?.gameObject;
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!isUnlocked);
            }

            var claimedOverlay = itemGO.transform.Find("ClaimedOverlay")?.gameObject;
            if (claimedOverlay != null)
            {
                claimedOverlay.SetActive(isClaimed);
            }

            var button = itemGO.GetComponent<Button>();
            if (button != null)
            {
                string achievementId = achievement.AchievementId;
                button.onClick.AddListener(() => OnAchievementItemClicked(achievementId));
            }

            if (!string.IsNullOrEmpty(_selectedAchievementId) && _selectedAchievementId == achievement.AchievementId)
            {
                var selectedImage = itemGO.transform.Find("Selected")?.GetComponent<Image>();
                if (selectedImage != null)
                {
                    selectedImage.enabled = true;
                }
            }

            _achievementItems.Add(itemGO);
        }

        /// <summary>
        /// 업적 목록 클리어
        /// </summary>
        private void ClearAchievementList()
        {
            foreach (var item in _achievementItems)
            {
                if (item != null) Destroy(item);
            }
            _achievementItems.Clear();
        }

        /// <summary>
        /// 업적 수량 텍스트 업데이트
        /// </summary>
        private void UpdateAchievementCount()
        {
            if (_achievementCountText != null)
            {
                _achievementCountText.text = $"({_achievementItems.Count})";
            }
        }

        /// <summary>
        /// 전체 진행률 업데이트
        /// </summary>
        private void UpdateTotalProgress()
        {
            if (_totalProgressText != null && _achievementManager != null)
            {
                _totalProgressText.text = $"달성: {_achievementManager.UnlockedCount}/{_achievementManager.TotalAchievements}";
            }
        }

        /// <summary>
        /// 업적 아이템 클릭 처리
        /// </summary>
        private void OnAchievementItemClicked(string achievementId)
        {
            _selectedAchievementId = achievementId;
            ShowDetailPopup(achievementId);
        }

        /// <summary>
        /// 상세 팝업 표시
        /// </summary>
        private void ShowDetailPopup(string achievementId)
        {
            var achievement = _achievementManager.GetAchievementData(achievementId);
            if (achievement == null) return;

            var progress = _achievementManager.GetProgress(achievementId);
            var isUnlocked = _achievementManager.IsUnlocked(achievementId);
            var isClaimed = _achievementManager.IsClaimed(achievementId);

            if (_detailPopup != null)
            {
                _detailPopup.SetActive(true);
            }

            if (_detailIcon != null)
            {
                _detailIcon.sprite = achievement.Icon;

                if (!isUnlocked && achievement.Hidden)
                {
                    _detailIcon.color = Color.black;
                }
                else
                {
                    _detailIcon.color = Color.white;
                }
            }

            if (_detailTitle != null)
            {
                if (!isUnlocked && achievement.Hidden)
                {
                    _detailTitle.text = "???";
                }
                else
                {
                    _detailTitle.text = achievement.Title;
                }
            }

            if (_detailDescription != null)
            {
                if (!isUnlocked && achievement.Hidden)
                {
                    _detailDescription.text = "비밀 업적입니다.";
                }
                else
                {
                    _detailDescription.text = achievement.Description;
                }
            }

            if (_detailProgressBar != null)
            {
                _detailProgressBar.maxValue = achievement.TargetValue;
                _detailProgressBar.value = progress?.CurrentValue ?? 0;
            }

            if (_detailProgressText != null)
            {
                int current = progress?.CurrentValue ?? 0;
                _detailProgressText.text = $"{current}/{achievement.TargetValue}";
            }

            UpdateRewardList(achievement);

            if (_detailClaimButton != null)
            {
                bool canClaim = isUnlocked && !isClaimed && achievement.HasRewards();
                _detailClaimButton.gameObject.SetActive(canClaim);
                _detailClaimButton.interactable = canClaim;
            }
        }

        /// <summary>
        /// 보상 목록 업데이트
        /// </summary>
        private void UpdateRewardList(AchievementData achievement)
        {
            ClearRewardItems();

            if (_detailRewardContainer == null || _rewardItemPrefab == null) return;
            if (achievement.Rewards == null || achievement.Rewards.Count == 0) return;

            foreach (var reward in achievement.Rewards)
            {
                var rewardGO = Instantiate(_rewardItemPrefab, _detailRewardContainer);

                var rewardText = rewardGO.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
                if (rewardText != null)
                {
                    string rewardName = GetRewardName(reward);
                    rewardText.text = $"{rewardName} x{reward.Amount}";
                }

                _rewardItems.Add(rewardGO);
            }
        }

        /// <summary>
        /// 보상 이름 조회
        /// </summary>
        private string GetRewardName(AchievementReward reward)
        {
            return reward.RewardType switch
            {
                AchievementRewardType.Gold => "골드",
                AchievementRewardType.Item => reward.RewardId,
                AchievementRewardType.Experience => "경험치",
                AchievementRewardType.UnlockFeature => "기능 해금",
                _ => "보상"
            };
        }

        /// <summary>
        /// 보상 아이템 클리어
        /// </summary>
        private void ClearRewardItems()
        {
            foreach (var item in _rewardItems)
            {
                if (item != null) Destroy(item);
            }
            _rewardItems.Clear();
        }

        /// <summary>
        /// 상세 팝업 숨기기
        /// </summary>
        private void HideDetailPopup()
        {
            if (_detailPopup != null)
            {
                _detailPopup.SetActive(false);
            }
        }

        /// <summary>
        /// 보상 수령 버튼 클릭 처리
        /// </summary>
        private void OnClaimButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedAchievementId)) return;

            if (_achievementManager != null)
            {
                _achievementManager.ClaimReward(_selectedAchievementId);
            }
        }

        #region Event Handlers

        private void OnAchievementProgress(AchievementProgressEvent evt)
        {
            if (_selectedAchievementId == evt.AchievementId)
            {
                ShowDetailPopup(evt.AchievementId);
            }
            RefreshAchievementList();
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            UpdateTotalProgress();
            RefreshAchievementList();
        }

        private void OnAchievementClaimed(AchievementClaimedEvent evt)
        {
            if (_selectedAchievementId == evt.AchievementId)
            {
                ShowDetailPopup(evt.AchievementId);
            }
            RefreshAchievementList();
        }

        #endregion
    }
}