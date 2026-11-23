#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using SlotMachine.Core;

namespace SlotMachine.Gameplay.QA
{
    /// <summary>
    /// QA-only random source. It normally passes straight through to the real
    /// generator, but a tester can queue the stop positions of the next spin
    /// to check rare outcomes (jackpot, free spins, big wins) on demand.
    ///
    /// Compiled only into the Editor and development builds: release builds
    /// contain no way to influence an outcome.
    /// </summary>
    public sealed class QaRandomSource : IRandomSource
    {
        private readonly IRandomSource inner;
        private readonly Queue<int> forced = new Queue<int>();

        public QaRandomSource(IRandomSource inner) => this.inner = inner;

        /// <summary>Queues stop indices that put <paramref name="line"/> on the payline next spin.</summary>
        public void ForceLine(IReadOnlyList<ReelStrip> strips, params SymbolType[] line)
        {
            forced.Clear();
            for (int reel = 0; reel < strips.Count; reel++)
            {
                var strip = strips[reel];
                for (int i = 0; i < strip.Length; i++)
                {
                    if (strip[i] != line[reel]) continue;
                    forced.Enqueue(i);
                    break;
                }
            }
        }

        public int Next(int maxExclusive) =>
            forced.Count > 0 ? forced.Dequeue() % maxExclusive : inner.Next(maxExclusive);
    }
}
#endif
