using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotMachine.Core
{
    /// <summary>
    /// Exact odds and expected return derived from the strips and paytable.
    /// Used by the paytable screen, the README and the tests that check the
    /// simulated machine against theory.
    /// </summary>
    public static class SlotMath
    {
        /// <summary>Probability that every reel stops on <paramref name="symbol"/>.</summary>
        public static double ProbabilityOfLine(IReadOnlyList<ReelStrip> strips, SymbolType symbol)
        {
            double p = 1.0;
            foreach (var strip in strips) p *= strip.Probability(symbol);
            return p;
        }

        /// <summary>Probability that a spin is any winning line.</summary>
        public static double HitRate(IReadOnlyList<ReelStrip> strips, Paytable paytable) =>
            paytable.Entries.Sum(e => ProbabilityOfLine(strips, e.Symbol));

        /// <summary>Expected free spins awarded per spin.</summary>
        public static double FreeSpinsPerSpin(IReadOnlyList<ReelStrip> strips, Paytable paytable) =>
            paytable.Entries.Sum(e => ProbabilityOfLine(strips, e.Symbol) * e.FreeSpins);

        /// <summary>Expected payout per credit bet on a single spin (fixed multipliers only).</summary>
        public static double BaseReturn(IReadOnlyList<ReelStrip> strips, Paytable paytable) =>
            paytable.Entries.Sum(e => ProbabilityOfLine(strips, e.Symbol) * e.Multiplier);

        /// <summary>
        /// Long-run return per credit bet including free spins (which can
        /// retrigger, so each paid spin is worth 1 + f + f² + ... spins).
        /// Excludes the progressive jackpot, which is funded by the bets
        /// themselves and only paid at max bet.
        /// </summary>
        public static double ReturnToPlayer(IReadOnlyList<ReelStrip> strips, Paytable paytable)
        {
            double f = FreeSpinsPerSpin(strips, paytable);
            if (f >= 1.0) throw new InvalidOperationException("Free spins would never end (expected retriggers >= 1).");
            return BaseReturn(strips, paytable) / (1.0 - f);
        }

        /// <summary>
        /// Long-run return per credit for a player who always bets max, so
        /// every jackpot line wins the progressive pool instead of its
        /// multiplier. Over many jackpots each win pays the seed plus whatever
        /// the paid spins fed in since the last one, so the pool returns the
        /// seed on every hit plus the contribution percentage of every bet.
        /// Assumes the seed is at least the line win it replaces.
        /// </summary>
        public static double ReturnToPlayerAtMaxBet(IReadOnlyList<ReelStrip> strips, SlotRules rules)
        {
            var paytable = rules.Paytable;
            double f = FreeSpinsPerSpin(strips, paytable);
            if (f >= 1.0) throw new InvalidOperationException("Free spins would never end (expected retriggers >= 1).");

            double p = ProbabilityOfLine(strips, rules.JackpotSymbol);
            double perSpin = BaseReturn(strips, paytable)
                             - p * paytable.Get(rules.JackpotSymbol).Multiplier
                             + p * rules.JackpotSeed / rules.MaxBet;
            // Free spins pay like spins but feed nothing into the pool.
            return perSpin / (1.0 - f) + rules.JackpotContributionPercent / 100.0;
        }
    }
}
