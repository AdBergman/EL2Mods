// action: CREATE
// namespace: EL2MapGenMod.Tuning
// class: RecessSeaLevelsRebuilder

using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Mercury.WorldGenerator.Generator.World;

namespace EL2MapGenMod.Tuning
{
    internal static class RecessSeaLevelsRebuilder
    {
        /// <summary>
        /// Rebuild ctx.RecessSeaLevels based on the FINAL elevation distribution.
        /// Keeps the same band count (ctx.RecessSeaLevels.Count) when possible.
        /// </summary>
        public static List<int> Rebuild(WorldGeneratorContext ctx)
        {
            if (ctx == null) return null;

            // Determine desired band count from existing list to preserve vanilla semantics/order.
            int bandCount = ctx.RecessSeaLevels != null ? ctx.RecessSeaLevels.Count : 0;
            if (bandCount <= 0)
                bandCount = 3; // fallback (your stated "recedes 3 times")

            var districts = ctx.AllDistrict;
            if (districts == null || districts.Length == 0)
                return null;

            // Gather elevations (we use all districts; lakes/ridges still occupy space and matter for reveal bands).
            var elevs = new int[districts.Length];
            int n = 0;
            for (int i = 0; i < districts.Length; i++)
            {
                var d = districts[i];
                if (d == null) continue;
                elevs[n++] = d.Elevation;
            }

            if (n == 0)
                return null;

            Array.Resize(ref elevs, n);
            Array.Sort(elevs); // ascending

            int minElev = elevs[0];
            int maxElev = elevs[n - 1];

            // Quantile-based thresholds:
            // For bandCount = N, we produce N thresholds t0..t(N-1) descending.
            // Band 0 is (t0, max], Band 1 is (t1, t0], ..., Band N-1 is (tN-1, tN-2]
            //
            // We choose tk from quantile q = 1 - (k+1)/N.
            // Example N=3: q=0.666, 0.333, 0.0
            var thresholds = new List<int>(bandCount);

            for (int k = 0; k < bandCount; k++)
            {
                double q = 1.0 - (k + 1) / (double)bandCount;
                int idx = (int)Math.Floor(q * (n - 1));
                if (idx < 0) idx = 0;
                if (idx > n - 1) idx = n - 1;

                int t = elevs[idx];

                thresholds.Add(t);
            }

            // Ensure descending + strictly decreasing to form valid (lower, upper] windows.
            // Also clamp the lowest threshold to PersistentSeaLevelFloor (recession safety).
            int floor = WorldGenTuningProfile.PersistentSeaLevelFloor;

            for (int i = 0; i < thresholds.Count; i++)
            {
                // Clamp overall to observed min/max for sanity
                if (thresholds[i] < minElev) thresholds[i] = minElev;
                if (thresholds[i] > maxElev) thresholds[i] = maxElev;
            }

            // Enforce descending order.
            thresholds.Sort((a, b) => b.CompareTo(a)); // descending

            // Make strictly decreasing.
            for (int i = 1; i < thresholds.Count; i++)
            {
                if (thresholds[i] >= thresholds[i - 1])
                    thresholds[i] = thresholds[i - 1] - 1;
            }

            // Apply floor to the LAST (lowest) sea level.
            int last = thresholds.Count - 1;
            if (last >= 0 && thresholds[last] < floor)
                thresholds[last] = floor;

            // Re-assert strict decreasing after floor clamp (in case floor pushes it up)
            for (int i = last; i > 0; i--)
            {
                if (thresholds[i] >= thresholds[i - 1])
                    thresholds[i - 1] = thresholds[i] + 1;
            }

            // Final sanity: keep within [floor..maxElev], and strictly decreasing.
            for (int i = 0; i < thresholds.Count; i++)
            {
                if (thresholds[i] < floor) thresholds[i] = floor;
                if (thresholds[i] > maxElev) thresholds[i] = maxElev;
                if (i > 0 && thresholds[i] >= thresholds[i - 1])
                    thresholds[i] = thresholds[i - 1] - 1;
            }

            return thresholds;
        }
    }
}