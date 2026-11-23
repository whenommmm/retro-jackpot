using System;

namespace SlotMachine.Core
{
    /// <summary>
    /// The player's wallet, the selected bet and the progressive jackpot pool.
    /// All money moves through here, so the balance can never go negative.
    /// </summary>
    public sealed class CreditBank
    {
        private readonly SlotRules rules;

        // Contributions are a percentage of whole-credit bets, so they can be
        // fractional (5% of 10 is 0.5). The leftover hundredths are carried
        // here and added to the pool once they make a whole credit.
        private int jackpotHundredths;

        public int Credits { get; private set; }
        public int BetIndex { get; private set; }
        public int JackpotPool { get; private set; }

        public int CurrentBet => rules.BetOptions[BetIndex];
        public bool IsMaxBet => BetIndex == rules.BetOptions.Length - 1;
        public bool CanDecreaseBet => BetIndex > 0;
        public bool CanIncreaseBet => BetIndex < rules.BetOptions.Length - 1;
        public bool CanAffordCurrentBet => Credits >= CurrentBet;
        public bool CanAffordAnyBet => Credits >= rules.MinBet;

        public CreditBank(SlotRules rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Credits = rules.StartingCredits;
            JackpotPool = rules.JackpotSeed;
        }

        public bool IncreaseBet()
        {
            if (!CanIncreaseBet) return false;
            BetIndex++;
            return true;
        }

        public bool DecreaseBet()
        {
            if (!CanDecreaseBet) return false;
            BetIndex--;
            return true;
        }

        /// <summary>Drops the bet to the highest option the player can still afford.</summary>
        public void ClampBetToCredits()
        {
            while (BetIndex > 0 && Credits < CurrentBet) BetIndex--;
        }

        /// <summary>
        /// Takes the current bet from the balance and feeds the jackpot.
        /// Returns false (and changes nothing) if the player can't afford it.
        /// </summary>
        public bool TryPlaceBet(out int bet)
        {
            bet = CurrentBet;
            if (Credits < bet) return false;
            Credits -= bet;
            jackpotHundredths += bet * rules.JackpotContributionPercent;
            JackpotPool += jackpotHundredths / 100;
            jackpotHundredths %= 100;
            return true;
        }

        public void Deposit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Credits += amount;
        }

        /// <summary>Removes credits without placing a bet (cash-out; also used by QA tools).</summary>
        public void Withdraw(int amount)
        {
            if (amount < 0 || amount > Credits) throw new ArgumentOutOfRangeException(nameof(amount));
            Credits -= amount;
        }

        /// <summary>Pays out the whole jackpot pool and reseeds it.</summary>
        public int TakeJackpot()
        {
            int won = JackpotPool;
            JackpotPool = rules.JackpotSeed;
            return won;
        }

        /// <summary>Tops the wallet back up to the starting balance (free-play refill).</summary>
        public void Refill()
        {
            Credits = Math.Max(Credits, rules.StartingCredits);
            ClampBetToCredits();
        }
    }
}
