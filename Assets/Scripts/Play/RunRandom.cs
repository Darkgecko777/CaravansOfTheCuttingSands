using System;
using System.Security.Cryptography;

namespace Caravans.Play
{
    public sealed class RunRandom : Random
    {
        const ulong Golden = 0x9E3779B97F4A7C15UL;

        long state;

        public RunRandom(long state)
            : base(1)
        {
            this.state = state;
        }

        public long State
        {
            get { return state; }
            set { state = value; }
        }

        public static int FreshSeed()
        {
            byte[] bytes = new byte[4];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            int seed = BitConverter.ToInt32(bytes, 0);
            if (seed == 0)
                seed = 1;
            return seed;
        }

        public static RunRandom ForStream(int seed, ulong salt)
        {
            ulong mixed = Mix((ulong)(uint)seed, salt);
            return new RunRandom(unchecked((long)mixed));
        }

        protected override double Sample()
        {
            return NextDouble();
        }

        public override int Next()
        {
            return (int)(NextULong() >> 33);
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException("maxValue");
            if (maxValue == 0)
                return 0;
            return (int)(NextDouble() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException("minValue");
            long range = (long)maxValue - minValue;
            if (range <= 0L)
                return minValue;
            return (int)((long)(NextDouble() * range) + minValue);
        }

        public override double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)NextULong();
        }

        static ulong Mix(ulong seed, ulong salt)
        {
            ulong z = seed ^ (salt + Golden);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            if (z == 0UL)
                z = salt | 1UL;
            return z;
        }

        ulong NextULong()
        {
            ulong z = (ulong)state + Golden;
            state = unchecked((long)z);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
