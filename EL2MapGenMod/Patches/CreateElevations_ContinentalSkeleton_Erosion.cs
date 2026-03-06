using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "ContinentalSkeletonValues")]
    internal static class CreateElevations_ContinentalSkeleton_Erosion
    {
        static void Postfix(CreateElevations __instance)
        {
            var ctx = __instance.Context;
            if (ctx == null) return;

            // Strength 5 for smaller/crowded grids to beat the bridge builder, 3 for Huge grids
            int erosionStrength = (ctx.Grid != null && ctx.Grid.Rows < 75) ? 5 : 3; 

            foreach (var district in ctx.AllDistrict)
            {
                int originalValue = district.SkeletonValue;
                district.SkeletonValue = System.Math.Max(0, originalValue - erosionStrength);
            }
        }
    }
}