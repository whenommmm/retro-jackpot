using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotMachine.Core
{
    /// <summary>
    /// Every tunable number of the machine in one immutable object, so balance
    /// changes never touch game logic. The Unity layer builds this from a
    /// designer-editable config asset.
    /// </summary>
    public sealed class SlotRules
    {
        public int ReelCount { get; }

        /// <summary>Copies of each symbol printed on every reel strip.</summary>
        public IReadOnlyDictionary<SymbolType, int> StripComposition { get; }

        public Paytable Paytable { get; }
        public int[] BetOptions { get; }
        public int StartingCredits { get; }

        /// <summary>Symbol that wins the progressive jackpot when it fills the line at max bet.</summary>
        public SymbolType JackpotSymbol { get; }

        /// <summary>Value the jackpot pool starts at and resets to after it is won.</summary>
        public int JackpotSeed { get; }

        /// <summary>Percentage of every paid bet added to the jackpot pool.</summary>
        public int JackpotContributionPercent { get; }

        /// <summary>Seed for the strip order; fixed so every session uses the same strips.</summary>
        public int StripSeed { get; }

        public int MinBet => BetOptions[0];
        public int MaxBet => BetOptions[BetOptions.Length - 1];

        public SlotRules(int reelCount, IReadOnlyDictionary<SymbolType, int> stripComposition, Paytable paytable,
            IEnumerable<int> betOptions, int startingCredits, SymbolType jackpotSymbol, int jackpotSeed,
            int jackpotContributionPercent, int stripSeed)
        {
            if (reelCount < 2) throw new ArgumentOutOfRangeException(nameof(reelCount), "Need at least two reels.");
            StripComposition = stripComposition ?? throw new ArgumentNullException(nameof(stripComposition));
            Paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));

            var bets = betOptions?.Distinct().OrderBy(b => b).ToArray() ?? throw new ArgumentNullException(nameof(betOptions));
            if (bets.Length == 0 || bets[0] <= 0) throw new ArgumentException("Bets must be positive.", nameof(betOptions));
            if (startingCredits < bets[0]) throw new ArgumentException("Starting credits must cover the minimum bet.", nameof(startingCredits));
            if (jackpotContributionPercent < 0 || jackpotContributionPercent > 100) throw new ArgumentOutOfRangeException(nameof(jackpotContributionPercent));
            foreach (var symbol in stripComposition.Keys)
                if (!paytable.Contains(symbol))
                    throw new ArgumentException("Symbol " + symbol + " is on the strip but missing from the paytable.", nameof(paytable));

            ReelCount = reelCount;
            BetOptions = bets;
            StartingCredits = startingCredits;
            JackpotSymbol = jackpotSymbol;
            JackpotSeed = jackpotSeed;
            JackpotContributionPercent = jackpotContributionPercent;
            StripSeed = stripSeed;
        }

        /// <summary>
        /// The shipped balance. Strips of 20: cheap symbols are printed often,
        /// rich ones rarely, so a hit happens about 1 spin in 10 and the
        /// expected return (before the jackpot) is about 91%.
        /// </summary>
        public static SlotRules Default() => new SlotRules(
            reelCount: 3,
            stripComposition: new Dictionary<SymbolType, int>
            {
                { SymbolType.Cherry, 8 },
                { SymbolType.Bell, 6 },
                { SymbolType.Bar, 4 },
                { SymbolType.Seven, 2 },
            },
            paytable: new Paytable(new[]
            {
                new PaytableEntry(SymbolType.Cherry, multiplier: 5),
                new PaytableEntry(SymbolType.Bell, multiplier: 10, freeSpins: 3),
                new PaytableEntry(SymbolType.Bar, multiplier: 25),
                new PaytableEntry(SymbolType.Seven, multiplier: 50),
            }),
            betOptions: new[] { 10, 50, 100 },
            startingCredits: 1000,
            jackpotSymbol: SymbolType.Seven,
            jackpotSeed: 1000,
            jackpotContributionPercent: 10,
            stripSeed: 7);
    }
}
