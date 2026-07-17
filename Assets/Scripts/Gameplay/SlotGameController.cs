using System;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private TextMeshProUGUI _cashText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _rollsText;
        private TextMeshProUGUI _organsText;
        private TextMeshProUGUI _ticketsText;
        private TextMeshProUGUI _payoutText;
        private TextMeshProUGUI _resultText;
        private TextMeshProUGUI _shopWalletText;
        private TextMeshProUGUI _ownedUpgradesText;
        private TextMeshProUGUI _refreshLabel;
        private readonly TextMeshProUGUI[] _offerLabels = new TextMeshProUGUI[3];
        private readonly Button[] _offerButtons = new Button[3];
        private readonly Image[,] _cellImages = new Image[GameBalance.GridRows, GameBalance.GridColumns];
        private readonly TextMeshProUGUI[,] _cellTexts = new TextMeshProUGUI[GameBalance.GridRows, GameBalance.GridColumns];

        private Button _spin1xButton;
        private Button _spin5xButton;
        private Button _spin10xButton;
        private Button _refreshButton;
        private Button _gameOverRestartButton;
        private Button _victoryRestartButton;
        private GameObject _gameOverOverlay;
        private GameObject _victoryOverlay;

        private void Start()
        {
            _symbolDefinitions = Resources.LoadAll<SymbolDefinition>("SerenaysGambit/Data/Symbols");
            _reelDefinitions = Resources.LoadAll<ReelDefinition>("SerenaysGambit/Data/Reels");
            _shopItemDefinitions = Resources.LoadAll<ShopItemDefinition>("SerenaysGambit/Data/ShopItems");
            _balanceDefinition = Resources.Load<BalanceDefinition>("SerenaysGambit/Data/Balance/DefaultBalance");
            _rulesConfig = RuntimeGameConfigFactory.Create(_symbolDefinitions, _reelDefinitions, _shopItemDefinitions, _balanceDefinition);
            PrepareDefaultFonts();
            if (!BindView())
            {
                enabled = false;
                return;
            }

            StartNewRun();
        }

        private void Update()
        {
            if (_state == null || _state.Phase != RunPhase.Playing)
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
            _runService = new RunService(Environment.TickCount, _rulesConfig);
            _shopService = _runService.Shop;
            _state = _runService.CreateNewRun();
            ClearGrid();
            _resultText.text = "Choose a batch to spin. Space = 1x, 5 = 5x, 0 = 10x.";
            _payoutText.text = "Last payout: TL 0.00";
            _gameOverOverlay.SetActive(false);
            _victoryOverlay.SetActive(false);
            RefreshView();
        }

        private void Spin(int batchFactor)
        {
            var result = _runService.TrySpin(_state, batchFactor);
            _resultText.text = result.Message;
            if (!result.Accepted)
            {
                RefreshView();
                return;
            }

            UpdateGrid(result.Grid);
            _payoutText.text = "Last payout: " + MoneyFormatter.FormatTL(result.Score.PayoutKurus) + " | combo x" + result.Score.ComboMultiplier + " | batch x" + result.Score.BatchFactor;
            _resultText.text = BuildResultSummary(result);

            if (_state.Phase == RunPhase.GameOver)
            {
                _gameOverOverlay.SetActive(true);
            }
            else if (_state.Phase == RunPhase.Victory)
            {
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
                _ownedUpgradesText = Require<TextMeshProUGUI>(root, "MainContent/SerenayShopPanel/OwnedUpgradesText");

                _spin1xButton = Require<Button>(root, "MainContent/SlotMachinePanel/BatchControls/Spin1xButton");
                _spin5xButton = Require<Button>(root, "MainContent/SlotMachinePanel/BatchControls/Spin5xButton");
                _spin10xButton = Require<Button>(root, "MainContent/SlotMachinePanel/BatchControls/Spin10xButton");
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
                _gameOverRestartButton = Require<Button>(root, "GameOverOverlay/RestartButton");
                _victoryRestartButton = Require<Button>(root, "VictoryOverlay/RestartButton");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message);
                return false;
            }

            _spin1xButton.onClick.AddListener(delegate { Spin(1); });
            _spin5xButton.onClick.AddListener(delegate { Spin(5); });
            _spin10xButton.onClick.AddListener(delegate { Spin(10); });
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
            _rollsText.text = "Rolls: " + _state.RollsRemaining;
            _organsText.text = "Organs: " + _state.RemainingOrgans + "/" + _state.Config.OrganCount + " (" + OrganStatusText() + ")";
            _ticketsText.text = "Refresh tickets: " + _state.RefreshTickets;
            _shopWalletText.text = "Your cash: " + MoneyFormatter.FormatTL(_state.CashKurus);
            _ownedUpgradesText.text = "Owned\nStrawberry: x" + _state.Modifiers.StrawberryValue + "\nCherry: x" + _state.Modifiers.CherryValue + "\nMoney: x" + _state.Modifiers.MoneyMultiplier + "\nBase rolls: x" + _state.Modifiers.BaseRollMultiplier + "\nFree spins: +" + _state.Modifiers.TemporaryFreeSpins + "\nMagnet: " + _state.Modifiers.MagnetTier + "/" + _state.Config.MaxMagnetTier;

            _spin1xButton.interactable = _state.Phase == RunPhase.Playing && _state.RollsRemaining >= 1;
            _spin5xButton.interactable = _state.Phase == RunPhase.Playing && _state.RollsRemaining >= 5;
            _spin10xButton.interactable = _state.Phase == RunPhase.Playing && _state.RollsRemaining >= 10;
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
                summary += win.Payline.Name + " (" + (win.IsTripleJoker ? "Triple Joker" : win.ResolvedSymbol.ToString()) + (win.IsMagnetCompletion ? "+ Magnet" : string.Empty) + ")";
            }

            if (result.ThresholdCleared || result.OrganLost)
            {
                summary += ". " + result.Message;
            }

            return summary;
        }

        private void ClearGrid()
        {
            for (var row = 0; row < GameBalance.GridRows; row++)
            {
                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    _cellTexts[row, column].text = "?";
                    _cellImages[row, column].color = new Color(0.88f, 0.88f, 0.88f, 1f);
                }
            }
        }

        private void UpdateGrid(SymbolKind[,] grid)
        {
            for (var row = 0; row < GameBalance.GridRows; row++)
            {
                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    var symbol = grid[row, column];
                    _cellTexts[row, column].text = SymbolLabel(symbol);
                    _cellImages[row, column].color = SymbolColor(symbol);
                }
            }
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
                default: return "JOKER";
            }
        }

        private static Color SymbolColor(SymbolKind symbol)
        {
            switch (symbol)
            {
                case SymbolKind.Strawberry: return new Color(0.95f, 0.84f, 0.84f, 1f);
                case SymbolKind.Cherry: return new Color(0.88f, 0.88f, 0.88f, 1f);
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
