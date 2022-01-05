﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vintagestory.API.MathTools
{

    public interface IRandom
    {
        int NextInt(int max);
        int NextInt();
        double NextDouble();
    }

    public class NormalRandom : Random, IRandom
    {
        public NormalRandom()
        {
        }

        public NormalRandom(int Seed) : base(Seed)
        {
        }

        public int NextInt(int max)
        {
            return this.Next(max);
        }

        public int NextInt()
        {
            return this.Next();
        }
    }

    /// <summary>
    /// An lcg random generator, particularly suitable for worldgen
    /// See also https://en.wikipedia.org/wiki/Linear_congruential_generator
    /// </summary>
    public class LCGRandom : IRandom
    {
        public long worldSeed;
        public long mapGenSeed;
        public long currentSeed;
        
        /// <summary>
        /// Initialize random with given seed
        /// </summary>
        /// <param name="worldSeed"></param>
        public LCGRandom(long worldSeed)
        {
            SetWorldSeed(worldSeed);
        }

        /// <summary>
        /// Initialize random with no seed. Use SetWorldSeed() to initialize
        /// </summary>
        public LCGRandom()
        {

        }

        /// <summary>
        /// Sets given seed
        /// </summary>
        /// <param name="worldSeed"></param>
        public void SetWorldSeed(long worldSeed)
        {
            this.worldSeed = worldSeed;

            currentSeed = mapGenSeed;
            currentSeed = currentSeed * 6364136223846793005L + 1442695040888963407L;

            mapGenSeed = worldSeed;
            mapGenSeed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
            mapGenSeed += 1;
            mapGenSeed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
            mapGenSeed += 2;
            mapGenSeed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
            mapGenSeed += 3;
        }

        /// <summary>
        /// Initializes a position dependent seed, if required
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="zPos"></param>
        public void InitPositionSeed(int xPos, int zPos)
        {
            currentSeed = mapGenSeed;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += xPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += zPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += xPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += zPos;
        }



        /// <summary>
        /// Initializes a position dependent seed, if required
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="zPos"></param>
        public void InitPositionSeed(int xPos, int yPos, int zPos)
        {
            currentSeed = mapGenSeed;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += xPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += yPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += zPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += xPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += yPos;
            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += zPos;
        }

        /// <summary>
        /// Returns a pseudo random number from 0 - max (excluding max)
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        public int NextInt(int max)
        {
            int r = (int)((currentSeed >> 24) % max);

            if (r < 0)
            {
                r += max;
            }

            currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
            currentSeed += mapGenSeed;

            return r;
        }


        public int NextInt()
        {
            return NextInt(int.MaxValue);
        }

        /// <summary>
        /// Returns a random number from 0 - 1
        /// </summary>
        /// <returns></returns>
        public float NextFloat()
        {
            return (float)NextInt(int.MaxValue) / int.MaxValue;
        }

        /// <summary>
        /// Returns a random number from 0 - 1
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            return (double)NextInt(int.MaxValue) / int.MaxValue;
        }
    }
}
