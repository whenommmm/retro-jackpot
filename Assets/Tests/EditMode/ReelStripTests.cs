using System.Collections.Generic;
using NUnit.Framework;
using SlotMachine.Core;

namespace SlotMachine.Tests
{
    public class ReelStripTests
    {
        private static readonly Dictionary<SymbolType, int> Composition = new Dictionary<SymbolType, int>
        {
            { SymbolType.Cherry, 8 }, { SymbolType.Bell, 6 }, { SymbolType.Bar, 4 }, { SymbolType.Seven, 2 },
        };

        [Test]
        public void Build_PrintsExactlyTheRequestedCopiesOfEachSymbol()
        {
            var strip = ReelStrip.Build(Composition, seed: 1);

            Assert.AreEqual(20, strip.Length);
            foreach (var kv in Composition)
                Assert.AreEqual(kv.Value, strip.Count(kv.Key), kv.Key.ToString());
        }

        [Test]
        public void Build_NeverPlacesASymbolNextToItself_IncludingAcrossTheWrap([Range(0, 49)] int seed)
        {
            var strip = ReelStrip.Build(Composition, seed);

            for (int i = 0; i < strip.Length; i++)
                Assert.AreNotEqual(strip[i], strip[i + 1], "Symbols at " + i + " and " + (i + 1) + " match (seed " + seed + ")");
        }

        [Test]
        public void Build_IsDeterministicForTheSameSeed()
        {
            var a = ReelStrip.Build(Composition, seed: 42);
            var b = ReelStrip.Build(Composition, seed: 42);

            CollectionAssert.AreEqual(a.Symbols, b.Symbols);
        }

        [Test]
        public void Build_RejectsASymbolThatWouldFillMoreThanHalfTheStrip()
        {
            var crowded = new Dictionary<SymbolType, int> { { SymbolType.Cherry, 6 }, { SymbolType.Bell, 4 } };

            Assert.Throws<System.ArgumentException>(() => ReelStrip.Build(crowded, seed: 1));
        }

        [Test]
        public void Indexer_WrapsBothWaysAroundTheLoop()
        {
            var strip = new ReelStrip(new[] { SymbolType.Seven, SymbolType.Cherry, SymbolType.Bell });

            Assert.AreEqual(SymbolType.Seven, strip[3]);
            Assert.AreEqual(SymbolType.Bell, strip[-1]);
            Assert.AreEqual(SymbolType.Cherry, strip[-302]);
        }

        [Test]
        public void Probability_IsCopiesOverLength()
        {
            var strip = ReelStrip.Build(Composition, seed: 3);

            Assert.AreEqual(0.4, strip.Probability(SymbolType.Cherry), 1e-12);
            Assert.AreEqual(0.1, strip.Probability(SymbolType.Seven), 1e-12);
        }
    }
}
