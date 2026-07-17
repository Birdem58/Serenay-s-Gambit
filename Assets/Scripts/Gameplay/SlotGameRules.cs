using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SerenaysGambit
{
    public enum SymbolKind
    {
        Strawberry,
        Cherry,
        Banana,
        Orange,
        Apple,
        Joker
    }

    public enum RunPhase
    {
        Playing,
        Victory,
        GameOver
    }

    public enum ShopOfferKind
    {
        StrawberryValue,
        CherryValue,
        BananaValue,
        OrangeValue,
        AppleValue,
        MoneyMultiplier,
        FreeSpins,
        BaseRollMultiplierX2,
        BaseRollMultiplierX10,
        MagnetTier
    }

    public struct GridPosition
    {
        public GridPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; private set; }
        public int Column { get; private set; }
    }

    public sealed class ReelState
    {
        private readonly SymbolKind[] _faces;

        public ReelState(SymbolKind[] faces, int stopIndex)
        {
            if (faces == null || faces.Length != GameBalance.ReelLength)
            {
                throw new ArgumentException("A reel must contain exactly five faces.", nameof(faces));
            }

            _faces = faces;
            StopIndex = ((stopIndex % _faces.Length) + _faces.Length) % _faces.Length;
        }

        public int StopIndex { get; private set; }

        public SymbolKind VisibleFaceAt(int row)
        {
            if (row < 0 || row >= GameBalance.GridRows)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            return _faces[(StopIndex + row) % _faces.Length];
        }
    }

    public sealed class Payline
    {
        public Payline(string name, int order, params GridPosition[] positions)
        {
            Name = name;
            Order = order;
            Positions = positions;
        }

        public string Name { get; private set; }
        public int Order { get; private set; }
        public GridPosition[] Positions { get; private set; }
    }

    public static class GameBalance
    {
        public const int GridRows = 3;
        public const int GridColumns = 3;
        public const int ReelLength = 5;
        public const int MaxThresholdLevel = 10;
        public const int OrganCount = 5;
        public const int BaseRolls = 10;
        public const int FreeSpinBundle = 20;
        public const int MaxMagnetTier = 5;
        public static readonly BigInteger BaseLinePayoutKurus = new BigInteger(1000); // TL 10.00
        public static readonly BigInteger TripleJokerMultiplierNumerator = new BigInteger(4692);
        public static readonly BigInteger TripleJokerMultiplierDenominator = new BigInteger(100);

        public static readonly SymbolKind[][] InitialReels =
        {
            new[] { SymbolKind.Strawberry, SymbolKind.Cherry, SymbolKind.Banana, SymbolKind.Orange, SymbolKind.Joker },
            new[] { SymbolKind.Cherry, SymbolKind.Banana, SymbolKind.Orange, SymbolKind.Apple, SymbolKind.Joker },
            new[] { SymbolKind.Banana, SymbolKind.Orange, SymbolKind.Apple, SymbolKind.Strawberry, SymbolKind.Joker }
        };

        public static readonly IReadOnlyList<Payline> Paylines = new List<Payline>
        {
            new Payline("Top row", 0, new GridPosition(0, 0), new GridPosition(0, 1), new GridPosition(0, 2)),
            new Payline("Middle row", 1, new GridPosition(1, 0), new GridPosition(1, 1), new GridPosition(1, 2)),
            new Payline("Bottom row", 2, new GridPosition(2, 0), new GridPosition(2, 1), new GridPosition(2, 2)),
            new Payline("Left reel", 3, new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0)),
            new Payline("Middle reel", 4, new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(2, 1)),
            new Payline("Right reel", 5, new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2)),
            new Payline("Top-left diagonal", 6, new GridPosition(0, 0), new GridPosition(1, 1), new GridPosition(2, 2)),
            new Payline("Top-right diagonal", 7, new GridPosition(0, 2), new GridPosition(1, 1), new GridPosition(2, 0))
        };

        public static BigInteger TargetKurus(int level)
        {
            if (level < 1 || level > MaxThresholdLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return BigInteger.Pow(new BigInteger(100), level) * 100;
        }

        public static string OrganNameForLoss(int lossNumber)
        {
            switch (lossNumber)
            {
                case 1: return "Mide";
                case 2: return "Karaciğer";
                case 3: return "Bağırsak";
                case 4: return "Akciğer";
                case 5: return "Kalp";
                default: return "Organ";
            }
        }
    }

    public sealed class ShopItemText
    {
        public ShopItemText(string displayName, string description)
        {
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string DisplayName { get; private set; }
        public string Description { get; private set; }
    }

    // A Unity-free snapshot of authored content. Unity ScriptableObjects are converted to this
    // before a run starts so the simulation remains deterministic and easy to test.
    public sealed class GameRulesConfig
    {
        private readonly SymbolKind[][] _reelStrips;
        private readonly Dictionary<ShopOfferKind, ShopItemText> _shopItemTexts;

        private readonly Dictionary<SymbolKind, int> _startingValues = new Dictionary<SymbolKind, int>();

        public GameRulesConfig(
            IEnumerable<SymbolKind[]> reelStrips,
            int strawberryStartingValue,
            int cherryStartingValue,
            int baseRolls,
            int organCount,
            int thresholdCount,
            int freeSpinBundle,
            int maxMagnetTier,
            IDictionary<ShopOfferKind, ShopItemText> shopItemTexts = null,
            IDictionary<SymbolKind, int> customStartingValues = null)
        {
            if (reelStrips == null)
            {
                throw new ArgumentNullException(nameof(reelStrips));
            }

            _reelStrips = new List<SymbolKind[]>(reelStrips).ToArray();
            if (_reelStrips.Length != GameBalance.GridColumns)
            {
                throw new ArgumentException("The game requires exactly three reel strips.", nameof(reelStrips));
            }

            for (var index = 0; index < _reelStrips.Length; index++)
            {
                if (_reelStrips[index] == null || _reelStrips[index].Length != GameBalance.ReelLength)
                {
                    throw new ArgumentException("Every reel strip must contain exactly five faces.", nameof(reelStrips));
                }

                _reelStrips[index] = (SymbolKind[])_reelStrips[index].Clone();
            }

            if (strawberryStartingValue < 1 || cherryStartingValue < 1 || baseRolls < 1 || organCount < 1 || thresholdCount < 1 || freeSpinBundle < 0 || maxMagnetTier < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRolls), "Runtime balance values must be positive, except free spins which may be zero.");
            }

            StrawberryStartingValue = strawberryStartingValue;
            CherryStartingValue = cherryStartingValue;
            BaseRolls = baseRolls;
            OrganCount = organCount;
            ThresholdCount = thresholdCount;
            FreeSpinBundle = freeSpinBundle;
            MaxMagnetTier = maxMagnetTier;
            _shopItemTexts = shopItemTexts == null
                ? new Dictionary<ShopOfferKind, ShopItemText>()
                : new Dictionary<ShopOfferKind, ShopItemText>(shopItemTexts);

            _startingValues[SymbolKind.Strawberry] = strawberryStartingValue;
            _startingValues[SymbolKind.Cherry] = cherryStartingValue;
            _startingValues[SymbolKind.Banana] = 10;
            _startingValues[SymbolKind.Orange] = 15;
            _startingValues[SymbolKind.Apple] = 20;
            _startingValues[SymbolKind.Joker] = 0;

            if (customStartingValues != null)
            {
                foreach (var kvp in customStartingValues)
                {
                    _startingValues[kvp.Key] = kvp.Value;
                }
            }
        }

        public int GetStartingValue(SymbolKind symbol)
        {
            int val;
            return _startingValues.TryGetValue(symbol, out val) ? val : 0;
        }

        public int StrawberryStartingValue { get; private set; }
        public int CherryStartingValue { get; private set; }
        public int BaseRolls { get; private set; }
        public int OrganCount { get; private set; }
        public int ThresholdCount { get; private set; }
        public int FreeSpinBundle { get; private set; }
        public int MaxMagnetTier { get; private set; }

        public SymbolKind[] ReelStripAt(int column)
        {
            if (column < 0 || column >= _reelStrips.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            return _reelStrips[column];
        }

        public BigInteger TargetKurus(int level)
        {
            if (level < 1 || level > ThresholdCount)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return BigInteger.Pow(new BigInteger(100), level) * 100;
        }

        public ShopItemText FindShopItemText(ShopOfferKind kind)
        {
            ShopItemText text;
            return _shopItemTexts.TryGetValue(kind, out text) ? text : null;
        }

        public static GameRulesConfig CreateDefault()
        {
            return new GameRulesConfig(
                GameBalance.InitialReels,
                1,
                5,
                GameBalance.BaseRolls,
                GameBalance.OrganCount,
                GameBalance.MaxThresholdLevel,
                GameBalance.FreeSpinBundle,
                GameBalance.MaxMagnetTier);
        }
    }

    public sealed class RunModifiers
    {
        private readonly GameRulesConfig _config;
        private readonly Dictionary<SymbolKind, int> _symbolValues = new Dictionary<SymbolKind, int>();

        public RunModifiers() : this(GameRulesConfig.CreateDefault())
        {
        }

        public RunModifiers(GameRulesConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _symbolValues[SymbolKind.Strawberry] = config.GetStartingValue(SymbolKind.Strawberry);
            _symbolValues[SymbolKind.Cherry] = config.GetStartingValue(SymbolKind.Cherry);
            _symbolValues[SymbolKind.Banana] = config.GetStartingValue(SymbolKind.Banana);
            _symbolValues[SymbolKind.Orange] = config.GetStartingValue(SymbolKind.Orange);
            _symbolValues[SymbolKind.Apple] = config.GetStartingValue(SymbolKind.Apple);
            MoneyMultiplier = BigInteger.One;
            BaseRollMultiplier = 1;
            TemporaryFreeSpins = 0;
            MagnetTier = 0;
        }

        public int StrawberryValue { get { return SymbolValue(SymbolKind.Strawberry); } }
        public int CherryValue { get { return SymbolValue(SymbolKind.Cherry); } }
        public int BananaValue { get { return SymbolValue(SymbolKind.Banana); } }
        public int OrangeValue { get { return SymbolValue(SymbolKind.Orange); } }
        public int AppleValue { get { return SymbolValue(SymbolKind.Apple); } }
        public BigInteger MoneyMultiplier { get; private set; }
        public int BaseRollMultiplier { get; private set; }
        public int TemporaryFreeSpins { get; private set; }
        public int MagnetTier { get; private set; }

        public int StartingRolls
        {
            get { return (_config.BaseRolls * BaseRollMultiplier) + TemporaryFreeSpins; }
        }

        public int SymbolValue(SymbolKind symbol)
        {
            int val;
            return _symbolValues.TryGetValue(symbol, out val) ? val : 0;
        }

        public void ImproveSymbol(SymbolKind symbol)
        {
            if (_symbolValues.ContainsKey(symbol))
            {
                _symbolValues[symbol]++;
            }
        }

        public void DoubleMoneyMultiplier()
        {
            MoneyMultiplier *= 2;
        }

        public void AddFreeSpins(int amount)
        {
            TemporaryFreeSpins += Math.Max(0, amount);
        }

        public void ClearThresholdFreeSpins()
        {
            TemporaryFreeSpins = 0;
        }

        public bool SetBaseRollMultiplier(int multiplier)
        {
            if (multiplier <= BaseRollMultiplier)
            {
                return false;
            }

            BaseRollMultiplier = multiplier;
            return true;
        }

        public bool IncreaseMagnetTier()
        {
            if (MagnetTier >= _config.MaxMagnetTier)
            {
                return false;
            }

            MagnetTier++;
            return true;
        }
    }

    public sealed class ShopOffer
    {
        public ShopOffer(ShopOfferKind kind, BigInteger costKurus, string title, string description)
        {
            Kind = kind;
            CostKurus = costKurus;
            Title = title;
            Description = description;
        }

        public ShopOfferKind Kind { get; private set; }
        public BigInteger CostKurus { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public bool Purchased { get; internal set; }
    }

    public sealed class ShopState
    {
        public ShopState()
        {
            Offers = new List<ShopOffer>();
        }

        public List<ShopOffer> Offers { get; private set; }
    }

    public sealed class RunState
    {
        internal RunState() : this(GameRulesConfig.CreateDefault())
        {
        }

        internal RunState(GameRulesConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            ThresholdLevel = 1;
            CashKurus = BigInteger.Zero;
            RollsRemaining = Config.BaseRolls;
            OrganLosses = 0;
            RefreshTickets = 0;
            Phase = RunPhase.Playing;
            Modifiers = new RunModifiers(Config);
            Shop = new ShopState();
        }

        public int ThresholdLevel { get; internal set; }
        public BigInteger CashKurus { get; set; }
        public int RollsRemaining { get; set; }
        public int OrganLosses { get; internal set; }
        public int RefreshTickets { get; internal set; }
        public RunPhase Phase { get; internal set; }
        public RunModifiers Modifiers { get; private set; }
        public GameRulesConfig Config { get; private set; }
        public ShopState Shop { get; private set; }
        public List<ShopOffer> ShopOffers { get { return Shop.Offers; } }

        public BigInteger CurrentTargetKurus
        {
            get { return Config.TargetKurus(ThresholdLevel); }
        }

        public int RemainingOrgans
        {
            get { return Math.Max(0, Config.OrganCount - OrganLosses); }
        }
    }

    public sealed class PaylineWin
    {
        public PaylineWin(Payline payline, SymbolKind resolvedSymbol, BigInteger linePayoutKurus, bool tripleJoker, bool magnetCompletion)
        {
            Payline = payline;
            ResolvedSymbol = resolvedSymbol;
            LinePayoutKurus = linePayoutKurus;
            IsTripleJoker = tripleJoker;
            IsMagnetCompletion = magnetCompletion;
        }

        public Payline Payline { get; private set; }
        public SymbolKind ResolvedSymbol { get; private set; }
        public BigInteger LinePayoutKurus { get; private set; }
        public bool IsTripleJoker { get; private set; }
        public bool IsMagnetCompletion { get; private set; }
    }

    public sealed class ScoredSpin
    {
        public ScoredSpin(IReadOnlyList<PaylineWin> wins, BigInteger payoutKurus, int comboMultiplier, int batchFactor)
        {
            Wins = wins;
            PayoutKurus = payoutKurus;
            ComboMultiplier = comboMultiplier;
            BatchFactor = batchFactor;
        }

        public IReadOnlyList<PaylineWin> Wins { get; private set; }
        public BigInteger PayoutKurus { get; private set; }
        public int ComboMultiplier { get; private set; }
        public int BatchFactor { get; private set; }
    }

    public sealed class SpinResult
    {
        public bool Accepted { get; internal set; }
        public string Message { get; internal set; }
        public SymbolKind[,] Grid { get; internal set; }
        public ScoredSpin Score { get; internal set; }
        public bool ThresholdCleared { get; internal set; }
        public bool OrganLost { get; internal set; }
        public string LostOrganName { get; internal set; }
    }

    public static class MatchResolver
    {
        public static bool TryResolveWinningLine(SymbolKind[] symbols, out SymbolKind resolvedSymbol, out bool tripleJoker)
        {
            resolvedSymbol = SymbolKind.Joker;
            tripleJoker = true;
            var regularFound = false;

            foreach (var symbol in symbols)
            {
                if (symbol == SymbolKind.Joker)
                {
                    continue;
                }

                if (!regularFound)
                {
                    resolvedSymbol = symbol;
                    regularFound = true;
                    tripleJoker = false;
                    continue;
                }

                if (resolvedSymbol != symbol)
                {
                    tripleJoker = false;
                    return false;
                }
            }

            return true;
        }

        public static bool TryResolveMagnetCandidate(SymbolKind[] symbols, out SymbolKind resolvedSymbol)
        {
            resolvedSymbol = SymbolKind.Joker;
            var counts = new Dictionary<SymbolKind, int>();

            foreach (var symbol in symbols)
            {
                if (symbol == SymbolKind.Joker)
                {
                    return false;
                }

                if (!counts.ContainsKey(symbol))
                {
                    counts[symbol] = 0;
                }

                counts[symbol]++;
            }

            if (counts.Count != 2)
            {
                return false;
            }

            foreach (var pair in counts)
            {
                if (pair.Value == 2)
                {
                    resolvedSymbol = pair.Key;
                    return true;
                }
            }

            return false;
        }
    }

    public static class PayoutCalculator
    {
        public static BigInteger CalculateLinePayout(SymbolKind symbol, RunModifiers modifiers, bool tripleJoker)
        {
            if (tripleJoker)
            {
                return (GameBalance.BaseLinePayoutKurus * GameBalance.TripleJokerMultiplierNumerator) / GameBalance.TripleJokerMultiplierDenominator;
            }

            return GameBalance.BaseLinePayoutKurus * modifiers.SymbolValue(symbol);
        }

        public static int ComboMultiplierFor(int winCount)
        {
            if (winCount <= 1)
            {
                return 1;
            }

            return winCount == 2 ? 4 : 9;
        }

        public static BigInteger CalculateFinalPayout(BigInteger rawPayout, int comboMultiplier, BigInteger moneyMultiplier, int batchFactor)
        {
            return rawPayout * comboMultiplier * moneyMultiplier * batchFactor;
        }
    }

    public static class SlotScoring
    {
        public static ScoredSpin Evaluate(SymbolKind[,] grid, RunModifiers modifiers, int batchFactor)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (grid.GetLength(0) != GameBalance.GridRows || grid.GetLength(1) != GameBalance.GridColumns)
            {
                throw new ArgumentException("The slot grid must be 3 by 3.", nameof(grid));
            }

            if (batchFactor != 1 && batchFactor != 5 && batchFactor != 10)
            {
                throw new ArgumentOutOfRangeException(nameof(batchFactor));
            }

            var wins = new List<PaylineWin>();
            var magnetCandidates = new List<PaylineWin>();

            foreach (var payline in GameBalance.Paylines)
            {
                var symbols = ReadSymbols(grid, payline);
                SymbolKind resolved;
                bool tripleJoker;

                if (MatchResolver.TryResolveWinningLine(symbols, out resolved, out tripleJoker))
                {
                    wins.Add(new PaylineWin(payline, resolved, PayoutCalculator.CalculateLinePayout(resolved, modifiers, tripleJoker), tripleJoker, false));
                    continue;
                }

                if (MatchResolver.TryResolveMagnetCandidate(symbols, out resolved))
                {
                    magnetCandidates.Add(new PaylineWin(payline, resolved, PayoutCalculator.CalculateLinePayout(resolved, modifiers, false), false, true));
                }
            }

            magnetCandidates.Sort(CompareMagnetCandidates);
            var magnetCount = Math.Min(modifiers.MagnetTier, magnetCandidates.Count);
            for (var index = 0; index < magnetCount; index++)
            {
                wins.Add(magnetCandidates[index]);
            }

            var comboMultiplier = PayoutCalculator.ComboMultiplierFor(wins.Count);
            var rawPayout = BigInteger.Zero;
            foreach (var win in wins)
            {
                rawPayout += win.LinePayoutKurus;
            }

            var finalPayout = PayoutCalculator.CalculateFinalPayout(rawPayout, comboMultiplier, modifiers.MoneyMultiplier, batchFactor);
            return new ScoredSpin(wins, finalPayout, comboMultiplier, batchFactor);
        }

        public static int ComboMultiplierFor(int winCount)
        {
            return PayoutCalculator.ComboMultiplierFor(winCount);
        }

        private static int CompareMagnetCandidates(PaylineWin left, PaylineWin right)
        {
            var payoutComparison = right.LinePayoutKurus.CompareTo(left.LinePayoutKurus);
            return payoutComparison != 0 ? payoutComparison : left.Payline.Order.CompareTo(right.Payline.Order);
        }

        private static SymbolKind[] ReadSymbols(SymbolKind[,] grid, Payline payline)
        {
            var symbols = new SymbolKind[payline.Positions.Length];
            for (var index = 0; index < payline.Positions.Length; index++)
            {
                var position = payline.Positions[index];
                symbols[index] = grid[position.Row, position.Column];
            }

            return symbols;
        }

    }

    public sealed class SlotGameEngine
    {
        private readonly Random _random;
        private readonly GameRulesConfig _config;
        private readonly Func<SymbolKind[,]> _boardFactory;

        public SlotGameEngine(int seed) : this(seed, GameRulesConfig.CreateDefault(), null)
        {
        }

        public SlotGameEngine(int seed, Func<SymbolKind[,]> boardFactory) : this(seed, GameRulesConfig.CreateDefault(), boardFactory)
        {
        }

        public SlotGameEngine(int seed, GameRulesConfig config) : this(seed, config, null)
        {
        }

        public SlotGameEngine(int seed, GameRulesConfig config, Func<SymbolKind[,]> boardFactory)
        {
            _random = new Random(seed);
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _boardFactory = boardFactory;
        }

        public RunState CreateNewRun()
        {
            var state = new RunState(_config);
            RefillRolls(state);
            GenerateShop(state);
            return state;
        }

        public SpinResult TrySpin(RunState state, int batchFactor)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Phase != RunPhase.Playing)
            {
                return RejectedResult("Start a new run to spin again.");
            }

            if (batchFactor != 1 && batchFactor != 5 && batchFactor != 10)
            {
                return RejectedResult("Batch must be 1x, 5x, or 10x.");
            }

            if (state.RollsRemaining < batchFactor)
            {
                return RejectedResult("Not enough rolls for that batch.");
            }

            state.RollsRemaining -= batchFactor;
            var grid = CreateGrid();
            var score = SlotScoring.Evaluate(grid, state.Modifiers, batchFactor);
            state.CashKurus += score.PayoutKurus;

            var result = new SpinResult
            {
                Accepted = true,
                Message = score.Wins.Count == 0 ? "No matching paylines." : "Winning paylines resolved.",
                Grid = grid,
                Score = score
            };

            if (TrySettleThreshold(state))
            {
                result.ThresholdCleared = true;
                result.Message = state.Phase == RunPhase.Victory ? "All ten thresholds cleared!" : "Threshold paid. Serenay has new offers.";
            }
            else if (ResolveFailureIfOutOfRolls(state, out var lostOrgan))
            {
                result.OrganLost = true;
                result.LostOrganName = lostOrgan;
                result.Message = state.Phase == RunPhase.GameOver ? "Kalp lost. Game over." : lostOrgan + " lost. Same threshold, one refresh ticket gained.";
            }

            return result;
        }

        public bool TrySettleThreshold(RunState state)
        {
            if (state == null || state.Phase != RunPhase.Playing || state.CashKurus < state.CurrentTargetKurus)
            {
                return false;
            }

            state.CashKurus -= state.CurrentTargetKurus;
            if (state.ThresholdLevel >= state.Config.ThresholdCount)
            {
                state.Phase = RunPhase.Victory;
                return true;
            }

            state.ThresholdLevel++;
            state.Modifiers.ClearThresholdFreeSpins();
            RefillRolls(state);
            GenerateShop(state);
            return true;
        }

        public bool ResolveFailureIfOutOfRolls(RunState state, out string lostOrgan)
        {
            lostOrgan = string.Empty;
            if (state == null || state.Phase != RunPhase.Playing || state.RollsRemaining > 0)
            {
                return false;
            }

            state.OrganLosses++;
            lostOrgan = GameBalance.OrganNameForLoss(state.OrganLosses);
            if (state.OrganLosses >= state.Config.OrganCount)
            {
                state.Phase = RunPhase.GameOver;
                return true;
            }

            state.RefreshTickets++;
            RefillRolls(state);
            return true;
        }

        public bool TryPurchase(RunState state, int offerIndex, out string message)
        {
            message = string.Empty;
            if (state == null || state.Phase != RunPhase.Playing)
            {
                message = "The shop is closed.";
                return false;
            }

            if (offerIndex < 0 || offerIndex >= state.ShopOffers.Count)
            {
                message = "That shop offer does not exist.";
                return false;
            }

            var offer = state.ShopOffers[offerIndex];
            if (offer.Purchased)
            {
                message = "That offer is sold out.";
                return false;
            }

            if (state.CashKurus < offer.CostKurus)
            {
                message = "Not enough TL.";
                return false;
            }

            state.CashKurus -= offer.CostKurus;
            var rollsBeforeUpgrade = state.Modifiers.StartingRolls;

            switch (offer.Kind)
            {
                case ShopOfferKind.StrawberryValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Strawberry);
                    break;
                case ShopOfferKind.CherryValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Cherry);
                    break;
                case ShopOfferKind.BananaValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Banana);
                    break;
                case ShopOfferKind.OrangeValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Orange);
                    break;
                case ShopOfferKind.AppleValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Apple);
                    break;
                case ShopOfferKind.MoneyMultiplier:
                    state.Modifiers.DoubleMoneyMultiplier();
                    break;
                case ShopOfferKind.FreeSpins:
                    state.Modifiers.AddFreeSpins(state.Config.FreeSpinBundle);
                    state.RollsRemaining += state.Config.FreeSpinBundle;
                    break;
                case ShopOfferKind.BaseRollMultiplierX2:
                    state.Modifiers.SetBaseRollMultiplier(2);
                    state.RollsRemaining += state.Modifiers.StartingRolls - rollsBeforeUpgrade;
                    break;
                case ShopOfferKind.BaseRollMultiplierX10:
                    state.Modifiers.SetBaseRollMultiplier(10);
                    state.RollsRemaining += state.Modifiers.StartingRolls - rollsBeforeUpgrade;
                    break;
                case ShopOfferKind.MagnetTier:
                    state.Modifiers.IncreaseMagnetTier();
                    break;
            }

            offer.Purchased = true;
            message = offer.Title + " purchased.";
            return true;
        }

        public bool TryRefreshShop(RunState state, out string message)
        {
            message = string.Empty;
            if (state == null || state.Phase != RunPhase.Playing)
            {
                message = "The shop is closed.";
                return false;
            }

            if (state.RefreshTickets <= 0)
            {
                message = "No refresh tickets available.";
                return false;
            }

            state.RefreshTickets--;
            GenerateShop(state, true);
            message = "Serenay refreshed the shop.";
            return true;
        }

        private SpinResult RejectedResult(string message)
        {
            return new SpinResult
            {
                Accepted = false,
                Message = message
            };
        }

        private SymbolKind[,] CreateGrid()
        {
            if (_boardFactory != null)
            {
                return _boardFactory();
            }

            var grid = new SymbolKind[GameBalance.GridRows, GameBalance.GridColumns];
            for (var column = 0; column < GameBalance.GridColumns; column++)
            {
                var strip = _config.ReelStripAt(column);
                var stop = _random.Next(GameBalance.ReelLength);
                var reel = new ReelState(strip, stop);
                for (var row = 0; row < GameBalance.GridRows; row++)
                {
                    grid[row, column] = reel.VisibleFaceAt(row);
                }
            }

            return grid;
        }

        private void RefillRolls(RunState state)
        {
            state.RollsRemaining = state.Modifiers.StartingRolls;
        }

        private void GenerateShop(RunState state, bool rerollUnsoldOnly = false)
        {
            var options = new List<ShopOfferKind>
            {
                ShopOfferKind.StrawberryValue,
                ShopOfferKind.CherryValue,
                ShopOfferKind.BananaValue,
                ShopOfferKind.OrangeValue,
                ShopOfferKind.AppleValue,
                ShopOfferKind.MoneyMultiplier,
                ShopOfferKind.FreeSpins
            };

            if (state.Modifiers.BaseRollMultiplier < 2)
            {
                options.Add(ShopOfferKind.BaseRollMultiplierX2);
            }
            else if (state.Modifiers.BaseRollMultiplier < 10)
            {
                options.Add(ShopOfferKind.BaseRollMultiplierX10);
            }

            if (state.Modifiers.MagnetTier < state.Config.MaxMagnetTier)
            {
                options.Add(ShopOfferKind.MagnetTier);
            }

            if (!rerollUnsoldOnly)
            {
                state.ShopOffers.Clear();
                while (state.ShopOffers.Count < 3 && options.Count > 0)
                {
                    var pickIndex = _random.Next(options.Count);
                    var kind = options[pickIndex];
                    options.RemoveAt(pickIndex);
                    state.ShopOffers.Add(CreateOffer(state, kind));
                }

                return;
            }

            for (var offerIndex = 0; offerIndex < state.ShopOffers.Count && options.Count > 0; offerIndex++)
            {
                if (state.ShopOffers[offerIndex].Purchased)
                {
                    continue;
                }

                var pickIndex = _random.Next(options.Count);
                var kind = options[pickIndex];
                options.RemoveAt(pickIndex);
                state.ShopOffers[offerIndex] = CreateOffer(state, kind);
            }
        }

        private ShopOffer CreateOffer(RunState state, ShopOfferKind kind)
        {
            var target = state.CurrentTargetKurus;
            var cost = target / 20;
            string title;
            string description;

            switch (kind)
            {
                case ShopOfferKind.StrawberryValue:
                    title = "Strawberry Value +1x";
                    description = "Permanently raises Strawberry line value.";
                    cost = target / 25;
                    break;
                case ShopOfferKind.CherryValue:
                    title = "Cherry Value +1x";
                    description = "Permanently raises Cherry line value.";
                    cost = target / 16;
                    break;
                case ShopOfferKind.BananaValue:
                    title = "Banana Value +1x";
                    description = "Permanently raises Banana line value.";
                    cost = target / 12;
                    break;
                case ShopOfferKind.OrangeValue:
                    title = "Orange Value +1x";
                    description = "Permanently raises Orange line value.";
                    cost = target / 10;
                    break;
                case ShopOfferKind.AppleValue:
                    title = "Apple Value +1x";
                    description = "Permanently raises Apple line value.";
                    cost = target / 8;
                    break;
                case ShopOfferKind.MoneyMultiplier:
                    title = "Money Output x2";
                    description = "Doubles all future final TL payouts.";
                    cost = target / 10;
                    break;
                case ShopOfferKind.FreeSpins:
                    title = "+20 Free Spins";
                    description = "Adds rolls until this threshold is cleared.";
                    cost = target / 20;
                    break;
                case ShopOfferKind.BaseRollMultiplierX2:
                    title = "Base Rolls x2";
                    description = "Raises the ten-roll base to twenty for this run.";
                    cost = target / 5;
                    break;
                case ShopOfferKind.BaseRollMultiplierX10:
                    title = "Base Rolls x10";
                    description = "Raises the ten-roll base to one hundred for this run.";
                    cost = target / 2;
                    break;
                case ShopOfferKind.MagnetTier:
                    title = "Magnet Tier " + (state.Modifiers.MagnetTier + 1);
                    description = "Completes up to " + (state.Modifiers.MagnetTier + 1) + " near-miss paylines each board.";
                    cost = (target / 20) * (state.Modifiers.MagnetTier + 1);
                    break;
                default:
                    title = "Unknown Offer";
                    description = string.Empty;
                    break;
            }

            if (cost < 100)
            {
                cost = 100;
            }

            var authoredText = state.Config.FindShopItemText(kind);
            if (authoredText != null)
            {
                if (!string.IsNullOrEmpty(authoredText.DisplayName))
                {
                    title = kind == ShopOfferKind.MagnetTier
                        ? authoredText.DisplayName + " " + (state.Modifiers.MagnetTier + 1)
                        : authoredText.DisplayName;
                }

                if (!string.IsNullOrEmpty(authoredText.Description))
                {
                    description = kind == ShopOfferKind.MagnetTier
                        ? authoredText.Description + " Tier " + (state.Modifiers.MagnetTier + 1) + " completes that many eligible near-miss paylines."
                        : authoredText.Description;
                }
            }

            return new ShopOffer(kind, cost, title, description);
        }
    }

    public sealed class RunService
    {
        private readonly SlotGameEngine _engine;

        public RunService(int seed) : this(seed, GameRulesConfig.CreateDefault())
        {
        }

        public RunService(int seed, GameRulesConfig config)
        {
            _engine = new SlotGameEngine(seed, config);
            Shop = new ShopService(_engine);
        }

        public ShopService Shop { get; private set; }

        public RunState CreateNewRun()
        {
            return _engine.CreateNewRun();
        }

        public SpinResult TrySpin(RunState state, int batchFactor)
        {
            return _engine.TrySpin(state, batchFactor);
        }
    }

    public sealed class ShopService
    {
        private readonly SlotGameEngine _engine;

        internal ShopService(SlotGameEngine engine)
        {
            _engine = engine;
        }

        public bool TryPurchase(RunState state, int offerIndex, out string message)
        {
            return _engine.TryPurchase(state, offerIndex, out message);
        }

        public bool TryRefresh(RunState state, out string message)
        {
            return _engine.TryRefreshShop(state, out message);
        }
    }

    public static class MoneyFormatter
    {
        public static string FormatTL(BigInteger kurus)
        {
            var negative = kurus.Sign < 0;
            var absolute = BigInteger.Abs(kurus);
            var whole = absolute / 100;
            var fraction = (int)(absolute % 100);
            return (negative ? "-" : string.Empty) + "TL " + GroupDigits(whole.ToString()) + "." + fraction.ToString("D2");
        }

        private static string GroupDigits(string digits)
        {
            var result = new StringBuilder();
            for (var index = 0; index < digits.Length; index++)
            {
                if (index > 0 && (digits.Length - index) % 3 == 0)
                {
                    result.Append(',');
                }

                result.Append(digits[index]);
            }

            return result.ToString();
        }
    }
}
