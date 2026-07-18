using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;

namespace SerenaysGambit
{
    public sealed class SlotGameController : MonoBehaviour
    {
        private RunService _runService;
        private ShopService _shopService;
        private RunState _state;
        private SymbolDefinition[] _symbolDefinitions;
        private ReelDefinition[] _reelDefinitions;
        private ShopItemDefinition[] _shopItemDefinitions;
        private BalanceDefinition _balanceDefinition;
        private GameRulesConfig _rulesConfig;
        private readonly Dictionary<ShopOfferKind, ShopItemDefinition> _shopItemDefinitionsByKind = new Dictionary<ShopOfferKind, ShopItemDefinition>();
        private readonly Dictionary<ShopOfferKind, OwnedUpgradeView> _ownedUpgradeViews = new Dictionary<ShopOfferKind, OwnedUpgradeView>();
        private readonly Dictionary<string, OwnedUpgradeView> _ownedGambitViews = new Dictionary<string, OwnedUpgradeView>();
        private RectTransform _ownedGambitsLayout;
        private GameObject _gambitOverlay;

        private readonly ReelScroller[] _reelScrollers = new ReelScroller[GameBalance.GridColumns];
        private bool _isSpinAnimating;
        private RectTransform _mainContent;
        private Vector3 _originalMainContentPosition;
        private bool _hasOriginalMainContentPosition;
        private readonly Vector3[] _originalReelPositions = new Vector3[GameBalance.GridColumns];
        private readonly bool[] _hasOriginalReelPositions = new bool[GameBalance.GridColumns];

        [SerializeField] private RewardAnimationSettings _animationSettings;
        private readonly List<GameObject> _coinPool = new List<GameObject>();

        private float FirstWinPulseDuration => _animationSettings != null ? _animationSettings.FirstWinPulseDuration : 0.6f;
        private float MinimumWinPulseDuration => _animationSettings != null ? _animationSettings.MinimumWinPulseDuration : 0.025f;
        private int MaximumRewardPulseWaves => _animationSettings != null ? _animationSettings.MaximumRewardPulseWaves : 25;
        private int MaximumCoinFlightsPerCellPerPulse => _animationSettings != null ? _animationSettings.MaximumCoinFlightsPerCellPerPulse : 12;
        private float RewardPulseSpeedIncrease => _animationSettings != null ? _animationSettings.RewardPulseSpeedIncrease : 0.08f;

        private TextMeshProUGUI _cashText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _rollsText;
        private RectTransform _organsLayout;
        private readonly List<OwnedUpgradeView> _organViews = new List<OwnedUpgradeView>();
        private bool _isFirstRefresh = true;
        private TextMeshProUGUI _ticketsText;
        private TextMeshProUGUI _payoutText;
        private TextMeshProUGUI _resultText;
        private TextMeshProUGUI _shopWalletText;
        private RectTransform _ownedUpgradesLayout;
        private UpgradeTooltip _upgradeTooltip;
        private TextMeshProUGUI _refreshLabel;
        private readonly TextMeshProUGUI[] _offerLabels = new TextMeshProUGUI[3];
        private readonly Button[] _offerButtons = new Button[3];
        private readonly Image[,] _cellImages = new Image[GameBalance.GridRows, GameBalance.GridColumns];
        private readonly TextMeshProUGUI[,] _cellTexts = new TextMeshProUGUI[GameBalance.GridRows, GameBalance.GridColumns];

        private RectTransform _thresholdBarRect;
        private Image _thresholdBarBackground;
        private Image _thresholdBarFill;
        private TextMeshProUGUI _thresholdBarText;

        private Button _spin1xButton;
        private Button _spin5xButton;
        private Button _spin10xButton;
        private Button _refreshButton;
        private Button _gameOverRestartButton;
        private Button _victoryRestartButton;
        private GameObject _gameOverOverlay;
        private GameObject _victoryOverlay;
        private TextMeshProUGUI _gameOverRunStatsText;
        private TextMeshProUGUI _victoryRunStatsText;

        private SlotLever _lever;
        [SerializeField] private GameObject _coinPrefab;
        [SerializeField] private OwnedUpgradeView _ownedUpgradePrefab;
        private int _currentBatchFactor = 1;
        private readonly Color _buttonSelectedColor = new Color(0.2f, 0.62f, 0.3f, 1f); // Green
        private readonly Color _buttonNormalColor = new Color(0.2f, 0.38f, 0.62f, 1f); // Blue

        private void Start()
        {

            _symbolDefinitions = Resources.LoadAll<SymbolDefinition>("SerenaysGambit/Data/Symbols");
            _reelDefinitions = Resources.LoadAll<ReelDefinition>("SerenaysGambit/Data/Reels");
            _shopItemDefinitions = Resources.LoadAll<ShopItemDefinition>("SerenaysGambit/Data/ShopItems");
            _balanceDefinition = Resources.Load<BalanceDefinition>("SerenaysGambit/Data/Balance/DefaultBalance");
            IndexShopItemDefinitions();
            _rulesConfig = RuntimeGameConfigFactory.Create(_symbolDefinitions, _reelDefinitions, _shopItemDefinitions, _balanceDefinition);
            PrepareDefaultFonts();
            if (!BindView())
            {
                enabled = false;
                return;
            }

            if (_targetText != null)
            {
                _targetText.gameObject.SetActive(false);
            }

            var canvas = GameObject.Find("GameCanvas");
            if (canvas != null)
            {
                var mainCamera = Camera.main;
                var canvasComp = canvas.GetComponent<Canvas>();
                if (canvasComp != null)
                {
                    canvasComp.renderMode = RenderMode.ScreenSpaceCamera;
                    canvasComp.worldCamera = mainCamera;
                }

                var mainContentTrans = canvas.transform.Find("MainContent");
                if (mainContentTrans != null)
                {
                    _mainContent = mainContentTrans.GetComponent<RectTransform>();
                    _originalMainContentPosition = _mainContent.anchoredPosition;
                    _hasOriginalMainContentPosition = true;
                }

                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    var reelTransform = canvas.transform.Find("MainContent/SlotMachinePanel/SlotGrid/Reel" + (column + 1));
                    if (reelTransform != null)
                    {
                        var scroller = reelTransform.gameObject.AddComponent<ReelScroller>();
                        scroller.Initialize(column, _rulesConfig.ReelStripAt(column), SymbolLabel, SymbolColor, SymbolSprite);
                        _reelScrollers[column] = scroller;
                    }
                }
            }

            StartNewRun();
        }

