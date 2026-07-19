using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SerenaysGambit
{
    public enum SymbolKind
    {
        Absolut = 0,
        Dollar = 1,
        Cat = 3,
        Cigarette = 4,
        Kiss = 5
    }

    public enum RunPhase
    {
        Playing,
        Victory,
        GameOver
    }

    // Every payline belongs to one visual family. Match-count upgrades use this
    // instead of an individual line ID so all rows, reels, or diagonals level up together.
    public enum PaylineGroup
    {
        Horizontal,
        Vertical,
        CrissCross
    }

    public enum GambitKind
    {
        Absolut,
        BatchTen,
        Kiss1000x,
        CigaretteDecay,
        BatchHundred,
        BatchThousand,
        BatchTenThousand,
        CigaretteSkip,
        AbsolutPoisoning
    }

    public enum ThresholdLevel
    {
        Any = 0,
        Threshold1 = 1,
        Threshold2 = 2,
        Threshold3 = 3,
        Threshold4 = 4,
        Threshold5 = 5,
        Threshold6 = 6,
        Threshold7 = 7,
        Threshold8 = 8,
        Threshold9 = 9,
        Threshold10 = 10
    }

    public enum ShopOfferKind
    {
        AbsolutValue = 0,
        DollarValue = 1,
        CatValue = 3,
        CigaretteValue = 4,
        MoneyMultiplier = 5,
        FreeSpins = 6,
        BaseRollMultiplierX2 = 7,
        BaseRollMultiplierX10 = 8,
        BaseOutputMultiplier = 9,
        HorizontalMatchMultiplier = 10,
        VerticalMatchMultiplier = 11,
        CrissCrossMatchMultiplier = 12,
        KissValue = 13
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

            _faces = (SymbolKind[])faces.Clone();
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
            : this(name, order, PaylineGroup.Horizontal, positions)
        {
        }

        public Payline(string name, int order, PaylineGroup group, params GridPosition[] positions)
        {
            Name = name;
            Order = order;
            Group = group;
            Positions = positions;
        }

        public string Name { get; private set; }
        public int Order { get; private set; }
        public PaylineGroup Group { get; private set; }
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
        public const int MaximumBatchFactor = 10000;
        public static readonly BigInteger BaseLinePayoutKurus = new BigInteger(1000); // TL 10.00
        public static readonly BigInteger TripleKissMultiplierNumerator = new BigInteger(4692);
        public static readonly BigInteger TripleKissMultiplierDenominator = new BigInteger(100);
        public const int MaxPlusWinScoringInterval = 5;
        public const int MaxPlusWinChanceDivisor = 3;
        public const int MaxPlusWinPayoutMultiplier = 100;

        public static readonly SymbolKind[][] InitialReels =
        {
            new[] { SymbolKind.Absolut, SymbolKind.Dollar, SymbolKind.Cigarette, SymbolKind.Cat, SymbolKind.Kiss },
            new[] { SymbolKind.Dollar, SymbolKind.Cigarette, SymbolKind.Cat, SymbolKind.Cigarette, SymbolKind.Kiss },
            new[] { SymbolKind.Cigarette, SymbolKind.Cat, SymbolKind.Cigarette, SymbolKind.Absolut, SymbolKind.Kiss }
        };

        public static readonly IReadOnlyList<Payline> Paylines = new List<Payline>
        {
            new Payline("Top row", 0, PaylineGroup.Horizontal, new GridPosition(0, 0), new GridPosition(0, 1), new GridPosition(0, 2)),
            new Payline("Middle row", 1, PaylineGroup.Horizontal, new GridPosition(1, 0), new GridPosition(1, 1), new GridPosition(1, 2)),
            new Payline("Bottom row", 2, PaylineGroup.Horizontal, new GridPosition(2, 0), new GridPosition(2, 1), new GridPosition(2, 2)),
            new Payline("Left reel", 3, PaylineGroup.Vertical, new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0)),
            new Payline("Middle reel", 4, PaylineGroup.Vertical, new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(2, 1)),
            new Payline("Right reel", 5, PaylineGroup.Vertical, new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2)),
            new Payline("Top-left diagonal", 6, PaylineGroup.CrissCross, new GridPosition(0, 0), new GridPosition(1, 1), new GridPosition(2, 2)),
            new Payline("Top-right diagonal", 7, PaylineGroup.CrissCross, new GridPosition(0, 2), new GridPosition(1, 1), new GridPosition(2, 0))
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

        public static bool IsSupportedBatchFactor(int batchFactor)
        {
            return batchFactor == 1
                || batchFactor == 5
                || batchFactor == 10
                || batchFactor == 100
                || batchFactor == 1000
                || batchFactor == MaximumBatchFactor;
        }
    }

    public sealed class ShopItemConfig
    {
        public ShopItemConfig(string displayName, string description, int symbolImprovementDelta = 0, int baseRollMultiplierValue = 0, int costDivisor = 0, ThresholdLevel displayThreshold = ThresholdLevel.Any)
        {
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            SymbolImprovementDelta = symbolImprovementDelta;
            BaseRollMultiplierValue = baseRollMultiplierValue;
            CostDivisor = costDivisor;
            DisplayThreshold = displayThreshold;
        }

        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public int SymbolImprovementDelta { get; private set; }
        public int BaseRollMultiplierValue { get; private set; }
        public int CostDivisor { get; private set; }
        public ThresholdLevel DisplayThreshold { get; private set; }
    }

    public sealed class GambitItemConfig
    {
        public GambitItemConfig(
            int payoutMultiplier = 1,
            int rollMultiplier = 1,
            int sacrificePercent = 0,
            int riskPercent = 0,
            int decayPerMiss = 0)
        {
            PayoutMultiplier = Math.Max(1, payoutMultiplier);
            RollMultiplier = Math.Max(1, rollMultiplier);
            SacrificePercent = Math.Max(0, Math.Min(100, sacrificePercent));
            RiskPercent = Math.Max(0, Math.Min(100, riskPercent));
            DecayPerMiss = Math.Max(0, decayPerMiss);
        }

        public int PayoutMultiplier { get; private set; }
        public int RollMultiplier { get; private set; }
        public int SacrificePercent { get; private set; }
        public int RiskPercent { get; private set; }
        public int DecayPerMiss { get; private set; }
    }

    // A Unity-free snapshot of authored content. Unity ScriptableObjects are converted to this
    // before a run starts so the simulation remains deterministic and easy to test.
    public sealed class GameRulesConfig
    {
        private readonly SymbolKind[][] _reelStrips;
        private readonly Dictionary<string, ShopItemConfig> _shopItemConfigs;
        private readonly Dictionary<GambitKind, GambitItemConfig> _gambitItemConfigs;

        private readonly Dictionary<SymbolKind, int> _startingValues = new Dictionary<SymbolKind, int>();

        public GameRulesConfig(
            IEnumerable<SymbolKind[]> reelStrips,
            int absolutStartingValue,
            int dollarStartingValue,
            int baseRolls,
            int organCount,
            int thresholdCount,
            int freeSpinBundle,
            IEnumerable<KeyValuePair<ShopOfferKind, ShopItemConfig>> shopItemConfigs = null,
            IDictionary<SymbolKind, int> customStartingValues = null,
            IDictionary<GambitKind, GambitItemConfig> gambitItemConfigs = null)
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

            if (absolutStartingValue < 1 || dollarStartingValue < 1 || baseRolls < 1 || organCount < 1 || thresholdCount < 1 || freeSpinBundle < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRolls), "Runtime balance values must be positive, except free spins which may be zero.");
            }

            AbsolutStartingValue = absolutStartingValue;
            DollarStartingValue = dollarStartingValue;
            BaseRolls = baseRolls;
            OrganCount = organCount;
            ThresholdCount = thresholdCount;
            FreeSpinBundle = freeSpinBundle;
            _shopItemConfigs = new Dictionary<string, ShopItemConfig>();
            if (shopItemConfigs != null)
            {
                foreach (var entry in shopItemConfigs)
                {
                    string key = $"{(int)entry.Key}_{(int)entry.Value.DisplayThreshold}";
                    _shopItemConfigs[key] = entry.Value;
                }
            }
            _gambitItemConfigs = CreateDefaultGambitConfigs();
            if (gambitItemConfigs != null)
            {
                foreach (var kvp in gambitItemConfigs)
                {
                    if (Enum.IsDefined(typeof(GambitKind), kvp.Key) && kvp.Value != null)
                    {
                        _gambitItemConfigs[kvp.Key] = kvp.Value;
                    }
                }
            }

            _startingValues[SymbolKind.Absolut] = absolutStartingValue;
            _startingValues[SymbolKind.Dollar] = dollarStartingValue;
            _startingValues[SymbolKind.Cat] = 15;
            _startingValues[SymbolKind.Cigarette] = 20;
            _startingValues[SymbolKind.Kiss] = 1;

            if (customStartingValues != null)
            {
                foreach (var kvp in customStartingValues)
                {
                    _startingValues[kvp.Key] = kvp.Value;
                }
            }
        }

        internal GameRulesConfig(
            IEnumerable<SymbolKind[]> reelStrips,
            int absolutStartingValue,
            int dollarStartingValue,
            int baseRolls,
            int organCount,
            int thresholdCount,
            int freeSpinBundle,
            Dictionary<string, ShopItemConfig> shopItemConfigs,
            IDictionary<SymbolKind, int> startingValues,
            IDictionary<GambitKind, GambitItemConfig> gambitItemConfigs,
            bool isCopy)
        {
            _reelStrips = new List<SymbolKind[]>(reelStrips).ToArray();
            for (var index = 0; index < _reelStrips.Length; index++)
            {
                _reelStrips[index] = (SymbolKind[])_reelStrips[index].Clone();
            }
            AbsolutStartingValue = absolutStartingValue;
            DollarStartingValue = dollarStartingValue;
            BaseRolls = baseRolls;
            OrganCount = organCount;
            ThresholdCount = thresholdCount;
            FreeSpinBundle = freeSpinBundle;
            _shopItemConfigs = shopItemConfigs == null
                ? new Dictionary<string, ShopItemConfig>()
                : new Dictionary<string, ShopItemConfig>(shopItemConfigs);
            _startingValues = startingValues == null
                ? new Dictionary<SymbolKind, int>()
                : new Dictionary<SymbolKind, int>(startingValues);
            _gambitItemConfigs = gambitItemConfigs == null
                ? new Dictionary<GambitKind, GambitItemConfig>()
                : new Dictionary<GambitKind, GambitItemConfig>(gambitItemConfigs);
        }

        public int GetStartingValue(SymbolKind symbol)
        {
            int val;
            return _startingValues.TryGetValue(symbol, out val) ? val : 0;
        }

        public int AbsolutStartingValue { get; private set; }
        public int DollarStartingValue { get; private set; }
        public int BaseRolls { get; private set; }
        public int OrganCount { get; private set; }
        public int ThresholdCount { get; private set; }
        public int FreeSpinBundle { get; private set; }

        public SymbolKind[] ReelStripAt(int column)
        {
            if (column < 0 || column >= _reelStrips.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            return (SymbolKind[])_reelStrips[column].Clone();
        }

        public void ReplaceSymbolOnStrip(int column, int index, SymbolKind newSymbol)
        {
            if (column >= 0 && column < _reelStrips.Length && index >= 0 && index < _reelStrips[column].Length)
            {
                _reelStrips[column][index] = newSymbol;
            }
        }

        public BigInteger TargetKurus(int level)
        {
            if (level < 1 || level > ThresholdCount)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return BigInteger.Pow(new BigInteger(100), level) * 100;
        }

        public ShopItemConfig FindShopItemConfig(ShopOfferKind kind, int thresholdLevel)
        {
            ShopItemConfig config;
            string key = $"{(int)kind}_{thresholdLevel}";
            if (_shopItemConfigs.TryGetValue(key, out config))
            {
                return config;
            }
            string fallbackKey = $"{(int)kind}_{(int)ThresholdLevel.Any}";
            if (_shopItemConfigs.TryGetValue(fallbackKey, out config))
            {
                return config;
            }
            return null;
        }

        public ShopItemConfig FindShopItemConfig(ShopOfferKind kind)
        {
            return FindShopItemConfig(kind, (int)ThresholdLevel.Any);
        }

        public bool HasAnyShopItemConfig(ShopOfferKind kind)
        {
            string prefix = $"{(int)kind}_";
            foreach (var key in _shopItemConfigs.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        public GambitItemConfig FindGambitItemConfig(GambitKind kind)
        {
            GambitItemConfig config;
            return _gambitItemConfigs.TryGetValue(kind, out config) ? config : null;
        }

        // Reel strips can change during a run (for example, when a Kiss is lost),
        // so every new run needs an independent copy of the authored rules.
        public GameRulesConfig CreateRunCopy()
        {
            return new GameRulesConfig(
                _reelStrips,
                AbsolutStartingValue,
                DollarStartingValue,
                BaseRolls,
                OrganCount,
                ThresholdCount,
                FreeSpinBundle,
                _shopItemConfigs,
                _startingValues,
                _gambitItemConfigs,
                true);
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
                null,
                null,
                CreateDefaultGambitConfigs());
        }

        private static Dictionary<GambitKind, GambitItemConfig> CreateDefaultGambitConfigs()
        {
            return new Dictionary<GambitKind, GambitItemConfig>
            {
                { GambitKind.Absolut, new GambitItemConfig(payoutMultiplier: 10, sacrificePercent: 25) },
                { GambitKind.BatchTen, new GambitItemConfig(rollMultiplier: 10) },
                { GambitKind.Kiss1000x, new GambitItemConfig(payoutMultiplier: 1000, riskPercent: 15) },
                { GambitKind.CigaretteDecay, new GambitItemConfig(payoutMultiplier: 5, decayPerMiss: 1) },
                { GambitKind.BatchHundred, new GambitItemConfig(rollMultiplier: 10) },
                { GambitKind.BatchThousand, new GambitItemConfig(rollMultiplier: 15) },
                { GambitKind.BatchTenThousand, new GambitItemConfig(rollMultiplier: 20) },
                { GambitKind.CigaretteSkip, new GambitItemConfig() },
                { GambitKind.AbsolutPoisoning, new GambitItemConfig(payoutMultiplier: 10) }
            };
        }
    }

    public sealed class RunModifiers
    {
        private readonly GameRulesConfig _config;
        private readonly Dictionary<SymbolKind, int> _symbolValues = new Dictionary<SymbolKind, int>();
        private readonly Dictionary<PaylineGroup, int> _matchCountMultiplierIndexes = new Dictionary<PaylineGroup, int>();
        private readonly Dictionary<GambitKind, int> _gambitCounts = new Dictionary<GambitKind, int>();

        public RunModifiers() : this(GameRulesConfig.CreateDefault())
        {
        }

        private static readonly int[] BaseOutputMultipliers = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 1024 };
        private static readonly int[] MatchCountMultipliers = { 1, 2, 5, 10, 100 };

        public RunModifiers(GameRulesConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _symbolValues[SymbolKind.Absolut] = config.GetStartingValue(SymbolKind.Absolut);
            _symbolValues[SymbolKind.Dollar] = config.GetStartingValue(SymbolKind.Dollar);
            _symbolValues[SymbolKind.Cat] = config.GetStartingValue(SymbolKind.Cat);
            _symbolValues[SymbolKind.Cigarette] = config.GetStartingValue(SymbolKind.Cigarette);
            _symbolValues[SymbolKind.Kiss] = config.GetStartingValue(SymbolKind.Kiss);
            MoneyMultiplier = BigInteger.One;
            BaseRollMultiplier = 1;
            TemporaryFreeSpins = 0;
            BaseOutputMultiplierIndex = 0;
            foreach (PaylineGroup group in Enum.GetValues(typeof(PaylineGroup)))
            {
                _matchCountMultiplierIndexes[group] = 0;
            }
        }

        public int AbsolutValue { get { return SymbolValue(SymbolKind.Absolut); } }
        public int DollarValue { get { return SymbolValue(SymbolKind.Dollar); } }
        public int CatValue { get { return SymbolValue(SymbolKind.Cat); } }
        public int CigaretteValue { get { return SymbolValue(SymbolKind.Cigarette); } }
        public int KissValue { get { return SymbolValue(SymbolKind.Kiss); } }
        public BigInteger MoneyMultiplier { get; private set; }
        public int BaseRollMultiplier { get; private set; }
        public int TemporaryFreeSpins { get; private set; }
        public int BaseOutputMultiplierIndex { get; private set; }
        public int HorizontalMatchCountMultiplier { get { return MatchCountMultiplier(PaylineGroup.Horizontal); } }
        public int VerticalMatchCountMultiplier { get { return MatchCountMultiplier(PaylineGroup.Vertical); } }
        public int CrissCrossMatchCountMultiplier { get { return MatchCountMultiplier(PaylineGroup.CrissCross); } }

        public int AbsolutGambitCount
        {
            get { return GambitCount(GambitKind.Absolut); }
            set { SetGambitCount(GambitKind.Absolut, value); }
        }

        public int BatchTenGambitCount
        {
            get { return GambitCount(GambitKind.BatchTen); }
            set { SetGambitCount(GambitKind.BatchTen, value); }
        }

        public int Kiss1000xGambitCount
        {
            get { return GambitCount(GambitKind.Kiss1000x); }
            set { SetGambitCount(GambitKind.Kiss1000x, value); }
        }

        public int CigaretteDecayGambitCount
        {
            get { return GambitCount(GambitKind.CigaretteDecay); }
            set { SetGambitCount(GambitKind.CigaretteDecay, value); }
        }

        public int BatchHundredGambitCount
        {
            get { return GambitCount(GambitKind.BatchHundred); }
            set { SetGambitCount(GambitKind.BatchHundred, value); }
        }

        public int BatchThousandGambitCount
        {
            get { return GambitCount(GambitKind.BatchThousand); }
            set { SetGambitCount(GambitKind.BatchThousand, value); }
        }

        public int BatchTenThousandGambitCount
        {
            get { return GambitCount(GambitKind.BatchTenThousand); }
            set { SetGambitCount(GambitKind.BatchTenThousand, value); }
        }

        public int CigaretteSkipGambitCount
        {
            get { return GambitCount(GambitKind.CigaretteSkip); }
            set { SetGambitCount(GambitKind.CigaretteSkip, value); }
        }

        public int AbsolutPoisoningGambitCount
        {
            get { return GambitCount(GambitKind.AbsolutPoisoning); }
            set { SetGambitCount(GambitKind.AbsolutPoisoning, value); }
        }

        public int GambitCount(GambitKind kind)
        {
            int count;
            return _gambitCounts.TryGetValue(kind, out count) ? count : 0;
        }

        public void AddGambit(GambitKind kind)
        {
            var currentCount = GambitCount(kind);
            SetGambitCount(kind, currentCount == int.MaxValue ? int.MaxValue : currentCount + 1);
        }

        public GambitItemConfig GambitConfig(GambitKind kind)
        {
            return _config.FindGambitItemConfig(kind);
        }

        private void SetGambitCount(GambitKind kind, int count)
        {
            _gambitCounts[kind] = Math.Max(0, count);
        }

        public BigInteger BaseOutputMultiplier
        {
            get { return BaseOutputMultiplierForIndex(BaseOutputMultiplierIndex); }
        }

        public static BigInteger BaseOutputMultiplierForIndex(int index)
        {
            if (index < 0) return BigInteger.One;
            if (index >= BaseOutputMultipliers.Length) return BaseOutputMultipliers[BaseOutputMultipliers.Length - 1];
            return BaseOutputMultipliers[index];
        }

        public static int MatchCountMultiplierForIndex(int index)
        {
            if (index < 0) return 1;
            if (index >= MatchCountMultipliers.Length) return MatchCountMultipliers[MatchCountMultipliers.Length - 1];
            return MatchCountMultipliers[index];
        }

        public int MatchCountMultiplier(PaylineGroup group)
        {
            return MatchCountMultiplierForIndex(MatchCountMultiplierIndex(group));
        }

        public int MatchCountMultiplierIndex(PaylineGroup group)
        {
            int index;
            return _matchCountMultiplierIndexes.TryGetValue(group, out index) ? index : 0;
        }

        public bool CanIncreaseMatchCountMultiplier(PaylineGroup group)
        {
            return MatchCountMultiplierIndex(group) < MatchCountMultipliers.Length - 1;
        }

        public int StartingRolls
        {
            get 
            {
                long baseRolls = (long)_config.BaseRolls * BaseRollMultiplier + TemporaryFreeSpins;
                baseRolls = ApplyBatchMultiplier(baseRolls, GambitKind.BatchTen, BatchTenGambitCount);
                baseRolls = ApplyBatchMultiplier(baseRolls, GambitKind.BatchHundred, BatchHundredGambitCount);
                baseRolls = ApplyBatchMultiplier(baseRolls, GambitKind.BatchThousand, BatchThousandGambitCount);
                baseRolls = ApplyBatchMultiplier(baseRolls, GambitKind.BatchTenThousand, BatchTenThousandGambitCount);
                return baseRolls >= int.MaxValue ? int.MaxValue : (int)baseRolls;
            }
        }

        private long ApplyBatchMultiplier(long currentRolls, GambitKind kind, int count)
        {
            if (count <= 0) return currentRolls;
            var config = GambitConfig(kind);
            var multiplier = config == null ? 10 : config.RollMultiplier;
            for (int i = 0; i < count; i++)
            {
                if (currentRolls > int.MaxValue / multiplier)
                {
                    return int.MaxValue;
                }
                currentRolls *= multiplier;
            }
            return currentRolls;
        }

        public int SymbolValue(SymbolKind symbol)
        {
            int val;
            return _symbolValues.TryGetValue(symbol, out val) ? val : 0;
        }

        public void ImproveSymbol(SymbolKind symbol, int delta = 0)
        {
            if (_symbolValues.ContainsKey(symbol))
            {
                _symbolValues[symbol] += delta > 0 ? delta : 1;
            }
        }

        public void DecayCigarette(int amount)
        {
            if (_symbolValues.ContainsKey(SymbolKind.Cigarette))
            {
                _symbolValues[SymbolKind.Cigarette] = Math.Max(1, _symbolValues[SymbolKind.Cigarette] - amount);
            }
        }

        public void DoubleMoneyMultiplier()
        {
            MoneyMultiplier *= 2;
        }

        public void AddFreeSpins(int amount)
        {
            var normalizedAmount = Math.Max(0, amount);
            TemporaryFreeSpins = (int)Math.Min(int.MaxValue, (long)TemporaryFreeSpins + normalizedAmount);
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

        public bool IncreaseBaseOutputMultiplier()
        {
            if (BaseOutputMultiplierIndex >= BaseOutputMultipliers.Length - 1)
            {
                return false;
            }

            BaseOutputMultiplierIndex++;
            return true;
        }

        public bool IncreaseMatchCountMultiplier(PaylineGroup group)
        {
            var currentIndex = MatchCountMultiplierIndex(group);
            if (currentIndex >= MatchCountMultipliers.Length - 1)
            {
                return false;
            }

            _matchCountMultiplierIndexes[group] = currentIndex + 1;
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

    public sealed class SymbolRunStats
    {
        internal SymbolRunStats()
        {
        }

        public int WinningPaylineCount { get; private set; }
        public BigInteger GeneratedKurus { get; private set; }

        internal void RecordWinningPayline(BigInteger payoutKurus)
        {
            RecordWinningPayline(payoutKurus, 1);
        }

        internal void RecordWinningPayline(BigInteger payoutKurus, int matchCountMultiplier)
        {
            WinningPaylineCount += Math.Max(1, matchCountMultiplier);
            GeneratedKurus += payoutKurus;
        }
    }

    public sealed class RunStats
    {
        private readonly Dictionary<SymbolKind, SymbolRunStats> _symbolStats = new Dictionary<SymbolKind, SymbolRunStats>();

        internal RunStats(int startingThresholdLevel)
        {
            HighestThresholdReached = Math.Max(1, startingThresholdLevel);
            foreach (SymbolKind symbol in Enum.GetValues(typeof(SymbolKind)))
            {
                _symbolStats.Add(symbol, new SymbolRunStats());
            }
        }

        public int RollsUsed { get; private set; }
        public int HighestThresholdReached { get; private set; }
        public BigInteger TotalEarnedKurus { get; private set; }
        public BigInteger TotalSpentKurus { get; private set; }
        public int JackpotsScored { get; private set; }
        public int TotalItemsPurchased { get; private set; }
        public IReadOnlyDictionary<SymbolKind, SymbolRunStats> SymbolStats { get { return _symbolStats; } }

        public SymbolRunStats GetSymbolStats(SymbolKind symbol)
        {
            SymbolRunStats stats;
            if (!_symbolStats.TryGetValue(symbol, out stats))
            {
                throw new ArgumentOutOfRangeException(nameof(symbol));
            }

            return stats;
        }

        internal void RecordSpin(ScoredSpin score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            RollsUsed += score.BatchFactor;
            TotalEarnedKurus += score.PayoutKurus;

            var scoredTripleKiss = false;
            foreach (var win in score.Wins)
            {
                var symbol = win.IsTripleKiss ? SymbolKind.Kiss : win.ResolvedSymbol;
                GetSymbolStats(symbol).RecordWinningPayline(win.FinalPayoutKurus, win.MatchCountMultiplier);
                scoredTripleKiss |= win.IsTripleKiss;
            }

            if (scoredTripleKiss)
            {
                JackpotsScored++;
            }
        }

        internal void RecordPurchase(BigInteger costKurus)
        {
            TotalSpentKurus += costKurus;
            TotalItemsPurchased++;
        }

        internal void RecordThresholdReached(int thresholdLevel)
        {
            HighestThresholdReached = Math.Max(HighestThresholdReached, thresholdLevel);
        }
    }

    public sealed class RunState
    {
        private readonly Dictionary<ShopOfferKind, int> _ownedUpgradeCounts = new Dictionary<ShopOfferKind, int>();

        internal RunState() : this(GameRulesConfig.CreateDefault())
        {
        }

        internal RunState(GameRulesConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            ThresholdLevel = 1;
            CashKurus = BigInteger.Zero;
            RollsRemaining = Config.BaseRolls;
            ScoringCount = 0;
            OrganLosses = 0;
            RefreshTickets = 0;
            Phase = RunPhase.Playing;
            Modifiers = new RunModifiers(Config);
            Shop = new ShopState();
            Stats = new RunStats(ThresholdLevel);
            ConsecutiveAbsolutWins = 0;
        }

        public int ConsecutiveAbsolutWins { get; set; }
        public int ThresholdLevel { get; internal set; }
        public BigInteger CashKurus { get; set; }
        public int RollsRemaining { get; set; }
        public int ScoringCount { get; private set; }
        public int OrganLosses { get; internal set; }
        public int RefreshTickets { get; internal set; }
        public void AddRefreshTickets(int amount) { RefreshTickets += amount; }
        public RunPhase Phase { get; internal set; }
        public RunModifiers Modifiers { get; private set; }
        public GameRulesConfig Config { get; private set; }
        public ShopState Shop { get; private set; }
        public RunStats Stats { get; private set; }
        public List<ShopOffer> ShopOffers { get { return Shop.Offers; } }

        internal void RegisterScoring()
        {
            if (ScoringCount < int.MaxValue)
            {
                ScoringCount++;
            }
        }

        public int OwnedUpgradeCount(ShopOfferKind kind)
        {
            int count;
            return _ownedUpgradeCounts.TryGetValue(kind, out count) ? count : 0;
        }

        public void RecordOwnedUpgrade(ShopOfferKind kind)
        {
            _ownedUpgradeCounts[kind] = OwnedUpgradeCount(kind) + 1;
        }

        internal void ClearOwnedUpgrade(ShopOfferKind kind)
        {
            _ownedUpgradeCounts.Remove(kind);
        }

        internal void AddRolls(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            RollsRemaining = (int)Math.Min(int.MaxValue, (long)RollsRemaining + amount);
        }

        internal void MultiplyRolls(int multiplier)
        {
            if (multiplier <= 0)
            {
                return;
            }

            var multipliedRolls = (long)RollsRemaining * multiplier;
            RollsRemaining = multipliedRolls >= int.MaxValue
                ? int.MaxValue
                : multipliedRolls <= int.MinValue
                    ? int.MinValue
                    : (int)multipliedRolls;
        }

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
        public PaylineWin(Payline payline, SymbolKind resolvedSymbol, BigInteger linePayoutKurus, bool tripleKiss)
            : this(payline, resolvedSymbol, linePayoutKurus, tripleKiss, 1)
        {
        }

        public PaylineWin(Payline payline, SymbolKind resolvedSymbol, BigInteger linePayoutKurus, bool tripleKiss, int matchCountMultiplier)
        {
            Payline = payline;
            ResolvedSymbol = resolvedSymbol;
            LinePayoutKurus = linePayoutKurus;
            FinalPayoutKurus = BigInteger.Zero;
            IsTripleKiss = tripleKiss;
            MatchCountMultiplier = Math.Max(1, matchCountMultiplier);
        }

        public Payline Payline { get; private set; }
        public SymbolKind ResolvedSymbol { get; private set; }
        public BigInteger LinePayoutKurus { get; private set; }
        public BigInteger FinalPayoutKurus { get; internal set; }
        public bool IsTripleKiss { get; private set; }
        public int MatchCountMultiplier { get; private set; }

        public bool IsMaxPlusWin
        {
            get
            {
                return Payline != null
                    && Payline.Positions != null
                    && Payline.Positions.Length == 3
                    && (Payline.Group == PaylineGroup.Horizontal || Payline.Group == PaylineGroup.CrissCross);
            }
        }
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

        internal void ApplyPayoutMultiplier(BigInteger multiplier)
        {
            if (multiplier < BigInteger.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            }

            PayoutKurus *= multiplier;
            foreach (var win in Wins)
            {
                if (win != null)
                {
                    win.FinalPayoutKurus *= multiplier;
                }
            }
        }

        public PaylineWin MaxPlusWin
        {
            get
            {
                PaylineWin bestWin = null;
                if (Wins == null)
                {
                    return null;
                }

                foreach (var win in Wins)
                {
                    if (win == null || !win.IsMaxPlusWin)
                    {
                        continue;
                    }

                    if (bestWin == null || win.FinalPayoutKurus > bestWin.FinalPayoutKurus)
                    {
                        bestWin = win;
                    }
                }

                return bestWin;
            }
        }

        public int RewardAnimationCount(PaylineWin win)
        {
            if (win == null)
            {
                throw new ArgumentNullException(nameof(win));
            }

            return Math.Max(1, BatchFactor) * Math.Max(1, win.MatchCountMultiplier);
        }
    }

    public sealed class SpinResult
    {
        public bool Accepted { get; internal set; }
        public string Message { get; internal set; }
        public SymbolKind[,] Grid { get; internal set; }
        public ScoredSpin Score { get; internal set; }
        public PaylineWin MaxPlusWin { get; internal set; }
        public bool MaxPlusWinTriggered { get; internal set; }
        public bool ThresholdCleared { get; internal set; }
        public bool OrganLost { get; internal set; }
        public string LostOrganName { get; internal set; }
        public BigInteger CashBeforeSpinKurus { get; internal set; }
        public BigInteger TargetBeforeSpinKurus { get; internal set; }
        public int ThresholdLevelBeforeSpin { get; internal set; }
        public bool KissLostOnLeftReel { get; internal set; }
        public bool CigaretteDecayed { get; internal set; }
        public int CarriedRollsToNextThreshold { get; internal set; }
    }

    public static class MatchResolver
    {
        public static bool TryResolveWinningLine(SymbolKind[] symbols, out SymbolKind resolvedSymbol, out bool tripleKiss)
        {
            resolvedSymbol = SymbolKind.Kiss;
            tripleKiss = true;
            var regularFound = false;

            foreach (var symbol in symbols)
            {
                if (symbol == SymbolKind.Kiss)
                {
                    continue;
                }

                if (!regularFound)
                {
                    resolvedSymbol = symbol;
                    regularFound = true;
                    tripleKiss = false;
                    continue;
                }

                if (resolvedSymbol != symbol)
                {
                    tripleKiss = false;
                    return false;
                }
            }

            return true;
        }
    }

    public static class PayoutCalculator
    {
        public static BigInteger CalculateLinePayout(SymbolKind symbol, RunModifiers modifiers, bool tripleKiss)
        {
            if (tripleKiss)
            {
                var basePayout = (GameBalance.BaseLinePayoutKurus * GameBalance.TripleKissMultiplierNumerator) / GameBalance.TripleKissMultiplierDenominator;
                return basePayout * modifiers.SymbolValue(SymbolKind.Kiss);
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

        public static BigInteger CalculateFinalPayout(BigInteger rawPayout, int comboMultiplier, BigInteger moneyMultiplier, BigInteger baseOutputMultiplier, int batchFactor)
        {
            return CalculateFinalPayout(rawPayout, comboMultiplier, moneyMultiplier, baseOutputMultiplier, batchFactor, 1);
        }

        public static BigInteger CalculateFinalPayout(
            BigInteger rawPayout,
            int comboMultiplier,
            BigInteger moneyMultiplier,
            BigInteger baseOutputMultiplier,
            int batchFactor,
            int matchCountMultiplier)
        {
            return rawPayout
                * comboMultiplier
                * moneyMultiplier
                * baseOutputMultiplier
                * batchFactor
                * Math.Max(1, matchCountMultiplier);
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

            // TrySpin accepts the named batch presets, but it may reduce a preset to the
            // exact number of rolls remaining (for example, 10x can become 8x).
            if (batchFactor < 1 || batchFactor > GameBalance.MaximumBatchFactor)
            {
                throw new ArgumentOutOfRangeException(nameof(batchFactor));
            }

            var wins = new List<PaylineWin>();

            foreach (var payline in GameBalance.Paylines)
            {
                var symbols = ReadSymbols(grid, payline);
                SymbolKind resolved;
                bool tripleKiss;

                if (MatchResolver.TryResolveWinningLine(symbols, out resolved, out tripleKiss))
                {
                    wins.Add(new PaylineWin(
                        payline,
                        resolved,
                        PayoutCalculator.CalculateLinePayout(resolved, modifiers, tripleKiss),
                        tripleKiss,
                        modifiers.MatchCountMultiplier(payline.Group)));
                }
            }

            var comboMultiplier = PayoutCalculator.ComboMultiplierFor(wins.Count);
            var finalPayout = BigInteger.Zero;
            foreach (var win in wins)
            {
                win.FinalPayoutKurus = PayoutCalculator.CalculateFinalPayout(
                    win.LinePayoutKurus,
                    comboMultiplier,
                    modifiers.MoneyMultiplier,
                    modifiers.BaseOutputMultiplier,
                    batchFactor,
                    win.MatchCountMultiplier);

                if (modifiers.CigaretteDecayGambitCount > 0 && win.ResolvedSymbol == SymbolKind.Cigarette)
                {
                    var cigaretteConfig = modifiers.GambitConfig(GambitKind.CigaretteDecay);
                    var cigarettePayoutMultiplier = cigaretteConfig == null ? 5 : cigaretteConfig.PayoutMultiplier;
                    BigInteger cigaretteMult = BigInteger.One;
                    for (int i = 0; i < modifiers.CigaretteDecayGambitCount; i++)
                    {
                        cigaretteMult *= cigarettePayoutMultiplier;
                    }
                    win.FinalPayoutKurus *= cigaretteMult;
                }

                finalPayout += win.FinalPayoutKurus;
            }

            BigInteger overallMultiplier = BigInteger.One;
            if (modifiers.Kiss1000xGambitCount > 0)
            {
                bool scoredKissOnLeftReel = false;
                foreach (var win in wins)
                {
                    foreach (var pos in win.Payline.Positions)
                    {
                        if (pos.Column == 0 && grid[pos.Row, pos.Column] == SymbolKind.Kiss)
                        {
                            scoredKissOnLeftReel = true;
                            break;
                        }
                    }
                    if (scoredKissOnLeftReel) break;
                }

                if (scoredKissOnLeftReel)
                {
                    var kissConfig = modifiers.GambitConfig(GambitKind.Kiss1000x);
                    var kissPayoutMultiplier = kissConfig == null ? 1000 : kissConfig.PayoutMultiplier;
                    for (int i = 0; i < modifiers.Kiss1000xGambitCount; i++)
                    {
                        overallMultiplier *= kissPayoutMultiplier;
                    }
                }
            }

            if (modifiers.AbsolutGambitCount > 0)
            {
                bool hasAbsolutWin = false;
                foreach (var win in wins)
                {
                    if (win.ResolvedSymbol == SymbolKind.Absolut)
                    {
                        hasAbsolutWin = true;
                        break;
                    }
                }

                if (hasAbsolutWin)
                {
                    var absolutPositions = new HashSet<GridPosition>();
                    foreach (var win in wins)
                    {
                        if (win.ResolvedSymbol == SymbolKind.Absolut)
                        {
                            foreach (var pos in win.Payline.Positions)
                            {
                                if (grid[pos.Row, pos.Column] == SymbolKind.Absolut)
                                {
                                    absolutPositions.Add(pos);
                                }
                            }
                        }
                    }

                    int count = absolutPositions.Count;
                    if (count > 0)
                    {
                        var absolutConfig = modifiers.GambitConfig(GambitKind.Absolut);
                        var absolutPayoutMultiplier = absolutConfig == null ? 10 : absolutConfig.PayoutMultiplier;
                        BigInteger absolutMult = BigInteger.One;
                        for (int i = 0; i < modifiers.AbsolutGambitCount; i++)
                        {
                            absolutMult *= (absolutPayoutMultiplier * count);
                        }
                        overallMultiplier *= absolutMult;
                    }
                }
                else
                {
                    int count = 0;
                    for (int row = 0; row < GameBalance.GridRows; row++)
                    {
                        for (int col = 0; col < GameBalance.GridColumns; col++)
                        {
                            if (grid[row, col] == SymbolKind.Absolut)
                            {
                                count++;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        var absolutConfig = modifiers.GambitConfig(GambitKind.Absolut);
                        var sacrificePercent = absolutConfig == null ? 25 : absolutConfig.SacrificePercent;
                        double multiplier = 1.0;
                        for (int i = 0; i < modifiers.AbsolutGambitCount; i++)
                        {
                            multiplier *= Math.Max(0.0, 1.0 - (sacrificePercent / 100.0) * count);
                        }

                        finalPayout = (finalPayout * (long)(multiplier * 10000)) / 10000;
                        foreach (var win in wins)
                        {
                            win.FinalPayoutKurus = (win.FinalPayoutKurus * (long)(multiplier * 10000)) / 10000;
                        }
                        overallMultiplier = BigInteger.Zero; // Already applied deduction
                    }
                }
            }

            if (overallMultiplier > 1)
            {
                finalPayout *= overallMultiplier;
                foreach (var win in wins)
                {
                    win.FinalPayoutKurus *= overallMultiplier;
                }
            }

            return new ScoredSpin(wins, finalPayout, comboMultiplier, batchFactor);
        }

        public static int ComboMultiplierFor(int winCount)
        {
            return PayoutCalculator.ComboMultiplierFor(winCount);
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
            var state = new RunState(_config.CreateRunCopy());
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

            if (!GameBalance.IsSupportedBatchFactor(batchFactor))
            {
                return RejectedResult("Batch must be 1x, 5x, 10x, 100x, 1000x, or 10000x.");
            }

            if (state.RollsRemaining < batchFactor)
            {
                batchFactor = state.RollsRemaining;
            }

            if (batchFactor <= 0)
            {
                return RejectedResult("Not enough rolls for that batch.");
            }

            var cashBeforeSpin = state.CashKurus;
            var targetBeforeSpin = state.CurrentTargetKurus;
            var thresholdLevelBeforeSpin = state.ThresholdLevel;

            state.RollsRemaining -= batchFactor;
            var grid = CreateGrid(state.Config);
            var score = SlotScoring.Evaluate(grid, state.Modifiers, batchFactor);

            var isScoring = score.Wins != null && score.Wins.Count > 0;
            if (isScoring)
            {
                state.RegisterScoring();
            }

            var maxPlusWinTriggered = isScoring
                && state.ScoringCount % GameBalance.MaxPlusWinScoringInterval == 0
                && score.MaxPlusWin != null
                && _random.Next(GameBalance.MaxPlusWinChanceDivisor) == 0;
            if (maxPlusWinTriggered)
            {
                // Max Plus is a bonus to the complete scoring result, not just its highlighted line.
                score.ApplyPayoutMultiplier(new BigInteger(GameBalance.MaxPlusWinPayoutMultiplier));
            }

            bool hasAbsolutWin = false;
            foreach (var win in score.Wins)
            {
                if (win.ResolvedSymbol == SymbolKind.Absolut)
                {
                    hasAbsolutWin = true;
                    break;
                }
            }

            bool alcoholPoisoningTriggered = false;
            if (state.Modifiers.AbsolutPoisoningGambitCount > 0)
            {
                if (hasAbsolutWin)
                {
                    score.ApplyPayoutMultiplier(10);
                    state.ConsecutiveAbsolutWins++;
                }
                else
                {
                    state.ConsecutiveAbsolutWins = 0;
                }

                if (state.ConsecutiveAbsolutWins >= 3)
                {
                    alcoholPoisoningTriggered = true;
                    state.ConsecutiveAbsolutWins = 0;
                }
            }

            state.Stats.RecordSpin(score);
            state.CashKurus += score.PayoutKurus;

            if (alcoholPoisoningTriggered)
            {
                state.CashKurus = BigInteger.Zero;
            }

            var result = new SpinResult
            {
                Accepted = true,
                Message = alcoholPoisoningTriggered
                    ? "Alcohol Poisoning! Lose all cash to hospital bills."
                    : (score.Wins.Count == 0 ? "No matching paylines." : "Winning paylines resolved."),
                Grid = grid,
                Score = score,
                MaxPlusWin = maxPlusWinTriggered ? score.MaxPlusWin : null,
                MaxPlusWinTriggered = maxPlusWinTriggered,
                CashBeforeSpinKurus = cashBeforeSpin,
                TargetBeforeSpinKurus = targetBeforeSpin,
                ThresholdLevelBeforeSpin = thresholdLevelBeforeSpin
            };

            // Process Left Reel Kiss Loss Chance
            if (state.Modifiers.Kiss1000xGambitCount > 0)
            {
                var kissConfig = state.Modifiers.GambitConfig(GambitKind.Kiss1000x);
                var kissRiskPercent = kissConfig == null ? 15 : kissConfig.RiskPercent;
                bool scoredKissOnLeftReel = false;
                foreach (var win in score.Wins)
                {
                    foreach (var pos in win.Payline.Positions)
                    {
                        if (pos.Column == 0 && grid[pos.Row, pos.Column] == SymbolKind.Kiss)
                        {
                            scoredKissOnLeftReel = true;
                            break;
                        }
                    }
                    if (scoredKissOnLeftReel) break;
                }

                if (scoredKissOnLeftReel)
                {
                    int kissesLost = 0;
                    for (int i = 0; i < state.Modifiers.Kiss1000xGambitCount; i++)
                    {
                        if (_random.Next(100) < kissRiskPercent)
                        {
                            kissesLost++;
                        }
                    }

                    if (kissesLost > 0)
                    {
                        var leftReel = state.Config.ReelStripAt(0);
                        var kissIndices = new List<int>();
                        for (int i = 0; i < leftReel.Length; i++)
                        {
                            if (leftReel[i] == SymbolKind.Kiss)
                            {
                                kissIndices.Add(i);
                            }
                        }

                        int actualReplaced = 0;
                        for (int k = 0; k < kissesLost && kissIndices.Count > 0; k++)
                        {
                            int rIdx = _random.Next(kissIndices.Count);
                            int stripIdx = kissIndices[rIdx];
                            kissIndices.RemoveAt(rIdx);

                            SymbolKind[] regularSymbols = { SymbolKind.Absolut, SymbolKind.Dollar, SymbolKind.Cat, SymbolKind.Cigarette };
                            SymbolKind replacement = regularSymbols[_random.Next(regularSymbols.Length)];

                            state.Config.ReplaceSymbolOnStrip(0, stripIdx, replacement);
                            actualReplaced++;
                        }

                        if (actualReplaced > 0)
                        {
                            result.KissLostOnLeftReel = true;
                            result.Message += $" (Kiss lost on left reel!)";
                        }
                    }
                }
            }

            // Process Cigarette Decay Absence Check
            if (state.Modifiers.CigaretteDecayGambitCount > 0)
            {
                var cigaretteConfig = state.Modifiers.GambitConfig(GambitKind.CigaretteDecay);
                var cigaretteDecayPerMiss = cigaretteConfig == null ? 1 : cigaretteConfig.DecayPerMiss;
                bool hasCigarette = false;
                for (int r = 0; r < GameBalance.GridRows; r++)
                {
                    for (int c = 0; c < GameBalance.GridColumns; c++)
                    {
                        if (grid[r, c] == SymbolKind.Cigarette)
                        {
                            hasCigarette = true;
                            break;
                        }
                    }
                    if (hasCigarette) break;
                }

                if (!hasCigarette)
                {
                    state.Modifiers.DecayCigarette(cigaretteDecayPerMiss * state.Modifiers.CigaretteDecayGambitCount);
                    result.CigaretteDecayed = true;
                    result.Message += $" (Cigarette decayed to {state.Modifiers.SymbolValue(SymbolKind.Cigarette)}x!)";
                }
            }

            bool cigaretteSkipTriggered = false;
            if (state.Modifiers.CigaretteSkipGambitCount > 0)
            {
                int cigaretteWins = 0;
                foreach (var win in score.Wins)
                {
                    if (win.ResolvedSymbol == SymbolKind.Cigarette)
                    {
                        cigaretteWins++;
                    }
                }

                if (cigaretteWins >= 3)
                {
                    state.CashKurus += state.CurrentTargetKurus;
                    cigaretteSkipTriggered = true;
                }
            }

            if (TrySettleThreshold(state))
            {
                result.ThresholdCleared = true;
                result.CarriedRollsToNextThreshold = state.Phase == RunPhase.Playing
                    ? Math.Max(0, state.RollsRemaining - state.Modifiers.StartingRolls)
                    : 0;
                if (cigaretteSkipTriggered)
                {
                    result.Message = "Cigarette Skip! Next threshold reached and Kurus granted!";
                }
                else
                {
                    result.Message = state.Phase == RunPhase.Victory ? "All ten thresholds cleared!" : "Threshold paid. Serenay has new offers.";
                }
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
            state.Modifiers.ClearThresholdFreeSpins();
            state.ClearOwnedUpgrade(ShopOfferKind.FreeSpins);
            if (state.ThresholdLevel >= state.Config.ThresholdCount)
            {
                state.Stats.RecordThresholdReached(state.ThresholdLevel);
                state.Phase = RunPhase.Victory;
                return true;
            }

            state.ThresholdLevel++;
            state.Stats.RecordThresholdReached(state.ThresholdLevel);
            // A threshold can now be cleared before every roll is spent. Preserve those
            // unused rolls and add the normal roll allotment for the new threshold.
            state.AddRolls(state.Modifiers.StartingRolls);
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
                case ShopOfferKind.AbsolutValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Absolut, state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.SymbolImprovementDelta ?? 0);
                    break;
                case ShopOfferKind.DollarValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Dollar, state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.SymbolImprovementDelta ?? 0);
                    break;
                case ShopOfferKind.CatValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Cat, state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.SymbolImprovementDelta ?? 0);
                    break;
                case ShopOfferKind.CigaretteValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Cigarette, state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.SymbolImprovementDelta ?? 0);
                    break;
                case ShopOfferKind.KissValue:
                    state.Modifiers.ImproveSymbol(SymbolKind.Kiss, state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.SymbolImprovementDelta ?? 0);
                    break;
                case ShopOfferKind.MoneyMultiplier:
                    state.Modifiers.DoubleMoneyMultiplier();
                    break;
                case ShopOfferKind.FreeSpins:
                    state.Modifiers.AddFreeSpins(state.Config.FreeSpinBundle);
                    state.AddRolls(state.Config.FreeSpinBundle);
                    break;
                case ShopOfferKind.BaseRollMultiplierX2:
                    {
                        var mult = state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.BaseRollMultiplierValue ?? 0;
                        state.Modifiers.SetBaseRollMultiplier(mult > 0 ? mult : 2);
                        state.AddRolls(state.Modifiers.StartingRolls - rollsBeforeUpgrade);
                    }
                    break;
                case ShopOfferKind.BaseRollMultiplierX10:
                    {
                        var mult = state.Config.FindShopItemConfig(offer.Kind, state.ThresholdLevel)?.BaseRollMultiplierValue ?? 0;
                        state.Modifiers.SetBaseRollMultiplier(mult > 0 ? mult : 10);
                        state.AddRolls(state.Modifiers.StartingRolls - rollsBeforeUpgrade);
                    }
                    break;
                case ShopOfferKind.BaseOutputMultiplier:
                    state.Modifiers.IncreaseBaseOutputMultiplier();
                    break;
                case ShopOfferKind.HorizontalMatchMultiplier:
                    state.Modifiers.IncreaseMatchCountMultiplier(PaylineGroup.Horizontal);
                    break;
                case ShopOfferKind.VerticalMatchMultiplier:
                    state.Modifiers.IncreaseMatchCountMultiplier(PaylineGroup.Vertical);
                    break;
                case ShopOfferKind.CrissCrossMatchMultiplier:
                    state.Modifiers.IncreaseMatchCountMultiplier(PaylineGroup.CrissCross);
                    break;
            }

            state.Stats.RecordPurchase(offer.CostKurus);
            offer.Purchased = true;
            state.RecordOwnedUpgrade(offer.Kind);
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

        private SymbolKind[,] CreateGrid(GameRulesConfig config)
        {
            if (_boardFactory != null)
            {
                return _boardFactory();
            }

            var grid = new SymbolKind[GameBalance.GridRows, GameBalance.GridColumns];
            for (var column = 0; column < GameBalance.GridColumns; column++)
            {
                var strip = config.ReelStripAt(column);
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


        private ShopOfferKind PickWeightedRandom(List<ShopOfferKind> candidates, RunState state)
        {
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("No candidates to pick from.");
            }

            int totalWeight = 0;
            List<int> weights = new List<int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                int weight = state.OwnedUpgradeCount(candidates[i]) > 0 ? 2 : 1;
                totalWeight += weight;
                weights.Add(weight);
            }

            int r = _random.Next(totalWeight);
            int cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (r < cumulative)
                {
                    var picked = candidates[i];
                    candidates.RemoveAt(i);
                    return picked;
                }
            }

            var last = candidates[candidates.Count - 1];
            candidates.RemoveAt(candidates.Count - 1);
            return last;
        }

        private void GenerateShop(RunState state, bool rerollUnsoldOnly = false)
        {
            var allCandidates = new List<ShopOfferKind>
            {
                ShopOfferKind.AbsolutValue,
                ShopOfferKind.DollarValue,
                ShopOfferKind.CatValue,
                ShopOfferKind.CigaretteValue,
                ShopOfferKind.KissValue,
                ShopOfferKind.MoneyMultiplier,
                ShopOfferKind.FreeSpins
            };

            if (state.Modifiers.BaseRollMultiplier < 2)
            {
                allCandidates.Add(ShopOfferKind.BaseRollMultiplierX2);
            }
            else if (state.Modifiers.BaseRollMultiplier < 10)
            {
                allCandidates.Add(ShopOfferKind.BaseRollMultiplierX10);
            }

            if (state.Modifiers.BaseOutputMultiplierIndex < 9)
            {
                allCandidates.Add(ShopOfferKind.BaseOutputMultiplier);
            }

            if (state.Modifiers.CanIncreaseMatchCountMultiplier(PaylineGroup.Horizontal))
            {
                allCandidates.Add(ShopOfferKind.HorizontalMatchMultiplier);
            }

            if (state.Modifiers.CanIncreaseMatchCountMultiplier(PaylineGroup.Vertical))
            {
                allCandidates.Add(ShopOfferKind.VerticalMatchMultiplier);
            }

            if (state.Modifiers.CanIncreaseMatchCountMultiplier(PaylineGroup.CrissCross))
            {
                allCandidates.Add(ShopOfferKind.CrissCrossMatchMultiplier);
            }

            var options = new List<ShopOfferKind>();
            foreach (var kind in allCandidates)
            {
                var itemConfig = state.Config.FindShopItemConfig(kind, state.ThresholdLevel);
                if (itemConfig != null)
                {
                    if (itemConfig.DisplayThreshold == ThresholdLevel.Any || (int)itemConfig.DisplayThreshold == state.ThresholdLevel)
                    {
                        options.Add(kind);
                    }
                }
                else
                {
                    if (!state.Config.HasAnyShopItemConfig(kind))
                    {
                        options.Add(kind);
                    }
                }
            }

            if (!rerollUnsoldOnly)
            {
                state.ShopOffers.Clear();
                while (state.ShopOffers.Count < 3 && options.Count > 0)
                {
                    var kind = PickWeightedRandom(options, state);
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

                var kind = PickWeightedRandom(options, state);
                state.ShopOffers[offerIndex] = CreateOffer(state, kind);
            }
        }

        private ShopOffer CreateOffer(RunState state, ShopOfferKind kind)
        {
            var target = state.CurrentTargetKurus;
            var authoredConfig = state.Config.FindShopItemConfig(kind, state.ThresholdLevel);
            var costDivisor = authoredConfig != null ? authoredConfig.CostDivisor : 0;
            var cost = costDivisor > 0 ? target / costDivisor : target / 20;

            string title;
            string description;
            PaylineGroup matchGroup;
            var isMatchCountOffer = TryGetPaylineGroupForOffer(kind, out matchGroup);
            var nextMatchCountMultiplier = isMatchCountOffer
                ? RunModifiers.MatchCountMultiplierForIndex(state.Modifiers.MatchCountMultiplierIndex(matchGroup) + 1)
                : 1;

            switch (kind)
            {
                case ShopOfferKind.AbsolutValue:
                    var absolutDelta = authoredConfig != null && authoredConfig.SymbolImprovementDelta > 0 ? authoredConfig.SymbolImprovementDelta : 1;
                    title = "Absolut Value +" + absolutDelta + "x";
                    description = "Permanently raises Absolut line value.";
                    if (costDivisor <= 0) cost = target / 25;
                    break;
                case ShopOfferKind.DollarValue:
                    var dollarDelta = authoredConfig != null && authoredConfig.SymbolImprovementDelta > 0 ? authoredConfig.SymbolImprovementDelta : 1;
                    title = "Dollar Value +" + dollarDelta + "x";
                    description = "Permanently raises Dollar line value.";
                    if (costDivisor <= 0) cost = target / 16;
                    break;
                case ShopOfferKind.CatValue:
                    var catDelta = authoredConfig != null && authoredConfig.SymbolImprovementDelta > 0 ? authoredConfig.SymbolImprovementDelta : 1;
                    title = "Cat Value +" + catDelta + "x";
                    description = "Permanently raises Cat line value.";
                    if (costDivisor <= 0) cost = target / 10;
                    break;
                case ShopOfferKind.CigaretteValue:
                    var cigaretteDelta = authoredConfig != null && authoredConfig.SymbolImprovementDelta > 0 ? authoredConfig.SymbolImprovementDelta : 1;
                    title = "Cigarette Value +" + cigaretteDelta + "x";
                    description = "Permanently raises Cigarette line value.";
                    if (costDivisor <= 0) cost = target / 8;
                    break;
                case ShopOfferKind.KissValue:
                    var kissDelta = authoredConfig != null && authoredConfig.SymbolImprovementDelta > 0 ? authoredConfig.SymbolImprovementDelta : 1;
                    title = "Kiss Value +" + kissDelta + "x";
                    description = "Permanently raises Kiss line value.";
                    if (costDivisor <= 0) cost = target / 12;
                    break;
                case ShopOfferKind.MoneyMultiplier:
                    title = "Money Output x2";
                    description = "Doubles all future final TL payouts.";
                    if (costDivisor <= 0) cost = target / 10;
                    break;
                case ShopOfferKind.FreeSpins:
                    title = "+20 Free Spins";
                    description = "Adds rolls until this threshold is cleared.";
                    if (costDivisor <= 0) cost = target / 20;
                    break;
                case ShopOfferKind.BaseRollMultiplierX2:
                    var rollMult2 = authoredConfig != null && authoredConfig.BaseRollMultiplierValue > 0 ? authoredConfig.BaseRollMultiplierValue : 2;
                    title = "Base Rolls x" + rollMult2;
                    description = "Raises the ten-roll base to " + (10 * rollMult2) + " for this run.";
                    if (costDivisor <= 0) cost = target / 5;
                    break;
                case ShopOfferKind.BaseRollMultiplierX10:
                    var rollMult10 = authoredConfig != null && authoredConfig.BaseRollMultiplierValue > 0 ? authoredConfig.BaseRollMultiplierValue : 10;
                    title = "Base Rolls x" + rollMult10;
                    description = "Raises the ten-roll base to " + (10 * rollMult10) + " for this run.";
                    if (costDivisor <= 0) cost = target / 2;
                    break;
                case ShopOfferKind.BaseOutputMultiplier:
                    {
                        int nextIndex = state.Modifiers.BaseOutputMultiplierIndex + 1;
                        var nextMultiplier = RunModifiers.BaseOutputMultiplierForIndex(nextIndex);
                        title = "Output Mult. x" + nextMultiplier;
                        description = "Multiplies all payouts by " + nextMultiplier + " for this run.";
                        if (costDivisor <= 0) cost = (target / 20) * nextIndex;
                    }
                    break;
                case ShopOfferKind.HorizontalMatchMultiplier:
                case ShopOfferKind.VerticalMatchMultiplier:
                case ShopOfferKind.CrissCrossMatchMultiplier:
                    title = PaylineGroupDisplayName(matchGroup) + " Match Echo x" + nextMatchCountMultiplier;
                    description = "Counts every " + PaylineGroupDisplayName(matchGroup).ToLowerInvariant()
                        + " match x" + nextMatchCountMultiplier + ". Reward pulses echo at the same count and accelerate for bigger bursts.";
                    if (costDivisor <= 0) cost = MatchCountUpgradeCost(target, nextMatchCountMultiplier);
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

            var authoredText = state.Config.FindShopItemConfig(kind, state.ThresholdLevel);
            if (authoredText != null)
            {
                if (!string.IsNullOrEmpty(authoredText.DisplayName))
                {
                    if (kind == ShopOfferKind.BaseOutputMultiplier)
                    {
                        title = authoredText.DisplayName + " x" + RunModifiers.BaseOutputMultiplierForIndex(state.Modifiers.BaseOutputMultiplierIndex + 1);
                    }
                    else if (isMatchCountOffer)
                    {
                        title = authoredText.DisplayName + " x" + nextMatchCountMultiplier;
                    }
                    else
                    {
                        title = authoredText.DisplayName;
                    }
                }

                if (!string.IsNullOrEmpty(authoredText.Description))
                {
                    if (kind == ShopOfferKind.BaseOutputMultiplier)
                    {
                        description = authoredText.Description + " (x" + RunModifiers.BaseOutputMultiplierForIndex(state.Modifiers.BaseOutputMultiplierIndex + 1) + ")";
                    }
                    else if (isMatchCountOffer)
                    {
                        description = authoredText.Description + " (x" + nextMatchCountMultiplier + ")";
                    }
                    else
                    {
                        description = authoredText.Description;
                    }
                }
            }

            return new ShopOffer(kind, cost, title, description);
        }

        private static BigInteger MatchCountUpgradeCost(BigInteger target, int multiplier)
        {
            switch (multiplier)
            {
                case 2: return target / 16;
                case 5: return target / 8;
                case 10: return target / 4;
                case 100: return target / 2;
                default: return target / 20;
            }
        }

        private static bool TryGetPaylineGroupForOffer(ShopOfferKind kind, out PaylineGroup group)
        {
            switch (kind)
            {
                case ShopOfferKind.HorizontalMatchMultiplier:
                    group = PaylineGroup.Horizontal;
                    return true;
                case ShopOfferKind.VerticalMatchMultiplier:
                    group = PaylineGroup.Vertical;
                    return true;
                case ShopOfferKind.CrissCrossMatchMultiplier:
                    group = PaylineGroup.CrissCross;
                    return true;
                default:
                    group = PaylineGroup.Horizontal;
                    return false;
            }
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
            return (negative ? "-" : string.Empty) + GroupDigits(whole.ToString()) + "." + fraction.ToString("D2") + " TL";
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
