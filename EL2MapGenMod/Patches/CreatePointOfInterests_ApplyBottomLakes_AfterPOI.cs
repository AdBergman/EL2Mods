using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreatePointOfInterests), nameof(CreatePointOfInterests.Execute))]
    internal static class CreatePointOfInterests_ApplyBottomLakes_AfterPOI
    {
        private static void Postfix(CreatePointOfInterests __instance)
        {
            var ctx = __instance.Context;
            if (ctx == null) return;

            BottomLayerLakeEnforcer.Apply(ctx);
        }
    }
}