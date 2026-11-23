using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotMachine.Core
{
    /// <summary>What lining up three of one symbol pays.</summary>
    public sealed class PaytableEntry
    {
        public SymbolType Symbol { get; }

        /// <summary>Payout for three of a kind, as a multiple of the bet.</summary>
        public int Multiplier { get; }

        /// <summary>Free spins awarded on top of the payout (0 for most symbols).</summary>
        public int FreeSpins { get; }

        public PaytableEntry(SymbolType symbol, int multiplier, int freeSpins = 0)
        {
            if (multiplier <= 0) throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be positive.");
            if (freeSpins < 0) throw new ArgumentOutOfRangeException(nameof(freeSpins));
            Symbol = symbol;
            Multiplier = multiplier;
            FreeSpins = freeSpins;
        }
    }

    /// <summary>The payout for every winning line.</summary>
    public sealed class Paytable
    {
        private readonly Dictionary<SymbolType, PaytableEntry> entries;

        public IEnumerable<PaytableEntry> Entries => entries.Values.OrderByDescending(e => e.Multiplier);

        public Paytable(IEnumerable<PaytableEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            this.entries = new Dictionary<SymbolType, PaytableEntry>();
            foreach (var entry in entries)
            {
                if (this.entries.ContainsKey(entry.Symbol))
                    throw new ArgumentException("Duplicate paytable entry for " + entry.Symbol, nameof(entries));
                this.entries.Add(entry.Symbol, entry);
            }
            if (this.entries.Count == 0)
                throw new ArgumentException("A paytable needs at least one entry.", nameof(entries));
        }

        public bool Contains(SymbolType symbol) => entries.ContainsKey(symbol);

        public PaytableEntry Get(SymbolType symbol) =>
            entries.TryGetValue(symbol, out var entry)
                ? entry
                : throw new KeyNotFoundException("No paytable entry for " + symbol);
    }
}
