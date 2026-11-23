using NUnit.Framework;
using SlotMachine.Core;

namespace SlotMachine.Tests
{
    public class CreditBankTests
    {
        private SlotRules rules;
        private CreditBank bank;

        [SetUp]
        public void SetUp()
        {
            rules = SlotRules.Default();
            bank = new CreditBank(rules);
        }

        [Test]
        public void StartsWithStartingCreditsMinBetAndSeededJackpot()
        {
            Assert.AreEqual(1000, bank.Credits);
            Assert.AreEqual(10, bank.CurrentBet);
            Assert.AreEqual(1000, bank.JackpotPool);
        }

        [Test]
        public void TryPlaceBet_TakesTheBetAndFeedsTenPercentToTheJackpot()
        {
            bank.IncreaseBet(); // 50

            Assert.IsTrue(bank.TryPlaceBet(out int bet));
            Assert.AreEqual(50, bet);
            Assert.AreEqual(950, bank.Credits);
            Assert.AreEqual(1005, bank.JackpotPool);
        }

        [Test]
        public void TryPlaceBet_WhenBroke_FailsAndChangesNothing()
        {
            while (bank.Credits >= 10) bank.TryPlaceBet(out _);
            int credits = bank.Credits, pool = bank.JackpotPool;

            Assert.IsFalse(bank.TryPlaceBet(out _));
            Assert.AreEqual(credits, bank.Credits);
            Assert.AreEqual(pool, bank.JackpotPool);
        }

        [Test]
        public void BetSteps_StopAtTheSmallestAndLargestOption()
        {
            Assert.IsFalse(bank.DecreaseBet());
            Assert.IsTrue(bank.IncreaseBet());
            Assert.IsTrue(bank.IncreaseBet());
            Assert.IsFalse(bank.IncreaseBet());
            Assert.AreEqual(100, bank.CurrentBet);
            Assert.IsTrue(bank.IsMaxBet);
        }

        [Test]
        public void ClampBetToCredits_DropsToTheBiggestAffordableBet()
        {
            bank.IncreaseBet(); bank.IncreaseBet(); // 100
            while (bank.Credits > 60) bank.TryPlaceBet(out _); // 1000 -> 0 in 100s; refill partway below
            bank.Deposit(60);

            bank.ClampBetToCredits();

            Assert.AreEqual(50, bank.CurrentBet);
        }

        [Test]
        public void TakeJackpot_PaysThePoolAndReseedsIt()
        {
            bank.IncreaseBet(); bank.IncreaseBet();
            bank.TryPlaceBet(out _); // pool 1010

            Assert.AreEqual(1010, bank.TakeJackpot());
            Assert.AreEqual(1000, bank.JackpotPool);
        }

        [Test]
        public void Refill_RestoresTheStartingBalance()
        {
            while (bank.Credits >= 10) bank.TryPlaceBet(out _);

            bank.Refill();

            Assert.AreEqual(1000, bank.Credits);
        }

        [Test]
        public void Withdraw_RemovesCredits_AndRefusesToOverdraw()
        {
            bank.Withdraw(400);

            Assert.AreEqual(600, bank.Credits);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => bank.Withdraw(601));
            Assert.AreEqual(600, bank.Credits);
        }

        [Test]
        public void Deposit_RejectsNegativeAmounts()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => bank.Deposit(-1));
        }
    }
}
