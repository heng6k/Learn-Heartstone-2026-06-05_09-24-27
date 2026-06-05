using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class SeededRng
    {
        private uint state;

        public SeededRng(int seed)
        {
            state = MixSeed((uint)seed);
        }

        public double Next()
        {
            state = unchecked(1664525u * state + 1013904223u);
            return state / 4294967296.0;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Random upper bound must be greater than zero.");
            }

            return (int)Math.Floor(Next() * maxExclusive);
        }

        public T Pick<T>(IList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
            }

            return items[NextInt(items.Count)];
        }

        private static uint MixSeed(uint seed)
        {
            var value = seed;
            value ^= value >> 16;
            value = unchecked(value * 0x7feb352du);
            value ^= value >> 15;
            value = unchecked(value * 0x846ca68bu);
            value ^= value >> 16;
            return value;
        }
    }
}
