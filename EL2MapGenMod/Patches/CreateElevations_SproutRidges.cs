using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using EL2MapGenMod.Tuning;

// Removed the using EL2MapGenMod.Util;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "SproutRidges")]
    internal static class CreateElevations_SproutRidges
    {
        private static bool Prefix(CreateElevations __instance, int ridgePresencePercent)
        {
            // FIX: Just access the Context property directly! No reflection needed.
            WorldGeneratorContext ctx = __instance?.Context;
            
            if (ctx == null)
                return true;

            ElevationBandRebalance.SproutRidgesBanded(ctx, ridgePresencePercent);
            return false;
        }
    }
}