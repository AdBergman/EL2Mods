using System;
using System.Collections.Generic;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(SetMercuryData), nameof(SetMercuryData.Execute))]
    internal static class SetMercuryData_Execute
    {
        private static void Postfix(object context)
        {
            WorldGeneratorContext ctx = context as WorldGeneratorContext;
            if (ctx == null)
                return;

            List<int> seaLevels = ctx.RecessSeaLevels;
            if (seaLevels == null || seaLevels.Count == 0)
                return;

            // No compensation! We WANT sea to be StartLandElevation - 1.
            // Only enforce persistent water after 3rd recession step + monotonic safety.
            for (int i = 1; i < seaLevels.Count; i++)
            {
                if (i >= WorldGenTuningProfile.PersistentWaterClampFromRecessIndex)
                {
                    if (seaLevels[i] < WorldGenTuningProfile.PersistentWaterMinSeaLevel)
                        seaLevels[i] = WorldGenTuningProfile.PersistentWaterMinSeaLevel;
                }

                // Ensure monotonic non-increasing
                if (seaLevels[i] > seaLevels[i - 1])
                    seaLevels[i] = seaLevels[i - 1];
            }
        }
    }
}