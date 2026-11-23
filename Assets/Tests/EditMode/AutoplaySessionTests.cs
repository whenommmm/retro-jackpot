using System;
using NUnit.Framework;
using SlotMachine.Core;

namespace SlotMachine.Tests
{
    public class AutoplaySessionTests
    {
        [Test]
        public void PlaysTheChosenNumberOfSpinsThenFinishes()
        {
            var session = new AutoplaySession(spins: 3, startingCredits: 1000, lossLimit: 500);

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(session.TryBeginSpin(canAffordBet: true));
                session.RecordSpin(creditsNow: 1000 - 10 * (i + 1), hadFeature: false);
            }

            Assert.AreEqual(AutoplayStopReason.Finished, session.StopReason);
            Assert.AreEqual(0, session.SpinsRemaining);
            Assert.IsFalse(session.TryBeginSpin(canAffordBet: true));
        }

        [Test]
        public void StopsForAFeatureEvenWhenSpinsAreLeft()
        {
            var session = new AutoplaySession(10, 1000, 500);

            session.RecordSpin(1250, hadFeature: true);

            Assert.AreEqual(AutoplayStopReason.Feature, session.StopReason);
            Assert.AreEqual(9, session.SpinsRemaining);
        }

        [Test]
        public void StopsOnceTheBalanceIsDownByTheLossLimit()
        {
            var session = new AutoplaySession(10, 1000, 300);

            session.RecordSpin(800, false);
            Assert.IsTrue(session.IsRunning, "down 200: under the limit");

            session.RecordSpin(700, false);
            Assert.AreEqual(AutoplayStopReason.LossLimit, session.StopReason, "down 300: at the limit");
        }

        [Test]
        public void WinsCountTowardsTheLossLimit()
        {
            var session = new AutoplaySession(10, 1000, 300);

            session.RecordSpin(750, false);  // down 250
            session.RecordSpin(950, false);  // a win: down 50
            session.RecordSpin(800, false);  // down 200

            Assert.IsTrue(session.IsRunning);
            Assert.AreEqual(-200, session.NetResult(800));
        }

        [Test]
        public void StopsWhenTheBetCanNoLongerBePaid()
        {
            var session = new AutoplaySession(10, 1000, 500);

            Assert.IsFalse(session.TryBeginSpin(canAffordBet: false));
            Assert.AreEqual(AutoplayStopReason.CannotAffordBet, session.StopReason);
        }

        [Test]
        public void PlayerStop_LetsTheCurrentSpinFinishAndKeepsTheReason()
        {
            var session = new AutoplaySession(10, 1000, 500);
            session.TryBeginSpin(true);

            session.Stop();
            session.RecordSpin(2000, hadFeature: true); // the spin in progress lands a big win

            Assert.AreEqual(AutoplayStopReason.Player, session.StopReason);
            Assert.AreEqual(1, session.SpinsPlayed);
        }

        [Test]
        public void TheFeatureWinsOverTheOtherStopsOnTheSameSpin()
        {
            var session = new AutoplaySession(1, 1000, 100);

            session.RecordSpin(800, hadFeature: true); // last spin, over the loss limit, and a feature

            Assert.AreEqual(AutoplayStopReason.Feature, session.StopReason);
        }

        [Test]
        public void LossLimitFor_IsAPercentageOfTheBalanceAndAtLeastOneCredit()
        {
            Assert.AreEqual(500, AutoplaySession.LossLimitFor(1000, 50));
            Assert.AreEqual(1, AutoplaySession.LossLimitFor(1, 50));
            Assert.Throws<ArgumentOutOfRangeException>(() => AutoplaySession.LossLimitFor(1000, 0));
        }

        [Test]
        public void RejectsInvalidSettings()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AutoplaySession(0, 1000, 500));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AutoplaySession(10, -1, 500));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AutoplaySession(10, 1000, 0));
        }
    }
}
