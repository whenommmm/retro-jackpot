using System;
using System.Collections.Generic;
using SlotMachine.Core;

namespace SlotMachine.Tests
{
    /// <summary>A random source that returns a queued script, so tests can force exact outcomes.</summary>
    internal sealed class ScriptedRandom : IRandomSource
    {
        private readonly Queue<int> values = new Queue<int>();

        public ScriptedRandom(params int[] script)
        {
            foreach (var v in script) values.Enqueue(v);
        }

        public void Enqueue(params int[] script)
        {
            foreach (var v in script) values.Enqueue(v);
        }

        public int Next(int maxExclusive)
        {
            if (values.Count == 0) throw new InvalidOperationException("Scripted random ran out of values.");
            int v = values.Dequeue();
            if (v < 0 || v >= maxExclusive) throw new InvalidOperationException("Scripted value " + v + " outside [0," + maxExclusive + ").");
            return v;
        }
    }

    internal static class TestStrips
    {
        /// <summary>First stop index on a strip that shows the given symbol.</summary>
        public static int StopFor(ReelStrip strip, SymbolType symbol)
        {
            for (int i = 0; i < strip.Length; i++)
                if (strip[i] == symbol) return i;
            throw new ArgumentException("Symbol " + symbol + " is not on the strip.");
        }

        /// <summary>Stop indices that put the given symbols on the payline, reel by reel.</summary>
        public static int[] StopsFor(SlotEngine engine, params SymbolType[] symbols)
        {
            var stops = new int[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
                stops[i] = StopFor(engine.Strips[i], symbols[i]);
            return stops;
        }
    }
}
