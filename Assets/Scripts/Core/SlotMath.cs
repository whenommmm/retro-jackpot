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
    }
}
