/* * * * *
 * XorShift64 implementation
 * ------------------------------
 * 
 * It provides a pseudo random number generator with a 64bit internal state and
 * a range of 2^64-1. It also provides some convenient methods for requesting a
 * value from a certain range.
 * 
 * In addition due to alignment issues when using modulo it also provides a
 * FairRange implementation to actually provide unique distribution
 * 
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016-2018 Markus Göbel (Bunny83)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * * * * */

namespace B83.Random.XorShift64
{
    public class RandomXor64
    {
        protected static ulong SEED_OFFSET = 13726359678912485784UL;
        protected static double DOUBLE_MUL = 5.42101086242752E-20;
        protected ulong state;

        public ulong seed
        {
            get { return state ^ SEED_OFFSET; }
            set
            {
                state = value ^ SEED_OFFSET;
                if (state == 0UL)
                    state = SEED_OFFSET;
            }
        }

        public RandomXor64()
        {
            state = (ulong)System.DateTime.Now.Ticks ^ SEED_OFFSET;
            Next();
            state ^= (ulong)System.Diagnostics.Stopwatch.GetTimestamp() << 7;
            Next();
            state ^= (ulong)System.Diagnostics.Stopwatch.GetTimestamp() << 11;
            if (state == 0UL)
                state = SEED_OFFSET;
        }
        public RandomXor64(ulong aSeed)
        {
            seed = aSeed;
        }

        public ulong Next()
        {
            // https://en.wikipedia.org/wiki/Xorshift#xorshift.2A
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            return state * 2685821657736338717UL; // 0x2545F4914F6CDD1DUL
        }
        public double NextDouble()
        {
            return (double)Next() * DOUBLE_MUL;

        }
        public ulong Range(ulong aMin, ulong aMax)
        {
            return aMin + Next() % (aMax - aMin);
        }
        public int Range(int aMin, int aMax)
        {
            return (int)((long)aMin + (uint)Next() % (uint)((long)aMax - (long)aMin));
        }
        public double Range(double aMin, double aMax)
        {
            return aMin + NextDouble() * (aMax - aMin);
        }
        // corrects bit alignment which might shift the probability slightly to the
        // lower numbers based on the choosen range.
        public ulong FairRange(ulong aRange)
        {
            ulong dif = ulong.MaxValue % aRange;
            // if aligned or range too big, just pick a number
            if (dif == 0 || ulong.MaxValue / (aRange / 4UL) < 2UL)
                return Next() % aRange;
            ulong v = Next();
            // avoid the last incomplete set
            while (ulong.MaxValue - v < dif)
                v = Next();
            return v % aRange;
        }
        public ulong FairRange(ulong aMin, ulong aMax)
        {
            return aMin + FairRange(aMax - aMin);
        }
    }
}
