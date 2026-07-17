using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace SerenaysGambit.Tests
{
    public sealed class SlotGameRulesTests
    {
        [Test]
        public void ReelStateWrapsTheFiveFaceStripForTheVisibleRows()
        {
            var mockStrip = new[] { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Cherry, SymbolKind.Joker };
            var reel = new ReelState(mockStrip, 4);

            Assert.That(reel.VisibleFaceAt(0), Is.EqualTo(SymbolKind.Joker));
            Assert.That(reel.VisibleFaceAt(1), Is.EqualTo(SymbolKind.Strawberry));
            Assert.That(reel.VisibleFaceAt(2), Is.EqualTo(SymbolKind.Strawberry));
        }

        [Test]
        public void AllEightConfiguredPaylinesScoreOnAFullMatchingBoard()
        {
            var grid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry }
            };

            var score = SlotScoring.Evaluate(grid, new RunModifiers(), 1);

            Assert.That(GameBalance.Paylines.Count, Is.EqualTo(8));
            Assert.That(score.Wins.Count, Is.EqualTo(8));
            Assert.That(score.Wins[0].Payline.Name, Is.EqualTo("Top row"));
            Assert.That(score.Wins[3].Payline.Name, Is.EqualTo("Left reel"));
            Assert.That(score.Wins[6].Payline.Name, Is.EqualTo("Top-left diagonal"));
            Assert.That(score.Wins[7].Payline.Name, Is.EqualTo("Top-right diagonal"));
        }

        [Test]
        public void MiddleJokerResolvesAsTheMatchingRegularSymbol()
        {
            var grid = new[,]
            {
                { SymbolKind.Cherry, SymbolKind.Strawberry, SymbolKind.Cherry },
                { SymbolKind.Strawberry, SymbolKind.Joker, SymbolKind.Strawberry },
                { SymbolKind.Cherry, SymbolKind.Strawberry, SymbolKind.Cherry }
            };

            var score = SlotScoring.Evaluate(grid, new RunModifiers(), 1);
            PaylineWin middleRow = null;
            foreach (var win in score.Wins)
            {
                if (win.Payline.Name == "Middle row")
                {
                    middleRow = win;
                    break;
                }
            }

            Assert.That(middleRow, Is.Not.Null);
            Assert.That(middleRow.ResolvedSymbol, Is.EqualTo(SymbolKind.Strawberry));
            Assert.That(middleRow.IsTripleJoker, Is.False);
        }

        [Test]
        public void TripleJokerUsesTheSpecialMultiplierOnEveryWinningLine()
        {
            var grid = new[,]
            {
                { SymbolKind.Joker, SymbolKind.Joker, SymbolKind.Joker },
                { SymbolKind.Joker, SymbolKind.Joker, SymbolKind.Joker },
                { SymbolKind.Joker, SymbolKind.Joker, SymbolKind.Joker }
            };

            var score = SlotScoring.Evaluate(grid, new RunModifiers(), 1);

            Assert.That(score.Wins.Count, Is.EqualTo(8));
            Assert.That(score.ComboMultiplier, Is.EqualTo(9));
            Assert.That(score.PayoutKurus, Is.EqualTo(new BigInteger(3378240)));
            Assert.That(score.Wins[0].IsTripleJoker, Is.True);
        }

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 4)]
        [TestCase(3, 9)]
        [TestCase(8, 9)]
        public void ComboMultiplierUsesTheConfiguredRule(int winCount, int expectedMultiplier)
        {
            Assert.That(SlotScoring.ComboMultiplierFor(winCount), Is.EqualTo(expectedMultiplier));
        }

        [Test]
        public void BatchFactorMultipliesTheFinalPayout()
        {
            var grid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry }
            };

            var modifiers = new RunModifiers();
            var single = SlotScoring.Evaluate(grid, modifiers, 1);
            var batch = SlotScoring.Evaluate(grid, modifiers, 5);

            Assert.That(batch.PayoutKurus, Is.EqualTo(single.PayoutKurus * 5));
        }

        [Test]
        public void MoneyMultiplierAppliesAfterLineAndComboCalculation()
        {
            var grid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry }
            };

            var modifiers = new RunModifiers();
            var baseline = SlotScoring.Evaluate(grid, modifiers, 1);
            modifiers.DoubleMoneyMultiplier();
            var doubled = SlotScoring.Evaluate(grid, modifiers, 1);
            var doubledBatch = SlotScoring.Evaluate(grid, modifiers, 5);

            Assert.That(doubled.PayoutKurus, Is.EqualTo(baseline.PayoutKurus * 2));
            Assert.That(doubledBatch.PayoutKurus, Is.EqualTo(baseline.PayoutKurus * 10));
        }

        [Test]
        public void BaseOutputMultiplierMultipliesFinalPayout()
        {
            var grid = new[,]
            {
                { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Banana, SymbolKind.Banana, SymbolKind.Banana }
            };
            var modifiers = new RunModifiers();
            var baseline = SlotScoring.Evaluate(grid, modifiers, 1);

            modifiers.IncreaseBaseOutputMultiplier(); // level 1 = x2
            var doubled = SlotScoring.Evaluate(grid, modifiers, 1);

            modifiers.IncreaseBaseOutputMultiplier(); // level 2 = x4
            var quadrupled = SlotScoring.Evaluate(grid, modifiers, 1);

            Assert.That(doubled.PayoutKurus, Is.EqualTo(baseline.PayoutKurus * 2));
            Assert.That(quadrupled.PayoutKurus, Is.EqualTo(baseline.PayoutKurus * 4));
        }

        [Test]
        public void FinalPaylinePayoutsIncludeEveryMultiplierAndSumToTheSpinPayout()
        {
            var grid = new[,]
            {
                { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Banana, SymbolKind.Banana, SymbolKind.Banana }
            };
            var modifiers = new RunModifiers();
            modifiers.DoubleMoneyMultiplier();
            modifiers.IncreaseBaseOutputMultiplier(); // x2
            modifiers.IncreaseBaseOutputMultiplier(); // x4

            var score = SlotScoring.Evaluate(grid, modifiers, 5);
            var displayedPayout = BigInteger.Zero;
            foreach (var win in score.Wins)
            {
                displayedPayout += win.FinalPayoutKurus;
                Assert.That(
                    win.FinalPayoutKurus,
                    Is.EqualTo(win.LinePayoutKurus * score.ComboMultiplier * modifiers.MoneyMultiplier * modifiers.BaseOutputMultiplier * score.BatchFactor));
            }

            Assert.That(displayedPayout, Is.EqualTo(score.PayoutKurus));
        }

        [Test]
        public void SpinResultKeepsPreSpinCashAndThresholdForScoreAnimation()
        {
            var winningGrid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry }
            };
            var engine = new SlotGameEngine(1, () => winningGrid);
            var state = engine.CreateNewRun();
            var cashBeforeSpin = state.CurrentTargetKurus - 1;
            var targetBeforeSpin = state.CurrentTargetKurus;
            var thresholdLevelBeforeSpin = state.ThresholdLevel;
            state.CashKurus = cashBeforeSpin;
            state.RollsRemaining = 1;

            var result = engine.TrySpin(state, 1);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ThresholdCleared, Is.True);
            Assert.That(result.CashBeforeSpinKurus, Is.EqualTo(cashBeforeSpin));
            Assert.That(result.TargetBeforeSpinKurus, Is.EqualTo(targetBeforeSpin));
            Assert.That(result.ThresholdLevelBeforeSpin, Is.EqualTo(thresholdLevelBeforeSpin));
        }

        [Test]
        public void BatchSpinConsumesTheRequestedNumberOfRolls()
        {
            var engine = new SlotGameEngine(1, CreateNoWinGrid);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 6;

            var result = engine.TrySpin(state, 5);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Score.PayoutKurus, Is.EqualTo(BigInteger.Zero));
            Assert.That(state.RollsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void ThresholdPaymentCarriesOnlyTheSurplusAndClearsTemporaryFreeSpins()
        {
            var engine = new SlotGameEngine(7);
            var state = engine.CreateNewRun();
            state.Modifiers.AddFreeSpins(GameBalance.FreeSpinBundle);
            state.CashKurus = state.CurrentTargetKurus + 12345;
            state.RollsRemaining = 0;

            var settled = engine.TrySettleThreshold(state);

            Assert.That(settled, Is.True);
            Assert.That(state.ThresholdLevel, Is.EqualTo(2));
            Assert.That(state.CashKurus, Is.EqualTo(new BigInteger(12345)));
            Assert.That(state.Modifiers.TemporaryFreeSpins, Is.EqualTo(0));
            Assert.That(state.RollsRemaining, Is.EqualTo(GameBalance.BaseRolls));
        }

        [Test]
        public void FailedAttemptRetainsTheRunAndAwardsOneRefreshTicket()
        {
            var engine = new SlotGameEngine(11);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 0;

            string lostOrgan;
            var failed = engine.ResolveFailureIfOutOfRolls(state, out lostOrgan);

            Assert.That(failed, Is.True);
            Assert.That(lostOrgan, Is.EqualTo("Mide"));
            Assert.That(state.OrganLosses, Is.EqualTo(1));
            Assert.That(state.RefreshTickets, Is.EqualTo(1));
            Assert.That(state.RollsRemaining, Is.EqualTo(GameBalance.BaseRolls));
            Assert.That(state.Phase, Is.EqualTo(RunPhase.Playing));
        }

        [Test]
        public void FifthFailedAttemptLosesKalpAndEndsTheRun()
        {
            var engine = new SlotGameEngine(11);
            var state = engine.CreateNewRun();
            string lostOrgan = string.Empty;

            for (var attempt = 0; attempt < GameBalance.OrganCount; attempt++)
            {
                state.RollsRemaining = 0;
                Assert.That(engine.ResolveFailureIfOutOfRolls(state, out lostOrgan), Is.True);
            }

            Assert.That(lostOrgan, Is.EqualTo("Kalp"));
            Assert.That(state.OrganLosses, Is.EqualTo(5));
            Assert.That(state.RefreshTickets, Is.EqualTo(4));
            Assert.That(state.Phase, Is.EqualTo(RunPhase.GameOver));
        }

        [Test]
        public void RefreshTicketRerollsOnlyUnsoldOffers()
        {
            var engine = new SlotGameEngine(13);
            var state = engine.CreateNewRun();
            state.CashKurus = state.ShopOffers[0].CostKurus;
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            var purchasedOffer = state.ShopOffers[0];
            state.RollsRemaining = 0;
            string lostOrgan;
            engine.ResolveFailureIfOutOfRolls(state, out lostOrgan);

            Assert.That(engine.TryRefreshShop(state, out message), Is.True);
            Assert.That(state.RefreshTickets, Is.EqualTo(0));
            Assert.That(state.ShopOffers[0], Is.SameAs(purchasedOffer));
            Assert.That(state.ShopOffers[0].Purchased, Is.True);
            Assert.That(state.ShopOffers.Count, Is.EqualTo(3));
        }

        [Test]
        public void PermanentUpgradePersistsAcrossFailureAndThresholdCompletion()
        {
            var engine = new SlotGameEngine(17);
            var state = engine.CreateNewRun();
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.MoneyMultiplier, BigInteger.Zero, "Money Output x2", "Test offer"));
            state.CashKurus = BigInteger.Zero;
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            state.RollsRemaining = 0;
            string lostOrgan;
            engine.ResolveFailureIfOutOfRolls(state, out lostOrgan);
            state.CashKurus = state.CurrentTargetKurus;
            state.RollsRemaining = 0;

            Assert.That(engine.TrySettleThreshold(state), Is.True);
            Assert.That(state.Modifiers.MoneyMultiplier, Is.EqualTo(new BigInteger(2)));
            Assert.That(state.ThresholdLevel, Is.EqualTo(2));
        }

        [Test]
        public void AuthoredConfigDrivesRunValuesAndUpgradesNeverChangeReelFaces()
        {
            var configuredReels = new[]
            {
                new[] { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                new[] { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                new[] { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry }
            };
            var config = new GameRulesConfig(configuredReels, 3, 7, 12, 3, 4, 6);
            var engine = new SlotGameEngine(23, config);
            var state = engine.CreateNewRun();
            var firstStripBeforeUpgrade = (SymbolKind[])config.ReelStripAt(0).Clone();

            Assert.That(state.RollsRemaining, Is.EqualTo(12));
            Assert.That(state.RemainingOrgans, Is.EqualTo(3));
            Assert.That(state.Modifiers.StrawberryValue, Is.EqualTo(3));
            Assert.That(state.Modifiers.CherryValue, Is.EqualTo(7));

            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.StrawberryValue, BigInteger.Zero, "Strawberry Value +1x", "Test offer"));
            string message;
            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            Assert.That(state.Modifiers.StrawberryValue, Is.EqualTo(4));
            Assert.That(config.ReelStripAt(0), Is.EqualTo(firstStripBeforeUpgrade));

            var spin = engine.TrySpin(state, 1);
            Assert.That(spin.Accepted, Is.True);
            for (var row = 0; row < GameBalance.GridRows; row++)
            {
                for (var column = 0; column < GameBalance.GridColumns; column++)
                {
                    Assert.That(spin.Grid[row, column], Is.EqualTo(SymbolKind.Cherry));
                }
            }

            for (int i = 0; i < 9; i++)
            {
                Assert.That(state.Modifiers.IncreaseBaseOutputMultiplier(), Is.True);
            }
            Assert.That(state.Modifiers.IncreaseBaseOutputMultiplier(), Is.False);
        }

        [Test]
        public void AuthoredShopCopyIsUsedForGeneratedOffers()
        {
            var texts = new Dictionary<ShopOfferKind, ShopItemText>();
            foreach (ShopOfferKind kind in System.Enum.GetValues(typeof(ShopOfferKind)))
            {
                texts.Add(kind, new ShopItemText("Authored " + kind, "Description " + kind));
            }

            var config = new GameRulesConfig(
                GameBalance.InitialReels,
                1,
                5,
                GameBalance.BaseRolls,
                GameBalance.OrganCount,
                GameBalance.MaxThresholdLevel,
                GameBalance.FreeSpinBundle,
                texts);
            var state = new SlotGameEngine(31, config).CreateNewRun();
            ShopOffer authoredOffer = null;
            foreach (var offer in state.ShopOffers)
            {
                if (offer.Kind != ShopOfferKind.BaseOutputMultiplier)
                {
                    authoredOffer = offer;
                    break;
                }
            }

            Assert.That(authoredOffer, Is.Not.Null);
            Assert.That(authoredOffer.Title, Is.EqualTo("Authored " + authoredOffer.Kind));
            Assert.That(authoredOffer.Description, Is.EqualTo("Description " + authoredOffer.Kind));
        }

        [Test]
        public void CannotSettleThresholdUnlessRollsAreDepleted()
        {
            var engine = new SlotGameEngine(42);
            var state = engine.CreateNewRun();
            state.CashKurus = state.CurrentTargetKurus;
            state.RollsRemaining = 1;

            Assert.That(engine.TrySettleThreshold(state), Is.False);

            state.RollsRemaining = 0;
            Assert.That(engine.TrySettleThreshold(state), Is.True);
        }

        [Test]
        public void CappingBatchFactorToRemainingRollsWhenFewerRollsLeft()
        {
            var engine = new SlotGameEngine(1, CreateNoWinGrid);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 8;

            var result = engine.TrySpin(state, 10);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Score.BatchFactor, Is.EqualTo(8));
            Assert.That(result.OrganLost, Is.True);
            Assert.That(state.OrganLosses, Is.EqualTo(1));
            Assert.That(state.RollsRemaining, Is.EqualTo(state.Modifiers.StartingRolls));
        }

        [Test]
        public void RunStatsResetForEachNewRun()
        {
            var engine = new SlotGameEngine(1, CreateThreeSymbolWinningGrid);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 1;

            Assert.That(engine.TrySpin(state, 1).Accepted, Is.True);
            Assert.That(state.Stats.RollsUsed, Is.EqualTo(1));

            var restarted = engine.CreateNewRun();
            Assert.That(restarted.Stats.RollsUsed, Is.EqualTo(0));
            Assert.That(restarted.Stats.HighestThresholdReached, Is.EqualTo(1));
            Assert.That(restarted.Stats.TotalEarnedKurus, Is.EqualTo(BigInteger.Zero));
            Assert.That(restarted.Stats.TotalSpentKurus, Is.EqualTo(BigInteger.Zero));
            Assert.That(restarted.Stats.JackpotsScored, Is.EqualTo(0));
            Assert.That(restarted.Stats.TotalItemsPurchased, Is.EqualTo(0));

            foreach (SymbolKind symbol in System.Enum.GetValues(typeof(SymbolKind)))
            {
                var symbolStats = restarted.Stats.GetSymbolStats(symbol);
                Assert.That(symbolStats.WinningPaylineCount, Is.EqualTo(0));
                Assert.That(symbolStats.GeneratedKurus, Is.EqualTo(BigInteger.Zero));
            }
        }

        [Test]
        public void AcceptedSpinRecordsCappedRollsAndFinalSymbolEarnings()
        {
            var engine = new SlotGameEngine(1, CreateThreeSymbolWinningGrid);
            var state = engine.CreateNewRun();
            state.Modifiers.DoubleMoneyMultiplier();
            state.Modifiers.IncreaseBaseOutputMultiplier();
            state.RollsRemaining = 8;

            var result = engine.TrySpin(state, 10);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Score.BatchFactor, Is.EqualTo(8));
            Assert.That(state.Stats.RollsUsed, Is.EqualTo(8));
            Assert.That(state.Stats.TotalEarnedKurus, Is.EqualTo(result.Score.PayoutKurus));
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Strawberry).WinningPaylineCount, Is.EqualTo(1));
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Cherry).WinningPaylineCount, Is.EqualTo(1));
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Banana).WinningPaylineCount, Is.EqualTo(1));
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Joker).WinningPaylineCount, Is.EqualTo(0));

            var attributedPayout = BigInteger.Zero;
            foreach (SymbolKind symbol in System.Enum.GetValues(typeof(SymbolKind)))
            {
                attributedPayout += state.Stats.GetSymbolStats(symbol).GeneratedKurus;
            }

            Assert.That(attributedPayout, Is.EqualTo(result.Score.PayoutKurus));
            Assert.That(
                state.Stats.GetSymbolStats(SymbolKind.Strawberry).GeneratedKurus,
                Is.EqualTo(result.Score.Wins[0].FinalPayoutKurus));
        }

        [Test]
        public void RejectedSpinDoesNotChangeRunStats()
        {
            var engine = new SlotGameEngine(1, CreateNoWinGrid);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 0;

            var result = engine.TrySpin(state, 1);

            Assert.That(result.Accepted, Is.False);
            Assert.That(state.Stats.RollsUsed, Is.EqualTo(0));
            Assert.That(state.Stats.TotalEarnedKurus, Is.EqualTo(BigInteger.Zero));
            Assert.That(state.Stats.JackpotsScored, Is.EqualTo(0));
        }

        [Test]
        public void SuccessfulPurchaseRecordsSpendAndItemCountOnlyOnce()
        {
            var engine = new SlotGameEngine(1);
            var state = engine.CreateNewRun();
            var cost = new BigInteger(12345);
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.FreeSpins, cost, "Free Spins", "Test offer"));
            string message;

            state.CashKurus = cost - 1;
            Assert.That(engine.TryPurchase(state, 0, out message), Is.False);
            Assert.That(state.Stats.TotalSpentKurus, Is.EqualTo(BigInteger.Zero));
            Assert.That(state.Stats.TotalItemsPurchased, Is.EqualTo(0));

            state.CashKurus = cost;
            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            Assert.That(state.Stats.TotalSpentKurus, Is.EqualTo(cost));
            Assert.That(state.Stats.TotalItemsPurchased, Is.EqualTo(1));

            Assert.That(engine.TryPurchase(state, 0, out message), Is.False);
            Assert.That(state.Stats.TotalSpentKurus, Is.EqualTo(cost));
            Assert.That(state.Stats.TotalItemsPurchased, Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulPurchasesTrackOwnedUpgradeCounts()
        {
            var engine = new SlotGameEngine(1);
            var state = engine.CreateNewRun();
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.StrawberryValue, BigInteger.Zero, "Strawberry", "Test offer"));
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.StrawberryValue), Is.EqualTo(1));

            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.StrawberryValue, BigInteger.Zero, "Strawberry", "Test offer"));
            Assert.That(engine.TryPurchase(state, 1, out message), Is.True);
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.StrawberryValue), Is.EqualTo(2));

            var restarted = engine.CreateNewRun();
            Assert.That(restarted.OwnedUpgradeCount(ShopOfferKind.StrawberryValue), Is.EqualTo(0));
        }

        [Test]
        public void FailedPurchaseDoesNotTrackAnOwnedUpgrade()
        {
            var engine = new SlotGameEngine(1);
            var state = engine.CreateNewRun();
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.CherryValue, new BigInteger(100), "Cherry", "Test offer"));
            state.CashKurus = BigInteger.Zero;
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.False);
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.CherryValue), Is.EqualTo(0));
        }

        [Test]
        public void ClearingAThresholdRemovesTemporaryFreeSpinsFromOwnedUpgrades()
        {
            var engine = new SlotGameEngine(1);
            var state = engine.CreateNewRun();
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.FreeSpins, BigInteger.Zero, "Free Spins", "Test offer"));
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.FreeSpins), Is.EqualTo(1));
            Assert.That(state.Modifiers.TemporaryFreeSpins, Is.EqualTo(state.Config.FreeSpinBundle));

            state.CashKurus = state.CurrentTargetKurus;
            state.RollsRemaining = 0;
            Assert.That(engine.TrySettleThreshold(state), Is.True);

            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.FreeSpins), Is.EqualTo(0));
            Assert.That(state.Modifiers.TemporaryFreeSpins, Is.EqualTo(0));
        }

        [Test]
        public void TripleJokerSpinCountsOneJackpotAndEveryWinningPayline()
        {
            var engine = new SlotGameEngine(1, CreateTripleJokerGrid);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 1;

            var result = engine.TrySpin(state, 1);
            var tripleJokerStats = state.Stats.GetSymbolStats(SymbolKind.Joker);

            Assert.That(result.Accepted, Is.True);
            Assert.That(state.Stats.JackpotsScored, Is.EqualTo(1));
            Assert.That(tripleJokerStats.WinningPaylineCount, Is.EqualTo(result.Score.Wins.Count));
            Assert.That(tripleJokerStats.GeneratedKurus, Is.EqualTo(result.Score.PayoutKurus));
        }

        [Test]
        public void HighestThresholdReachedTracksSuccessfulSettlementsThroughVictory()
        {
            var engine = new SlotGameEngine(7);
            var state = engine.CreateNewRun();

            while (state.Phase == RunPhase.Playing)
            {
                state.CashKurus = state.CurrentTargetKurus;
                state.RollsRemaining = 0;
                Assert.That(engine.TrySettleThreshold(state), Is.True);
            }

            Assert.That(state.Phase, Is.EqualTo(RunPhase.Victory));
            Assert.That(state.Stats.HighestThresholdReached, Is.EqualTo(state.Config.ThresholdCount));
        }

        private static SymbolKind[,] CreateNoWinGrid()
        {
            return new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Cherry, SymbolKind.Strawberry },
                { SymbolKind.Cherry, SymbolKind.Strawberry, SymbolKind.Cherry },
                { SymbolKind.Cherry, SymbolKind.Strawberry, SymbolKind.Cherry }
            };
        }

        private static SymbolKind[,] CreateThreeSymbolWinningGrid()
        {
            return new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Cherry, SymbolKind.Cherry, SymbolKind.Cherry },
                { SymbolKind.Banana, SymbolKind.Banana, SymbolKind.Banana }
            };
        }

        private static SymbolKind[,] CreateTripleJokerGrid()
        {
            return new[,]
            {
                { SymbolKind.Joker, SymbolKind.Joker, SymbolKind.Joker },
                { SymbolKind.Joker, SymbolKind.Joker, SymbolKind.Joker },
                { SymbolKind.Joker, SymbolKind.Joker, SymbolKind.Joker }
            };
        }
    }
}
