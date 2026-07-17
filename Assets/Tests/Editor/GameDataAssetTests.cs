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
            Assert.That(config.FindShopItemText(ShopOfferKind.MoneyMultiplier).DisplayName, Is.EqualTo(moneyMultiplier.DisplayName));
            Assert.That(config.FindShopItemText(ShopOfferKind.MoneyMultiplier).Description, Is.EqualTo(moneyMultiplier.Description));
        }
    }
}
