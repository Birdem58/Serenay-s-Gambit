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
            var shopTexts = CreateShopTexts(shopItems);

            return new GameRulesConfig(
                ExtractReelStrips(reelDefinitions, defaults),
                FindSymbolValue(symbols, SymbolKind.Strawberry, defaults.StrawberryStartingValue),
                FindSymbolValue(symbols, SymbolKind.Cherry, defaults.CherryStartingValue),
                PositiveOrDefault(balance == null ? 0 : balance.BaseRolls, defaults.BaseRolls),
                PositiveOrDefault(balance == null ? 0 : balance.OrganCount, defaults.OrganCount),
                PositiveOrDefault(balance == null ? 0 : balance.ThresholdCount, defaults.ThresholdCount),
                NonNegativeOrDefault(balance == null ? -1 : balance.FreeSpinBundle, defaults.FreeSpinBundle),
                PositiveOrDefault(balance == null ? 0 : balance.MaxMagnetTier, defaults.MaxMagnetTier),
                shopTexts);
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

        private static Dictionary<ShopOfferKind, ShopItemText> CreateShopTexts(ShopItemDefinition[] definitions)
        {
            var texts = new Dictionary<ShopOfferKind, ShopItemText>();
            if (definitions == null)
            {
                return texts;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || !Enum.IsDefined(typeof(ShopOfferKind), definition.Kind))
                {
                    continue;
                }

                texts[definition.Kind] = new ShopItemText(definition.DisplayName, definition.Description);
            }

            return texts;
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
