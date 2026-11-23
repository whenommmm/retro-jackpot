using System;

namespace SlotMachine.Core
{
    /// <summary>
    /// The only source of randomness the slot logic uses. Keeping it behind an
    /// interface lets tests drive exact outcomes and lets a real-money build swap
    /// in a certified or server-side generator without touching the game rules.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Returns a uniformly distributed integer in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }

    /// <summary>
    /// Default generator backed by System.Random. Seeded from a GUID so two
    /// sessions started in the same millisecond still diverge. Integer draws
    /// (not floats) keep weighted picks free of rounding bias.
    /// </summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource() : this(Guid.NewGuid().GetHashCode()) { }

        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Upper bound must be positive.");
            return random.Next(maxExclusive);
        }
    }
}
