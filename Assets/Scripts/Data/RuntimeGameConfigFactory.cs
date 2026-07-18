using System;
using System.Collections.Generic;

namespace SerenaysGambit
{
    // Bridges authored Unity assets to the Unity-independent game simulation.
    // Invalid or missing optional data safely falls back to the vertical-slice defaults.
    public static class RuntimeGameConfigFactory
    {
        public static GameRulesConfig Create(
            SymbolDefinition[] symbols,
            ReelDefinition[] reelDefinitions,
            ShopItemDefinition[] shopItems,
            BalanceDefinition balance)
        {
            var defaults = GameRulesConfig.CreateDefault();
            var shopConfigs = CreateShopConfigs(shopItems);

            var customStartingValues = new Dictionary<SymbolKind, int>();
            if (symbols != null)
            {
                foreach (var def in symbols)
                {
                    if (def != null)
                    {
                        customStartingValues[def.Symbol] = def.StartingValue;
                    }
                }
            }

            return new GameRulesConfig(
                ExtractReelStrips(reelDefinitions, defaults),
                FindSymbolValue(symbols, SymbolKind.Strawberry, defaults.StrawberryStartingValue),
                FindSymbolValue(symbols, SymbolKind.Cherry, defaults.CherryStartingValue),
                PositiveOrDefault(balance == null ? 0 : balance.BaseRolls, defaults.BaseRolls),
                PositiveOrDefault(balance == null ? 0 : balance.OrganCount, defaults.OrganCount),
                PositiveOrDefault(balance == null ? 0 : balance.ThresholdCount, defaults.ThresholdCount),
                NonNegativeOrDefault(balance == null ? -1 : balance.FreeSpinBundle, defaults.FreeSpinBundle),
                shopConfigs,
                customStartingValues);
        }

        private static SymbolKind[][] ExtractReelStrips(ReelDefinition[] definitions, GameRulesConfig defaults)
        {
            var validReels = new List<ReelDefinition>();
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    if (definition != null && definition.Faces != null && definition.Faces.Length == GameBalance.ReelLength)
                    {
                        validReels.Add(definition);
                    }
                }
            }

            if (validReels.Count != GameBalance.GridColumns)
            {
                return DefaultReelStrips(defaults);
            }

            validReels.Sort(CompareByName);
            var strips = new SymbolKind[GameBalance.GridColumns][];
            for (var index = 0; index < strips.Length; index++)
            {
                strips[index] = (SymbolKind[])validReels[index].Faces.Clone();
            }

            return strips;
        }

        private static int CompareByName(ReelDefinition left, ReelDefinition right)
        {
            return string.CompareOrdinal(left.name, right.name);
        }

        private static SymbolKind[][] DefaultReelStrips(GameRulesConfig defaults)
        {
            var strips = new SymbolKind[GameBalance.GridColumns][];
            for (var index = 0; index < strips.Length; index++)
            {
                strips[index] = (SymbolKind[])defaults.ReelStripAt(index).Clone();
            }

            return strips;
        }

        private static int FindSymbolValue(SymbolDefinition[] definitions, SymbolKind kind, int fallback)
        {
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    if (definition != null && definition.Symbol == kind && definition.StartingValue > 0)
                    {
                        return definition.StartingValue;
                    }
                }
            }

            return fallback;
        }

        private static Dictionary<ShopOfferKind, ShopItemConfig> CreateShopConfigs(ShopItemDefinition[] definitions)
        {
            var configs = new Dictionary<ShopOfferKind, ShopItemConfig>();
            if (definitions == null)
            {
                return configs;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || !Enum.IsDefined(typeof(ShopOfferKind), definition.Kind))
                {
                    continue;
                }

                configs[definition.Kind] = new ShopItemConfig(
                    definition.DisplayName, 
                    definition.Description,
                    definition.SymbolImprovementDelta,
                    definition.BaseRollMultiplierValue,
                    definition.CostDivisor);
            }

            return configs;
        }

        private static int PositiveOrDefault(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static int NonNegativeOrDefault(int value, int fallback)
        {
            return value >= 0 ? value : fallback;
        }
    }
}
