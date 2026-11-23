using System.Collections.Generic;
using System.Linq;
using SlotMachine.Core;
using UnityEngine;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// The machine's designer-facing settings. It turns Inspector values into
    /// the immutable <see cref="SlotRules"/> the core engine plays by.
    /// </summary>
    [CreateAssetMenu(menuName = "Slot Machine/Machine Config", fileName = "SlotMachineConfig")]
    public sealed class SlotMachineConfig : ScriptableObject
    {
        [SerializeField] private SymbolDefinition[] symbols;

        [Header("Economy")]
        [SerializeField] private int[] betOptions = { 10, 50, 100 };
        [SerializeField, Min(1)] private int startingCredits = 1000;

        [Header("Progressive jackpot")]
        [SerializeField] private SymbolType jackpotSymbol = SymbolType.Seven;
        [Tooltip("Pool value at start and after every win. Keep it at or above the jackpot symbol's multiplier x max bet.")]
        [SerializeField, Min(0)] private int jackpotSeed = 5000;
        [Tooltip("Percentage of every paid bet that grows the jackpot.")]
        [SerializeField, Range(0, 100)] private int jackpotContributionPercent = 5;

        [Header("Reels")]
        [SerializeField, Min(2)] private int reelCount = 3;
        [Tooltip("Fixes the order of symbols on the strips, so every session uses the same reels.")]
        [SerializeField] private int stripSeed = 7;

        [Header("Autoplay")]
        [Tooltip("Spin counts the player can pick, smallest first.")]
        [SerializeField] private int[] autoplaySpinOptions = { 10, 25, 50 };
        [Tooltip("Autoplay stops once the player is down this percentage of the balance they started it with.")]
        [SerializeField, Range(1, 100)] private int autoplayLossLimitPercent = 50;

        private Dictionary<SymbolType, SymbolDefinition> lookup;

        public IReadOnlyList<SymbolDefinition> Symbols => symbols;
        public SymbolType JackpotSymbol => jackpotSymbol;
        public IReadOnlyList<int> AutoplaySpinOptions => autoplaySpinOptions;
        public int AutoplayLossLimitPercent => autoplayLossLimitPercent;

        public SymbolDefinition GetSymbol(SymbolType type)
        {
            if (lookup == null || lookup.Count != symbols.Length)
                lookup = symbols.ToDictionary(s => s.Type);
            return lookup[type];
        }

        /// <summary>Builds the rules the engine uses from the current Inspector values.</summary>
        public SlotRules BuildRules()
        {
            var composition = symbols.ToDictionary(s => s.Type, s => s.PrintedPerReel);
            var paytable = new Paytable(symbols.Select(s => new PaytableEntry(s.Type, s.Multiplier, s.FreeSpins)));
            return new SlotRules(reelCount, composition, paytable, betOptions, startingCredits,
                jackpotSymbol, jackpotSeed, jackpotContributionPercent, stripSeed);
        }

        private void OnValidate() => lookup = null;
    }
}
