namespace SlotMachine.Core
{
    /// <summary>
    /// Everything one spin decided. The outcome is fixed the moment the spin
    /// starts; the reels only animate towards these stop positions.
    /// </summary>
    public sealed class SpinResult
    {
        /// <summary>Stop position on each reel's strip (the symbol on the payline).</summary>
        public int[] StopIndices { get; }

        /// <summary>The payline symbol of each reel, left to right.</summary>
        public SymbolType[] Symbols { get; }

        public int Bet { get; }

        /// <summary>True when this spin was a free spin (no bet was taken).</summary>
        public bool IsFreeSpin { get; }

        public bool IsWin { get; }
        public bool IsJackpot { get; }

        /// <summary>Credits paid for this spin (0 on a loss).</summary>
        public int Payout { get; }

        /// <summary>Free spins this spin awarded (they are queued on the engine).</summary>
        public int FreeSpinsAwarded { get; }

        /// <summary>Free spins still waiting to be played after this one.</summary>
        public int FreeSpinsRemaining { get; }

        /// <summary>Balance after the bet was taken but before any payout; shown while the reels spin.</summary>
        public int CreditsAfterBet { get; }

        /// <summary>Balance once the payout has been added.</summary>
        public int CreditsAfterPayout { get; }

        public int JackpotPoolAfter { get; }

        /// <summary>The symbol that filled the line, when the spin won.</summary>
        public SymbolType WinningSymbol => Symbols[0];

        /// <summary>Payout as a multiple of the bet (0 on a loss).</summary>
        public int WinMultiple => Bet > 0 ? Payout / Bet : 0;

        public SpinResult(int[] stopIndices, SymbolType[] symbols, int bet, bool isFreeSpin, bool isWin,
            bool isJackpot, int payout, int freeSpinsAwarded, int freeSpinsRemaining,
            int creditsAfterBet, int creditsAfterPayout, int jackpotPoolAfter)
        {
            StopIndices = stopIndices;
            Symbols = symbols;
            Bet = bet;
            IsFreeSpin = isFreeSpin;
            IsWin = isWin;
            IsJackpot = isJackpot;
            Payout = payout;
            FreeSpinsAwarded = freeSpinsAwarded;
            FreeSpinsRemaining = freeSpinsRemaining;
            CreditsAfterBet = creditsAfterBet;
            CreditsAfterPayout = creditsAfterPayout;
            JackpotPoolAfter = jackpotPoolAfter;
        }
    }
}
