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

        private readonly ReelScroller[] _reelScrollers = new ReelScroller[GameBalance.GridColumns];
        private bool _isSpinAnimating;

        private TextMeshProUGUI _cashText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _rollsText;
        private TextMeshProUGUI _organsText;
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
                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    var reelTransform = canvas.transform.Find("MainContent/SlotMachinePanel/SlotGrid/Reel" + (column + 1));
                    if (reelTransform != null)
                    {
                        var scroller = reelTransform.gameObject.AddComponent<ReelScroller>();
                        scroller.Initialize(column, _rulesConfig.ReelStripAt(column), SymbolLabel, SymbolColor);
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

        private void StartNewRun()
        {
            ClearOwnedUpgradeViews();
            _runService = new RunService(Environment.TickCount, _rulesConfig);
            _shopService = _runService.Shop;
            _state = _runService.CreateNewRun();
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

            float spinSpeed = 1800f;
            for (var col = 0; col < GameBalance.GridColumns; col++)
            {
                if (_reelScrollers[col] != null)
                {
                    _reelScrollers[col].StartSpin(spinSpeed);
                }
            }

            var stopDurations = new[] { 1.0f, 1.25f, 1.50f };
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
                    _reelScrollers[capturedCol].StopSpin(stopIndex, stopDurations[capturedCol], null);
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
                _rollsText = Require<TextMeshProUGUI>(root, "TopHud/RollsText");
                _organsText = Require<TextMeshProUGUI>(root, "TopHud/OrgansText");
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

            return true;
        }

        private void RefreshView()
        {
            _cashText.text = "Cash: " + MoneyFormatter.FormatTL(_state.CashKurus);
            _targetText.text = "Threshold " + _state.ThresholdLevel + "/" + _state.Config.ThresholdCount + ": " + MoneyFormatter.FormatTL(_state.CurrentTargetKurus);
            UpdateThresholdBar(_state.CashKurus, _state.CurrentTargetKurus);
            _rollsText.text = "Rolls: " + _state.RollsRemaining;
            _organsText.text = "Organs: " + _state.RemainingOrgans + "/" + _state.Config.OrganCount + " (" + OrganStatusText() + ")";
            _ticketsText.text = "Refresh tickets: " + _state.RefreshTickets;
            _shopWalletText.text = "Your cash: " + MoneyFormatter.FormatTL(_state.CashKurus);
            RefreshOwnedUpgradeViews();

            _spin1xButton.image.color = _currentBatchFactor == 1 ? _buttonSelectedColor : _buttonNormalColor;
            _spin5xButton.image.color = _currentBatchFactor == 5 ? _buttonSelectedColor : _buttonNormalColor;
            _spin10xButton.image.color = _currentBatchFactor == 10 ? _buttonSelectedColor : _buttonNormalColor;

            _spin1xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
            _spin5xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;
            _spin10xButton.interactable = _state.Phase == RunPhase.Playing && !_isSpinAnimating;

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
                default: return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        private string OrganStatusText()
        {
            var names = new[] { "Mide", "Karaciğer", "Bağırsak", "Akciğer", "Kalp" };
            var output = string.Empty;
            for (var index = 0; index < names.Length; index++)
            {
                if (index > 0)
                {
                    output += " | ";
                }

                output += index < _state.OrganLosses ? names[index] + " lost" : names[index];
            }

            return output;
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

            for (int i = 0; i < wins.Count; i++)
            {
                var win = wins[i];
                float duration = Mathf.Max(0.12f, 0.6f / (1f + i * 0.4f));

                // 1. Gather all cells involved in this payline
                var positions = win.Payline.Positions;
                var cells = new List<CellRef>();
                for (int j = 0; j < positions.Length; j++)
                {
                    var pos = positions[j];
                    var img = GetGridCellImage(pos.Row, pos.Column);
                    var txt = GetGridCellText(pos.Row, pos.Column);
                    if (img != null)
                    {
                        cells.Add(new CellRef
                        {
                            Image = img,
                            Text = txt,
                            OriginalScale = img.rectTransform.localScale,
                            OriginalColor = img.color,
                            OriginalTextColor = txt != null ? txt.color : Color.white
                        });

                        // Add a thick gold outline to show slot highlight
                        var outline = img.gameObject.GetComponent<Outline>();
                        if (outline == null)
                        {
                            outline = img.gameObject.AddComponent<Outline>();
                        }
                        outline.enabled = true;
                        outline.effectColor = new Color(1f, 0.8f, 0f, 1f); // Gold outline
                        outline.effectDistance = new Vector2(4f, 4f);
                    }
                }

                _resultText.text = "Scored: " + win.Payline.Name + " (" + (win.IsTripleJoker ? "Triple Joker" : win.ResolvedSymbol.ToString()) + ")";

                // 3. Spawn coins for DOTween suck animation
                var coins = new List<GameObject>();
                int loops = result.Score.BatchFactor;
                float pulseDuration = duration;
                if (loops > 1)
                {
                    pulseDuration = Mathf.Max(0.08f, duration / 2f);
                }
                float totalDuration = loops * pulseDuration;

                for (int j = 0; j < cells.Count; j++)
                {
                    var cell = cells[j];
                    var coin = SpawnCoin(cell.Image.transform.position, spawnParent);
                    if (coin != null)
                    {
                        coins.Add(coin);
                        // Animate coin using DOTween
                        Vector3 startPos = cell.Image.transform.position;
                        // Random offset in screen coordinates for burst
                        Vector3 burstOffset = new Vector3(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-40f, 40f), 0f);
                        Vector3 midPos = startPos + burstOffset;

                        Sequence seq = DOTween.Sequence();
                        seq.Append(coin.transform.DOMove(midPos, totalDuration * 0.3f).SetEase(Ease.OutQuad));
                        seq.Join(coin.transform.DOPunchScale(Vector3.one * 0.2f, totalDuration * 0.3f));
                        Vector3 targetPos = _thresholdBarRect != null ? _thresholdBarRect.position : _payoutText.transform.position;
                        seq.Append(coin.transform.DOMove(targetPos, totalDuration * 0.7f).SetEase(Ease.InQuad));
                        seq.Join(coin.transform.DOScale(Vector3.zero, totalDuration * 0.7f).SetEase(Ease.InQuad));
                        seq.OnComplete(() => Destroy(coin));
                    }
                }

                // 4. Run cell scale punch and text lerp over duration
                BigInteger winPayout = win.FinalPayoutKurus;
                BigInteger startPayout = accumulatedPayout;
                accumulatedPayout += winPayout;

                Color highlightColor = new Color(1f, 0.9f, 0.2f, 1f); // Gold glow

                for (int loop = 0; loop < loops; loop++)
                {
                    float elapsed = 0f;
                    while (elapsed < pulseDuration)
                    {
                        elapsed += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsed / pulseDuration);

                        // Scale factor: sine wave peak at 1.18f
                        float scaleFactor = 1f + 0.18f * Mathf.Sin(progress * Mathf.PI);
                        float colorBlend = Mathf.Sin(progress * Mathf.PI);

                        for (int c = 0; c < cells.Count; c++)
                        {
                            var cell = cells[c];
                            cell.Image.rectTransform.localScale = cell.OriginalScale * scaleFactor;
                            cell.Image.color = Color.Lerp(cell.OriginalColor, highlightColor, colorBlend);

                            if (cell.Text != null)
                            {
                                cell.Text.transform.localScale = Vector3.one * scaleFactor;
                                cell.Text.color = Color.Lerp(cell.OriginalTextColor, new Color(1f, 0.3f, 0.1f, 1f), colorBlend);
                            }
                        }

                        // Lerp display payout based on global progress
                        float globalProgress = Mathf.Clamp01(((float)loop + progress) / loops);
                        double doubleProgress = (double)globalProgress;
                        BigInteger currentDisplay = startPayout + (BigInteger)((double)(accumulatedPayout - startPayout) * doubleProgress);
                        _payoutText.text = "Last payout: " + MoneyFormatter.FormatTL(currentDisplay) + " | combo x" + result.Score.ComboMultiplier + " | batch x" + result.Score.BatchFactor;
                        UpdateThresholdBar(
                            startCash + currentDisplay,
                            result.TargetBeforeSpinKurus,
                            result.ThresholdLevelBeforeSpin);

                        yield return null;
                    }
                }

                // 5. Restore original scale & color of cells
                for (int c = 0; c < cells.Count; c++)
                {
                    var cell = cells[c];
                    cell.Image.rectTransform.localScale = cell.OriginalScale;
                    cell.Image.color = cell.OriginalColor;

                    if (cell.Text != null)
                    {
                        cell.Text.transform.localScale = Vector3.one;
                        cell.Text.color = cell.OriginalTextColor;
                    }

                    var outline = cell.Image.GetComponent<Outline>();
                    if (outline != null)
                    {
                        Destroy(outline);
                    }
                }

                // Ensure final coins are cleaned up if any sequence was interrupted
                for (int c = 0; c < coins.Count; c++)
                {
                    if (coins[c] != null)
                    {
                        Destroy(coins[c]);
                    }
                }
            }

            // Finally, snap payout text and show final results summary
            _payoutText.text = "Last payout: " + MoneyFormatter.FormatTL(result.Score.PayoutKurus) + " | combo x" + result.Score.ComboMultiplier + " | batch x" + result.Score.BatchFactor;
            _resultText.text = BuildResultSummary(result);
            UpdateThresholdBar(_state.CashKurus, _state.CurrentTargetKurus);
        }

        private GameObject SpawnCoin(Vector3 spawnPosition, Transform parent)
        {
            GameObject coin;
            if (_coinPrefab != null)
            {
                coin = Instantiate(_coinPrefab, parent);
            }
            else
            {
                // Dynamic fallback UI image
                coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
                coin.transform.SetParent(parent, false);
                var rect = coin.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(30f, 30f);
                var image = coin.GetComponent<Image>();
                image.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold
                
                var outline = coin.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1.5f, 1.5f);
            }
            coin.transform.position = spawnPosition;
            return coin;
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
