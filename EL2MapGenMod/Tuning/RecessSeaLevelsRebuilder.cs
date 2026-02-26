// action: UPDATE
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
        /// Uses UNIQUE elevation levels to avoid quantile thresholds landing on spikes/gaps,
        /// producing tiny or degenerate (duplicate) bands.
        /// </summary>
        public static List<int> Rebuild(WorldGeneratorContext ctx)
        {
            if (ctx == null) return null;

            int bandCount = ctx.RecessSeaLevels != null ? ctx.RecessSeaLevels.Count : 0;
            if (bandCount <= 0)
                bandCount = 3; // fallback

            var districts = ctx.AllDistrict;
            if (districts == null || districts.Length == 0)
                return null;

            // Collect unique elevation levels actually present.
            var unique = new SortedSet<int>();
            for (int i = 0; i < districts.Length; i++)
            {
                var d = districts[i];
                if (d == null) continue;
                unique.Add(d.Elevation);
            }

            if (unique.Count == 0)
                return null;

            var levels = unique.ToList(); // ascending
            int maxElev = levels[levels.Count - 1];

            var thresholds = new List<int>(bandCount);

            // Map bands evenly across the unique levels from top -> bottom.
            // This makes each band correspond to at least one "real" elevation present in the map.
            if (bandCount == 1)
            {
                thresholds.Add(levels[levels.Count - 1]);
            }
            else
            {
                for (int k = 0; k < bandCount; k++)
                {
                    int topDown = (bandCount - 1) - k;
                    int idx = (int)Math.Floor(topDown * (levels.Count - 1) / (double)(bandCount - 1));
                    if (idx < 0) idx = 0;
                    if (idx > levels.Count - 1) idx = levels.Count - 1;

                    thresholds.Add(levels[idx]);
                }
            }

            // Enforce descending + strict decreasing (required by the HasPossiblePoi windowing logic).
            thresholds.Sort((a, b) => b.CompareTo(a)); // descending
            for (int i = 1; i < thresholds.Count; i++)
            {
                if (thresholds[i] >= thresholds[i - 1])
                    thresholds[i] = thresholds[i - 1] - 1;
            }

            // Clamp lowest band to recession floor.
            int floor = WorldGenTuningProfile.PersistentSeaLevelFloor;
            int last = thresholds.Count - 1;
            if (last >= 0 && thresholds[last] < floor)
                thresholds[last] = floor;

            // Re-enforce strictness after floor clamp.
            for (int i = last; i > 0; i--)
            {
                if (thresholds[i] >= thresholds[i - 1])
                    thresholds[i - 1] = thresholds[i] + 1;
            }

            // Final clamp + strictness
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