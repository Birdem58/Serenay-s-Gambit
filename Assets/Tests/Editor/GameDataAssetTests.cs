using NUnit.Framework;
using UnityEditor;

namespace SerenaysGambit.Tests
{
    public sealed class GameDataAssetTests
    {
        [Test]
        public void DefaultDataAssetsMatchTheInitialSliceConfiguration()
        {
            var strawberry = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Strawberry.asset");
            var reel1 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel1.asset");
            var moneyMultiplier = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/MoneyMultiplier.asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceDefinition>("Assets/Resources/SerenaysGambit/Data/Balance/DefaultBalance.asset");

            Assert.That(strawberry, Is.Not.Null);
            Assert.That(strawberry.Symbol, Is.EqualTo(SymbolKind.Strawberry));
            Assert.That(strawberry.StartingValue, Is.EqualTo(1));
            Assert.That(reel1, Is.Not.Null);
            Assert.That(reel1.Faces, Is.EqualTo(GameBalance.InitialReels[0]));
            Assert.That(moneyMultiplier, Is.Not.Null);
            Assert.That(moneyMultiplier.Kind, Is.EqualTo(ShopOfferKind.MoneyMultiplier));
            Assert.That(balance, Is.Not.Null);
            Assert.That(balance.BaseRolls, Is.EqualTo(GameBalance.BaseRolls));
            Assert.That(balance.OrganCount, Is.EqualTo(GameBalance.OrganCount));
            Assert.That(balance.ThresholdCount, Is.EqualTo(GameBalance.MaxThresholdLevel));
        }

        [Test]
        public void DefaultAuthoredAssetsBuildTheRuntimeRulesSnapshot()
        {
            var strawberry = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Strawberry.asset");
            var cherry = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Cherry.asset");
            var reel1 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel1.asset");
            var reel2 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel2.asset");
            var reel3 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel3.asset");
            var moneyMultiplier = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/MoneyMultiplier.asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceDefinition>("Assets/Resources/SerenaysGambit/Data/Balance/DefaultBalance.asset");

            var config = RuntimeGameConfigFactory.Create(
                new[] { strawberry, cherry },
                new[] { reel1, reel2, reel3 },
                new[] { moneyMultiplier },
                balance);

            Assert.That(config.StrawberryStartingValue, Is.EqualTo(strawberry.StartingValue));
            Assert.That(config.CherryStartingValue, Is.EqualTo(cherry.StartingValue));
            Assert.That(config.ReelStripAt(0), Is.EqualTo(reel1.Faces));
            Assert.That(config.ReelStripAt(1), Is.EqualTo(reel2.Faces));
            Assert.That(config.ReelStripAt(2), Is.EqualTo(reel3.Faces));
            Assert.That(config.BaseRolls, Is.EqualTo(balance.BaseRolls));
            Assert.That(config.OrganCount, Is.EqualTo(balance.OrganCount));
            Assert.That(config.ThresholdCount, Is.EqualTo(balance.ThresholdCount));
            Assert.That(config.FreeSpinBundle, Is.EqualTo(balance.FreeSpinBundle));
            Assert.That(config.FindShopItemConfig(ShopOfferKind.MoneyMultiplier).DisplayName, Is.EqualTo(moneyMultiplier.DisplayName));
            Assert.That(config.FindShopItemConfig(ShopOfferKind.MoneyMultiplier).Description, Is.EqualTo(moneyMultiplier.Description));
        }

        [Test]
        public void DirectionalMatchUpgradeAssetsAreAvailableToTheRuntimeShop()
        {
            var horizontal = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/HorizontalMatchMultiplier.asset");
            var vertical = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/VerticalMatchMultiplier.asset");
            var crissCross = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/CrissCrossMatchMultiplier.asset");

            Assert.That(horizontal, Is.Not.Null);
            Assert.That(horizontal.Kind, Is.EqualTo(ShopOfferKind.HorizontalMatchMultiplier));
            Assert.That(vertical, Is.Not.Null);
            Assert.That(vertical.Kind, Is.EqualTo(ShopOfferKind.VerticalMatchMultiplier));
            Assert.That(crissCross, Is.Not.Null);
            Assert.That(crissCross.Kind, Is.EqualTo(ShopOfferKind.CrissCrossMatchMultiplier));
        }

        [Test]
        public void GambitItemAssetsExposeEveryThresholdChoice()
        {
            var strawberry = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/StrawberryGambit.asset");
            var batchTen = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/BatchTenGambit.asset");
            var joker = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/Joker1000xGambit.asset");
            var apple = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/AppleDecayGambit.asset");

            Assert.That(strawberry, Is.Not.Null);
            Assert.That(strawberry.Kind, Is.EqualTo(GambitKind.Strawberry));
            Assert.That(strawberry.PayoutMultiplier, Is.EqualTo(10));
            Assert.That(strawberry.SacrificePercent, Is.EqualTo(25));
            Assert.That(batchTen, Is.Not.Null);
            Assert.That(batchTen.Kind, Is.EqualTo(GambitKind.BatchTen));
            Assert.That(batchTen.RollMultiplier, Is.EqualTo(10));
            Assert.That(joker, Is.Not.Null);
            Assert.That(joker.Kind, Is.EqualTo(GambitKind.Joker1000x));
            Assert.That(joker.PayoutMultiplier, Is.EqualTo(1000));
            Assert.That(joker.RiskPercent, Is.EqualTo(15));
            Assert.That(apple, Is.Not.Null);
            Assert.That(apple.Kind, Is.EqualTo(GambitKind.AppleDecay));
            Assert.That(apple.PayoutMultiplier, Is.EqualTo(5));
            Assert.That(apple.DecayPerMiss, Is.EqualTo(1));
        }

        [Test]
        public void AuthoredGambitValuesBuildTheRuntimeConfig()
        {
            var strawberry = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/StrawberryGambit.asset");
            var batchTen = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/BatchTenGambit.asset");
            var joker = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/Joker1000xGambit.asset");
            var apple = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/AppleDecayGambit.asset");

            var config = RuntimeGameConfigFactory.Create(
                null,
                null,
                null,
                null,
                new[] { strawberry, batchTen, joker, apple });

            Assert.That(config.FindGambitItemConfig(GambitKind.Strawberry).PayoutMultiplier, Is.EqualTo(strawberry.PayoutMultiplier));
            Assert.That(config.FindGambitItemConfig(GambitKind.BatchTen).RollMultiplier, Is.EqualTo(batchTen.RollMultiplier));
            Assert.That(config.FindGambitItemConfig(GambitKind.Joker1000x).RiskPercent, Is.EqualTo(joker.RiskPercent));
            Assert.That(config.FindGambitItemConfig(GambitKind.AppleDecay).DecayPerMiss, Is.EqualTo(apple.DecayPerMiss));
        }
    }
}
