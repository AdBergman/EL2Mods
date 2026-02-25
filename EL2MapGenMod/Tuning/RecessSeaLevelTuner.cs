using System.Collections.Generic;
using Amplitude.Mercury.WorldGenerator.Generator.World;

namespace EL2MapGenMod.Tuning
{
    internal static class RecessSeaLevelTuner
    {
        public static void Apply(WorldGeneratorContext ctx)
        {
            if (ctx == null)
                return;

            List<int> seaLevels = ctx.RecessSeaLevels;
            if (seaLevels == null || seaLevels.Count == 0)
                return;

            // Enforce persistence + monotonic safety:
            // - Sea schedule should never increase between steps
            // - After recession step N, keep at least M levels underwater (min sea level)
            for (int i = 1; i < seaLevels.Count; i++)
            {
                if (i >= WorldGenTuningProfile.PersistentWaterClampFromRecessIndex &&
                    seaLevels[i] < WorldGenTuningProfile.PersistentWaterMinSeaLevel)
                {
                    seaLevels[i] = WorldGenTuningProfile.PersistentWaterMinSeaLevel;
                }

                if (seaLevels[i] > seaLevels[i - 1])
                    seaLevels[i] = seaLevels[i - 1];
            }

            // Keep generator output consistent with schedule.
            ctx.WorldSeaLevel = seaLevels[0];
        }
    }
}