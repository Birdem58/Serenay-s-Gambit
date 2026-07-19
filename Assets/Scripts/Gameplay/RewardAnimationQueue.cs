using System;
using System.Collections.Generic;
using System.Numerics;

namespace SerenaysGambit
{
    public enum RewardAnimationEventKind
    {
        MatchAddition,
        Multiplier
    }

    public sealed class RewardAnimationCellReward
    {
        public RewardAnimationCellReward(GridPosition position, BigInteger amountKurus)
        {
            Position = position;
            AmountKurus = amountKurus;
        }

        public GridPosition Position { get; private set; }
        public BigInteger AmountKurus { get; private set; }
    }

    public sealed class RewardAnimationEvent
    {
        public RewardAnimationEvent(
            RewardAnimationEventKind kind,
            PaylineWin win,
            int hitIndex,
            int totalHits,
            BigInteger baseAmountKurus,
            IReadOnlyList<RewardAnimationCellReward> cellRewards)
        {
            if (win == null)
            {
                throw new ArgumentNullException(nameof(win));
            }

            Kind = kind;
            Win = win;
            HitIndex = hitIndex;
            TotalHits = totalHits;
            BaseAmountKurus = baseAmountKurus;
            CellRewards = cellRewards ?? throw new ArgumentNullException(nameof(cellRewards));
        }

        public RewardAnimationEventKind Kind { get; private set; }
        public PaylineWin Win { get; private set; }
        public int HitIndex { get; private set; }
        public int TotalHits { get; private set; }
        public BigInteger BaseAmountKurus { get; private set; }
        public IReadOnlyList<RewardAnimationCellReward> CellRewards { get; private set; }
    }

    public static class RewardAnimationQueueBuilder
    {
        public static List<RewardAnimationEvent> Build(ScoredSpin score)
        {
            return Build(score, int.MaxValue);
        }

        public static List<RewardAnimationEvent> Build(ScoredSpin score, int maxVisibleMatchEvents)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            var safeMaxVisibleMatchEvents = Math.Max(1, maxVisibleMatchEvents);
            var events = new List<RewardAnimationEvent>();
            if (score.Wins == null)
            {
                return events;
            }

            for (var winIndex = 0; winIndex < score.Wins.Count; winIndex++)
            {
                var win = score.Wins[winIndex];
                var positions = win.Payline.Positions;
                var totalHits = score.RewardAnimationCount(win);
                var visibleHitCount = Math.Min(totalHits, safeMaxVisibleMatchEvents);
                var totalMatchAmount = win.LinePayoutKurus * totalHits;
                var visibleAmount = totalMatchAmount / visibleHitCount;
                var visibleRemainder = totalMatchAmount % visibleHitCount;

                for (var visibleHitIndex = 0; visibleHitIndex < visibleHitCount; visibleHitIndex++)
                {
                    var eventAmount = visibleAmount;
                    if (visibleHitIndex == visibleHitCount - 1)
                    {
                        eventAmount += visibleRemainder;
                    }

                    var cellAmounts = SplitAcrossCells(eventAmount, positions.Length);
                    var cellRewards = new List<RewardAnimationCellReward>(positions.Length);
                    for (var cellIndex = 0; cellIndex < positions.Length; cellIndex++)
                    {
                        cellRewards.Add(new RewardAnimationCellReward(positions[cellIndex], cellAmounts[cellIndex]));
                    }

                    var representativeHitIndex = totalHits == visibleHitCount
                        ? visibleHitIndex
                        : (int)Math.Min(
                            totalHits - 1L,
                            ((long)visibleHitIndex * totalHits) / visibleHitCount);

                    events.Add(new RewardAnimationEvent(
                        RewardAnimationEventKind.MatchAddition,
                        win,
                        representativeHitIndex,
                        totalHits,
                        eventAmount,
                        cellRewards));
                }

                events.Add(new RewardAnimationEvent(
                    RewardAnimationEventKind.Multiplier,
                    win,
                    totalHits,
                    totalHits,
                    totalMatchAmount,
                    new List<RewardAnimationCellReward>()));
            }

            return events;
        }

        public static int MaxVisibleMatchEventsForBatch(int batchFactor)
        {
            if (batchFactor <= 10)
            {
                return int.MaxValue;
            }

            if (batchFactor <= 100)
            {
                return 25;
            }

            if (batchFactor <= 1000)
            {
                return 10;
            }

            return 5;
        }

        public static int QueueLengthForSpeed(ScoredSpin score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            long queueLength = 0;
            if (score.Wins != null)
            {
                for (var winIndex = 0; winIndex < score.Wins.Count; winIndex++)
                {
                    var win = score.Wins[winIndex];
                    if (win == null)
                    {
                        continue;
                    }

                    queueLength += (long)score.RewardAnimationCount(win) + 1L;
                    if (queueLength >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }
            }

            return (int)Math.Max(1L, queueLength);
        }

        public static BigInteger[] SplitAcrossCells(BigInteger amountKurus, int cellCount)
        {
            if (cellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cellCount));
            }

            var result = new BigInteger[cellCount];
            var share = amountKurus / cellCount;
            var remainder = amountKurus % cellCount;
            for (var index = 0; index < cellCount; index++)
            {
                result[index] = share;
            }

            // Keeping the remainder on the final cell makes the visible cells add back to
            // the exact payline amount without introducing floating-point money values.
            result[cellCount - 1] += remainder;
            return result;
        }

        public static float DurationForQueue(
            int queueLength,
            float baseDuration,
            float minimumDuration,
            float speedupPerEvent)
        {
            if (queueLength < 1)
            {
                queueLength = 1;
            }

            var safeBaseDuration = Math.Max(0.0, (double)baseDuration);
            var safeMinimumDuration = Math.Max(0.0, (double)minimumDuration);
            var safeSpeedup = Math.Max(0.0, (double)speedupPerEvent);
            var compressedDuration = safeBaseDuration / (1f + queueLength * safeSpeedup);
            return (float)Math.Max(safeMinimumDuration, compressedDuration);
        }
    }
}
