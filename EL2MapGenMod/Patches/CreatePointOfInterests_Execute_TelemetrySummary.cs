
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using EL2MapGenMod.Util;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreatePointOfInterests), nameof(CreatePointOfInterests.Execute))]
    internal static class CreatePointOfInterests_Execute_TelemetrySummary
    {
        private static void Postfix(CreatePointOfInterests __instance)
        {
            if (!WorldgenTelemetry.Enabled)
                return;

            var ctx = __instance?.Context;
            if (ctx == null)
                return;

            var state = WorldgenTelemetry.GetOrCreate(ctx);
            if (state == null)
                return;

            WorldgenTelemetry.WriteJsonOncePerRun(ctx, "MapGen_PoiTelemetry", state.Poi, ref state.PoiSummaryWritten);
        }
    }
}