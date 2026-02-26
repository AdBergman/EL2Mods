using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using EL2MapGenMod.Tuning;
using EL2MapGenMod.Util;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "SproutRidges")]
    internal static class CreateElevations_SproutRidges
    {
        private static bool Prefix(CreateElevations __instance, int ridgePresencePercent)
        {
            WorldGeneratorContext ctx = WorldGenReflection.GetTaskContext(__instance) as WorldGeneratorContext;
            if (ctx == null)
                return true;

            ElevationBandRebalance.SproutRidgesBanded(ctx, ridgePresencePercent);
            return false;
        }
    }
}