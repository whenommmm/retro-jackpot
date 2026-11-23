using System;
using System.Collections.Generic;

namespace SlotMachine.Core
{
    /// <summary>
    /// The rules of a spin: take the bet (unless it is a free spin), pick a stop
    /// position on every reel strip, decide the win and pay it. Pure C# with no
    /// Unity dependency, so the maths is unit-tested and the presentation layer
    /// can never alter an outcome.
    /// </summary>
    public sealed class SlotEngine
    {
        private readonly IRandomSource random;
        private readonly ReelStrip[] strips;

        public SlotRules Rules { get; }
        public CreditBank Bank { get; }
        public IReadOnlyList<ReelStrip> Strips => strips;

        /// <summary>Free spins queued for the player.</summary>
        public int FreeSpinsRemaining { get; private set; }

        /// <summary>The bet locked in when free spins were triggered; free spins play at this stake.</summary>
        public int FreeSpinBet { get; private set; }

        public bool HasFreeSpins => FreeSpinsRemaining > 0;

        public SlotEngine(SlotRules rules, CreditBank bank, IRandomSource random)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Bank = bank ?? throw new ArgumentNullException(nameof(bank));
            this.random = random ?? throw new ArgumentNullException(nameof(random));

            // Each reel gets its own fixed strip order (same symbol counts,
            // different arrangement), like a real three-reel machine.
            strips = new ReelStrip[rules.ReelCount];
            for (int i = 0; i < strips.Length; i++)
                strips[i] = ReelStrip.Build(rules.StripComposition, rules.StripSeed + i * 7919);
        }

        /// <summary>
        /// Plays one spin. A queued free spin is used first; otherwise the
        /// current bet is taken. Returns false, changing nothing, when the
        /// player can't afford a paid spin.
        /// </summary>
        public bool TrySpin(out SpinResult result)
        {
            result = null;

            bool isFreeSpin = FreeSpinsRemaining > 0;
            int bet;
            if (isFreeSpin)
            {
                bet = FreeSpinBet;
                FreeSpinsRemaining--;
            }
            else if (!Bank.TryPlaceBet(out bet))
            {
                return false;
            }
            int creditsAfterBet = Bank.Credits;

            // The outcome: one uniform stop position per reel.
            var stops = new int[strips.Length];
            var symbols = new SymbolType[strips.Length];
            for (int i = 0; i < strips.Length; i++)
            {
                stops[i] = random.Next(strips[i].Length);
                symbols[i] = strips[i][stops[i]];
            }

            bool isWin = AllMatch(symbols);
            bool isJackpot = false;
            int payout = 0;
            int freeSpinsAwarded = 0;

            if (isWin)
            {
                var entry = Rules.Paytable.Get(symbols[0]);

                // Jackpot symbols at max bet win the progressive pool; at any
                // lower stake they pay their ordinary multiplier.
                if (symbols[0] == Rules.JackpotSymbol && bet == Rules.MaxBet)
                {
                    isJackpot = true;
                    payout = Bank.TakeJackpot();
                }
                else
                {
                    payout = bet * entry.Multiplier;
                }
                Bank.Deposit(payout);

                freeSpinsAwarded = entry.FreeSpins;
                if (freeSpinsAwarded > 0)
                {
                    // A fresh trigger locks the stake; a retrigger keeps it.
                    if (!isFreeSpin) FreeSpinBet = bet;
                    FreeSpinsRemaining += freeSpinsAwarded;
                }
            }

            result = new SpinResult(stops, symbols, bet, isFreeSpin, isWin, isJackpot, payout,
                freeSpinsAwarded, FreeSpinsRemaining, creditsAfterBet, Bank.Credits, Bank.JackpotPool);
            return true;
        }

        /// <summary>The win rule from the brief: every reel shows the same symbol on the payline.</summary>
        public static bool AllMatch(IReadOnlyList<SymbolType> symbols)
        {
            if (symbols == null || symbols.Count == 0) return false;
            for (int i = 1; i < symbols.Count; i++)
                if (symbols[i] != symbols[0]) return false;
            return true;
        }
    }
}