        private void Update()
        {
            if (_state == null || _state.Phase != RunPhase.Playing || _isSpinAnimating)
            {
                return;
            }

            if (_state.Modifiers.BatchTenGambitCount > 0)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Alpha0))
                {
                    Spin(10);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Spin(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Spin(5);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Spin(10);
            }
        }

        private void ShakeMainContent(Vector3 punchDirection, float duration, int vibrato, float elasticity)
        {
            if (_mainContent == null) return;

            _mainContent.DOComplete();
            if (_hasOriginalMainContentPosition)
            {
                _mainContent.anchoredPosition = _originalMainContentPosition;
            }

            _mainContent.DOPunchPosition(punchDirection, duration, vibrato, elasticity);
        }

        private void PunchDownCameraShake()
        {
            Vector3 punch = new Vector3(0f, -40f, 0f);
            ShakeMainContent(punch, 0.35f, 12, 1f);
        }

        private void PunchUpCameraShake(int reelIndex)
        {
            float intensity = 15f + reelIndex * 15f;
            Vector3 punch = new Vector3(0f, intensity, 0f);
            ShakeMainContent(punch, 0.25f, 10, 1f);
        }

        private void PunchReelDown(int reelIndex)
        {
            var scroller = _reelScrollers[reelIndex];
            if (scroller == null) return;

            Transform reelTrans = scroller.transform;
            if (!_hasOriginalReelPositions[reelIndex])
            {
                _originalReelPositions[reelIndex] = reelTrans.localPosition;
                _hasOriginalReelPositions[reelIndex] = true;
            }

            reelTrans.DOComplete();
            reelTrans.localPosition = _originalReelPositions[reelIndex];

            float intensity = 15f + reelIndex * 15f;
            Vector3 punch = new Vector3(0f, -intensity, 0f);
            reelTrans.DOPunchPosition(punch, 0.25f, 10, 1f);
        }

        private void OnDestroy()
        {
            if (_mainContent != null)
            {
                _mainContent.DOComplete();
                if (_hasOriginalMainContentPosition)
                {
                    _mainContent.anchoredPosition = _originalMainContentPosition;
                }
            }

            for (int i = 0; i < GameBalance.GridColumns; i++)
            {
                if (_reelScrollers[i] != null)
                {
                    _reelScrollers[i].transform.DOComplete();
                    if (_hasOriginalReelPositions[i])
                    {
                        _reelScrollers[i].transform.localPosition = _originalReelPositions[i];
                    }
                }
            }

            foreach (var coin in _coinPool)
            {
                if (coin != null)
                {
                    Destroy(coin);
                }
            }
            _coinPool.Clear();
        }

        private void StartNewRun()
        {
            ClearOwnedUpgradeViews();
            ClearOrganViews();
            _isFirstRefresh = true;
            _runService = new RunService(Environment.TickCount, _rulesConfig);
            _shopService = _runService.Shop;
            _state = _runService.CreateNewRun();
            InitializeOrganViews();
            ClearGrid();
            _resultText.text = "Choose a batch to spin. Space = 1x, 5 = 5x, 0 = 10x.";
            _payoutText.text = "Last payout: TL 0.00";
            _gameOverOverlay.SetActive(false);
            _victoryOverlay.SetActive(false);
            RefreshEndScreenStats();
            RefreshView();
        }

        private void Spin(int batchFactor)
        {
            _currentBatchFactor = batchFactor;
            if (_lever != null)
            {
                _lever.IsAvailable = false;
            }
            var result = _runService.TrySpin(_state, batchFactor);
            if (!result.Accepted)
            {
                _resultText.text = result.Message;
                RefreshView();
                return;
            }

            StartCoroutine(DoSpinAnimation(result));
        }

        private IEnumerator DoSpinAnimation(SpinResult result)
        {
            _isSpinAnimating = true;

            _spin1xButton.interactable = false;
            _spin5xButton.interactable = false;
            _spin10xButton.interactable = false;
            _refreshButton.interactable = false;
            for (var i = 0; i < _offerButtons.Length; i++)
            {
                _offerButtons[i].interactable = false;
            }

            PunchDownCameraShake();

            float spinSpeed = 1800f;
            for (var col = 0; col < GameBalance.GridColumns; col++)
            {
                if (_reelScrollers[col] != null)
                {
                    _reelScrollers[col].StartSpin(spinSpeed);
                }
            }

            var stopDurations = new[] { 1.0f, 1.50f, 2.00f };
            for (var col = 0; col < GameBalance.GridColumns; col++)
            {
                var capturedCol = col;
                var strip = _rulesConfig.ReelStripAt(capturedCol);
                var g0 = result.Grid[0, capturedCol];
                var g1 = result.Grid[1, capturedCol];
                var g2 = result.Grid[2, capturedCol];
                var stopIndex = FindStopIndex(strip, g0, g1, g2);

                if (_reelScrollers[capturedCol] != null)
                {
                    _reelScrollers[capturedCol].StopSpin(stopIndex, stopDurations[capturedCol], delegate
                    {
                        PunchUpCameraShake(capturedCol);
                        PunchReelDown(capturedCol);
                    });
                }
            }

            while (true)
            {
                bool anyAnimating = false;
                for (var col = 0; col < GameBalance.GridColumns; col++)
                {
                    if (_reelScrollers[col] != null && _reelScrollers[col].IsAnimating)
                    {
                        anyAnimating = true;
                        break;
                    }
                }
                if (!anyAnimating)
                {
                    break;
                }
                yield return null;
            }

            UpdateGrid(result.Grid);

            if (result.Score.Wins != null && result.Score.Wins.Count > 0)
            {
                yield return StartCoroutine(AnimateWinHighlighting(result));
            }
            else
            {
                _payoutText.text = "Last payout: " + MoneyFormatter.FormatTL(result.Score.PayoutKurus) + " | combo x" + result.Score.ComboMultiplier + " | batch x" + result.Score.BatchFactor;
                _resultText.text = BuildResultSummary(result);
            }

            _isSpinAnimating = false;

            if (result.JokerLostOnLeftReel)
            {
                if (_reelScrollers[0] != null)
                {
                    _reelScrollers[0].UpdateStrip(_rulesConfig.ReelStripAt(0));
                }
            }

            if (_state.Phase == RunPhase.GameOver)
            {
                RefreshEndScreenStats();
                _gameOverOverlay.SetActive(true);
            }
            else if (_state.Phase == RunPhase.Victory)
            {
                RefreshEndScreenStats();
                _victoryOverlay.SetActive(true);
            }
            else if (result.ThresholdCleared)
            {
                ShowGambitSelectionOverlay();
            }

            RefreshView();
        }

        private void BuyOffer(int index)
        {
            string message;
            _shopService.TryPurchase(_state, index, out message);
            _resultText.text = message;
            RefreshView();
        }

        private void RefreshShop()
        {
            string message;
            _shopService.TryRefresh(_state, out message);
            _resultText.text = message;
            RefreshView();
        }

        private bool BindView()
        {
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null)
            {
                Debug.LogError("SlotGameController requires a GameCanvas in the scene.");
                return false;
            }

            var root = canvas.transform;
            try
            {
                _cashText = Require<TextMeshProUGUI>(root, "TopHud/CashText");
                _targetText = Require<TextMeshProUGUI>(root, "TopHud/TargetText");
                _rollsText = Require<TextMeshProUGUI>(root, "MainContent/RollsText");
                _organsLayout = Require<RectTransform>(root, "MainContent/OrgansText");
                var organsTextComp = _organsLayout.GetComponent<TextMeshProUGUI>();
                if (organsTextComp != null)
                {
                    Destroy(organsTextComp);
                }
                var organsLayoutGroup = _organsLayout.GetComponent<HorizontalLayoutGroup>();
                if (organsLayoutGroup == null)
                {
                    organsLayoutGroup = _organsLayout.gameObject.AddComponent<HorizontalLayoutGroup>();
                }
                organsLayoutGroup.spacing = 12f;
                organsLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                organsLayoutGroup.childForceExpandWidth = false;
                organsLayoutGroup.childForceExpandHeight = false;
                organsLayoutGroup.childControlWidth = false;
                organsLayoutGroup.childControlHeight = false;

                _ticketsText = Require<TextMeshProUGUI>(root, "TopHud/TicketsText");
                _payoutText = Require<TextMeshProUGUI>(root, "MainContent/SlotMachinePanel/PayoutText");
                _resultText = Require<TextMeshProUGUI>(root, "MainContent/SlotMachinePanel/ResultText");
                _shopWalletText = Require<TextMeshProUGUI>(root, "MainContent/SerenayShopPanel/ShopWalletText");
                _ownedUpgradesLayout = Require<RectTransform>(root, "MainContent/SerenayShopPanel/OwnedUpgradesLayout");
                _upgradeTooltip = Require<UpgradeTooltip>(root, "UpgradeTooltip");
                if (_ownedUpgradePrefab == null)
                {
                    throw new InvalidOperationException("SlotGameController requires an owned-upgrade prefab reference.");
                }

                _thresholdBarRect = Require<RectTransform>(root, "MainContent/SlotMachinePanel/ThresholdBar");
                _thresholdBarBackground = Require<Image>(root, "MainContent/SlotMachinePanel/ThresholdBar");
                _thresholdBarFill = Require<Image>(root, "MainContent/SlotMachinePanel/ThresholdBar/Fill");
                _thresholdBarText = Require<TextMeshProUGUI>(root, "MainContent/SlotMachinePanel/ThresholdBar/Label");

                _lever = Require<SlotLever>(root, "MainContent/SlotMachinePanel/LeverPanel/Lever");
                _spin1xButton = Require<Button>(root, "MainContent/SlotMachinePanel/LeverPanel/BatchControls/ButtonsRow/Spin1xButton");
                _spin5xButton = Require<Button>(root, "MainContent/SlotMachinePanel/LeverPanel/BatchControls/ButtonsRow/Spin5xButton");
                _spin10xButton = Require<Button>(root, "MainContent/SlotMachinePanel/LeverPanel/BatchControls/ButtonsRow/Spin10xButton");
                _refreshButton = Require<Button>(root, "MainContent/SerenayShopPanel/RefreshButton");
                _refreshLabel = Require<TextMeshProUGUI>(root, "MainContent/SerenayShopPanel/RefreshButton/Label");

                for (var offerIndex = 0; offerIndex < 3; offerIndex++)
                {
                    var name = "Offer" + (offerIndex + 1);
                    _offerButtons[offerIndex] = Require<Button>(root, "MainContent/SerenayShopPanel/OfferList/" + name);
                    _offerLabels[offerIndex] = Require<TextMeshProUGUI>(root, "MainContent/SerenayShopPanel/OfferList/" + name + "/Label");
                }

                for (var row = 0; row < GameBalance.GridRows; row++)
                {
                    for (var column = 0; column < GameBalance.GridColumns; column++)
                    {
                        var cellPath = "MainContent/SlotMachinePanel/SlotGrid/Reel" + (column + 1) + "/Cell_R" + (column + 1) + "_" + (row + 1);
                        _cellImages[row, column] = Require<Image>(root, cellPath);
                        _cellTexts[row, column] = Require<TextMeshProUGUI>(root, cellPath + "/SymbolText");
                    }
                }

                _gameOverOverlay = RequireTransform(root, "GameOverOverlay").gameObject;
                _victoryOverlay = RequireTransform(root, "VictoryOverlay").gameObject;
                _gameOverRunStatsText = Require<TextMeshProUGUI>(root, "GameOverOverlay/RunStatsText");
                _victoryRunStatsText = Require<TextMeshProUGUI>(root, "VictoryOverlay/RunStatsText");
                _gameOverRestartButton = Require<Button>(root, "GameOverOverlay/RestartButton");
                _victoryRestartButton = Require<Button>(root, "VictoryOverlay/RestartButton");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message);
                return false;
            }

            _lever.OnPulled = delegate { Spin(_currentBatchFactor); };
            _spin1xButton.onClick.AddListener(delegate { _currentBatchFactor = 1; RefreshView(); });
            _spin5xButton.onClick.AddListener(delegate { _currentBatchFactor = 5; RefreshView(); });
            _spin10xButton.onClick.AddListener(delegate { _currentBatchFactor = 10; RefreshView(); });
            _refreshButton.onClick.AddListener(RefreshShop);
            _gameOverRestartButton.onClick.AddListener(StartNewRun);
            _victoryRestartButton.onClick.AddListener(StartNewRun);
            for (var offerIndex = 0; offerIndex < _offerButtons.Length; offerIndex++)
            {
                var capturedIndex = offerIndex;
                _offerButtons[offerIndex].onClick.AddListener(delegate { BuyOffer(capturedIndex); });
            }

            // Create OwnedGambitsLayout and OwnedGambitsHeading by shifting existing components
            try
            {
                var upgradesHeading = root.Find("MainContent/SerenayShopPanel/OwnedUpgradesHeading")?.GetComponent<RectTransform>();
                var upgradesLayout = root.Find("MainContent/SerenayShopPanel/OwnedUpgradesLayout")?.GetComponent<RectTransform>();

                if (upgradesHeading != null && upgradesLayout != null)
                {
                    // Shift upgrades up to make room
                    upgradesHeading.anchorMin = new Vector2(0.05f, 0.38f);
                    upgradesHeading.anchorMax = new Vector2(0.95f, 0.42f);
                    upgradesHeading.offsetMin = Vector2.zero;
                    upgradesHeading.offsetMax = Vector2.zero;

                    upgradesLayout.anchorMin = new Vector2(0.05f, 0.24f);
                    upgradesLayout.anchorMax = new Vector2(0.95f, 0.38f);
                    upgradesLayout.offsetMin = Vector2.zero;
                    upgradesLayout.offsetMax = Vector2.zero;

                    // Create OwnedGambitsHeading
                    var gambitsHeadingObj = Instantiate(upgradesHeading.gameObject, upgradesHeading.parent);
                    gambitsHeadingObj.name = "OwnedGambitsHeading";
                    var gambitsHeadingRect = gambitsHeadingObj.GetComponent<RectTransform>();
                    gambitsHeadingRect.anchorMin = new Vector2(0.05f, 0.19f);
                    gambitsHeadingRect.anchorMax = new Vector2(0.95f, 0.23f);
                    gambitsHeadingRect.offsetMin = Vector2.zero;
                    gambitsHeadingRect.offsetMax = Vector2.zero;

                    var headingText = gambitsHeadingObj.GetComponent<TextMeshProUGUI>();
                    if (headingText != null)
                    {
                        headingText.text = "ACTIVE GAMBITS";
                    }

                    // Create OwnedGambitsLayout
                    var gambitsLayoutObj = Instantiate(upgradesLayout.gameObject, upgradesLayout.parent);
                    gambitsLayoutObj.name = "OwnedGambitsLayout";
                    
                    for (int i = gambitsLayoutObj.transform.childCount - 1; i >= 0; i--)
                    {
                        Destroy(gambitsLayoutObj.transform.GetChild(i).gameObject);
                    }

                    _ownedGambitsLayout = gambitsLayoutObj.GetComponent<RectTransform>();
                    _ownedGambitsLayout.anchorMin = new Vector2(0.05f, 0.05f);
                    _ownedGambitsLayout.anchorMax = new Vector2(0.95f, 0.19f);
                    _ownedGambitsLayout.offsetMin = Vector2.zero;
                    _ownedGambitsLayout.offsetMax = Vector2.zero;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not setup dynamic Active Gambits layout: " + ex.Message);
            }

            return true;
        }

        private void RefreshView()
        {
            _cashText.text = "Cash: " + MoneyFormatter.FormatTL(_state.CashKurus);
            _targetText.text = "Threshold " + _state.ThresholdLevel + "/" + _state.Config.ThresholdCount + ": " + MoneyFormatter.FormatTL(_state.CurrentTargetKurus);
            UpdateThresholdBar(_state.CashKurus, _state.CurrentTargetKurus);
            _rollsText.text = "Rolls: " + _state.RollsRemaining;
            RefreshOrganViews(!_isFirstRefresh);
            _ticketsText.text = "Refresh tickets: " + _state.RefreshTickets;
            _shopWalletText.text = "Your cash: " + MoneyFormatter.FormatTL(_state.CashKurus);
            RefreshOwnedUpgradeViews();

            if (_state.Modifiers.BatchTenGambitCount > 0)
            {
                _currentBatchFactor = 10;
                _spin1xButton.image.color = _buttonNormalColor;
                _spin5xButton.image.color = _buttonNormalColor;
                _spin10xButton.image.color = _buttonSelectedColor;

                _spin1xButton.interactable = false;
                _spin5xButton.interactable = false;
                _spin10xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
            }
            else
            {
                _spin1xButton.image.color = _currentBatchFactor == 1 ? _buttonSelectedColor : _buttonNormalColor;
                _spin5xButton.image.color = _currentBatchFactor == 5 ? _buttonSelectedColor : _buttonNormalColor;
                _spin10xButton.image.color = _currentBatchFactor == 10 ? _buttonSelectedColor : _buttonNormalColor;

                _spin1xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin5xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin10xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
            }

            if (_lever != null)
            {
                _lever.IsAvailable = _state.Phase == RunPhase.Playing && _state.RollsRemaining > 0 && !_isSpinAnimating;
            }
            _refreshButton.interactable = _state.Phase == RunPhase.Playing && _state.RefreshTickets > 0;
            _refreshLabel.text = "Refresh shop (" + _state.RefreshTickets + ")";

            for (var index = 0; index < _offerButtons.Length; index++)
            {
                var offer = _state.ShopOffers[index];
                _offerLabels[index].text = offer.Purchased
                    ? offer.Title + "\nSOLD"
                    : offer.Title + "\n" + offer.Description + "\n" + MoneyFormatter.FormatTL(offer.CostKurus);
                _offerButtons[index].interactable = _state.Phase == RunPhase.Playing && !offer.Purchased && _state.CashKurus >= offer.CostKurus;
            }
        }

        private void IndexShopItemDefinitions()
        {
            _shopItemDefinitionsByKind.Clear();
            if (_shopItemDefinitions == null)
            {
                return;
            }

            foreach (var definition in _shopItemDefinitions)
            {
                if (definition != null)
                {
                    _shopItemDefinitionsByKind[definition.Kind] = definition;
                }
            }
        }

        private void RefreshOwnedUpgradeViews()
        {
            var activeKinds = new HashSet<ShopOfferKind>();
            foreach (ShopOfferKind kind in Enum.GetValues(typeof(ShopOfferKind)))
            {
                var ownedCount = _state.OwnedUpgradeCount(kind);
                if (ownedCount <= 0)
                {
                    continue;
                }

                activeKinds.Add(kind);
                OwnedUpgradeView view;
                if (!_ownedUpgradeViews.TryGetValue(kind, out view) || view == null)
                {
                    view = Instantiate(_ownedUpgradePrefab, _ownedUpgradesLayout);
                    _ownedUpgradeViews[kind] = view;
                }

                ShopItemDefinition definition;
                _shopItemDefinitionsByKind.TryGetValue(kind, out definition);
                var title = definition != null && !string.IsNullOrEmpty(definition.DisplayName) ? definition.DisplayName : kind.ToString();
                var description = definition != null && !string.IsNullOrEmpty(definition.Description)
                    ? definition.Description
                    : "Upgrade details are unavailable.";
                view.Bind(
                    definition == null ? null : definition.Icon,
                    UpgradeFallbackLabel(kind),
                    UpgradeFallbackColor(kind),
                    ownedCount,
                    title,
                    description,
                    CurrentUpgradeEffect(kind),
                    _upgradeTooltip);
            }

            var staleKinds = new List<ShopOfferKind>();
            foreach (var pair in _ownedUpgradeViews)
            {
                if (!activeKinds.Contains(pair.Key))
                {
                    staleKinds.Add(pair.Key);
                }
            }

            foreach (var kind in staleKinds)
            {
                var view = _ownedUpgradeViews[kind];
                _ownedUpgradeViews.Remove(kind);
                if (view == null)
                {
                    continue;
                }

                if (kind == ShopOfferKind.FreeSpins)
                {
                    view.FadeOutAndDestroy(0.2f);
                }
                else
                {
                    Destroy(view.gameObject);
                }
            }

            // Refresh Gambits in their layout
            if (_ownedGambitsLayout != null)
            {
                RefreshGambitView("StrawberryGambit", _state.Modifiers.StrawberryGambitCount, "Strawberry Gambit", "Strawberry wins: x10 per Strawberry symbol.\nSacrifice: if no Strawberry wins, -25% output per Strawberry on grid.", "SG", new Color(0.85f, 0.2f, 0.3f, 1f));
                RefreshGambitView("BatchTenGambit", _state.Modifiers.BatchTenGambitCount, "Tenfold Batch", "Forced to roll at 10x batch factor.\nBonus: gain 10x the roll amount.", "TB", new Color(0.2f, 0.6f, 0.8f, 1f));
                RefreshGambitView("Joker1000xGambit", _state.Modifiers.Joker1000xGambitCount, "Joker 1000x", "Score win with Joker on left reel for 1000x roll output.\nRisk: 15% chance to lose left reel Joker.", "J1K", new Color(0.7f, 0.3f, 0.85f, 1f));
                RefreshGambitView("AppleDecayGambit", _state.Modifiers.AppleDecayGambitCount, "Apple Decay", "Apple wins: pay 5x.\nSacrifice: every spin without an Apple reduces Apple value by 1.", "AD", new Color(0.95f, 0.65f, 0.1f, 1f));
            }
        }

        private void RefreshGambitView(string key, int count, string title, string description, string shortLabel, Color color)
        {
            OwnedUpgradeView view;
            _ownedGambitViews.TryGetValue(key, out view);

            if (count <= 0)
            {
                if (view != null)
                {
                    _ownedGambitViews.Remove(key);
                    Destroy(view.gameObject);
                }
                return;
            }

            if (view == null)
            {
                view = Instantiate(_ownedUpgradePrefab, _ownedGambitsLayout);
                _ownedGambitViews[key] = view;
            }

            view.Bind(
                null,
                shortLabel,
                color,
                count,
                title,
                description,
                "Active Stack: x" + count,
                _upgradeTooltip);
        }

        private void ClearOwnedUpgradeViews()
        {
            if (_upgradeTooltip != null)
            {
                _upgradeTooltip.Hide();
            }

            foreach (var pair in _ownedUpgradeViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    Destroy(pair.Value.gameObject);
                }
            }
            _ownedUpgradeViews.Clear();

            foreach (var pair in _ownedGambitViews)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }
            _ownedGambitViews.Clear();
        }

        private void ShowGambitSelectionOverlay()
        {
            if (_gambitOverlay != null)
            {
                Destroy(_gambitOverlay);
            }

            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) return;

            _isSpinAnimating = true; // Lock controls while overlay is open

            _gambitOverlay = new GameObject("GambitOverlay", typeof(RectTransform));
            _gambitOverlay.transform.SetParent(canvas.transform, false);
            var rect = _gambitOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImage = _gambitOverlay.AddComponent<Image>();
            bgImage.color = new Color(0.04f, 0.04f, 0.08f, 0.95f);

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(_gambitOverlay.transform, false);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "SELECT A GAMBIT";
            titleText.fontSize = 46;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.82f, 0f, 1f); // Gold
            titleText.alignment = TextAlignmentOptions.Center;
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Subtitle
            var subObj = new GameObject("Subtitle", typeof(RectTransform));
            subObj.transform.SetParent(_gambitOverlay.transform, false);
            var subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = "Choose a gamble to carry forward. They will stack.";
            subText.fontSize = 20;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.7f, 0.8f, 0.9f, 1f);
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.75f);
            subRect.anchorMax = new Vector2(1f, 0.8f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            // Panel for Cards
            var cardPanelObj = new GameObject("CardPanel", typeof(RectTransform));
            cardPanelObj.transform.SetParent(_gambitOverlay.transform, false);
            var cardPanelRect = cardPanelObj.GetComponent<RectTransform>();
            cardPanelRect.anchorMin = new Vector2(0.12f, 0.15f);
            cardPanelRect.anchorMax = new Vector2(0.88f, 0.7f);
            cardPanelRect.offsetMin = Vector2.zero;
            cardPanelRect.offsetMax = Vector2.zero;

            var layout = cardPanelObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Pick 3 random gambits out of 4
            var pool = new List<int> { 1, 2, 3, 4 };
            // Simple Fisher-Yates shuffle
            for (int i = 0; i < pool.Count; i++)
            {
                int temp = pool[i];
                int rIdx = UnityEngine.Random.Range(i, pool.Count);
                pool[i] = pool[rIdx];
                pool[rIdx] = temp;
            }

            for (int i = 0; i < 3; i++)
            {
                int option = pool[i];
                if (option == 1)
                {
                    CreateGambitCard(cardPanelObj.transform, 
                        "Strawberry Gambit", 
                        "Strawberry wins: x10 per Strawberry symbol.\n\n<color=#ff4444>Sacrifice:</color>\nIf no Strawberry wins, -25% total output per Strawberry symbol on the grid.",
                        new Color(0.85f, 0.2f, 0.3f, 1f),
                        delegate { SelectGambit(1); });
                }
                else if (option == 2)
                {
                    CreateGambitCard(cardPanelObj.transform, 
                        "Tenfold Batch", 
                        "Forced to roll at 10x batch factor.\n\n<color=#55ff55>Bonus:</color>\nImmediately receive 10 times the roll amount.",
                        new Color(0.2f, 0.6f, 0.8f, 1f),
                        delegate { SelectGambit(2); });
                }
                else if (option == 3)
                {
                    CreateGambitCard(cardPanelObj.transform, 
                        "Joker's 1000x", 
                        "Score win with Joker on left reel for a 1000x multiplier.\n\n<color=#ffaa00>Risk:</color>\n15% chance to lose the Joker on the left reel strip (replaced by random fruit).",
                        new Color(0.7f, 0.3f, 0.85f, 1f),
                        delegate { SelectGambit(3); });
                }
                else if (option == 4)
                {
                    CreateGambitCard(cardPanelObj.transform, 
                        "Apple Decay", 
                        "Apple wins pay 5x multiplier.\n\n<color=#ff6600>Sacrifice:</color>\nEvery spin that does not contain an Apple reduces value of Apples by 1.",
                        new Color(0.95f, 0.65f, 0.1f, 1f),
                        delegate { SelectGambit(4); });
                }
            }

            PrepareDefaultFonts();
        }

        private void CreateGambitCard(Transform parent, string title, string description, Color cardColor, Action onClickAction)
        {
            var cardObj = new GameObject(title + "_Card", typeof(RectTransform));
            cardObj.transform.SetParent(parent, false);

            var cardImage = cardObj.AddComponent<Image>();
            cardImage.color = new Color(0.08f, 0.08f, 0.12f, 1f);

            var outlineObj = new GameObject("Outline", typeof(RectTransform));
            outlineObj.transform.SetParent(cardObj.transform, false);
            var outlineRect = outlineObj.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = new Vector2(4f, 4f);
            outlineRect.offsetMax = new Vector2(-4f, -4f);
            var outlineImage = outlineObj.AddComponent<Image>();
            outlineImage.color = cardColor * 0.35f;

            var button = cardObj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            button.onClick.AddListener(delegate
            {
                cardObj.transform.DOPunchScale(Vector3.one * -0.05f, 0.15f, 0, 0f).OnComplete(delegate
                {
                    onClickAction();
                });
            });

            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(outlineObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(15f, 15f);
            contentRect.offsetMax = new Vector2(-15f, -15f);

            var vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 15f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            var titleTextObj = new GameObject("Title", typeof(RectTransform));
            titleTextObj.transform.SetParent(contentObj.transform, false);
            var tText = titleTextObj.AddComponent<TextMeshProUGUI>();
            tText.text = title;
            tText.fontSize = 24;
            tText.fontStyle = FontStyles.Bold;
            tText.color = cardColor;
            tText.alignment = TextAlignmentOptions.Center;

            var descTextObj = new GameObject("Description", typeof(RectTransform));
            descTextObj.transform.SetParent(contentObj.transform, false);
            var dText = descTextObj.AddComponent<TextMeshProUGUI>();
            dText.text = description;
            dText.fontSize = 16;
            dText.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            dText.alignment = TextAlignmentOptions.Center;
        }

        private void SelectGambit(int option)
        {
            if (_state == null) return;

            if (option == 1)
            {
                _state.Modifiers.StrawberryGambitCount++;
                _resultText.text = "Accepted: Strawberry Gambit!";
            }
            else if (option == 2)
            {
                _state.Modifiers.BatchTenGambitCount++;
                _state.RollsRemaining *= 10;
                _resultText.text = "Accepted: Tenfold Batch!";
            }
            else if (option == 3)
            {
                _state.Modifiers.Joker1000xGambitCount++;
                _resultText.text = "Accepted: Joker's 1000x!";
            }
            else if (option == 4)
            {
                _state.Modifiers.AppleDecayGambitCount++;
                _resultText.text = "Accepted: Apple Decay!";
            }

            if (_gambitOverlay != null)
            {
                Destroy(_gambitOverlay);
                _gambitOverlay = null;
            }

            _isSpinAnimating = false;
            RefreshView();
        }

        private void ClearOrganViews()
        {
            foreach (var view in _organViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _organViews.Clear();
        }

        private void InitializeOrganViews()
        {
            ClearOrganViews();
            if (_organsLayout == null || _ownedUpgradePrefab == null)
            {
                return;
            }

            int count = _state.Config.OrganCount;
            for (int i = 0; i < count; i++)
            {
                int organNumber = i + 1;
                var view = Instantiate(_ownedUpgradePrefab, _organsLayout);
                _organViews.Add(view);

                string organName = GameBalance.OrganNameForLoss(organNumber);

                var countBadge = view.transform.Find("CountBadge");
                if (countBadge != null)
                {
                    countBadge.gameObject.SetActive(false);
                }

                view.Bind(
                    null,
                    OrganFallbackLabel(organNumber),
                    OrganFallbackColor(organNumber),
                    1,
                    organName,
                    "Organ needed to survive.",
                    "Healthy",
                    _upgradeTooltip
                );

                var tooltipTrigger = view.GetComponent<UpgradeTooltipTrigger>();
                if (tooltipTrigger != null)
                {
                    tooltipTrigger.Bind(_upgradeTooltip, organName, "Organ needed to survive.", "Status: Healthy");
                }
            }
        }

        private void RefreshOrganViews(bool playAnimation)
        {
            int losses = _state.OrganLosses;
            for (int i = 0; i < _organViews.Count; i++)
            {
                int organNumber = i + 1;
                var view = _organViews[i];
                if (view == null)
                {
                    continue;
                }

                bool isLost = organNumber <= losses;
                if (isLost)
                {
                    _organViews[i] = null;
                    if (playAnimation)
                    {
                        view.PunchScaleAndFadeOut(0.5f);
                    }
                    else
                    {
                        Destroy(view.gameObject);
                    }
                }
            }
        }

        private static string OrganFallbackLabel(int organNumber)
        {
            switch (organNumber)
            {
                case 1: return "M";
                case 2: return "KC";
                case 3: return "B";
                case 4: return "AC";
                case 5: return "K";
                default: return "O";
            }
        }

        private static Color OrganFallbackColor(int organNumber)
        {
            switch (organNumber)
            {
                case 1: return new Color(0.85f, 0.45f, 0.45f, 1f);
                case 2: return new Color(0.5f, 0.2f, 0.2f, 1f);
                case 3: return new Color(0.9f, 0.6f, 0.4f, 1f);
                case 4: return new Color(0.4f, 0.7f, 0.8f, 1f);
                case 5: return new Color(0.9f, 0.1f, 0.2f, 1f);
                default: return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        private string CurrentUpgradeEffect(ShopOfferKind kind)
        {
            switch (kind)
            {
                case ShopOfferKind.StrawberryValue: return "Current Strawberry value: x" + _state.Modifiers.StrawberryValue;
                case ShopOfferKind.CherryValue: return "Current Cherry value: x" + _state.Modifiers.CherryValue;
                case ShopOfferKind.BananaValue: return "Current Banana value: x" + _state.Modifiers.BananaValue;
                case ShopOfferKind.OrangeValue: return "Current Orange value: x" + _state.Modifiers.OrangeValue;
                case ShopOfferKind.AppleValue: return "Current Apple value: x" + _state.Modifiers.AppleValue;
                case ShopOfferKind.MoneyMultiplier: return "Current money multiplier: x" + _state.Modifiers.MoneyMultiplier;
                case ShopOfferKind.FreeSpins: return "Active free spins: +" + _state.Modifiers.TemporaryFreeSpins;
                case ShopOfferKind.BaseRollMultiplierX2:
                case ShopOfferKind.BaseRollMultiplierX10: return "Current base rolls: x" + _state.Modifiers.BaseRollMultiplier;
                case ShopOfferKind.BaseOutputMultiplier: return "Current output multiplier: x" + _state.Modifiers.BaseOutputMultiplier;
                case ShopOfferKind.HorizontalMatchMultiplier: return "Horizontal matches and reward pulses: x" + _state.Modifiers.HorizontalMatchCountMultiplier;
                case ShopOfferKind.VerticalMatchMultiplier: return "Vertical matches and reward pulses: x" + _state.Modifiers.VerticalMatchCountMultiplier;
                case ShopOfferKind.CrissCrossMatchMultiplier: return "Criss-cross matches and reward pulses: x" + _state.Modifiers.CrissCrossMatchCountMultiplier;
                default: return string.Empty;
            }
        }

        private static string UpgradeFallbackLabel(ShopOfferKind kind)
        {
            switch (kind)
            {
                case ShopOfferKind.StrawberryValue: return "S";
                case ShopOfferKind.CherryValue: return "C";
                case ShopOfferKind.BananaValue: return "B";
                case ShopOfferKind.OrangeValue: return "O";
                case ShopOfferKind.AppleValue: return "A";
                case ShopOfferKind.MoneyMultiplier: return "TL";
                case ShopOfferKind.FreeSpins: return "+";
                case ShopOfferKind.BaseRollMultiplierX2: return "R2";
                case ShopOfferKind.BaseRollMultiplierX10: return "R10";
                case ShopOfferKind.BaseOutputMultiplier: return "OUT";
                case ShopOfferKind.HorizontalMatchMultiplier: return "H";
                case ShopOfferKind.VerticalMatchMultiplier: return "V";
                case ShopOfferKind.CrissCrossMatchMultiplier: return "X";
                default: return "?";
            }
        }

        private static Color UpgradeFallbackColor(ShopOfferKind kind)
        {
            switch (kind)
            {
                case ShopOfferKind.StrawberryValue: return new Color(0.85f, 0.36f, 0.42f, 1f);
                case ShopOfferKind.CherryValue: return new Color(0.72f, 0.25f, 0.32f, 1f);
                case ShopOfferKind.BananaValue: return new Color(0.95f, 0.78f, 0.26f, 1f);
                case ShopOfferKind.OrangeValue: return new Color(0.94f, 0.49f, 0.18f, 1f);
                case ShopOfferKind.AppleValue: return new Color(0.53f, 0.72f, 0.33f, 1f);
                case ShopOfferKind.MoneyMultiplier: return new Color(0.30f, 0.66f, 0.45f, 1f);
                case ShopOfferKind.FreeSpins: return new Color(0.34f, 0.62f, 0.85f, 1f);
                case ShopOfferKind.BaseRollMultiplierX2:
                case ShopOfferKind.BaseRollMultiplierX10: return new Color(0.57f, 0.44f, 0.78f, 1f);
                case ShopOfferKind.BaseOutputMultiplier: return new Color(0.78f, 0.49f, 0.76f, 1f);
                case ShopOfferKind.HorizontalMatchMultiplier: return new Color(0.24f, 0.67f, 0.86f, 1f);
                case ShopOfferKind.VerticalMatchMultiplier: return new Color(0.35f, 0.78f, 0.57f, 1f);
                case ShopOfferKind.CrissCrossMatchMultiplier: return new Color(0.95f, 0.50f, 0.30f, 1f);
                default: return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }



        private string BuildResultSummary(SpinResult result)
        {
            if (result.Score.Wins.Count == 0)
            {
                return result.Message;
            }

            var summary = "Wins: ";
            for (var index = 0; index < result.Score.Wins.Count; index++)
            {
                if (index > 0)
                {
                    summary += ", ";
                }

                var win = result.Score.Wins[index];
                summary += win.Payline.Name + " (" + (win.IsTripleJoker ? "Triple Joker" : win.ResolvedSymbol.ToString()) + ")";
            }

            if (result.ThresholdCleared || result.OrganLost)
            {
                summary += ". " + result.Message;
            }

            return summary;
        }

        private void RefreshEndScreenStats()
        {
            if (_state == null)
            {
                return;
            }

            var summary = BuildRunStatsSummary();
            _gameOverRunStatsText.text = summary;
            _victoryRunStatsText.text = summary;
        }

        private string BuildRunStatsSummary()
        {
            var stats = _state.Stats;
            var summary = new StringBuilder();
            summary.AppendLine("RUN STATS");
            summary.AppendLine("Rolls used: " + stats.RollsUsed);
            summary.AppendLine("Highest threshold: " + stats.HighestThresholdReached + "/" + _state.Config.ThresholdCount);
            summary.AppendLine("Total earned: " + MoneyFormatter.FormatTL(stats.TotalEarnedKurus));
            summary.AppendLine("Total spent: " + MoneyFormatter.FormatTL(stats.TotalSpentKurus));
            summary.AppendLine("Jackpots: " + stats.JackpotsScored);
            summary.AppendLine("Items purchased: " + stats.TotalItemsPurchased);
            summary.AppendLine();
            summary.AppendLine("SYMBOL EARNINGS");
            AppendSymbolRunStats(summary, SymbolKind.Strawberry, "Strawberry");
            AppendSymbolRunStats(summary, SymbolKind.Cherry, "Cherry");
            AppendSymbolRunStats(summary, SymbolKind.Banana, "Banana");
            AppendSymbolRunStats(summary, SymbolKind.Orange, "Orange");
            AppendSymbolRunStats(summary, SymbolKind.Apple, "Apple");
            AppendSymbolRunStats(summary, SymbolKind.Joker, "Triple Joker");
            return summary.ToString().TrimEnd();
        }

        private void AppendSymbolRunStats(StringBuilder summary, SymbolKind symbol, string label)
        {
            var symbolStats = _state.Stats.GetSymbolStats(symbol);
            summary.Append(label);
            summary.Append(": ");
            summary.Append(symbolStats.WinningPaylineCount);
            summary.Append(symbolStats.WinningPaylineCount == 1 ? " payline — " : " paylines — ");
            summary.Append(MoneyFormatter.FormatTL(symbolStats.GeneratedKurus));
            summary.AppendLine();
        }

        private void ClearGrid()
        {
            for (var column = 0; column < GameBalance.GridColumns; column++)
            {
                if (_reelScrollers[column] != null)
                {
                    _reelScrollers[column].Clear();
                }
            }

            for (var row = 0; row < GameBalance.GridRows; row++)
            {
                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    if (_reelScrollers[column] == null)
                    {
                        _cellTexts[row, column].text = "?";
                        _cellImages[row, column].color = new Color(0.88f, 0.88f, 0.88f, 1f);
                    }
                }
            }
        }

        private void UpdateGrid(SymbolKind[,] grid)
        {
            for (var column = 0; column < GameBalance.GridColumns; column++)
            {
                var strip = _rulesConfig.ReelStripAt(column);
                var g0 = grid[0, column];
                var g1 = grid[1, column];
                var g2 = grid[2, column];
                var stopIndex = FindStopIndex(strip, g0, g1, g2);

                if (_reelScrollers[column] != null)
                {
                    _reelScrollers[column].StopSpin(stopIndex, 0f, null);
                }
            }

            for (var row = 0; row < GameBalance.GridRows; row++)
            {
                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    if (_reelScrollers[column] == null)
                    {
                        var symbol = grid[row, column];
                        _cellTexts[row, column].text = SymbolLabel(symbol);
                        _cellImages[row, column].color = SymbolColor(symbol);
                    }
                }
            }
        }

        private int FindStopIndex(SymbolKind[] strip, SymbolKind g0, SymbolKind g1, SymbolKind g2)
        {
            for (int i = 0; i < strip.Length; i++)
            {
                if (strip[i] == g0 && strip[(i + 1) % strip.Length] == g1 && strip[(i + 2) % strip.Length] == g2)
                {
                    return i;
                }
            }
            return 0;
        }

        private struct CellRef
        {
            public Image Image;
            public TextMeshProUGUI Text;
            public Vector3 OriginalScale;
            public Vector3 OriginalTextScale;
            public Color OriginalColor;
            public Color OriginalTextColor;
        }

        private Image GetGridCellImage(int row, int column)
        {
            if (_reelScrollers[column] != null)
            {
                return _reelScrollers[column].GetCellImage(row);
            }
            return _cellImages[row, column];
        }

        private TextMeshProUGUI GetGridCellText(int row, int column)
        {
            if (_reelScrollers[column] != null)
            {
                return _reelScrollers[column].GetCellText(row);
            }
            return _cellTexts[row, column];
        }

        private IEnumerator AnimateWinHighlighting(SpinResult result)
        {
            var wins = result.Score.Wins;
            BigInteger accumulatedPayout = BigInteger.Zero;
            var canvas = GameObject.Find("GameCanvas");
            Transform spawnParent = canvas != null ? canvas.transform : transform;
            BigInteger startCash = result.CashBeforeSpinKurus;

            // Each logical hit gets a payout share. x2, x5, and x10 upgrades therefore play
            // exactly that many fast reward pulses. The x100 tier is compacted into rapid
            // bursts so a full-board win stays punchy instead of trapping the player in a long queue.

            for (int winIndex = 0; winIndex < wins.Count; winIndex++)
            {
                var win = wins[winIndex];
                var cells = CollectWinningCells(win);

                BigInteger winStartPayout = accumulatedPayout;
                BigInteger winPayout = win.FinalPayoutKurus;
                int logicalRewardHits = result.Score.RewardAnimationCount(win);
                int pulseWaves = Mathf.Min(logicalRewardHits, MaximumRewardPulseWaves);
                int hitsPerPulse = Mathf.CeilToInt((float)logicalRewardHits / pulseWaves);

                for (int pulseIndex = 0; pulseIndex < pulseWaves; pulseIndex++)
                {
                    int completedHitsBeforePulse = pulseIndex * hitsPerPulse;
                    int completedHitsAfterPulse = Mathf.Min(logicalRewardHits, completedHitsBeforePulse + hitsPerPulse);
                    int hitsInPulse = completedHitsAfterPulse - completedHitsBeforePulse;
                    BigInteger stepStartPayout = PayoutAtRewardHit(winStartPayout, winPayout, completedHitsBeforePulse, logicalRewardHits);
                    BigInteger stepEndPayout = PayoutAtRewardHit(winStartPayout, winPayout, completedHitsAfterPulse, logicalRewardHits);
                    float stepDuration = RewardPulseDuration(winIndex, pulseIndex, pulseWaves);
                    var spawnedCoins = new List<GameObject>();
                    var stepSequence = CreateRewardPulse(
                        result,
                        win,
                        cells,
                        spawnParent,
                        startCash,
                        completedHitsBeforePulse,
                        completedHitsAfterPulse,
                        logicalRewardHits,
                        hitsInPulse,
                        stepStartPayout,
                        stepEndPayout,
                        stepDuration,
                        spawnedCoins);

                    stepSequence.OnKill(delegate
                    {
                        RestorePaylineVisuals(cells);
                        ReturnSpawnedCoins(spawnedCoins);
                    });
                    stepSequence.Play();
                    yield return stepSequence.WaitForCompletion();

                    // This also covers projects whose DOTween settings disable auto-kill.
                    RestorePaylineVisuals(cells);
                    ReturnSpawnedCoins(spawnedCoins);
                }

                accumulatedPayout += winPayout;
            }

            // Finally, snap payout text and show final results summary
            _payoutText.text = "Last payout: " + MoneyFormatter.FormatTL(result.Score.PayoutKurus) + " | combo x" + result.Score.ComboMultiplier + " | batch x" + result.Score.BatchFactor;
            _resultText.text = BuildResultSummary(result);
            UpdateThresholdBar(_state.CashKurus, _state.CurrentTargetKurus);
        }

        private List<CellRef> CollectWinningCells(PaylineWin win)
        {
            var cells = new List<CellRef>();
            var positions = win.Payline.Positions;
            for (int index = 0; index < positions.Length; index++)
            {
                var position = positions[index];
                var image = GetGridCellImage(position.Row, position.Column);
                var text = GetGridCellText(position.Row, position.Column);
                if (image == null)
                {
                    continue;
                }

                cells.Add(new CellRef
                {
                    Image = image,
                    Text = text,
                    OriginalScale = image.rectTransform.localScale,
                    OriginalTextScale = text != null ? text.transform.localScale : Vector3.one,
                    OriginalColor = image.color,
                    OriginalTextColor = text != null ? text.color : Color.white
                });
            }

            return cells;
        }

        private Sequence CreateRewardPulse(
            SpinResult result,
            PaylineWin win,
            List<CellRef> cells,
            Transform spawnParent,
            BigInteger startCash,
            int completedHitsBeforePulse,
            int completedHitsAfterPulse,
            int logicalRewardHits,
            int hitsInPulse,
            BigInteger stepStartPayout,
            BigInteger stepEndPayout,
            float duration,
            List<GameObject> spawnedCoins)
        {
            var step = DOTween.Sequence();
            Color highlightColor = new Color(1f, 0.9f, 0.2f, 1f);

            step.AppendCallback(delegate
            {
                BeginPaylineHighlight(cells);
                var hitLabel = logicalRewardHits > 1
                    ? " — hit " + (completedHitsBeforePulse + 1) + (hitsInPulse > 1 ? "-" + completedHitsAfterPulse : string.Empty) + "/" + logicalRewardHits
                    : string.Empty;
                var matchEchoLabel = win.MatchCountMultiplier > 1
                    ? " — " + PaylineGroupDisplayName(win.Payline.Group) + " Echo x" + win.MatchCountMultiplier
                    : string.Empty;
                _resultText.text = "Scored: " + win.Payline.Name + " (" + (win.IsTripleJoker ? "Triple Joker" : win.ResolvedSymbol.ToString()) + ")"
                    + matchEchoLabel + hitLabel;
                UpdateBatchPayout(result, startCash, stepStartPayout, stepEndPayout, 0f);
            });

            step.Append(DOTween.To(
                () => 0f,
                progress => ApplyPaylinePulse(cells, progress, highlightColor),
                1f,
                duration).SetEase(Ease.Linear));

            step.Join(DOTween.To(
                () => 0f,
                progress => UpdateBatchPayout(result, startCash, stepStartPayout, stepEndPayout, progress),
                1f,
                duration).SetEase(Ease.Linear).OnComplete(delegate
                {
                    UpdateBatchPayout(result, startCash, stepStartPayout, stepEndPayout, 1f);
                }));

            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                int coinFlights = Mathf.Min(hitsInPulse, MaximumCoinFlightsPerCellPerPulse);
                for (int coinIndex = 0; coinIndex < coinFlights; coinIndex++)
                {
                    var coinFlight = CreateCoinFlight(cells[cellIndex], spawnParent, duration, spawnedCoins);
                    if (coinFlight != null)
                    {
                        step.Join(coinFlight);
                    }
                }
            }

            step.AppendCallback(delegate { RestorePaylineVisuals(cells); });
            return step;
        }

        private Sequence CreateCoinFlight(CellRef cell, Transform spawnParent, float duration, List<GameObject> spawnedCoins)
        {
            var coin = SpawnCoin(cell.Image.transform.position, spawnParent);
            if (coin == null)
            {
                return null;
            }

            spawnedCoins.Add(coin);
            Vector3 startPosition = coin.transform.position;
            Vector3 startScale = coin.transform.localScale;
            Vector3 burstOffset = new Vector3(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-40f, 40f), 0f);
            Vector3 burstPosition = startPosition + burstOffset;
            Vector3 targetPosition = _thresholdBarRect != null ? _thresholdBarRect.position : _payoutText.transform.position;
            float burstDuration = Mathf.Max(0.005f, duration * 0.35f);
            float flightDuration = Mathf.Max(0.005f, duration - burstDuration);

            coin.SetActive(false);
            var flight = DOTween.Sequence();
            flight.AppendCallback(delegate
            {
                if (coin == null)
                {
                    return;
                }

                coin.transform.position = startPosition;
                coin.transform.localScale = startScale;
                coin.SetActive(true);
            });
            flight.Append(coin.transform.DOMove(burstPosition, burstDuration).SetEase(Ease.OutQuad));
            flight.Join(coin.transform.DOPunchScale(Vector3.one * 0.2f, burstDuration));
            flight.Append(coin.transform.DOMove(targetPosition, flightDuration).SetEase(Ease.InQuad));
            flight.Join(coin.transform.DOScale(Vector3.zero, flightDuration).SetEase(Ease.InQuad));
            flight.OnComplete(delegate
            {
                if (coin != null)
                {
                    ReturnCoin(coin);
                }
            });
            return flight;
        }

        private float RewardPulseDuration(int winIndex, int pulseIndex, int pulseWaves)
        {
            float winDuration = FirstWinPulseDuration / (1f + winIndex * 0.4f);
            float initialPulseDuration = pulseWaves > 1 ? winDuration / pulseWaves : winDuration;
            return Mathf.Max(MinimumWinPulseDuration, initialPulseDuration / (1f + pulseIndex * RewardPulseSpeedIncrease));
        }

        private static BigInteger PayoutAtRewardHit(BigInteger winStartPayout, BigInteger winPayout, int completedRewardHits, int totalRewardHits)
        {
            return winStartPayout + winPayout * completedRewardHits / totalRewardHits;
        }

        private static string PaylineGroupDisplayName(PaylineGroup group)
        {
            switch (group)
            {
                case PaylineGroup.Horizontal: return "Horizontal";
                case PaylineGroup.Vertical: return "Vertical";
                case PaylineGroup.CrissCross: return "Criss-Cross";
                default: return "Match";
            }
        }

        private void BeginPaylineHighlight(List<CellRef> cells)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                var image = cells[index].Image;
                if (image == null)
                {
                    continue;
                }

                var outline = image.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = image.gameObject.AddComponent<Outline>();
                }

                outline.enabled = true;
                outline.effectColor = new Color(1f, 0.8f, 0f, 1f);
                outline.effectDistance = new Vector2(4f, 4f);
            }
        }

        private static void ApplyPaylinePulse(List<CellRef> cells, float progress, Color highlightColor)
        {
            float pulse = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            float scaleFactor = 1f + 0.18f * pulse;
            Color textHighlightColor = new Color(1f, 0.3f, 0.1f, 1f);

            for (int index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (cell.Image != null)
                {
                    cell.Image.rectTransform.localScale = cell.OriginalScale * scaleFactor;
                    cell.Image.color = Color.Lerp(cell.OriginalColor, highlightColor, pulse);
                }

                if (cell.Text != null)
                {
                    cell.Text.transform.localScale = cell.OriginalTextScale * scaleFactor;
                    cell.Text.color = Color.Lerp(cell.OriginalTextColor, textHighlightColor, pulse);
                }
            }
        }

        private static void RestorePaylineVisuals(List<CellRef> cells)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (cell.Image != null)
                {
                    cell.Image.rectTransform.localScale = cell.OriginalScale;
                    cell.Image.color = cell.OriginalColor;

                    var outline = cell.Image.GetComponent<Outline>();
                    if (outline != null)
                    {
                        Destroy(outline);
                    }
                }

                if (cell.Text != null)
                {
                    cell.Text.transform.localScale = cell.OriginalTextScale;
                    cell.Text.color = cell.OriginalTextColor;
                }
            }
        }

        private void ReturnSpawnedCoins(List<GameObject> spawnedCoins)
        {
            for (int index = 0; index < spawnedCoins.Count; index++)
            {
                if (spawnedCoins[index] != null)
                {
                    ReturnCoin(spawnedCoins[index]);
                }
            }
        }

        private void UpdateBatchPayout(
            SpinResult result,
            BigInteger startCash,
            BigInteger stepStartPayout,
            BigInteger stepEndPayout,
            float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            BigInteger currentDisplay = stepStartPayout + (BigInteger)((double)(stepEndPayout - stepStartPayout) * clampedProgress);
            _payoutText.text = "Last payout: " + MoneyFormatter.FormatTL(currentDisplay) + " | combo x" + result.Score.ComboMultiplier + " | batch x" + result.Score.BatchFactor;
            UpdateThresholdBar(
                startCash + currentDisplay,
                result.TargetBeforeSpinKurus,
                result.ThresholdLevelBeforeSpin);
        }

        private GameObject SpawnCoin(Vector3 spawnPosition, Transform parent)
        {
            GameObject coin = null;
            for (int i = _coinPool.Count - 1; i >= 0; i--)
            {
                if (_coinPool[i] == null)
                {
                    _coinPool.RemoveAt(i);
                    continue;
                }
                if (!_coinPool[i].activeSelf)
                {
                    coin = _coinPool[i];
                    _coinPool.RemoveAt(i);
                    break;
                }
            }

            if (coin == null)
            {
                if (_coinPrefab != null)
                {
                    coin = Instantiate(_coinPrefab, parent);
                }
                else
                {
                    // Dynamic fallback UI image
                    coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
                    var rect = coin.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(30f, 30f);
                    var image = coin.GetComponent<Image>();
                    image.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold
                    
                    var outline = coin.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(1.5f, 1.5f);
                }
            }

            coin.transform.SetParent(parent, false);
            coin.transform.position = spawnPosition;
            coin.SetActive(true);
            return coin;
        }

        private void ReturnCoin(GameObject coin)
        {
            if (coin != null && coin.activeSelf)
            {
                coin.SetActive(false);
                _coinPool.Add(coin);
            }
        }

        private Sprite SymbolSprite(SymbolKind symbol)
        {
            if (_symbolDefinitions != null)
            {
                foreach (var definition in _symbolDefinitions)
                {
                    if (definition != null && definition.Symbol == symbol)
                    {
                        return definition.Icon;
                    }
                }
            }
            return null;
        }

        private string SymbolLabel(SymbolKind symbol)
        {
            if (_symbolDefinitions != null)
            {
                foreach (var definition in _symbolDefinitions)
                {
                    if (definition != null && definition.Symbol == symbol && !string.IsNullOrEmpty(definition.DisplayName))
                    {
                        return definition.DisplayName.ToUpperInvariant();
                    }
                }
            }

            switch (symbol)
            {
                case SymbolKind.Strawberry: return "STRAWBERRY";
                case SymbolKind.Cherry: return "CHERRY";
                case SymbolKind.Banana: return "BANANA";
                case SymbolKind.Orange: return "ORANGE";
                case SymbolKind.Apple: return "APPLE";
                default: return "JOKER";
            }
        }

        private static Color SymbolColor(SymbolKind symbol)
        {
            switch (symbol)
            {
                case SymbolKind.Strawberry: return new Color(0.95f, 0.84f, 0.84f, 1f);
                case SymbolKind.Cherry: return new Color(0.88f, 0.88f, 0.88f, 1f);
                case SymbolKind.Banana: return new Color(0.98f, 0.95f, 0.65f, 1f);
                case SymbolKind.Orange: return new Color(0.98f, 0.80f, 0.60f, 1f);
                case SymbolKind.Apple: return new Color(0.98f, 0.72f, 0.72f, 1f);
                default: return new Color(0.96f, 0.92f, 0.72f, 1f);
            }
        }

        private void PrepareDefaultFonts()
        {
            var defaultFont = TMP_Settings.defaultFontAsset;
            var textElements = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var textElement in textElements)
            {
                if (textElement.font == null && defaultFont != null)
                {
                    textElement.font = defaultFont;
                }

                textElement.enableWordWrapping = true;
            }
        }

        private static T Require<T>(Transform root, string path) where T : Component
        {
            var transform = RequireTransform(root, path);
            var component = transform.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException("Expected " + typeof(T).Name + " at GameCanvas/" + path + ".");
            }

            return component;
        }

        private void UpdateThresholdBar(BigInteger currentCash, BigInteger targetKurus)
        {
            UpdateThresholdBar(currentCash, targetKurus, _state.ThresholdLevel);
        }

        private void UpdateThresholdBar(BigInteger currentCash, BigInteger targetKurus, int thresholdLevel)
        {
            if (_thresholdBarFill == null || _thresholdBarBackground == null || _thresholdBarText == null)
            {
                return;
            }

            if (targetKurus <= 0)
            {
                _thresholdBarFill.fillAmount = 0f;
                _thresholdBarText.text = "Threshold " + thresholdLevel + "/" + _state.Config.ThresholdCount + ": " + MoneyFormatter.FormatTL(currentCash) + " / " + MoneyFormatter.FormatTL(targetKurus);
                return;
            }

            float fraction = (float)((double)currentCash / (double)targetKurus);
            if (fraction < 0f) fraction = 0f;
            if (fraction > 1f) fraction = 1f;

            _thresholdBarFill.fillAmount = fraction;

            _thresholdBarText.text = "Threshold " + thresholdLevel + "/" + _state.Config.ThresholdCount + ": " + MoneyFormatter.FormatTL(currentCash) + " / " + MoneyFormatter.FormatTL(targetKurus);
        }

        private static Transform RequireTransform(Transform root, string path)
        {
            var transform = root.Find(path);
            if (transform == null)
            {
                throw new InvalidOperationException("Expected UI object at GameCanvas/" + path + ".");
            }

            return transform;
        }
    }
}
