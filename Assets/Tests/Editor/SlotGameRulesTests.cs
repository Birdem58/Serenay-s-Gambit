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
            var mockStrip = new[] { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Kiss };
            var reel = new ReelState(mockStrip, 4);

            Assert.That(reel.VisibleFaceAt(0), Is.EqualTo(SymbolKind.Kiss));
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
        public void MiddleKissResolvesAsTheMatchingRegularSymbol()
        {
            var grid = new[,]
            {
                { SymbolKind.Dollar, SymbolKind.Strawberry, SymbolKind.Dollar },
                { SymbolKind.Strawberry, SymbolKind.Kiss, SymbolKind.Strawberry },
                { SymbolKind.Dollar, SymbolKind.Strawberry, SymbolKind.Dollar }
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
            Assert.That(middleRow.IsTripleKiss, Is.False);
        }

        [Test]
        public void TripleKissUsesTheSpecialMultiplierOnEveryWinningLine()
        {
            var grid = new[,]
            {
                { SymbolKind.Kiss, SymbolKind.Kiss, SymbolKind.Kiss },
                { SymbolKind.Kiss, SymbolKind.Kiss, SymbolKind.Kiss },
                { SymbolKind.Kiss, SymbolKind.Kiss, SymbolKind.Kiss }
            };

            var score = SlotScoring.Evaluate(grid, new RunModifiers(), 1);

            Assert.That(score.Wins.Count, Is.EqualTo(8));
            Assert.That(score.ComboMultiplier, Is.EqualTo(9));
            Assert.That(score.PayoutKurus, Is.EqualTo(new BigInteger(3378240)));
            Assert.That(score.Wins[0].IsTripleKiss, Is.True);
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
        public void ExponentialUpgradeMultipliersApplyCorrectly()
        {
            var config = GameRulesConfig.CreateDefault();
            var modifiers = new RunModifiers(config);

            // Verify starting values
            Assert.That(modifiers.DollarValue, Is.EqualTo(5));
            Assert.That(modifiers.CigaretteValue, Is.EqualTo(20));
            Assert.That(modifiers.KissValue, Is.EqualTo(1));

            // Improve Dollar
            modifiers.ImproveSymbol(SymbolKind.Dollar, 8); // +8x
            Assert.That(modifiers.DollarValue, Is.EqualTo(13)); // 5 + 8 = 13

            // Improve Cigarette
            modifiers.ImproveSymbol(SymbolKind.Cigarette, 16); // +16x
            Assert.That(modifiers.CigaretteValue, Is.EqualTo(36)); // 20 + 16 = 36

            // Improve Kiss
            modifiers.ImproveSymbol(SymbolKind.Kiss, 64); // +64x
            Assert.That(modifiers.KissValue, Is.EqualTo(65)); // 1 + 64 = 65

            // Verify triple kiss calculation uses kiss multiplier
            var basePayout = (GameBalance.BaseLinePayoutKurus * GameBalance.TripleKissMultiplierNumerator) / GameBalance.TripleKissMultiplierDenominator;
            var expectedTripleKissPayout = basePayout * 65;
            Assert.That(PayoutCalculator.CalculateLinePayout(SymbolKind.Kiss, modifiers, true), Is.EqualTo(expectedTripleKissPayout));
        }

        [Test]
        public void BatchFactorMultipliesTheFinalPayout()
        {
            var grid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry }
            };

            var modifiers = new RunModifiers();
            var single = SlotScoring.Evaluate(grid, modifiers, 1);
            var batch = SlotScoring.Evaluate(grid, modifiers, 5);

            Assert.That(batch.PayoutKurus, Is.EqualTo(single.PayoutKurus * 5));
        }

        [TestCase(PaylineGroup.Horizontal)]
        [TestCase(PaylineGroup.Vertical)]
        [TestCase(PaylineGroup.CrissCross)]
        public void DirectionalMatchUpgradesProgressThroughEveryTierAndOnlyBoostTheirOwnLines(PaylineGroup group)
        {
            var grid = CreateWinningGridFor(group);
            var modifiers = new RunModifiers();
            var baseline = SlotScoring.Evaluate(grid, modifiers, 1);
            var expectedTiers = new[] { 2, 5, 10, 100 };

            Assert.That(baseline.Wins.Count, Is.GreaterThan(0));
            foreach (var win in baseline.Wins)
            {
                Assert.That(win.Payline.Group, Is.EqualTo(group));
                Assert.That(win.MatchCountMultiplier, Is.EqualTo(1));
            }

            foreach (var expectedTier in expectedTiers)
            {
                Assert.That(modifiers.IncreaseMatchCountMultiplier(group), Is.True);
                var boosted = SlotScoring.Evaluate(grid, modifiers, 1);

                Assert.That(modifiers.MatchCountMultiplier(group), Is.EqualTo(expectedTier));
                Assert.That(boosted.PayoutKurus, Is.EqualTo(baseline.PayoutKurus * expectedTier));
                foreach (var win in boosted.Wins)
                {
                    Assert.That(win.Payline.Group, Is.EqualTo(group));
                    Assert.That(win.MatchCountMultiplier, Is.EqualTo(expectedTier));
                    Assert.That(boosted.RewardAnimationCount(win), Is.EqualTo(expectedTier));
                }
            }

            Assert.That(modifiers.IncreaseMatchCountMultiplier(group), Is.False);
        }

        [Test]
        public void MatchCountUpgradeAddsTheSameNumberOfRewardAnimationsAsItsMultiplier()
        {
            var modifiers = new RunModifiers();
            Assert.That(modifiers.IncreaseMatchCountMultiplier(PaylineGroup.Horizontal), Is.True); // x2
            Assert.That(modifiers.IncreaseMatchCountMultiplier(PaylineGroup.Horizontal), Is.True); // x5

            var score = SlotScoring.Evaluate(CreateWinningGridFor(PaylineGroup.Horizontal), modifiers, 5);

            foreach (var win in score.Wins)
            {
                Assert.That(win.MatchCountMultiplier, Is.EqualTo(5));
                Assert.That(score.RewardAnimationCount(win), Is.EqualTo(25));
            }
        }

        [Test]
        public void RewardAnimationQueueSplitsLinePayoutAcrossThreeCellsWithRemainderOnLastCell()
        {
            var payline = new Payline(
                "Test line",
                0,
                PaylineGroup.Horizontal,
                new GridPosition(0, 0),
                new GridPosition(0, 1),
                new GridPosition(0, 2));
            var win = new PaylineWin(payline, SymbolKind.Cigarette, new BigInteger(1001), false);
            var score = new ScoredSpin(
                new List<PaylineWin> { win },
                new BigInteger(1001),
                1,
                1);

            var events = RewardAnimationQueueBuilder.Build(score);
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Kind, Is.EqualTo(RewardAnimationEventKind.MatchAddition));
            Assert.That(events[0].CellRewards.Count, Is.EqualTo(3));
            Assert.That(events[0].CellRewards[0].AmountKurus, Is.EqualTo(new BigInteger(333)));
            Assert.That(events[0].CellRewards[1].AmountKurus, Is.EqualTo(new BigInteger(333)));
            Assert.That(events[0].CellRewards[2].AmountKurus, Is.EqualTo(new BigInteger(335)));

            var sum = BigInteger.Zero;
            foreach (var reward in events[0].CellRewards)
            {
                sum += reward.AmountKurus;
            }
            Assert.That(sum, Is.EqualTo(win.LinePayoutKurus));
        }

        [Test]
        public void RewardAnimationQueueRepeatsMatchEventsForBatchAndMatchMultiplier()
        {
            var payline = new Payline(
                "Test line",
                0,
                PaylineGroup.Horizontal,
                new GridPosition(0, 0),
                new GridPosition(0, 1),
                new GridPosition(0, 2));
            var batchWin = new PaylineWin(payline, SymbolKind.Orange, new BigInteger(1500), false);
            var batchScore = new ScoredSpin(
                new List<PaylineWin> { batchWin },
                new BigInteger(1500) * 5,
                1,
                5);
            var batchEvents = RewardAnimationQueueBuilder.Build(batchScore);
            var batchMatchEventCount = 0;
            foreach (var animationEvent in batchEvents)
            {
                if (animationEvent.Kind == RewardAnimationEventKind.MatchAddition)
                {
                    batchMatchEventCount++;
                }
            }
            Assert.That(batchMatchEventCount, Is.EqualTo(5));

            var win = new PaylineWin(payline, SymbolKind.Orange, new BigInteger(1500), false, 2);
            var score = new ScoredSpin(
                new List<PaylineWin> { win },
                new BigInteger(1500) * 10,
                1,
                5);

            var events = RewardAnimationQueueBuilder.Build(score);
            var matchEventCount = 0;
            var cellRewardCount = 0;
            foreach (var animationEvent in events)
            {
                if (animationEvent.Kind == RewardAnimationEventKind.MatchAddition)
                {
                    matchEventCount++;
                    cellRewardCount += animationEvent.CellRewards.Count;
                }
            }

            Assert.That(matchEventCount, Is.EqualTo(10));
            Assert.That(cellRewardCount, Is.EqualTo(30));
            Assert.That(events[events.Count - 1].Kind, Is.EqualTo(RewardAnimationEventKind.Multiplier));
        }

        [Test]
        public void RewardAnimationQueueDurationCompressesAndHonorsMinimum()
        {
            var singleEventDuration = RewardAnimationQueueBuilder.DurationForQueue(1, 0.6f, 0.025f, 0.08f);
            var longQueueDuration = RewardAnimationQueueBuilder.DurationForQueue(20, 0.6f, 0.025f, 0.08f);
            var cappedQueueDuration = RewardAnimationQueueBuilder.DurationForQueue(100000, 0.6f, 0.025f, 0.08f);

            Assert.That(longQueueDuration, Is.LessThan(singleEventDuration));
            Assert.That(longQueueDuration, Is.GreaterThanOrEqualTo(0.025f));
            Assert.That(cappedQueueDuration, Is.EqualTo(0.025f).Within(0.0001f));
        }

        [Test]
        public void RewardAnimationQueueCoversOverlappingWinningPaylinesAndKeepsFinalScoreAuthoritative()
        {
            var grid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry }
            };
            var score = SlotScoring.Evaluate(grid, new RunModifiers(), 1);
            var events = RewardAnimationQueueBuilder.Build(score);
            var matchEventCount = 0;
            var multiplierEventCount = 0;
            var finalPayout = BigInteger.Zero;

            foreach (var animationEvent in events)
            {
                if (animationEvent.Kind == RewardAnimationEventKind.MatchAddition)
                {
                    matchEventCount++;
                }
                else
                {
                    multiplierEventCount++;
                    finalPayout += animationEvent.Win.FinalPayoutKurus;
                }
            }

            Assert.That(matchEventCount, Is.EqualTo(score.Wins.Count));
            Assert.That(multiplierEventCount, Is.EqualTo(score.Wins.Count));
            Assert.That(finalPayout, Is.EqualTo(score.PayoutKurus));
        }

        [Test]
        public void MoneyMultiplierAppliesAfterLineAndComboCalculation()
        {
            var grid = new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
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
                { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Cigarette, SymbolKind.Cigarette, SymbolKind.Cigarette }
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
                { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Cigarette, SymbolKind.Cigarette, SymbolKind.Cigarette }
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
        public void ClearingAThresholdAddsUnusedRollsToTheNextThreshold()
        {
            var engine = new SlotGameEngine(7);
            var state = engine.CreateNewRun();
            state.CashKurus = state.CurrentTargetKurus;
            state.RollsRemaining = 4;

            Assert.That(engine.TrySettleThreshold(state), Is.True);
            Assert.That(state.ThresholdLevel, Is.EqualTo(2));
            Assert.That(state.RollsRemaining, Is.EqualTo(GameBalance.BaseRolls + 4));
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
        public void PurchasingDirectionalMatchOffersAdvancesTheirTierAndTracksOwnership()
        {
            var engine = new SlotGameEngine(17);
            var state = engine.CreateNewRun();
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.HorizontalMatchMultiplier, BigInteger.Zero, "Horizontal Match Echo x2", "Test offer"));
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.True);
            Assert.That(state.Modifiers.HorizontalMatchCountMultiplier, Is.EqualTo(2));
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.HorizontalMatchMultiplier), Is.EqualTo(1));

            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.HorizontalMatchMultiplier, BigInteger.Zero, "Horizontal Match Echo x5", "Test offer"));
            Assert.That(engine.TryPurchase(state, 1, out message), Is.True);
            Assert.That(state.Modifiers.HorizontalMatchCountMultiplier, Is.EqualTo(5));
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.HorizontalMatchMultiplier), Is.EqualTo(2));
        }

        [Test]
        public void AuthoredConfigDrivesRunValuesAndUpgradesNeverChangeReelFaces()
        {
            var configuredReels = new[]
            {
                new[] { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                new[] { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                new[] { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar }
            };
            var config = new GameRulesConfig(configuredReels, 3, 7, 12, 3, 4, 6);
            var engine = new SlotGameEngine(23, config);
            var state = engine.CreateNewRun();
            var firstStripBeforeUpgrade = (SymbolKind[])config.ReelStripAt(0).Clone();

            Assert.That(state.RollsRemaining, Is.EqualTo(12));
            Assert.That(state.RemainingOrgans, Is.EqualTo(3));
            Assert.That(state.Modifiers.StrawberryValue, Is.EqualTo(3));
            Assert.That(state.Modifiers.DollarValue, Is.EqualTo(7));

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
                    Assert.That(spin.Grid[row, column], Is.EqualTo(SymbolKind.Dollar));
                }
            }

            for (int i = 0; i < 9; i++)
            {
                Assert.That(state.Modifiers.IncreaseBaseOutputMultiplier(), Is.True);
            }
            Assert.That(state.Modifiers.IncreaseBaseOutputMultiplier(), Is.False);
        }

        [Test]
        public void EachRunReceivesIndependentMutableReelStrips()
        {
            var engine = new SlotGameEngine(23);
            var firstRun = engine.CreateNewRun();
            var firstStrip = firstRun.Config.ReelStripAt(0);

            firstRun.Config.ReplaceSymbolOnStrip(0, 0, SymbolKind.Cigarette);
            firstStrip[1] = SymbolKind.Cigarette;

            Assert.That(firstRun.Config.ReelStripAt(0)[0], Is.EqualTo(SymbolKind.Cigarette));
            Assert.That(firstRun.Config.ReelStripAt(0)[1], Is.EqualTo(GameBalance.InitialReels[0][1]));

            var restarted = engine.CreateNewRun();

            Assert.That(restarted.Config, Is.Not.SameAs(firstRun.Config));
            Assert.That(restarted.Config.ReelStripAt(0), Is.EqualTo(GameBalance.InitialReels[0]));
        }

        [Test]
        public void SpinsUseTheCurrentRunsReelStrips()
        {
            var engine = new SlotGameEngine(29);
            var state = engine.CreateNewRun();
            for (var index = 0; index < GameBalance.ReelLength; index++)
            {
                state.Config.ReplaceSymbolOnStrip(0, index, SymbolKind.Cigarette);
            }

            state.RollsRemaining = 1;
            var result = engine.TrySpin(state, 1);

            Assert.That(result.Accepted, Is.True);
            for (var row = 0; row < GameBalance.GridRows; row++)
            {
                Assert.That(result.Grid[row, 0], Is.EqualTo(SymbolKind.Cigarette));
            }
        }

        [Test]
        public void StartingRollsSaturateInsteadOfOverflowingWithStackedTenfoldGambits()
        {
            var modifiers = new RunModifiers
            {
                BatchTenGambitCount = 9
            };

            Assert.That(modifiers.StartingRolls, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void PurchasingFreeSpinsCannotOverflowRemainingRolls()
        {
            var engine = new SlotGameEngine(31);
            var state = engine.CreateNewRun();
            state.RollsRemaining = int.MaxValue - 10;
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.FreeSpins, BigInteger.Zero, "Free Spins", "Test offer"));

            Assert.That(engine.TryPurchase(state, 0, out _), Is.True);
            Assert.That(state.RollsRemaining, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void AuthoredShopCopyIsUsedForGeneratedOffers()
        {
            var configs = new Dictionary<ShopOfferKind, ShopItemConfig>();
            foreach (ShopOfferKind kind in System.Enum.GetValues(typeof(ShopOfferKind)))
            {
                configs.Add(kind, new ShopItemConfig("Authored " + kind, "Description " + kind));
            }

            var config = new GameRulesConfig(
                GameBalance.InitialReels,
                1,
                5,
                GameBalance.BaseRolls,
                GameBalance.OrganCount,
                GameBalance.MaxThresholdLevel,
                GameBalance.FreeSpinBundle,
                configs);
            var state = new SlotGameEngine(31, config).CreateNewRun();
            ShopOffer authoredOffer = null;
            foreach (var offer in state.ShopOffers)
            {
                if (offer.Kind != ShopOfferKind.BaseOutputMultiplier &&
                    offer.Kind != ShopOfferKind.HorizontalMatchMultiplier &&
                    offer.Kind != ShopOfferKind.VerticalMatchMultiplier &&
                    offer.Kind != ShopOfferKind.CrissCrossMatchMultiplier)
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
        public void CanSettleThresholdBeforeRollsAreDepleted()
        {
            var engine = new SlotGameEngine(42);
            var state = engine.CreateNewRun();
            state.CashKurus = state.CurrentTargetKurus;
            state.RollsRemaining = 1;

            Assert.That(engine.TrySettleThreshold(state), Is.True);
            Assert.That(state.ThresholdLevel, Is.EqualTo(2));
            Assert.That(state.RollsRemaining, Is.EqualTo(GameBalance.BaseRolls + 1));
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
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Dollar).WinningPaylineCount, Is.EqualTo(1));
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Cigarette).WinningPaylineCount, Is.EqualTo(1));
            Assert.That(state.Stats.GetSymbolStats(SymbolKind.Kiss).WinningPaylineCount, Is.EqualTo(0));

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
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.DollarValue, new BigInteger(100), "Dollar", "Test offer"));
            state.CashKurus = BigInteger.Zero;
            string message;

            Assert.That(engine.TryPurchase(state, 0, out message), Is.False);
            Assert.That(state.OwnedUpgradeCount(ShopOfferKind.DollarValue), Is.EqualTo(0));
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
        public void TripleKissSpinCountsOneJackpotAndEveryWinningPayline()
        {
            var engine = new SlotGameEngine(1, CreateTripleKissGrid);
            var state = engine.CreateNewRun();
            state.RollsRemaining = 1;

            var result = engine.TrySpin(state, 1);
            var tripleKissStats = state.Stats.GetSymbolStats(SymbolKind.Kiss);

            Assert.That(result.Accepted, Is.True);
            Assert.That(state.Stats.JackpotsScored, Is.EqualTo(1));
            Assert.That(tripleKissStats.WinningPaylineCount, Is.EqualTo(result.Score.Wins.Count));
            Assert.That(tripleKissStats.GeneratedKurus, Is.EqualTo(result.Score.PayoutKurus));
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
                { SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Strawberry },
                { SymbolKind.Dollar, SymbolKind.Strawberry, SymbolKind.Dollar },
                { SymbolKind.Dollar, SymbolKind.Strawberry, SymbolKind.Dollar }
            };
        }

        private static SymbolKind[,] CreateThreeSymbolWinningGrid()
        {
            return new[,]
            {
                { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                { SymbolKind.Cigarette, SymbolKind.Cigarette, SymbolKind.Cigarette }
            };
        }

        private static SymbolKind[,] CreateWinningGridFor(PaylineGroup group)
        {
            switch (group)
            {
                case PaylineGroup.Horizontal:
                    return new[,]
                    {
                        { SymbolKind.Strawberry, SymbolKind.Strawberry, SymbolKind.Strawberry },
                        { SymbolKind.Dollar, SymbolKind.Dollar, SymbolKind.Dollar },
                        { SymbolKind.Cigarette, SymbolKind.Cigarette, SymbolKind.Cigarette }
                    };
                case PaylineGroup.Vertical:
                    return new[,]
                    {
                        { SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Cigarette },
                        { SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Cigarette },
                        { SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Cigarette }
                    };
                case PaylineGroup.CrissCross:
                    return new[,]
                    {
                        { SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Strawberry },
                        { SymbolKind.Dollar, SymbolKind.Strawberry, SymbolKind.Dollar },
                        { SymbolKind.Strawberry, SymbolKind.Dollar, SymbolKind.Strawberry }
                    };
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(group));
            }
        }

        private static SymbolKind[,] CreateTripleKissGrid()
        {
            return new[,]
            {
                { SymbolKind.Kiss, SymbolKind.Kiss, SymbolKind.Kiss },
                { SymbolKind.Kiss, SymbolKind.Kiss, SymbolKind.Kiss },
                { SymbolKind.Kiss, SymbolKind.Kiss, SymbolKind.Kiss }
            };
        }

        [Test]
        public void CustomizableShopItemConfigurationsAreRespected()
        {
            var configs = new Dictionary<ShopOfferKind, ShopItemConfig>
            {
                { ShopOfferKind.StrawberryValue, new ShopItemConfig("Strawberry Boost", "Add +5 value", symbolImprovementDelta: 5, costDivisor: 50) },
                { ShopOfferKind.BaseRollMultiplierX2, new ShopItemConfig("Roll Double", "Roll mult 4", baseRollMultiplierValue: 4, costDivisor: 5) }
            };

            var config = new GameRulesConfig(
                GameBalance.InitialReels,
                1,
                5,
                GameBalance.BaseRolls,
                GameBalance.OrganCount,
                GameBalance.MaxThresholdLevel,
                GameBalance.FreeSpinBundle,
                configs);

            var engine = new SlotGameEngine(42, config);
            var state = engine.CreateNewRun();

            // Clear shop and manually add the offers we want to test
            state.ShopOffers.Clear();
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.StrawberryValue, state.CurrentTargetKurus / 50, "Strawberry Boost", "Add +5 value"));
            state.ShopOffers.Add(new ShopOffer(ShopOfferKind.BaseRollMultiplierX2, state.CurrentTargetKurus / 5, "Roll Double", "Roll mult 4"));

            // 1. Purchase strawberry boost and check that delta is 5 instead of default 1
            state.CashKurus = state.CurrentTargetKurus;
            var initialStrawberryValue = state.Modifiers.SymbolValue(SymbolKind.Strawberry);
            Assert.That(engine.TryPurchase(state, 0, out _), Is.True);
            Assert.That(state.Modifiers.SymbolValue(SymbolKind.Strawberry), Is.EqualTo(initialStrawberryValue + 5));

            // 2. Purchase base roll multiplier and check that multiplier is 4 instead of default 2
            Assert.That(engine.TryPurchase(state, 1, out _), Is.True);
            Assert.That(state.Modifiers.BaseRollMultiplier, Is.EqualTo(4));
        }

        [Test]
        public void ShopItemsAreFilteredByDisplayThreshold()
        {
            // Set up shop items with different DisplayThreshold requirements:
            // - StrawberryValue: DisplayThreshold = ThresholdLevel.Threshold2
            // - DollarValue: DisplayThreshold = ThresholdLevel.Threshold1
            // - CigaretteValue: DisplayThreshold = ThresholdLevel.Any (always displayed)
            var configs = new Dictionary<ShopOfferKind, ShopItemConfig>
            {
                { ShopOfferKind.StrawberryValue, new ShopItemConfig("Strawberry Boost", "Description", displayThreshold: ThresholdLevel.Threshold2) },
                { ShopOfferKind.DollarValue, new ShopItemConfig("Dollar Boost", "Description", displayThreshold: ThresholdLevel.Threshold1) },
                { ShopOfferKind.CigaretteValue, new ShopItemConfig("Cigarette Boost", "Description", displayThreshold: ThresholdLevel.Any) }
            };

            var config = new GameRulesConfig(
                GameBalance.InitialReels,
                1,
                5,
                GameBalance.BaseRolls,
                GameBalance.OrganCount,
                GameBalance.MaxThresholdLevel,
                GameBalance.FreeSpinBundle,
                configs);

            var engine = new SlotGameEngine(42, config);
            var state = engine.CreateNewRun();

            // At ThresholdLevel = 1:
            // DollarValue (1) and CigaretteValue (0) are allowed. StrawberryValue (2) is forbidden.
            Assert.That(state.ThresholdLevel, Is.EqualTo(1));
            foreach (var offer in state.ShopOffers)
            {
                Assert.That(offer.Kind, Is.Not.EqualTo(ShopOfferKind.StrawberryValue));
            }

            // Settle threshold to advance to level 2:
            state.CashKurus = state.CurrentTargetKurus;
            state.RollsRemaining = 0;
            Assert.That(engine.TrySettleThreshold(state), Is.True);
            Assert.That(state.ThresholdLevel, Is.EqualTo(2));

            // At ThresholdLevel = 2:
            // StrawberryValue (2) and CigaretteValue (0) are allowed. DollarValue (1) is forbidden.
            foreach (var offer in state.ShopOffers)
            {
                Assert.That(offer.Kind, Is.Not.EqualTo(ShopOfferKind.DollarValue));
            }
        }

        [Test]
        public void GenerateShopDoublesOddsForOwnedUpgrades()
        {
            var config = GameRulesConfig.CreateDefault();
            var engine = new SlotGameEngine(42, config);
            var state = engine.CreateNewRun();

            // Settle threshold to level 1 so DollarValue and CigaretteValue are candidates
            // state.ThresholdLevel is already 1 by default
            state.RecordOwnedUpgrade(ShopOfferKind.CigaretteValue);

            int cigaretteCount = 0;
            int dollarCount = 0;

            for (int i = 0; i < 1000; i++)
            {
                state.AddRefreshTickets(1);
                string msg;
                Assert.That(engine.TryRefreshShop(state, out msg), Is.True);

                foreach (var offer in state.ShopOffers)
                {
                    if (offer.Kind == ShopOfferKind.CigaretteValue) cigaretteCount++;
                    if (offer.Kind == ShopOfferKind.DollarValue) dollarCount++;
                }
            }

            // Since CigaretteValue is owned, its weight is 2. DollarValue is unowned, its weight is 1.
            // Therefore, cigaretteCount should be significantly greater than dollarCount.
            // Typically around double. We assert that it's at least 1.3 times greater.
            Assert.That((double)cigaretteCount / dollarCount, Is.GreaterThan(1.3));
        }
    }
}
