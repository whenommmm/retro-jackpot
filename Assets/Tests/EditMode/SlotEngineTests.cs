using NUnit.Framework;
using SlotMachine.Core;

namespace SlotMachine.Tests
{
    public class SlotEngineTests
    {
        private SlotRules rules;
        private CreditBank bank;
        private ScriptedRandom random;
        private SlotEngine engine;

        [SetUp]
        public void SetUp()
        {
            rules = SlotRules.Default();
            bank = new CreditBank(rules);
            random = new ScriptedRandom();
            engine = new SlotEngine(rules, bank, random);
        }

        private SpinResult Spin(params SymbolType[] symbols)
        {
            random.Enqueue(TestStrips.StopsFor(engine, symbols));
            Assert.IsTrue(engine.TrySpin(out var result));
            return result;
        }

        [Test]
        public void ThreeCherries_PayFiveTimesTheBet()
        {
            var result = Spin(SymbolType.Cherry, SymbolType.Cherry, SymbolType.Cherry);

            Assert.IsTrue(result.IsWin);
            Assert.AreEqual(50, result.Payout);
            Assert.AreEqual(990, result.CreditsAfterBet);
            Assert.AreEqual(1040, result.CreditsAfterPayout);
            Assert.AreEqual(1040, bank.Credits);
        }

        [Test]
        public void MixedSymbols_LoseTheBet()
        {
            var result = Spin(SymbolType.Cherry, SymbolType.Bell, SymbolType.Cherry);

            Assert.IsFalse(result.IsWin);
            Assert.AreEqual(0, result.Payout);
            Assert.AreEqual(990, bank.Credits);
        }

        [Test]
        public void ResultSymbols_MatchTheStripsAtTheChosenStops()
        {
            var result = Spin(SymbolType.Bar, SymbolType.Seven, SymbolType.Bell);

            for (int i = 0; i < 3; i++)
                Assert.AreEqual(engine.Strips[i][result.StopIndices[i]], result.Symbols[i]);
        }

        [Test]
        public void ThreeSevensAtMaxBet_WinTheWholeJackpotPoolAndReseedIt()
        {
            bank.IncreaseBet(); bank.IncreaseBet(); // 100

            var result = Spin(SymbolType.Seven, SymbolType.Seven, SymbolType.Seven);

            Assert.IsTrue(result.IsJackpot);
            Assert.AreEqual(1010, result.Payout); // seed 1000 + 10% of this bet
            Assert.AreEqual(1000, result.JackpotPoolAfter);
        }

        [Test]
        public void ThreeSevensBelowMaxBet_PayTheirMultiplierInstead()
        {
            bank.IncreaseBet(); // 50

            var result = Spin(SymbolType.Seven, SymbolType.Seven, SymbolType.Seven);

            Assert.IsFalse(result.IsJackpot);
            Assert.AreEqual(2500, result.Payout);
            Assert.AreEqual(1005, bank.JackpotPool);
        }

        [Test]
        public void ThreeBells_AwardThreeFreeSpinsAtTheTriggeringBet()
        {
            bank.IncreaseBet(); // 50

            var result = Spin(SymbolType.Bell, SymbolType.Bell, SymbolType.Bell);

            Assert.AreEqual(500, result.Payout);
            Assert.AreEqual(3, result.FreeSpinsAwarded);
            Assert.AreEqual(3, engine.FreeSpinsRemaining);
            Assert.AreEqual(50, engine.FreeSpinBet);
        }

        [Test]
        public void FreeSpins_DoNotChargeOrFeedTheJackpot_AndPayAtTheLockedBet()
        {
            bank.IncreaseBet(); // 50
            Spin(SymbolType.Bell, SymbolType.Bell, SymbolType.Bell);
            bank.DecreaseBet(); // changing the bet mid-feature must not matter
            int credits = bank.Credits, pool = bank.JackpotPool;

            var free = Spin(SymbolType.Cherry, SymbolType.Cherry, SymbolType.Cherry);

            Assert.IsTrue(free.IsFreeSpin);
            Assert.AreEqual(50, free.Bet);
            Assert.AreEqual(credits, free.CreditsAfterBet);
            Assert.AreEqual(credits + 250, bank.Credits);
            Assert.AreEqual(pool, bank.JackpotPool);
            Assert.AreEqual(2, engine.FreeSpinsRemaining);
        }

        [Test]
        public void BellsDuringFreeSpins_Retrigger_KeepingTheOriginalBet()
        {
            Spin(SymbolType.Bell, SymbolType.Bell, SymbolType.Bell); // 3 free at 10
            bank.IncreaseBet();

            var retrigger = Spin(SymbolType.Bell, SymbolType.Bell, SymbolType.Bell);

            Assert.IsTrue(retrigger.IsFreeSpin);
            Assert.AreEqual(5, engine.FreeSpinsRemaining); // 3 - 1 + 3
            Assert.AreEqual(10, engine.FreeSpinBet);
        }

        [Test]
        public void WhenTheBetIsUnaffordable_TrySpinFailsAndChangesNothing()
        {
            var poor = new CreditBank(rules);
            while (poor.Credits >= 10) poor.TryPlaceBet(out _);
            var broke = new SlotEngine(rules, poor, new ScriptedRandom(0, 0, 0));
            int credits = poor.Credits;

            Assert.IsFalse(broke.TrySpin(out var result));
            Assert.IsNull(result);
            Assert.AreEqual(credits, poor.Credits);
        }

        [Test]
        public void EachReelHasItsOwnStripOrderWithTheSameSymbolCounts()
        {
            CollectionAssert.AreNotEqual(engine.Strips[0].Symbols, engine.Strips[1].Symbols);
            foreach (var strip in engine.Strips)
                Assert.AreEqual(2, strip.Count(SymbolType.Seven));
        }

        [TestCase(new[] { SymbolType.Bar, SymbolType.Bar, SymbolType.Bar }, true)]
        [TestCase(new[] { SymbolType.Bar, SymbolType.Bar, SymbolType.Seven }, false)]
        [TestCase(new[] { SymbolType.Seven, SymbolType.Bar, SymbolType.Bar }, false)]
        public void AllMatch_IsTheWinRule(SymbolType[] line, bool expected)
        {
            Assert.AreEqual(expected, SlotEngine.AllMatch(line));
        }
    }
}
