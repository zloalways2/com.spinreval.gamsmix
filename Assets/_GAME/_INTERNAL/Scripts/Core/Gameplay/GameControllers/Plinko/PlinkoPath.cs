using Core.SO;
using System;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public readonly struct PlinkoPath
    {
        public readonly PlinkoHop[] Hops;
        public readonly int BucketIndex;
        public readonly int Seed;

        public PlinkoPath(PlinkoHop[] hops, int bucketIndex, int seed)
        {
            if (hops == null || hops.Length < 2)
                throw new ArgumentException("Path must contain at least drop and final hops", nameof(hops));

            Hops = hops;
            BucketIndex = bucketIndex;
            Seed = seed;
        }
    }
}