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
        [SerializeField] private GameObject _gambitOverlay;
        [SerializeField] private RectTransform _gambitCardPanel;
        [SerializeField] private Button[] _gambitCardButtons = new Button[3];
        [SerializeField] private Image[] _gambitCardAccents = new Image[3];
        [SerializeField] private Image[] _gambitCardIcons = new Image[3];
        [SerializeField] private TextMeshProUGUI[] _gambitCardTitleTexts = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] _gambitCardDescriptionTexts = new TextMeshProUGUI[3];

        private readonly ReelScroller[] _reelScrollers = new ReelScroller[GameBalance.GridColumns];
        private bool _isSpinAnimating;
        private RectTransform _mainContent;
        private Vector3 _originalMainContentPosition;
        private bool _hasOriginalMainContentPosition;
        private readonly Vector3[] _originalReelPositions = new Vector3[GameBalance.GridColumns];
        private readonly bool[] _hasOriginalReelPositions = new bool[GameBalance.GridColumns];

        [SerializeField] private RewardAnimationSettings _animationSettings;
        [SerializeField] private CelebrationAnimationSettings _celebrationAnimationSettings;
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

        [SerializeField] private TextMeshProUGUI _cashText;
        [SerializeField] private TextMeshProUGUI _targetText;
        [SerializeField] private TextMeshProUGUI _rollsText;
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private RectTransform _organsLayout;
        private readonly List<OwnedUpgradeView> _organViews = new List<OwnedUpgradeView>();
        private readonly Sprite[] _organSprites = new Sprite[GameBalance.OrganCount];
        private bool _isFirstRefresh = true;
        [SerializeField] private TextMeshProUGUI _ticketsText;
        [SerializeField] private TextMeshProUGUI _payoutText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _shopWalletText;
        [SerializeField] private RectTransform _ownedUpgradesLayout;
        [SerializeField] private UpgradeTooltip _upgradeTooltip;
        [SerializeField] private TextMeshProUGUI _refreshLabel;
        [SerializeField] private TextMeshProUGUI[] _offerLabels = new TextMeshProUGUI[3];
        private readonly Image[] _offerIcons = new Image[3];
        [SerializeField] private Button[] _offerButtons = new Button[3];
        private readonly Image[,] _cellImages = new Image[GameBalance.GridRows, GameBalance.GridColumns];
        private readonly TextMeshProUGUI[,] _cellTexts = new TextMeshProUGUI[GameBalance.GridRows, GameBalance.GridColumns];

        [SerializeField] private RectTransform _thresholdBarRect;
        [SerializeField] private Image _thresholdBarBackground;
        [SerializeField] private Image _thresholdBarFill;
        [SerializeField] private TextMeshProUGUI _thresholdBarText;

        [SerializeField] private Button _spin1xButton;
        [SerializeField] private Button _spin5xButton;
        [SerializeField] private Button _spin10xButton;
        [SerializeField] private Button _spin100xButton;
        [SerializeField] private Button _spin1000xButton;
        [SerializeField] private Button _spin10000xButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _gameOverRestartButton;
        [SerializeField] private Button _victoryRestartButton;
        [SerializeField] private GameObject _gameOverOverlay;
        [SerializeField] private GameObject _victoryOverlay;
        [SerializeField] private GameObject _maxPlusWinOverlay;
        [SerializeField] private Image _winningItemImage;
        private SymbolScoreAnimationPlayer _winningItemAnimationPlayer;
        [SerializeField] private TextMeshProUGUI _winningItemNameText;
        [SerializeField] private TextMeshProUGUI _maxPlusWinTitleText;
        [SerializeField] private RectTransform _winningItemRect;
        [SerializeField] private RectTransform _maxPlusWinTitleRect;
        [SerializeField] private TextMeshProUGUI _gameOverRunStatsText;
        [SerializeField] private TextMeshProUGUI _victoryRunStatsText;
        private Canvas _gameCanvas;
        private RectTransform _rewardTextParent;

        [SerializeField] private SlotLever _lever;
        [SerializeField] private OwnedUpgradeView _ownedUpgradePrefab;
        private int _currentBatchFactor = 1;
        private readonly Color _buttonSelectedColor = new Color(1f, 0.29f, 0.26f, 1f); // Tomato red
        private readonly Color _buttonNormalColor = new Color(0.70f, 0.24f, 0.51f, 1f); // Magenta

        private PaylineWin _bestThresholdWin;
        private string _defaultMaxPlusWinTitle;
        private Coroutine _winSoundCoroutine;
        private bool _isScoringSoundActive;

        private void Start()
        {

            if (_celebrationAnimationSettings == null)
            {
                _celebrationAnimationSettings = Resources.Load<CelebrationAnimationSettings>(
                    "SerenaysGambit/Data/CelebrationAnimationSettings");
            }

            _symbolDefinitions = Resources.LoadAll<SymbolDefinition>("SerenaysGambit/Data/Symbols");
            _reelDefinitions = Resources.LoadAll<ReelDefinition>("SerenaysGambit/Data/Reels");
            _shopItemDefinitions = Resources.LoadAll<ShopItemDefinition>("SerenaysGambit/Data/ShopItems");
            _gambitItemDefinitions = Resources.LoadAll<GambitItemDefinition>("SerenaysGambit/Data/Gambits");
            _balanceDefinition = Resources.Load<BalanceDefinition>("SerenaysGambit/Data/Balance/DefaultBalance");
            LoadOrganSprites();
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
                        scroller.Initialize(
                            column,
                            _rulesConfig.ReelStripAt(column),
                            SymbolLabel,
                            SymbolColor,
                            SymbolSprite,
                            SymbolScoreAnimation);
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

        private static bool IsTripleReel(SpinResult result, int reelIndex)
        {
            if (result == null || result.Grid == null
                || reelIndex < 0 || reelIndex >= GameBalance.GridColumns)
            {
                return false;
            }

            var topSymbol = result.Grid[0, reelIndex];
            return topSymbol == result.Grid[1, reelIndex]
                && topSymbol == result.Grid[2, reelIndex];
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
            if (_maxPlusWinOverlay != null)
            {
                DOTween.Kill(_maxPlusWinOverlay);
                _maxPlusWinOverlay.SetActive(false);
            }

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
            return "<b>ROUND SCORE</b>\n<size=20><color=#FFF5E8>" + amount + "</color></size>\n<size=13>" + detail + "</size>";
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
            StopWinSound(fade: false);
            ClearRewardTextPool(false);
            ClearOwnedUpgradeViews();
            ClearOrganViews();
            _isFirstRefresh = true;
            _runService = new RunService(Environment.TickCount, _rulesConfig);
            _shopService = _runService.Shop;
            _state = _runService.CreateNewRun();
            _bestThresholdWin = null;
            ResetReelStripsForRun();
            InitializeOrganViews();
            ClearGrid();
            _resultText.text = "Choose a batch to spin. Space = 1x, 5 = 5x, 0 = 10x. Larger batches are on the canvas.";
            _payoutText.text = FormatSidebarPayout(MoneyFormatter.FormatTL(BigInteger.Zero), 1, 1);
            _gameOverOverlay.SetActive(false);
            _victoryOverlay.SetActive(false);
            if (_maxPlusWinOverlay != null)
            {
                _maxPlusWinOverlay.SetActive(false);
            }
            if (_gambitOverlay != null)
            {
                _gambitOverlay.SetActive(false);
            }
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
            StopWinSound(fade: false);

            _spin1xButton.interactable = false;
            _spin5xButton.interactable = false;
            _spin10xButton.interactable = false;
            _spin100xButton.interactable = false;
            _spin1000xButton.interactable = false;
            _spin10000xButton.interactable = false;
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

            // These are finish times because all reels begin stopping together.
            // Halving the old 0.50s spacing makes the reels settle faster in sequence.
            var stopDurations = new[] { 1.00f, 1.25f, 1.50f };
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
                        if (IsTripleReel(result, capturedCol))
                        {
                            PunchDownCameraShake();
                        }
                        else
                        {
                            PunchUpCameraShake(capturedCol);
                        }
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

            if (result.Score != null && result.Score.Wins != null)
            {
                for (var i = 0; i < result.Score.Wins.Count; i++)
                {
                    var win = result.Score.Wins[i];
                    if (win == null) continue;

                    if (_bestThresholdWin == null)
                    {
                        _bestThresholdWin = win;
                    }
                    else
                    {
                        bool currentIsMaxPlus = _bestThresholdWin.IsMaxPlusWin;
                        bool newIsMaxPlus = win.IsMaxPlusWin;

                        if (newIsMaxPlus && !currentIsMaxPlus)
                        {
                            _bestThresholdWin = win;
                        }
                        else if (newIsMaxPlus == currentIsMaxPlus)
                        {
                            if (win.FinalPayoutKurus > _bestThresholdWin.FinalPayoutKurus)
                            {
                                _bestThresholdWin = win;
                            }
                        }
                    }
                }
            }

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

            StopAllScoreAnimations();
            if (result.MaxPlusWin != null)
            {
                _isScoringSoundActive = true;
                if (_winSoundCoroutine == null)
                {
                    _winSoundCoroutine = StartCoroutine(PlayWinSoundLoop());
                }
                yield return StartCoroutine(ShowMaxPlusWin(result.MaxPlusWin));
            }

            if (result.ThresholdCleared)
            {
                if (_bestThresholdWin != null)
                {
                    _isScoringSoundActive = true;
                    if (_winSoundCoroutine == null)
                    {
                        _winSoundCoroutine = StartCoroutine(PlayWinSoundLoop());
                    }
                    yield return StartCoroutine(ShowMaxPlusWin(_bestThresholdWin, "YOUR MAX PRO PLUS WINNN!!!!! "));
                    _bestThresholdWin = null;
                }
            }

            StopWinSound(fade: true);
            _isSpinAnimating = false;

            if (result.KissLostOnLeftReel)
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
                if (_cashText == null) throw new InvalidOperationException("cashText is not assigned!");
                if (_targetText == null) throw new InvalidOperationException("targetText is not assigned!");
                if (_roundText == null) throw new InvalidOperationException("roundText is not assigned!");
                if (_rollsText == null) throw new InvalidOperationException("rollsText is not assigned!");

                if (_organsLayout == null) throw new InvalidOperationException("organsLayout is not assigned!");
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

                if (_ticketsText == null) throw new InvalidOperationException("ticketsText is not assigned!");
                if (_payoutText == null) throw new InvalidOperationException("payoutText is not assigned!");
                _payoutText.richText = true;
                _payoutText.enableAutoSizing = true;
                _payoutText.fontSizeMin = 8f;
                _payoutText.fontSizeMax = 20f;
                _payoutText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                if (_resultText == null) throw new InvalidOperationException("resultText is not assigned!");
                if (_shopWalletText == null) throw new InvalidOperationException("shopWalletText is not assigned!");
                if (_ownedUpgradesLayout == null) throw new InvalidOperationException("ownedUpgradesLayout is not assigned!");
                if (_upgradeTooltip == null) throw new InvalidOperationException("upgradeTooltip is not assigned!");
                if (_ownedUpgradePrefab == null)
                {
                    throw new InvalidOperationException("SlotGameController requires an owned-upgrade prefab reference.");
                }

                if (_thresholdBarRect == null) throw new InvalidOperationException("thresholdBarRect is not assigned!");
                if (_thresholdBarBackground == null) throw new InvalidOperationException("thresholdBarBackground is not assigned!");
                if (_thresholdBarFill == null) throw new InvalidOperationException("thresholdBarFill is not assigned!");
                if (_thresholdBarText == null) throw new InvalidOperationException("thresholdBarText is not assigned!");

                if (_lever == null) throw new InvalidOperationException("lever is not assigned!");
                if (_spin1xButton == null) throw new InvalidOperationException("spin1xButton is not assigned!");
                if (_spin5xButton == null) throw new InvalidOperationException("spin5xButton is not assigned!");
                if (_spin10xButton == null) throw new InvalidOperationException("spin10xButton is not assigned!");
                if (_spin100xButton == null) throw new InvalidOperationException("spin100xButton is not assigned!");
                if (_spin1000xButton == null) throw new InvalidOperationException("spin1000xButton is not assigned!");
                if (_spin10000xButton == null) throw new InvalidOperationException("spin10000xButton is not assigned!");
                if (_refreshButton == null) throw new InvalidOperationException("refreshButton is not assigned!");
                if (_refreshLabel == null) throw new InvalidOperationException("refreshLabel is not assigned!");

                for (var offerIndex = 0; offerIndex < 3; offerIndex++)
                {
                    if (_offerButtons[offerIndex] == null) throw new InvalidOperationException("offerButtons[" + offerIndex + "] is null!");
                    if (_offerLabels[offerIndex] == null) throw new InvalidOperationException("offerLabels[" + offerIndex + "] is null!");
                    _offerIcons[offerIndex] = CreateOfferIcon(_offerLabels[offerIndex]);
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

                if (_gameOverOverlay == null) throw new InvalidOperationException("gameOverOverlay is not assigned!");
                if (_victoryOverlay == null) throw new InvalidOperationException("victoryOverlay is not assigned!");
                if (_gameOverRunStatsText == null) throw new InvalidOperationException("gameOverRunStatsText is not assigned!");
                if (_victoryRunStatsText == null) throw new InvalidOperationException("victoryRunStatsText is not assigned!");
                if (_gameOverRestartButton == null) throw new InvalidOperationException("gameOverRestartButton is not assigned!");
                if (_victoryRestartButton == null) throw new InvalidOperationException("victoryRestartButton is not assigned!");

                if (_maxPlusWinOverlay == null) throw new InvalidOperationException("maxPlusWinOverlay is not assigned!");
                if (_winningItemImage == null) throw new InvalidOperationException("winningItemImage is not assigned!");
                _winningItemAnimationPlayer = _winningItemImage.GetComponent<SymbolScoreAnimationPlayer>();
                if (_winningItemAnimationPlayer == null)
                {
                    _winningItemAnimationPlayer = _winningItemImage.gameObject.AddComponent<SymbolScoreAnimationPlayer>();
                }
                if (_winningItemNameText == null) throw new InvalidOperationException("winningItemNameText is not assigned!");
                if (_maxPlusWinTitleText == null) throw new InvalidOperationException("maxPlusWinTitleText is not assigned!");
                _defaultMaxPlusWinTitle = _maxPlusWinTitleText.text;

                _winningItemRect = _winningItemImage.GetComponent<RectTransform>();
                _maxPlusWinTitleRect = _maxPlusWinTitleText.GetComponent<RectTransform>();

                if (_gambitOverlay == null) throw new InvalidOperationException("gambitOverlay is not assigned!");
                if (_gambitCardPanel == null) throw new InvalidOperationException("gambitCardPanel is not assigned!");
                if (_gambitCardButtons == null || _gambitCardButtons.Length == 0)
                {
                    throw new InvalidOperationException("gambitCardButtons are not assigned!");
                }
                if (_gambitCardAccents == null || _gambitCardAccents.Length != _gambitCardButtons.Length)
                {
                    throw new InvalidOperationException("gambitCardAccents must match gambitCardButtons!");
                }
                if (_gambitCardIcons == null || _gambitCardIcons.Length != _gambitCardButtons.Length)
                {
                    throw new InvalidOperationException("gambitCardIcons must match gambitCardButtons!");
                }
                if (_gambitCardTitleTexts == null || _gambitCardTitleTexts.Length != _gambitCardButtons.Length)
                {
                    throw new InvalidOperationException("gambitCardTitleTexts must match gambitCardButtons!");
                }
                if (_gambitCardDescriptionTexts == null || _gambitCardDescriptionTexts.Length != _gambitCardButtons.Length)
                {
                    throw new InvalidOperationException("gambitCardDescriptionTexts must match gambitCardButtons!");
                }
                for (var gambitCardIndex = 0; gambitCardIndex < _gambitCardButtons.Length; gambitCardIndex++)
                {
                    if (_gambitCardButtons[gambitCardIndex] == null) throw new InvalidOperationException("gambitCardButtons[" + gambitCardIndex + "] is null!");
                    if (_gambitCardAccents[gambitCardIndex] == null) throw new InvalidOperationException("gambitCardAccents[" + gambitCardIndex + "] is null!");
                    if (_gambitCardIcons[gambitCardIndex] == null) throw new InvalidOperationException("gambitCardIcons[" + gambitCardIndex + "] is null!");
                    if (_gambitCardTitleTexts[gambitCardIndex] == null) throw new InvalidOperationException("gambitCardTitleTexts[" + gambitCardIndex + "] is null!");
                    if (_gambitCardDescriptionTexts[gambitCardIndex] == null) throw new InvalidOperationException("gambitCardDescriptionTexts[" + gambitCardIndex + "] is null!");
                }
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message);
                return false;
            }

            _lever.OnPulled = delegate { Spin(_currentBatchFactor); };
            _spin1xButton.onClick.AddListener(delegate { Spin(1); });
            _spin5xButton.onClick.AddListener(delegate { Spin(5); });
            _spin10xButton.onClick.AddListener(delegate { Spin(10); });
            _spin100xButton.onClick.AddListener(delegate { Spin(100); });
            _spin1000xButton.onClick.AddListener(delegate { Spin(1000); });
            _spin10000xButton.onClick.AddListener(delegate { Spin(10000); });
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
            _isFirstRefresh = false;

            if (_state.Modifiers.BatchTenGambitCount > 0)
            {
                _currentBatchFactor = 10;
                _spin1xButton.image.color = _buttonNormalColor;
                _spin5xButton.image.color = _buttonNormalColor;
                _spin10xButton.image.color = _buttonSelectedColor;
                _spin100xButton.image.color = _buttonNormalColor;
                _spin1000xButton.image.color = _buttonNormalColor;
                _spin10000xButton.image.color = _buttonNormalColor;

                _spin1xButton.interactable = false;
                _spin5xButton.interactable = false;
                _spin10xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin100xButton.interactable = false;
                _spin1000xButton.interactable = false;
                _spin10000xButton.interactable = false;
            }
            else
            {
                _spin1xButton.image.color = _currentBatchFactor == 1 ? _buttonSelectedColor : _buttonNormalColor;
                _spin5xButton.image.color = _currentBatchFactor == 5 ? _buttonSelectedColor : _buttonNormalColor;
                _spin10xButton.image.color = _currentBatchFactor == 10 ? _buttonSelectedColor : _buttonNormalColor;
                _spin100xButton.image.color = _currentBatchFactor == 100 ? _buttonSelectedColor : _buttonNormalColor;
                _spin1000xButton.image.color = _currentBatchFactor == 1000 ? _buttonSelectedColor : _buttonNormalColor;
                _spin10000xButton.image.color = _currentBatchFactor == 10000 ? _buttonSelectedColor : _buttonNormalColor;

                _spin1xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin5xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin10xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin100xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin1000xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
                _spin10000xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
            }

            if (_lever != null)
            {
                bool isOverlayShowing = (_victoryOverlay != null && _victoryOverlay.activeSelf) ||
                                       (_gameOverOverlay != null && _gameOverOverlay.activeSelf) ||
                                       (_gambitOverlay != null && _gambitOverlay.activeSelf) ||
                                       (_maxPlusWinOverlay != null && _maxPlusWinOverlay.activeSelf);
                _lever.gameObject.SetActive(_state.Phase == RunPhase.Playing && !isOverlayShowing);
                _lever.IsAvailable = _state.Phase == RunPhase.Playing && _state.RollsRemaining > 0 && !_isSpinAnimating;
            }
            _refreshButton.interactable = _state.Phase == RunPhase.Playing && _state.RefreshTickets > 0;
            _refreshLabel.text = "Refresh shop (" + _state.RefreshTickets + ")";

            for (var index = 0; index < _offerButtons.Length; index++)
            {
                if (index >= _state.ShopOffers.Count)
                {
                    _offerLabels[index].text = string.Empty;
                    _offerLabels[index].enabled = true;
                    if (_offerIcons[index] != null)
                    {
                        _offerIcons[index].sprite = null;
                        _offerIcons[index].enabled = false;
                    }
                    _offerButtons[index].interactable = false;
                    continue;
                }

                var offer = _state.ShopOffers[index];
                var icon = ShopItemIcon(offer.Kind);
                var hasIcon = icon != null;

                _offerLabels[index].text = offer.Purchased
                    ? offer.Title + "\nSOLD"
                    : offer.Title + "\n" + offer.Description + "\n" + MoneyFormatter.FormatTL(offer.CostKurus);

                _offerLabels[index].enabled = !hasIcon;
                if (_offerIcons[index] != null)
                {
                    _offerIcons[index].sprite = icon;
                    _offerIcons[index].enabled = hasIcon;
                    _offerIcons[index].color = offer.Purchased
                        ? new Color(0.55f, 0.55f, 0.6f, 0.65f)
                        : Color.white;
                }

                _offerButtons[index].interactable = _state.Phase == RunPhase.Playing && !offer.Purchased && _state.CashKurus >= offer.CostKurus;
            }
        }

        private static Image CreateOfferIcon(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return null;
            }

            var iconObject = label.transform.Find("Icon");
            if (iconObject == null)
            {
                var iconGameObject = new GameObject("Icon", typeof(RectTransform));
                iconGameObject.transform.SetParent(label.transform, false);
                iconObject = iconGameObject.transform;
            }

            var icon = iconObject.GetComponent<Image>();
            if (icon == null)
            {
                icon = iconObject.gameObject.AddComponent<Image>();
            }

            var rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0.12f, 0.08f);
            rect.anchorMax = new Vector2(0.88f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            return icon;
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
                if (definition == null)
                {
                    continue;
                }

                ShopItemDefinition current;
                if (!_shopItemDefinitionsByKind.TryGetValue(definition.Kind, out current)
                    || current == null
                    || (current.Icon == null && definition.Icon != null))
                {
                    _shopItemDefinitionsByKind[definition.Kind] = definition;
                }
            }
        }

        private Sprite ShopItemIcon(ShopOfferKind kind)
        {
            ShopItemDefinition definition;
            if (_shopItemDefinitionsByKind.TryGetValue(kind, out definition)
                && definition != null
                && definition.Icon != null)
            {
                return definition.Icon;
            }

            if (_shopItemDefinitions != null)
            {
                foreach (var candidate in _shopItemDefinitions)
                {
                    if (candidate != null && candidate.Kind == kind && candidate.Icon != null)
                    {
                        return candidate.Icon;
                    }
                }
            }

            return null;
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
                    ShopItemIcon(kind),
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
                case GambitKind.Absolut: return "AG";
                case GambitKind.BatchTen: return "TB";
                case GambitKind.Kiss1000x: return "K1K";
                case GambitKind.CigaretteDecay: return "CD";
                default: return "?";
            }
        }

        private static Color GambitFallbackColor(GambitKind kind)
        {
            switch (kind)
            {
                case GambitKind.Absolut: return new Color(0.85f, 0.2f, 0.3f, 1f);
                case GambitKind.BatchTen: return new Color(0.2f, 0.6f, 0.8f, 1f);
                case GambitKind.Kiss1000x: return new Color(0.7f, 0.3f, 0.85f, 1f);
                case GambitKind.CigaretteDecay: return new Color(0.95f, 0.65f, 0.1f, 1f);
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
            if (_gambitOverlay == null || _gambitCardPanel == null || _gambitCardButtons == null)
            {
                Debug.LogError("Gambit selection overlay is not fully assigned in the scene.");
                _isSpinAnimating = false;
                return;
            }

            _isSpinAnimating = true;
            _gambitOverlay.SetActive(true);

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

            var cardsToShow = Mathf.Min(_gambitCardButtons.Length, pool.Count);
            if (cardsToShow == 0)
            {
                Debug.LogError("No GambitItemDefinition assets were found under Resources/SerenaysGambit/Data/Gambits.");
                _gambitOverlay.SetActive(false);
                _isSpinAnimating = false;
                return;
            }

            for (var i = 0; i < _gambitCardButtons.Length; i++)
            {
                var cardButton = _gambitCardButtons[i];
                cardButton.onClick.RemoveAllListeners();
                var hasDefinition = i < cardsToShow;
                cardButton.gameObject.SetActive(hasDefinition);
                if (!hasDefinition)
                {
                    continue;
                }

                var definition = pool[i];
                var title = definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                    ? definition.DisplayName
                    : definition == null ? "Unknown Gambit" : definition.Kind.ToString();
                var description = definition != null && !string.IsNullOrEmpty(definition.Description)
                    ? definition.Description
                    : "Gambit details are unavailable.";
                var cardColor = definition != null && definition.AccentColor != Color.clear
                    ? definition.AccentColor
                    : definition == null ? Color.gray : GambitFallbackColor(definition.Kind);

                _gambitCardAccents[i].color = new Color(cardColor.r, cardColor.g, cardColor.b, 0.35f);
                _gambitCardIcons[i].sprite = definition == null ? null : definition.Icon;
                _gambitCardIcons[i].gameObject.SetActive(_gambitCardIcons[i].sprite != null);
                _gambitCardTitleTexts[i].text = title;
                _gambitCardTitleTexts[i].color = cardColor;
                _gambitCardDescriptionTexts[i].text = description;

                var capturedDefinition = definition;
                cardButton.onClick.AddListener(delegate { SelectGambit(capturedDefinition); });
            }

            PrepareDefaultFonts();
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
                _gambitOverlay.SetActive(false);
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
                    _organSprites[i],
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

        private void LoadOrganSprites()
        {
            var organNames = new[] { "Stomach", "Liver", "Intestines", "Lungs", "Heart" };
            for (var index = 0; index < organNames.Length && index < _organSprites.Length; index++)
            {
                var texture = Resources.Load<Texture2D>("SerenaysGambit/Data/Organs/" + organNames[index]);
                if (texture == null)
                {
                    Debug.LogWarning("Missing organ artwork: " + organNames[index]);
                    continue;
                }

                _organSprites[index] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
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
                case ShopOfferKind.AbsolutValue: return "Current Absolut value: x" + _state.Modifiers.AbsolutValue;
                case ShopOfferKind.DollarValue: return "Current Dollar value: x" + _state.Modifiers.DollarValue;
                case ShopOfferKind.KissValue: return "Current Kiss value: x" + _state.Modifiers.KissValue;
                case ShopOfferKind.CatValue: return "Current Cat value: x" + _state.Modifiers.CatValue;
                case ShopOfferKind.CigaretteValue: return "Current Cigarette value: x" + _state.Modifiers.CigaretteValue;
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
                case ShopOfferKind.AbsolutValue: return "A";
                case ShopOfferKind.DollarValue: return "D";
                case ShopOfferKind.KissValue: return "K";
                case ShopOfferKind.CatValue: return "C";
                case ShopOfferKind.CigaretteValue: return "C";
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
                case ShopOfferKind.AbsolutValue: return new Color(0.85f, 0.36f, 0.42f, 1f);
                case ShopOfferKind.DollarValue: return new Color(0.72f, 0.25f, 0.32f, 1f);
                case ShopOfferKind.KissValue: return new Color(0.95f, 0.78f, 0.26f, 1f);
                case ShopOfferKind.CatValue: return new Color(0.94f, 0.49f, 0.18f, 1f);
                case ShopOfferKind.CigaretteValue: return new Color(0.53f, 0.72f, 0.33f, 1f);
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
                summary += win.Payline.Name + " (" + (win.IsTripleKiss ? "Triple Kiss" : win.ResolvedSymbol.ToString()) + ")";
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
            AppendSymbolRunStats(summary, SymbolKind.Absolut, "Absolut");
            AppendSymbolRunStats(summary, SymbolKind.Dollar, "Dollar");
            AppendSymbolRunStats(summary, SymbolKind.Cat, "Cat");
            AppendSymbolRunStats(summary, SymbolKind.Cigarette, "Cigarette");
            AppendSymbolRunStats(summary, SymbolKind.Kiss, "Kiss");
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
            _isScoringSoundActive = true;
            _winSoundCoroutine = StartCoroutine(PlayWinSoundLoop());

            var animationEvents = RewardAnimationQueueBuilder.Build(
                result.Score,
                RewardAnimationQueueBuilder.MaxVisibleMatchEventsForBatch(result.Score.BatchFactor));
            BigInteger startCash = result.CashBeforeSpinKurus;
            BigInteger displayedPayout = BigInteger.Zero;
            BigInteger accumulatedFinalPayout = BigInteger.Zero;
            var cachedCells = new Dictionary<PaylineWin, List<CellRef>>();
            Transform canvasTransform = _rewardTextParent != null ? _rewardTextParent : transform;
            var queueLength = RewardAnimationQueueBuilder.QueueLengthForSpeed(result.Score);
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

        private IEnumerator PlayWinSoundLoop()
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            var clip = Resources.Load<AudioClip>("SlotWin");
            if (clip == null)
            {
                Debug.LogWarning("SlotWin sound not found in Resources folder.");
                yield break;
            }

            audioSource.DOKill();
            audioSource.volume = 1f;
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.pitch = 1.0f;

            while (_isScoringSoundActive)
            {
                audioSource.Play();
                // Wait for the clip to finish playing or scoring to stop
                while (audioSource.isPlaying && _isScoringSoundActive)
                {
                    yield return null;
                }

                if (_isScoringSoundActive)
                {
                    // Resampling: Increase pitch to speed up the sound and compress the frequency (Tape Speed Effect / Pitch Shift)
                    audioSource.pitch += 0.2f;
                }
            }

            audioSource.Stop();
        }

        private void StopWinSound(bool fade = true)
        {
            _isScoringSoundActive = false;
            if (_winSoundCoroutine != null)
            {
                StopCoroutine(_winSoundCoroutine);
                _winSoundCoroutine = null;
            }

            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.DOKill();
                if (fade && audioSource.isPlaying)
                {
                    audioSource.DOFade(0f, 0.5f).OnComplete(() => {
                        audioSource.Stop();
                        audioSource.volume = 1f;
                    });
                }
                else
                {
                    audioSource.Stop();
                    audioSource.volume = 1f;
                }
            }
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
            return "Scored: " + win.Payline.Name + " (" + (win.IsTripleKiss ? "Triple Kiss" : win.ResolvedSymbol.ToString()) + ")"
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
            if (win.IsTripleKiss)
            {
                parts.Add("<color=#FFDD70>TRIPLE KISS</color>");
            }

            if (_state != null && _state.Modifiers.CigaretteDecayGambitCount > 0 && win.ResolvedSymbol == SymbolKind.Cigarette)
            {
                var cigaretteConfig = _state.Modifiers.GambitConfig(GambitKind.CigaretteDecay);
                var cigarettePayoutMultiplier = cigaretteConfig == null ? 5 : cigaretteConfig.PayoutMultiplier;
                var cigaretteMultiplier = BigInteger.Pow(new BigInteger(cigarettePayoutMultiplier), _state.Modifiers.CigaretteDecayGambitCount);
                parts.Add("<color=#FF8C69>CIGARETTE ×" + cigaretteMultiplier + "</color>");
            }

            if (_state != null && _state.Modifiers.Kiss1000xGambitCount > 0 && WinUsesLeftReelKiss(result, win))
            {
                var kissConfig = _state.Modifiers.GambitConfig(GambitKind.Kiss1000x);
                var kissPayoutMultiplier = kissConfig == null ? 1000 : kissConfig.PayoutMultiplier;
                var kissMultiplier = BigInteger.Pow(new BigInteger(kissPayoutMultiplier), _state.Modifiers.Kiss1000xGambitCount);
                parts.Add("<color=#D9A0FF>KISS ×" + kissMultiplier + "</color>");
            }

            if (_state != null && _state.Modifiers.AbsolutGambitCount > 0)
            {
                parts.Add("<color=#FF8FBA>ABSOLUT GAMBIT</color>");
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

        private static bool WinUsesLeftReelKiss(SpinResult result, PaylineWin win)
        {
            if (result == null || result.Grid == null)
            {
                return false;
            }

            foreach (var position in win.Payline.Positions)
            {
                if (position.Column == 0 && result.Grid[position.Row, position.Column] == SymbolKind.Kiss)
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
                var cell = cells[index];
                var image = cell.Image;
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

                if (_reelScrollers != null && cell.Position.Column >= 0 && cell.Position.Column < _reelScrollers.Length)
                {
                    var scroller = _reelScrollers[cell.Position.Column];
                    if (scroller != null)
                    {
                        scroller.PlayScoreAnimation(cell.Position.Row);
                    }
                }
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

        private void StopAllScoreAnimations()
        {
            for (var index = 0; index < _reelScrollers.Length; index++)
            {
                if (_reelScrollers[index] != null)
                {
                    _reelScrollers[index].StopScoreAnimations();
                }
            }
        }

        private void RestorePaylineVisuals(List<CellRef> cells)
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

                if (_reelScrollers != null && cell.Position.Column >= 0 && cell.Position.Column < _reelScrollers.Length)
                {
                    var scroller = _reelScrollers[cell.Position.Column];
                    if (scroller != null)
                    {
                        scroller.StopScoreAnimations();
                    }
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
            _payoutText.transform.DOPunchScale(Vector3.one * 0.05f, Mathf.Max(0.08f, MultiplierStepDuration), 1, 0.5f);
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
                case SymbolKind.Absolut: return "ABSOLUT";
                case SymbolKind.Dollar: return "DOLLAR";
                case SymbolKind.Cat: return "CAT";
                case SymbolKind.Cigarette: return "CIGARETTE";
                default: return "KISS";
            }
        }

        private static Color SymbolColor(SymbolKind symbol)
        {
            switch (symbol)
            {
                case SymbolKind.Absolut: return new Color(0.95f, 0.84f, 0.84f, 1f);
                case SymbolKind.Dollar: return new Color(0.88f, 0.88f, 0.88f, 1f);
                case SymbolKind.Cat: return new Color(0.98f, 0.80f, 0.60f, 1f);
                case SymbolKind.Cigarette: return new Color(0.98f, 0.72f, 0.72f, 1f);
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
                component = transform.GetComponentInChildren<T>(true);
            }

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

        private IEnumerator ShowMaxPlusWin(PaylineWin win, string overrideTitle = null)
        {
            if (_maxPlusWinOverlay == null || win == null || _celebrationAnimationSettings == null)
            {
                yield break;
            }

            if (_payoutText != null)
            {
                if (_winningItemNameText != null && (_winningItemNameText.font == null || _winningItemNameText.font == TMP_Settings.defaultFontAsset))
                {
                    _winningItemNameText.font = _payoutText.font;
                    _winningItemNameText.fontSharedMaterial = _payoutText.fontSharedMaterial;
                }
                if (_maxPlusWinTitleText != null)
                {
                    _maxPlusWinTitleText.text = string.IsNullOrEmpty(overrideTitle) ? _defaultMaxPlusWinTitle : overrideTitle;
                    if (_maxPlusWinTitleText.font == null || _maxPlusWinTitleText.font == TMP_Settings.defaultFontAsset)
                    {
                        _maxPlusWinTitleText.font = _payoutText.font;
                        _maxPlusWinTitleText.fontSharedMaterial = _payoutText.fontSharedMaterial;
                    }
                }
            }

            if (_winningItemImage != null)
            {
                var winningItemSprite = SymbolRotationImage(win.ResolvedSymbol);
                _winningItemImage.sprite = winningItemSprite;
                if (_winningItemAnimationPlayer != null)
                {
                    _winningItemAnimationPlayer.Configure(
                        winningItemSprite,
                        SymbolScoreAnimation(win.ResolvedSymbol));
                }
            }
            if (_winningItemNameText != null)
            {
                _winningItemNameText.text = SymbolLabel(win.ResolvedSymbol);
            }

            var overlayImage = _maxPlusWinOverlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                var overlayColor = overlayImage.color;
                overlayColor.a = _celebrationAnimationSettings.MaxPlusOverlayOpacity;
                overlayImage.color = overlayColor;
                overlayImage.raycastTarget = true;
            }

            var isSkipped = false;
            var button = _maxPlusWinOverlay.GetComponent<Button>();
            if (button == null)
            {
                button = _maxPlusWinOverlay.AddComponent<Button>();
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => isSkipped = true);

            _maxPlusWinOverlay.SetActive(true);
            RefreshView();

            if (_winningItemAnimationPlayer != null)
            {
                _winningItemAnimationPlayer.PlayOneShot();
            }

            if (_winningItemRect != null)
            {
                _winningItemRect.localScale = Vector3.one * _celebrationAnimationSettings.MaxPlusItemInitialScale;
            }
            if (_maxPlusWinTitleRect != null)
            {
                _maxPlusWinTitleRect.localScale = Vector3.one * _celebrationAnimationSettings.MaxPlusTitleInitialScale;
            }

            var sequence = DOTween.Sequence(_maxPlusWinOverlay);
            var maxPlusItemScaleUpTarget = Mathf.Max(
                _celebrationAnimationSettings.MaxPlusItemInitialScale,
                _celebrationAnimationSettings.MaxPlusItemOvershootScale);
            if (_winningItemRect != null)
            {
                sequence.Append(_winningItemRect.DOScale(
                    Vector3.one * maxPlusItemScaleUpTarget,
                    _celebrationAnimationSettings.MaxPlusItemScaleUpDuration).SetEase(Ease.OutCubic));
            }
            if (_maxPlusWinTitleRect != null)
            {
                sequence.Join(_maxPlusWinTitleRect.DOScale(
                    Vector3.one * Mathf.Max(1f, _celebrationAnimationSettings.MaxPlusTitleInitialScale),
                    _celebrationAnimationSettings.MaxPlusTitleScaleUpDuration).SetEase(Ease.OutCubic));
            }
            var maxPlusPunchDuration = Mathf.Max(
                0.01f,
                _celebrationAnimationSettings.MaxPlusWinDuration
                    - _celebrationAnimationSettings.MaxPlusItemScaleUpDuration
                    - _celebrationAnimationSettings.MaxPlusItemHoldDuration);
            if (_winningItemRect != null)
            {
                var maxPlusPunchScale = Mathf.Max(
                    maxPlusItemScaleUpTarget,
                    1f + _celebrationAnimationSettings.MaxPlusPunchScaleUpAmount);
                sequence.Append(_winningItemRect.DOScale(
                    Vector3.one * maxPlusPunchScale,
                    maxPlusPunchDuration).SetEase(Ease.OutCubic));
                sequence.AppendInterval(_celebrationAnimationSettings.MaxPlusItemHoldDuration);
            }
            else
            {
                sequence.AppendInterval(maxPlusPunchDuration + _celebrationAnimationSettings.MaxPlusItemHoldDuration);
            }

            sequence.Play();

            while (sequence.IsPlaying() && !isSkipped)
            {
                yield return null;
            }

            if (isSkipped)
            {
                sequence.Kill(true);
            }

            button.onClick.RemoveAllListeners();
            _maxPlusWinOverlay.SetActive(false);
            RefreshView();
        }

        private Sprite SymbolRotationImage(SymbolKind symbol)
        {
            if (_symbolDefinitions != null)
            {
                foreach (var definition in _symbolDefinitions)
                {
                    if (definition != null && definition.Symbol == symbol)
                    {
                        return definition.RotationImage;
                    }
                }
            }
            return null;
        }

        private AnimationClip SymbolScoreAnimation(SymbolKind symbol)
        {
            if (_symbolDefinitions != null)
            {
                foreach (var definition in _symbolDefinitions)
                {
                    if (definition != null && definition.Symbol == symbol)
                    {
                        return definition.ScoreAnimation;
                    }
                }
            }
            return null;
        }
    }
}
