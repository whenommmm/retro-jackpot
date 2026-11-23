using System;

namespace SlotMachine.Core
{
    /// <summary>Why an autoplay run ended.</summary>
    public enum AutoplayStopReason
    {
        /// <summary>Still running.</summary>
        None,
        /// <summary>Every chosen spin was played.</summary>
        Finished,
        /// <summary>A big win, the jackpot or free spins: worth the player's full attention.</summary>
        Feature,
        /// <summary>The player is down by the loss limit since autoplay started.</summary>
        LossLimit,
        /// <summary>The current bet can no longer be paid.</summary>
        CannotAffordBet,
        /// <summary>The player pressed stop.</summary>
        Player
    }

    /// <summary>
    /// One run of autoplay: a fixed number of paid spins with automatic stops,
    /// so the player never misses a big moment and never loses more than they
    /// agreed to. It holds no game rules and touches no credits; the
    /// controller reports each spin and asks whether to keep going.
    /// </summary>
    public sealed class AutoplaySession
    {
        /// <param name="spins">Paid spins to play. Free spins they trigger don't count.</param>
        /// <param name="startingCredits">The balance when autoplay started.</param>
        /// <param name="lossLimit">Stop once the balance has dropped this many credits below the start.</param>
        public AutoplaySession(int spins, int startingCredits, int lossLimit)
        {
            if (spins <= 0) throw new ArgumentOutOfRangeException(nameof(spins));
            if (startingCredits < 0) throw new ArgumentOutOfRangeException(nameof(startingCredits));
            if (lossLimit <= 0) throw new ArgumentOutOfRangeException(nameof(lossLimit));
            TotalSpins = spins;
            StartingCredits = startingCredits;
            LossLimit = lossLimit;
        }

        public int TotalSpins { get; }
        public int SpinsPlayed { get; private set; }
        public int SpinsRemaining => TotalSpins - SpinsPlayed;
        public int StartingCredits { get; }
        public int LossLimit { get; }
        public AutoplayStopReason StopReason { get; private set; }
        public bool IsRunning => StopReason == AutoplayStopReason.None;

        /// <summary>
        /// Asked before each spin. Returns false, and ends the run, when the
        /// run is over or the bet can't be paid.
        /// </summary>
        public bool TryBeginSpin(bool canAffordBet)
        {
            if (!IsRunning) return false;
            if (canAffordBet) return true;
            StopReason = AutoplayStopReason.CannotAffordBet;
            return false;
        }

        /// <summary>
        /// Reports a finished spin (including any free spins it triggered).
        /// Checks the stop conditions in order of importance: a feature first,
        /// so the player sees why it stopped, then the loss limit, then the count.
        /// A stop the player already asked for is kept as it is.
        /// </summary>
        public void RecordSpin(int creditsNow, bool hadFeature)
        {
            if (SpinsPlayed < TotalSpins) SpinsPlayed++;
            if (!IsRunning) return;

            if (hadFeature) StopReason = AutoplayStopReason.Feature;
            else if (StartingCredits - creditsNow >= LossLimit) StopReason = AutoplayStopReason.LossLimit;
            else if (SpinsRemaining == 0) StopReason = AutoplayStopReason.Finished;
        }

        /// <summary>The player asked to stop; the spin in progress still finishes.</summary>
        public void Stop()
        {
            if (IsRunning) StopReason = AutoplayStopReason.Player;
        }

        /// <summary>Credits won (positive) or lost (negative) since autoplay started.</summary>
        public int NetResult(int creditsNow) => creditsNow - StartingCredits;

        /// <summary>The loss limit for a balance: a percentage of it, never less than one credit.</summary>
        public static int LossLimitFor(int credits, int percent)
        {
            if (percent <= 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            return Math.Max(1, credits * percent / 100);
        }
    }
}
