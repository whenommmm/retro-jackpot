using SlotMachine.Core;
using UnityEngine;

namespace SlotMachine.Gameplay
{
    /// <summary>
    /// Art and balance for one reel symbol. Designers tune how often it is
    /// printed and what it pays here, in the Inspector, without code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Slot Machine/Symbol", fileName = "Symbol")]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [SerializeField] private SymbolType type;
        [SerializeField] private string displayName = "Symbol";

        [Header("Art")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Motion-blurred version shown while the reel spins fast.")]
        [SerializeField] private Sprite blurredSprite;

        [Header("Balance")]
        [Tooltip("Copies printed on each 20-symbol reel strip. More copies = more common.")]
        [SerializeField, Min(1)] private int printedPerReel = 4;
        [Tooltip("Three of a kind pays this many times the bet.")]
        [SerializeField, Min(1)] private int multiplier = 10;
        [Tooltip("Free spins awarded for three of a kind (0 for none).")]
        [SerializeField, Min(0)] private int freeSpins;

        public SymbolType Type => type;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Sprite BlurredSprite => blurredSprite != null ? blurredSprite : sprite;
        public int PrintedPerReel => printedPerReel;
        public int Multiplier => multiplier;
        public int FreeSpins => freeSpins;
    }
}
