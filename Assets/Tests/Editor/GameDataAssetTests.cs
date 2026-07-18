using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SerenaysGambit.Tests
{
    public sealed class GameDataAssetTests
    {
        [Test]
        public void DefaultDataAssetsMatchTheInitialSliceConfiguration()
        {
            var absolut = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Absolut.asset");
            var reel1 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel1.asset");
            var moneyMultiplier = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/MoneyMultiplier.asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceDefinition>("Assets/Resources/SerenaysGambit/Data/Balance/DefaultBalance.asset");

            Assert.That(absolut, Is.Not.Null);
            Assert.That(absolut.Symbol, Is.EqualTo(SymbolKind.Absolut));
            Assert.That(absolut.DisplayName, Is.EqualTo("Absolut"));
            Assert.That(absolut.StartingValue, Is.EqualTo(1));
            Assert.That(absolut.RotationImage, Is.Not.Null);
            Assert.That(absolut.Icon, Is.SameAs(absolut.RotationImage));
            Assert.That(absolut.ScoreAnimation, Is.Not.Null);

            var cat = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Cat.asset");
            Assert.That(cat, Is.Not.Null);
            Assert.That(cat.Symbol, Is.EqualTo(SymbolKind.Cat));
            Assert.That(cat.DisplayName, Is.EqualTo("Cat"));
            Assert.That(cat.StartingValue, Is.EqualTo(15));
            Assert.That(cat.RotationImage, Is.Not.Null);
            Assert.That(cat.Icon, Is.SameAs(cat.RotationImage));
            Assert.That(cat.ScoreAnimation, Is.Not.Null);

            var cigarette = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Cigarette.asset");
            Assert.That(cigarette, Is.Not.Null);
            Assert.That(cigarette.Symbol, Is.EqualTo(SymbolKind.Cigarette));
            Assert.That(cigarette.DisplayName, Is.EqualTo("Cigarette"));
            Assert.That(cigarette.StartingValue, Is.EqualTo(20));
            Assert.That(cigarette.RotationImage, Is.Not.Null);
            Assert.That(cigarette.ScoreAnimation, Is.Not.Null);

            var dollar = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Dollar.asset");
            Assert.That(dollar, Is.Not.Null);
            Assert.That(dollar.Symbol, Is.EqualTo(SymbolKind.Dollar));
            Assert.That(dollar.DisplayName, Is.EqualTo("Dollar"));
            Assert.That(dollar.StartingValue, Is.EqualTo(5));
            Assert.That(dollar.RotationImage, Is.Not.Null);
            Assert.That(dollar.Icon, Is.SameAs(dollar.RotationImage));
            Assert.That(dollar.ScoreAnimation, Is.Not.Null);

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
        public void SymbolDefinitionStoresItsRotationImageAndScoreAnimation()
        {
            var definition = ScriptableObject.CreateInstance<SymbolDefinition>();
            var texture = new Texture2D(1, 1);
            var rotationImage = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            var scoreAnimation = new AnimationClip();

            try
            {
                definition.Initialize(SymbolKind.Absolut, "Absolut", 1, rotationImage, scoreAnimation);

                Assert.That(definition.RotationImage, Is.SameAs(rotationImage));
                Assert.That(definition.ScoreAnimation, Is.SameAs(scoreAnimation));
            }
            finally
            {
                Object.DestroyImmediate(rotationImage);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(scoreAnimation);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void DefaultAuthoredAssetsBuildTheRuntimeRulesSnapshot()
        {
            var absolut = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Absolut.asset");
            var dollar = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Dollar.asset");
            var cat = AssetDatabase.LoadAssetAtPath<SymbolDefinition>("Assets/Resources/SerenaysGambit/Data/Symbols/Cat.asset");
            var reel1 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel1.asset");
            var reel2 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel2.asset");
            var reel3 = AssetDatabase.LoadAssetAtPath<ReelDefinition>("Assets/Resources/SerenaysGambit/Data/Reels/Reel3.asset");
            var moneyMultiplier = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/Resources/SerenaysGambit/Data/ShopItems/MoneyMultiplier.asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceDefinition>("Assets/Resources/SerenaysGambit/Data/Balance/DefaultBalance.asset");

            var config = RuntimeGameConfigFactory.Create(
                new[] { absolut, dollar, cat },
                new[] { reel1, reel2, reel3 },
                new[] { moneyMultiplier },
                balance);

            Assert.That(config.AbsolutStartingValue, Is.EqualTo(absolut.StartingValue));
            Assert.That(config.DollarStartingValue, Is.EqualTo(dollar.StartingValue));
            Assert.That(config.GetStartingValue(SymbolKind.Cat), Is.EqualTo(cat.StartingValue));
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
            var absolut = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/AbsolutGambit.asset");
            var batchTen = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/BatchTenGambit.asset");
            var kiss = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/Kiss1000xGambit.asset");
            var cigarette = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/CigaretteDecayGambit.asset");

            Assert.That(absolut, Is.Not.Null);
            Assert.That(absolut.Kind, Is.EqualTo(GambitKind.Absolut));
            Assert.That(absolut.PayoutMultiplier, Is.EqualTo(10));
            Assert.That(absolut.SacrificePercent, Is.EqualTo(25));
            Assert.That(batchTen, Is.Not.Null);
            Assert.That(batchTen.Kind, Is.EqualTo(GambitKind.BatchTen));
            Assert.That(batchTen.RollMultiplier, Is.EqualTo(10));
            Assert.That(kiss, Is.Not.Null);
            Assert.That(kiss.Kind, Is.EqualTo(GambitKind.Kiss1000x));
            Assert.That(kiss.PayoutMultiplier, Is.EqualTo(1000));
            Assert.That(kiss.RiskPercent, Is.EqualTo(15));
            Assert.That(cigarette, Is.Not.Null);
            Assert.That(cigarette.Kind, Is.EqualTo(GambitKind.CigaretteDecay));
            Assert.That(cigarette.PayoutMultiplier, Is.EqualTo(5));
            Assert.That(cigarette.DecayPerMiss, Is.EqualTo(1));
        }

        [Test]
        public void AuthoredGambitValuesBuildTheRuntimeConfig()
        {
            var absolut = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/AbsolutGambit.asset");
            var batchTen = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/BatchTenGambit.asset");
            var kiss = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/Kiss1000xGambit.asset");
            var cigarette = AssetDatabase.LoadAssetAtPath<GambitItemDefinition>("Assets/Resources/SerenaysGambit/Data/Gambits/CigaretteDecayGambit.asset");

            var config = RuntimeGameConfigFactory.Create(
                null,
                null,
                null,
                null,
                new[] { absolut, batchTen, kiss, cigarette });

            Assert.That(config.FindGambitItemConfig(GambitKind.Absolut).PayoutMultiplier, Is.EqualTo(absolut.PayoutMultiplier));
            Assert.That(config.FindGambitItemConfig(GambitKind.BatchTen).RollMultiplier, Is.EqualTo(batchTen.RollMultiplier));
            Assert.That(config.FindGambitItemConfig(GambitKind.Kiss1000x).RiskPercent, Is.EqualTo(kiss.RiskPercent));
            Assert.That(config.FindGambitItemConfig(GambitKind.CigaretteDecay).DecayPerMiss, Is.EqualTo(cigarette.DecayPerMiss));
        }
    }
}
