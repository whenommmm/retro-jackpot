using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotMachine.Core
{
    /// <summary>
    /// One reel's looping band of symbols, like the printed strip on a physical
    /// reel. The random generator picks a stop position on the strip, so a
    /// symbol's odds are simply how often it is printed, and the symbols seen
    /// above and below the payline are always the strip's real neighbours.
    /// </summary>
    public sealed class ReelStrip
    {
        private readonly SymbolType[] symbols;

        public int Length => symbols.Length;

        /// <summary>Symbol at any position; indices wrap around the loop in both directions.</summary>
        public SymbolType this[int index] => symbols[Wrap(index)];

        public ReelStrip(IEnumerable<SymbolType> symbols)
        {
            this.symbols = symbols?.ToArray() ?? throw new ArgumentNullException(nameof(symbols));
            if (this.symbols.Length < 3)
                throw new ArgumentException("A reel strip needs at least three symbols.", nameof(symbols));
        }

        public int Wrap(int index)
        {
            int m = index % symbols.Length;
            return m < 0 ? m + symbols.Length : m;
        }

        public int Count(SymbolType symbol)
        {
            int n = 0;
            foreach (var s in symbols)
                if (s == symbol) n++;
            return n;
        }

        /// <summary>Chance that a uniformly random stop lands this symbol on the payline.</summary>
        public double Probability(SymbolType symbol) => (double)Count(symbol) / symbols.Length;

        public IReadOnlyList<SymbolType> Symbols => symbols;

        /// <summary>
        /// Builds a strip with exactly <paramref name="composition"/> copies of each
        /// symbol, ordered so no symbol sits next to itself (including across the
        /// wrap point). The order is deterministic for a given seed, so every
        /// session plays on the same physical strips.
        /// </summary>
        public static ReelStrip Build(IReadOnlyDictionary<SymbolType, int> composition, int seed)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            var remaining = composition.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            int total = remaining.Values.Sum();
            if (remaining.Values.Max() * 2 > total)
                throw new ArgumentException("No symbol may fill more than half the strip, or it would have to touch itself.", nameof(composition));

            var random = new Random(seed);
            var strip = new List<SymbolType>(total);
            SymbolType? previous = null;

            // Greedy: always place the most plentiful symbol that differs from the
            // previous one; ties are broken by the seeded generator for variety.
            while (strip.Count < total)
            {
                var candidates = remaining
                    .Where(kv => kv.Value > 0 && (!previous.HasValue || kv.Key != previous.Value))
                    .ToList();
                int best = candidates.Max(kv => kv.Value);
                var top = candidates.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
                var pick = top[random.Next(top.Count)];
                strip.Add(pick);
                remaining[pick]--;
                previous = pick;
            }

            // Fix a clash across the wrap point by swapping the last symbol with
            // the first position where the swap keeps both sides clean.
            int last = strip.Count - 1;
            if (strip[last] == strip[0])
            {
                for (int i = 1; i < last - 1; i++)
                {
                    if (strip[i] != strip[last] && strip[i - 1] != strip[last] && strip[i + 1] != strip[last]
                        && strip[last - 1] != strip[i] && strip[0] != strip[i])
                    {
                        (strip[i], strip[last]) = (strip[last], strip[i]);
                        break;
                    }
                }
            }

            return new ReelStrip(strip);
        }
    }
}
