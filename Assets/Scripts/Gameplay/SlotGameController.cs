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
        private GambitItemDefinition[] _gambitItemDefinitions;
        private BalanceDefinition _balanceDefinition;
        private GameRulesConfig _rulesConfig;
        private readonly Dictionary<ShopOfferKind, ShopItemDefinition> _shopItemDefinitionsByKind = new Dictionary<ShopOfferKind, ShopItemDefinition>();
        private readonly Dictionary<GambitKind, GambitItemDefinition> _gambitItemDefinitionsByKind = new Dictionary<GambitKind, GambitItemDefinition>();
        private readonly Dictionary<ShopOfferKind, OwnedUpgradeView> _ownedUpgradeViews = new Dictionary<ShopOfferKind, OwnedUpgradeView>();
        private readonly Dictionary<GambitKind, OwnedUpgradeView> _ownedGambitViews = new Dictionary<GambitKind, OwnedUpgradeView>();
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
        private readonly List<GameObject> _rewardTextPool = new List<GameObject>();
        private readonly List<GameObject> _allRewardTextObjects = new List<GameObject>();

        private float FirstWinPulseDuration => _animationSettings != null ? _animationSettings.FirstWinPulseDuration : 0.6f;
        private float MinimumWinPulseDuration => _animationSettings != null ? _animationSettings.MinimumWinPulseDuration : 0.025f;
        private float RewardTextRiseDistance => _animationSettings != null ? _animationSettings.RewardTextRiseDistance : 90f;
        private float RewardTextFadeDuration => _animationSettings != null ? _animationSettings.RewardTextFadeDuration : 0.32f;
        private float RewardTextStartScale => _animationSettings != null ? _animationSettings.RewardTextStartScale : 0.75f;
        private float RewardTextPeakScale => _animationSettings != null ? _animationSettings.RewardTextPeakScale : 1.08f;
        private float RewardTextHorizontalSpread => _animationSettings != null ? _animationSettings.RewardTextHorizontalSpread : 28f;
        private float MultiplierStepDuration => _animationSettings != null ? _animationSettings.MultiplierStepDuration : 0.24f;
        private float QueueSpeedupPerEvent => _animationSettings != null ? _animationSettings.QueueSpeedupPerEvent : 0.08f;

        private TextMeshProUGUI _cashText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _rollsText;
        private TextMeshProUGUI _roundText;
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
        private Canvas _gameCanvas;
        private RectTransform _rewardTextParent;

        private SlotLever _lever;
        [SerializeField] private OwnedUpgradeView _ownedUpgradePrefab;
        private int _currentBatchFactor = 1;
        private readonly Color _buttonSelectedColor = new Color(1f, 0.29f, 0.26f, 1f); // Tomato red
        private readonly Color _buttonNormalColor = new Color(0.70f, 0.24f, 0.51f, 1f); // Magenta

        private void Start()
        {

            _symbolDefinitions = Resources.LoadAll<SymbolDefinition>("SerenaysGambit/Data/Symbols");
            _reelDefinitions = Resources.LoadAll<ReelDefinition>("SerenaysGambit/Data/Reels");
            _shopItemDefinitions = Resources.LoadAll<ShopItemDefinition>("SerenaysGambit/Data/ShopItems");
            _gambitItemDefinitions = Resources.LoadAll<GambitItemDefinition>("SerenaysGambit/Data/Gambits");
            _balanceDefinition = Resources.Load<BalanceDefinition>("SerenaysGambit/Data/Balance/DefaultBalance");
            IndexShopItemDefinitions();
            IndexGambitItemDefinitions();
            _rulesConfig = RuntimeGameConfigFactory.Create(
                _symbolDefinitions,
                _reelDefinitions,
                _shopItemDefinitions,
                _balanceDefinition,
                _gambitItemDefinitions);
            PrepareDefaultFonts();
            if (!BindView())
            {
                enabled = false;
                return;
            }

            var canvas = GameObject.Find("GameCanvas");
            if (canvas != null)
            {
                var mainCamera = Camera.main;
                _gameCanvas = canvas.GetComponent<Canvas>();
                _rewardTextParent = canvas.GetComponent<RectTransform>();
                if (_gameCanvas != null)
                {
                    _gameCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _gameCanvas.worldCamera = mainCamera;
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

            ClearRewardTextPool(true);
        }

        private static string FormatSidebarGoal(string amount)
        {
            return "<b>THE GOAL</b>\n<size=14>SCORE AT LEAST</size>\n<size=30><color=#FF534A>" + amount + "</color></size>";
        }

        private static string FormatSidebarGoalCleared(string amount)
        {
            return "<b>THE GOAL</b>\n<size=14>SCORE AT LEAST</size>\n<size=30><color=#55FF55><s>" + amount + "</s></color></size>";
        }

        private static string FormatSidebarPayout(string amount, int combo, int batch)
        {
            return FormatSidebarPayout(amount, combo, batch, "<color=#FFA001>COMBO x" + combo + "  |  BATCH x" + batch + "</color>");
        }

        private static string FormatSidebarPayout(string amount, int combo, int batch, string detail)
        {
            return "<b>ROUND SCORE</b>\n<size=25><color=#FFF5E8>" + amount + "</color></size>\n<size=13>" + detail + "</size>";
        }

        private static string FormatSidebarBank(string amount)
        {
            return "<b>BANK</b>\n<size=29><color=#FFA001>" + amount + "</color></size>";
        }

        private static string FormatSidebarRound(int level, int total)
        {
            return "<b>ANTE</b>\n<size=23><color=#FFA001>" + level + "</color><size=16> / " + total + "</size></size>";
        }

        private static string FormatSidebarRolls(int rollsRemaining)
        {
            return "<b>ROLLS LEFT</b>\n<size=29><color=#1599ED>" + rollsRemaining + "</color></size>";
        }

        private void StartNewRun()
        {
            ClearRewardTextPool(false);
            ClearOwnedUpgradeViews();
            ClearOrganViews();
            _isFirstRefresh = true;
            _runService = new RunService(Environment.TickCount, _rulesConfig);
            _shopService = _runService.Shop;
            _state = _runService.CreateNewRun();
            ResetReelStripsForRun();
            InitializeOrganViews();
            ClearGrid();
            _resultText.text = "Choose a batch to spin. Space = 1x, 5 = 5x, 0 = 10x.";
            _payoutText.text = FormatSidebarPayout(MoneyFormatter.FormatTL(BigInteger.Zero), 1, 1);
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

            var reelStripsBeforeSpin = CaptureReelStrips(_state.Config);
            var result = _runService.TrySpin(_state, batchFactor);
            if (!result.Accepted)
            {
                _resultText.text = result.Message;
                RefreshView();
                return;
            }

            StartCoroutine(DoSpinAnimation(result, reelStripsBeforeSpin));
        }

        private IEnumerator DoSpinAnimation(SpinResult result, SymbolKind[][] reelStripsBeforeSpin)
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
                var strip = reelStripsBeforeSpin[capturedCol];
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

            UpdateGrid(result.Grid, reelStripsBeforeSpin);

            if (result.Score.Wins != null && result.Score.Wins.Count > 0)
            {
                yield return StartCoroutine(AnimateWinHighlighting(result));
            }
            else
            {
                _payoutText.text = FormatSidebarPayout(
                    MoneyFormatter.FormatTL(result.Score.PayoutKurus),
                    result.Score.ComboMultiplier,
                    result.Score.BatchFactor);
                _resultText.text = BuildResultSummary(result);
            }

            _isSpinAnimating = false;

            if (result.JokerLostOnLeftReel)
            {
                if (_reelScrollers[0] != null)
                {
                    _reelScrollers[0].UpdateStrip(_state.Config.ReelStripAt(0));
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
                _cashText = Require<TextMeshProUGUI>(root, "MainContent/VerticalShelfPanel/CashText");
                _targetText = Require<TextMeshProUGUI>(root, "MainContent/VerticalShelfPanel/TargetText");
                _roundText = Require<TextMeshProUGUI>(root, "MainContent/VerticalShelfPanel/RoundText");
                _rollsText = Require<TextMeshProUGUI>(root, "MainContent/VerticalShelfPanel/RollsText");
                _organsLayout = Require<RectTransform>(root, "MainContent/VerticalShelfPanel/OrgansText");
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

                _ticketsText = Require<TextMeshProUGUI>(root, "MainContent/SerenayShopPanel/TicketsText");
                _payoutText = Require<TextMeshProUGUI>(root, "MainContent/VerticalShelfPanel/PayoutText");
                _payoutText.richText = true;
                _resultText = Require<TextMeshProUGUI>(root, "MainContent/SlotMachinePanel/ResultText");
                _shopWalletText = Require<TextMeshProUGUI>(root, "MainContent/SerenayShopPanel/ShopWalletText");
                _ownedUpgradesLayout = Require<RectTransform>(root, "MainContent/VerticalShelfPanel/OwnedUpgradesLayout");
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
                _spin1xButton = Require<Button>(root, "MainContent/VerticalShelfPanel/BatchControls/ButtonsRow/Spin1xButton");
                _spin5xButton = Require<Button>(root, "MainContent/VerticalShelfPanel/BatchControls/ButtonsRow/Spin5xButton");
                _spin10xButton = Require<Button>(root, "MainContent/VerticalShelfPanel/BatchControls/ButtonsRow/Spin10xButton");
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

            // Create OwnedGambitsLayout and OwnedGambitsHeading under vertical shelf
            try
            {
                var upgradesHeading = root.Find("MainContent/VerticalShelfPanel/OwnedUpgradesHeading")?.GetComponent<RectTransform>();
                var upgradesLayout = root.Find("MainContent/VerticalShelfPanel/OwnedUpgradesLayout")?.GetComponent<RectTransform>();

                if (upgradesHeading != null && upgradesLayout != null)
                {
                    // Create OwnedGambitsHeading
                    var gambitsHeadingObj = Instantiate(upgradesHeading.gameObject, upgradesHeading.parent);
                    gambitsHeadingObj.name = "OwnedGambitsHeading";

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
            if (_roundText != null)
            {
                _roundText.text = FormatSidebarRound(_state.ThresholdLevel, _state.Config.ThresholdCount);
            }
            _cashText.text = FormatSidebarBank(MoneyFormatter.FormatTL(_state.CashKurus));
            bool goalMet = _state.CashKurus >= _state.CurrentTargetKurus;
            _targetText.text = goalMet
                ? FormatSidebarGoalCleared(MoneyFormatter.FormatTL(_state.CurrentTargetKurus))
                : FormatSidebarGoal(MoneyFormatter.FormatTL(_state.CurrentTargetKurus));
            UpdateThresholdBar(_state.CashKurus, _state.CurrentTargetKurus);
            _rollsText.text = FormatSidebarRolls(_state.RollsRemaining);
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

        private void IndexGambitItemDefinitions()
        {
            _gambitItemDefinitionsByKind.Clear();
            if (_gambitItemDefinitions == null)
            {
                return;
            }

            foreach (var definition in _gambitItemDefinitions)
            {
                if (definition != null && Enum.IsDefined(typeof(GambitKind), definition.Kind))
                {
                    _gambitItemDefinitionsByKind[definition.Kind] = definition;
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

            if (_ownedGambitsLayout != null)
            {
                foreach (GambitKind kind in Enum.GetValues(typeof(GambitKind)))
                {
                    RefreshGambitView(kind, _state.Modifiers.GambitCount(kind));
                }
            }
        }

        private void RefreshGambitView(GambitKind kind, int count)
        {
            OwnedUpgradeView view;
            _ownedGambitViews.TryGetValue(kind, out view);

            if (count <= 0)
            {
                if (view != null)
                {
                    _ownedGambitViews.Remove(kind);
                    Destroy(view.gameObject);
                }
                return;
            }

            if (view == null)
            {
                view = Instantiate(_ownedUpgradePrefab, _ownedGambitsLayout);
                _ownedGambitViews[kind] = view;
            }

            GambitItemDefinition definition;
            _gambitItemDefinitionsByKind.TryGetValue(kind, out definition);
            var title = definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                ? definition.DisplayName
                : kind.ToString();
            var description = definition != null && !string.IsNullOrEmpty(definition.Description)
                ? definition.Description
                : "Gambit details are unavailable.";
            var shortLabel = definition != null && !string.IsNullOrEmpty(definition.ShortLabel)
                ? definition.ShortLabel
                : GambitFallbackLabel(kind);
            var color = definition != null && definition.AccentColor != Color.clear
                ? definition.AccentColor
                : GambitFallbackColor(kind);

            view.Bind(
                definition == null ? null : definition.Icon,
                shortLabel,
                color,
                count,
                title,
                description,
                "Active Stack: x" + count,
                _upgradeTooltip);
        }

        private static string GambitFallbackLabel(GambitKind kind)
        {
            switch (kind)
            {
                case GambitKind.Strawberry: return "SG";
                case GambitKind.BatchTen: return "TB";
                case GambitKind.Joker1000x: return "J1K";
                case GambitKind.AppleDecay: return "AD";
                default: return "?";
            }
        }

        private static Color GambitFallbackColor(GambitKind kind)
        {
            switch (kind)
            {
                case GambitKind.Strawberry: return new Color(0.85f, 0.2f, 0.3f, 1f);
                case GambitKind.BatchTen: return new Color(0.2f, 0.6f, 0.8f, 1f);
                case GambitKind.Joker1000x: return new Color(0.7f, 0.3f, 0.85f, 1f);
                case GambitKind.AppleDecay: return new Color(0.95f, 0.65f, 0.1f, 1f);
                default: return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
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

            var pool = new List<GambitItemDefinition>();
            var availableKinds = new HashSet<GambitKind>();
            if (_gambitItemDefinitions != null)
            {
                foreach (var definition in _gambitItemDefinitions)
                {
                    if (definition != null
                        && Enum.IsDefined(typeof(GambitKind), definition.Kind)
                        && availableKinds.Add(definition.Kind))
                    {
                        pool.Add(definition);
                    }
                }
            }

            for (int i = 0; i < pool.Count; i++)
            {
                var temp = pool[i];
                int rIdx = UnityEngine.Random.Range(i, pool.Count);
                pool[i] = pool[rIdx];
                pool[rIdx] = temp;
            }

            var cardsToShow = Mathf.Min(3, pool.Count);
            if (cardsToShow == 0)
            {
                Debug.LogError("No GambitItemDefinition assets were found under Resources/SerenaysGambit/Data/Gambits.");
                Destroy(_gambitOverlay);
                _gambitOverlay = null;
                _isSpinAnimating = false;
                return;
            }

            for (int i = 0; i < cardsToShow; i++)
            {
                var definition = pool[i];
                CreateGambitCard(
                    cardPanelObj.transform,
                    definition,
                    delegate { SelectGambit(definition); });
            }

            PrepareDefaultFonts();
        }

        private void CreateGambitCard(Transform parent, GambitItemDefinition definition, Action onClickAction)
        {
            var title = definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                ? definition.DisplayName
                : definition == null ? "Unknown Gambit" : definition.Kind.ToString();
            var description = definition != null && !string.IsNullOrEmpty(definition.Description)
                ? definition.Description
                : "Gambit details are unavailable.";
            var cardColor = definition != null && definition.AccentColor != Color.clear
                ? definition.AccentColor
                : definition == null ? Color.gray : GambitFallbackColor(definition.Kind);
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

            if (definition != null && definition.Icon != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(contentObj.transform, false);
                var icon = iconObj.AddComponent<Image>();
                icon.sprite = definition.Icon;
                icon.preserveAspect = true;
                var iconLayout = iconObj.AddComponent<LayoutElement>();
                iconLayout.preferredHeight = 72f;
                iconLayout.minHeight = 72f;
            }

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

        private void SelectGambit(GambitItemDefinition definition)
        {
            if (_state == null || definition == null)
            {
                return;
            }

            _state.Modifiers.AddGambit(definition.Kind);
            if (definition.Kind == GambitKind.BatchTen)
            {
                var gambitConfig = _state.Modifiers.GambitConfig(definition.Kind);
                var rollMultiplier = gambitConfig == null ? Math.Max(1, definition.RollMultiplier) : gambitConfig.RollMultiplier;
                _state.MultiplyRolls(rollMultiplier);
            }

            var displayName = string.IsNullOrEmpty(definition.DisplayName)
                ? definition.Kind.ToString()
                : definition.DisplayName;
            _resultText.text = "Accepted: " + displayName + "!";

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

        private void UpdateGrid(SymbolKind[,] grid, SymbolKind[][] reelStripsBeforeSpin)
        {
            for (var column = 0; column < GameBalance.GridColumns; column++)
            {
                var strip = reelStripsBeforeSpin[column];
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

        private void ResetReelStripsForRun()
        {
            for (var column = 0; column < GameBalance.GridColumns; column++)
            {
                if (_reelScrollers[column] != null)
                {
                    _reelScrollers[column].UpdateStrip(_state.Config.ReelStripAt(column));
                }
            }
        }

        private static SymbolKind[][] CaptureReelStrips(GameRulesConfig config)
        {
            var strips = new SymbolKind[GameBalance.GridColumns][];
            for (var column = 0; column < strips.Length; column++)
            {
                strips[column] = config.ReelStripAt(column);
            }

            return strips;
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
            public GridPosition Position;
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
            var animationEvents = RewardAnimationQueueBuilder.Build(result.Score);
            BigInteger startCash = result.CashBeforeSpinKurus;
            BigInteger displayedPayout = BigInteger.Zero;
            BigInteger accumulatedFinalPayout = BigInteger.Zero;
            var cachedCells = new Dictionary<PaylineWin, List<CellRef>>();
            Transform canvasTransform = _rewardTextParent != null ? _rewardTextParent : transform;
            var queueLength = animationEvents.Count;
            var matchDuration = RewardAnimationQueueBuilder.DurationForQueue(
                queueLength,
                FirstWinPulseDuration,
                MinimumWinPulseDuration,
                QueueSpeedupPerEvent);
            var multiplierDuration = RewardAnimationQueueBuilder.DurationForQueue(
                queueLength,
                MultiplierStepDuration,
                MinimumWinPulseDuration,
                QueueSpeedupPerEvent);

            for (var eventIndex = 0; eventIndex < animationEvents.Count; eventIndex++)
            {
                var animationEvent = animationEvents[eventIndex];
                List<CellRef> cells;
                if (!cachedCells.TryGetValue(animationEvent.Win, out cells))
                {
                    cells = CollectWinningCells(animationEvent.Win);
                    cachedCells.Add(animationEvent.Win, cells);
                }

                if (animationEvent.Kind == RewardAnimationEventKind.MatchAddition)
                {
                    var stepStartPayout = displayedPayout;
                    var stepEndPayout = displayedPayout + animationEvent.BaseAmountKurus;
                    var stepSequence = CreateMatchAdditionSequence(
                        result,
                        animationEvent,
                        cells,
                        canvasTransform,
                        startCash,
                        stepStartPayout,
                        stepEndPayout,
                        matchDuration);
                    stepSequence.Play();
                    yield return stepSequence.WaitForCompletion();
                    RestorePaylineVisuals(cells);
                    displayedPayout = stepEndPayout;
                }
                else
                {
                    var stepStartPayout = displayedPayout;
                    var stepEndPayout = accumulatedFinalPayout + animationEvent.Win.FinalPayoutKurus;
                    var stepSequence = CreateMultiplierSequence(
                        result,
                        animationEvent,
                        cells,
                        startCash,
                        stepStartPayout,
                        stepEndPayout,
                        multiplierDuration);
                    stepSequence.Play();
                    yield return stepSequence.WaitForCompletion();
                    RestorePaylineVisuals(cells);
                    displayedPayout = stepEndPayout;
                    accumulatedFinalPayout = stepEndPayout;
                }
            }

            _payoutText.text = FormatSidebarPayout(
                MoneyFormatter.FormatTL(result.Score.PayoutKurus),
                result.Score.ComboMultiplier,
                result.Score.BatchFactor);
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
                    Position = position,
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

        private Sequence CreateMatchAdditionSequence(
            SpinResult result,
            RewardAnimationEvent animationEvent,
            List<CellRef> cells,
            Transform spawnParent,
            BigInteger startCash,
            BigInteger stepStartPayout,
            BigInteger stepEndPayout,
            float duration)
        {
            var step = DOTween.Sequence();
            var highlightColor = new Color(1f, 0.9f, 0.2f, 1f);
            var matchDetail = FormatMatchStepDetail(animationEvent);

            step.AppendCallback(delegate
            {
                BeginPaylineHighlight(cells);
                for (var rewardIndex = 0; rewardIndex < animationEvent.CellRewards.Count; rewardIndex++)
                {
                    var reward = animationEvent.CellRewards[rewardIndex];
                    var cell = FindCell(cells, reward.Position);
                    if (cell.Image != null)
                    {
                        SpawnRewardText(cell.Image.transform.position, FormatFloatingReward(reward.AmountKurus), spawnParent, duration);
                    }
                }

                _resultText.text = BuildMatchResultText(animationEvent);
                UpdateQueuedPayout(result, startCash, stepStartPayout, matchDetail, 0f);
            });

            step.Append(DOTween.To(
                () => 0f,
                progress =>
                {
                    ApplyPaylinePulse(cells, progress, highlightColor);
                    UpdateQueuedPayout(result, startCash, stepStartPayout, matchDetail, progress, stepEndPayout);
                },
                1f,
                duration).SetEase(Ease.Linear));

            step.AppendCallback(delegate
            {
                UpdateQueuedPayout(result, startCash, stepStartPayout, matchDetail, 1f, stepEndPayout);
                RestorePaylineVisuals(cells);
            });
            step.OnKill(delegate { RestorePaylineVisuals(cells); });
            return step;
        }

        private Sequence CreateMultiplierSequence(
            SpinResult result,
            RewardAnimationEvent animationEvent,
            List<CellRef> cells,
            BigInteger startCash,
            BigInteger stepStartPayout,
            BigInteger stepEndPayout,
            float duration)
        {
            var step = DOTween.Sequence();
            var multiplierDetail = BuildMultiplierDetail(result, animationEvent.Win);

            step.AppendCallback(delegate
            {
                _resultText.text = "Applying multipliers to " + animationEvent.Win.Payline.Name + ".";
                UpdateQueuedPayout(result, startCash, stepStartPayout, multiplierDetail, 0f);
                PunchPayoutText();
            });

            step.Append(DOTween.To(
                () => 0f,
                progress => UpdateQueuedPayout(result, startCash, stepStartPayout, multiplierDetail, progress, stepEndPayout),
                1f,
                duration).SetEase(Ease.OutCubic));

            step.AppendCallback(delegate
            {
                UpdateQueuedPayout(result, startCash, stepEndPayout, multiplierDetail, 1f);
                PunchPayoutText();
            });
            step.OnKill(delegate { RestorePaylineVisuals(cells); });
            return step;
        }

        private static CellRef FindCell(List<CellRef> cells, GridPosition position)
        {
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index].Position.Row == position.Row && cells[index].Position.Column == position.Column)
                {
                    return cells[index];
                }
            }

            return new CellRef();
        }

        private static BigInteger PayoutAtProgress(BigInteger stepStartPayout, BigInteger stepEndPayout, float progress)
        {
            var clampedProgress = Mathf.Clamp01(progress);
            return stepStartPayout + (BigInteger)((double)(stepEndPayout - stepStartPayout) * clampedProgress);
        }

        private static string BuildMatchResultText(RewardAnimationEvent animationEvent)
        {
            var win = animationEvent.Win;
            return "Scored: " + win.Payline.Name + " (" + (win.IsTripleJoker ? "Triple Joker" : win.ResolvedSymbol.ToString()) + ")"
                + " — hit " + (animationEvent.HitIndex + 1) + "/" + animationEvent.TotalHits;
        }

        private string FormatMatchStepDetail(RewardAnimationEvent animationEvent)
        {
            var detail = "<b><color=#FFA001>" + FormatRewardAmount(animationEvent.BaseAmountKurus) + "</color></b>";
            if (animationEvent.TotalHits > 1)
            {
                detail += " <color=#74D7FF>REPEAT " + (animationEvent.HitIndex + 1) + "/" + animationEvent.TotalHits + "</color>";
            }

            return detail;
        }

        private string BuildMultiplierDetail(SpinResult result, PaylineWin win)
        {
            var parts = new List<string>();
            parts.Add("<color=#FFA001>×" + result.Score.ComboMultiplier + " COMBO</color>");

            if (_state != null && _state.Modifiers.MoneyMultiplier != BigInteger.One)
            {
                parts.Add("<color=#7EE7A8>×" + _state.Modifiers.MoneyMultiplier + " MONEY</color>");
            }

            if (_state != null && _state.Modifiers.BaseOutputMultiplier != BigInteger.One)
            {
                parts.Add("<color=#D89BFF>×" + _state.Modifiers.BaseOutputMultiplier + " OUTPUT</color>");
            }

            if (win.MatchCountMultiplier > 1)
            {
                parts.Add("<color=#74D7FF>REPEAT ×" + win.MatchCountMultiplier + "</color>");
            }

            if (result.Score.BatchFactor > 1)
            {
                parts.Add("<color=#5FA8FF>BATCH ×" + result.Score.BatchFactor + "</color>");
            }

            var gambitDetail = BuildGambitMultiplierDetail(result, win);
            if (!string.IsNullOrEmpty(gambitDetail))
            {
                parts.Add(gambitDetail);
            }

            var detail = new StringBuilder();
            for (var index = 0; index < parts.Count; index++)
            {
                if (index > 0)
                {
                    detail.Append("  ");
                }
                detail.Append(parts[index]);
            }
            return detail.ToString();
        }

        private string BuildGambitMultiplierDetail(SpinResult result, PaylineWin win)
        {
            var parts = new List<string>();
            if (win.IsTripleJoker)
            {
                parts.Add("<color=#FFDD70>TRIPLE JOKER</color>");
            }

            if (_state != null && _state.Modifiers.AppleDecayGambitCount > 0 && win.ResolvedSymbol == SymbolKind.Apple)
            {
                var appleConfig = _state.Modifiers.GambitConfig(GambitKind.AppleDecay);
                var applePayoutMultiplier = appleConfig == null ? 5 : appleConfig.PayoutMultiplier;
                var appleMultiplier = BigInteger.Pow(new BigInteger(applePayoutMultiplier), _state.Modifiers.AppleDecayGambitCount);
                parts.Add("<color=#FF8C69>APPLE ×" + appleMultiplier + "</color>");
            }

            if (_state != null && _state.Modifiers.Joker1000xGambitCount > 0 && WinUsesLeftReelJoker(result, win))
            {
                var jokerConfig = _state.Modifiers.GambitConfig(GambitKind.Joker1000x);
                var jokerPayoutMultiplier = jokerConfig == null ? 1000 : jokerConfig.PayoutMultiplier;
                var jokerMultiplier = BigInteger.Pow(new BigInteger(jokerPayoutMultiplier), _state.Modifiers.Joker1000xGambitCount);
                parts.Add("<color=#D9A0FF>JOKER ×" + jokerMultiplier + "</color>");
            }

            if (_state != null && _state.Modifiers.StrawberryGambitCount > 0)
            {
                parts.Add("<color=#FF8FBA>STRAWBERRY GAMBIT</color>");
            }

            var detail = new StringBuilder();
            for (var index = 0; index < parts.Count; index++)
            {
                if (index > 0)
                {
                    detail.Append("  ");
                }
                detail.Append(parts[index]);
            }
            return detail.ToString();
        }

        private static bool WinUsesLeftReelJoker(SpinResult result, PaylineWin win)
        {
            if (result == null || result.Grid == null)
            {
                return false;
            }

            foreach (var position in win.Payline.Positions)
            {
                if (position.Column == 0 && result.Grid[position.Row, position.Column] == SymbolKind.Joker)
                {
                    return true;
                }
            }

            return false;
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

        private void UpdateQueuedPayout(
            SpinResult result,
            BigInteger startCash,
            BigInteger stepStartPayout,
            string detail,
            float progress)
        {
            UpdateQueuedPayout(result, startCash, stepStartPayout, detail, progress, stepStartPayout);
        }

        private void UpdateQueuedPayout(
            SpinResult result,
            BigInteger startCash,
            BigInteger stepStartPayout,
            string detail,
            float progress,
            BigInteger stepEndPayout)
        {
            var currentDisplay = PayoutAtProgress(stepStartPayout, stepEndPayout, progress);
            if (_payoutText != null)
            {
                _payoutText.text = FormatSidebarPayout(
                    MoneyFormatter.FormatTL(currentDisplay),
                    result.Score.ComboMultiplier,
                    result.Score.BatchFactor,
                    detail);
            }

            UpdateThresholdBar(
                startCash + currentDisplay,
                result.TargetBeforeSpinKurus,
                result.ThresholdLevelBeforeSpin);
        }

        private string FormatFloatingReward(BigInteger amountKurus)
        {
            return "<b><size=22><color=#FFA001>" + FormatRewardAmount(amountKurus) + "</color></size></b>";
        }

        private static string FormatRewardAmount(BigInteger amountKurus)
        {
            var absolute = MoneyFormatter.FormatTL(BigInteger.Abs(amountKurus));
            return (amountKurus.Sign < 0 ? "- " : "+ ") + absolute.Substring(3) + " TL";
        }

        private void SpawnRewardText(Vector3 worldPosition, string richText, Transform parent, float stepDuration)
        {
            if (parent == null)
            {
                parent = transform;
            }

            var rewardObject = GetRewardTextObject(parent);
            var rect = rewardObject.GetComponent<RectTransform>();
            var canvasGroup = rewardObject.GetComponent<CanvasGroup>();
            var text = rewardObject.GetComponent<TextMeshProUGUI>();
            var startPosition = WorldToCanvasPosition(worldPosition);
            var endPosition = startPosition + new Vector2(
                UnityEngine.Random.Range(-RewardTextHorizontalSpread, RewardTextHorizontalSpread),
                RewardTextRiseDistance);
            var moveDuration = Mathf.Max(0.08f, stepDuration * 1.25f);
            var fadeDuration = Mathf.Max(0.08f, Mathf.Min(RewardTextFadeDuration, moveDuration));

            rect.anchoredPosition = startPosition;
            rect.localScale = Vector3.one * RewardTextStartScale;
            canvasGroup.alpha = 1f;
            text.text = richText;
            rewardObject.SetActive(true);

            var sequence = DOTween.Sequence(rewardObject);
            sequence.Append(rect.DOScale(Vector3.one * RewardTextPeakScale, Mathf.Max(0.06f, moveDuration * 0.24f)).SetEase(Ease.OutBack));
            sequence.Join(rect.DOAnchorPos(endPosition, moveDuration).SetEase(Ease.OutCubic));
            sequence.Join(canvasGroup.DOFade(0f, fadeDuration).SetDelay(moveDuration * 0.35f).SetEase(Ease.InQuad));
            sequence.OnComplete(delegate { ReturnRewardText(rewardObject); });
        }

        private GameObject GetRewardTextObject(Transform parent)
        {
            GameObject rewardObject = null;
            for (var index = _rewardTextPool.Count - 1; index >= 0; index--)
            {
                if (_rewardTextPool[index] == null)
                {
                    _rewardTextPool.RemoveAt(index);
                    continue;
                }

                if (!_rewardTextPool[index].activeSelf)
                {
                    rewardObject = _rewardTextPool[index];
                    _rewardTextPool.RemoveAt(index);
                    break;
                }
            }

            if (rewardObject == null)
            {
                rewardObject = new GameObject("FloatingRewardText", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
                _allRewardTextObjects.Add(rewardObject);
                var text = rewardObject.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontStyle = FontStyles.Bold;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;
                text.raycastTarget = false;
                text.richText = true;

                var outline = rewardObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.04f, 0.02f, 0.01f, 0.9f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            rewardObject.transform.SetParent(parent, false);
            var rect = rewardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 60f);

            var rewardText = rewardObject.GetComponent<TextMeshProUGUI>();
            if (_payoutText != null)
            {
                rewardText.font = _payoutText.font;
                rewardText.fontSharedMaterial = _payoutText.fontSharedMaterial;
                rewardText.fontSize = Mathf.Max(18f, _payoutText.fontSize * 0.9f);
            }

            return rewardObject;
        }

        private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            if (_rewardTextParent == null)
            {
                return new Vector2(worldPosition.x, worldPosition.y);
            }

            var eventCamera = _gameCanvas != null && _gameCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _gameCanvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rewardTextParent, screenPoint, eventCamera, out localPoint))
            {
                return localPoint;
            }

            return new Vector2(worldPosition.x, worldPosition.y);
        }

        private void PunchPayoutText()
        {
            if (_payoutText == null)
            {
                return;
            }

            _payoutText.transform.DOKill();
            _payoutText.transform.DOPunchScale(Vector3.one * 0.12f, Mathf.Max(0.08f, MultiplierStepDuration), 1, 0.5f);
        }

        private void ReturnRewardText(GameObject rewardObject)
        {
            if (rewardObject == null)
            {
                return;
            }

            DOTween.Kill(rewardObject);
            rewardObject.transform.DOKill();
            var canvasGroup = rewardObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
            }
            rewardObject.SetActive(false);
            if (!_rewardTextPool.Contains(rewardObject))
            {
                _rewardTextPool.Add(rewardObject);
            }
        }

        private void ClearRewardTextPool(bool destroyObjects)
        {
            for (var index = _allRewardTextObjects.Count - 1; index >= 0; index--)
            {
                var rewardObject = _allRewardTextObjects[index];
                if (rewardObject == null)
                {
                    _allRewardTextObjects.RemoveAt(index);
                    continue;
                }

                DOTween.Kill(rewardObject);
                rewardObject.transform.DOKill();
                var canvasGroup = rewardObject.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOKill();
                }

                if (destroyObjects)
                {
                    Destroy(rewardObject);
                }
                else
                {
                    rewardObject.SetActive(false);
                    if (!_rewardTextPool.Contains(rewardObject))
                    {
                        _rewardTextPool.Add(rewardObject);
                    }
                }
            }

            if (destroyObjects)
            {
                _rewardTextPool.Clear();
                _allRewardTextObjects.Clear();
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
