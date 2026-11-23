using NUnit.Framework;
using SlotMachine.Core;

namespace SlotMachine.Tests
{
    /// <summary>
    /// Checks the exact odds, then plays hundreds of thousands of real spins to
    /// show the machine pays what the maths says it should.
    /// </summary>
    public class SlotMathTests
    {
        private SlotRules rules;
        private SlotEngine engine;

        [SetUp]
        public void SetUp()
        {
            rules = SlotRules.Default();
            engine = new SlotEngine(rules, new CreditBank(rules), new SystemRandomSource(1234));
        }

        [Test]
        public void HitRate_IsTheSumOfEachSymbolCubed()
        {
            // 0.4^3 + 0.3^3 + 0.2^3 + 0.1^3 = 0.1
            Assert.AreEqual(0.1, SlotMath.HitRate(engine.Strips, rules.Paytable), 1e-12);
        }

        [Test]
        public void ReturnToPlayer_IncludesRetriggeringFreeSpins()
        {
            // Base 0.064*5 + 0.027*10 + 0.008*25 + 0.001*50 = 0.84; free spins 0.027*3 = 0.081 per spin.
            double expected = 0.84 / (1 - 0.081);
            Assert.AreEqual(expected, SlotMath.ReturnToPlayer(engine.Strips, rules.Paytable), 1e-9);
        }

        [Test]
        public void ReturnAtMaxBet_AddsTheJackpotContributionOnTop()
        {
            // The 5000 seed pays exactly what the 777 line would (50 x 100), so
            // always betting max returns the ordinary RTP plus the 5% contribution.
            double expected = 0.84 / (1 - 0.081) + 0.05;
            Assert.AreEqual(expected, SlotMath.ReturnToPlayerAtMaxBet(engine.Strips, rules), 1e-9);
        }

        [Test]
        public void SimulatedMaxBetPlay_MatchesTheReturnIncludingTheJackpot()
        {
            // Jackpots are rare (1 in 1000) and large, so this needs more spins
            // and a looser tolerance than the base-game simulation.
            const int paidSpins = 1_000_000;
            engine.Bank.IncreaseBet(); engine.Bank.IncreaseBet(); // 100
            long wagered = 0, paid = 0;
            int spins = 0, jackpots = 0;

            while (spins < paidSpins || engine.HasFreeSpins)
            {
                if (engine.Bank.Credits < rules.MaxBet) engine.Bank.Deposit(10_000_000);
                Assert.IsTrue(engine.TrySpin(out var r));
                if (!r.IsFreeSpin) { wagered += r.Bet; spins++; }
                if (r.IsJackpot) jackpots++;
                paid += r.Payout;
            }

            Assert.Greater(jackpots, 0);
            double rtp = (double)paid / wagered;
            Assert.AreEqual(SlotMath.ReturnToPlayerAtMaxBet(engine.Strips, rules), rtp, 0.03, "max-bet return");
        }

        [Test]
        public void SimulatedPlay_MatchesTheoreticalHitRateAndReturn()
        {
            const int paidSpins = 300_000;
            long wagered = 0, paid = 0;
            int spins = 0, wins = 0;

            while (spins < paidSpins || engine.HasFreeSpins)
            {
                if (engine.Bank.Credits < rules.MinBet) engine.Bank.Deposit(1_000_000);
                Assert.IsTrue(engine.TrySpin(out var r));
                if (!r.IsFreeSpin) { wagered += r.Bet; spins++; if (r.IsWin) wins++; }
                paid += r.Payout;
            }

            double hitRate = (double)wins / spins;
            double rtp = (double)paid / wagered;
            Assert.AreEqual(SlotMath.HitRate(engine.Strips, rules.Paytable), hitRate, 0.004, "hit rate");
            Assert.AreEqual(SlotMath.ReturnToPlayer(engine.Strips, rules.Paytable), rtp, 0.03, "return to player");
        }

        [Test]
        public void SystemRandomSource_IsUniformAcrossStripPositions()
        {
            // Chi-square goodness of fit over 20 stops; 19 degrees of freedom,
            // 43.8 is the 0.1% critical value, so a fair generator stays well below.
            var random = new SystemRandomSource(99);
            const int samples = 200_000;
            var counts = new int[20];
            for (int i = 0; i < samples; i++) counts[random.Next(20)]++;

            double expected = samples / 20.0, chi2 = 0;
            foreach (var c in counts) chi2 += (c - expected) * (c - expected) / expected;
            Assert.Less(chi2, 43.8);
        }
    }
}
