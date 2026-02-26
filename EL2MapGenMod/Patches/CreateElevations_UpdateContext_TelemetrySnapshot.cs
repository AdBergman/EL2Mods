
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;
using EL2MapGenMod.Util;
using EL2MapGenMod.Tuning;

namespace EL2MapGenMod.Patches
{
    [HarmonyPatch(typeof(CreateElevations), "UpdateContext")]
    internal static class CreateElevations_UpdateContext_TelemetrySnapshot
    {
        private static readonly FieldInfo FinalVerifyPassField =
            AccessTools.Field(typeof(CreateElevations), "finalVerifyPass");

        private static void Postfix(CreateElevations __instance)
        {
            if (!WorldgenTelemetry.Enabled)
                return;

            // Only act on final verify pass
            if (FinalVerifyPassField != null)
            {
                object val = FinalVerifyPassField.GetValue(__instance);
                if (val is bool b && !b)
                    return;
            }

            WorldGeneratorContext ctx = __instance?.Context;
            if (ctx == null)
                return;

            var state = WorldgenTelemetry.GetOrCreate(ctx);
            if (state == null)
                return;

            // -------------------------
            // Rebuild RecessSeaLevels (Option B)
            // -------------------------
            List<int> before = null;
            if (ctx.RecessSeaLevels != null && ctx.RecessSeaLevels.Count > 0)
                before = new List<int>(ctx.RecessSeaLevels);

            var rebuilt = RecessSeaLevelsRebuilder.Rebuild(ctx);
            if (rebuilt != null && rebuilt.Count > 0)
            {
                // Replace in-place to ensure downstream tasks see the rebuilt values
                ctx.RecessSeaLevels.Clear();
                for (int i = 0; i < rebuilt.Count; i++)
                    ctx.RecessSeaLevels.Add(rebuilt[i]);
            }

            // -------------------------
            // Snapshot
            // -------------------------
            var snap = state.Elevations;
            snap.Stage = "CreateElevations.UpdateContext.finalVerifyPass";
            snap.Rows = ctx.Grid?.Rows ?? 0;
            snap.Columns = ctx.Grid?.Columns ?? 0;

            snap.SeaLevelsBefore = before;
            snap.SeaLevelsAfter = (ctx.RecessSeaLevels != null && ctx.RecessSeaLevels.Count > 0)
                ? new List<int>(ctx.RecessSeaLevels)
                : null;

            snap.ElevationHistogram.Clear();

            var all = ctx.AllDistrict;
            if (all == null || all.Length == 0)
                return;

            snap.LandCount = 0;
            snap.CoastalCount = 0;
            snap.OceanCount = 0;
            snap.LakeCount = 0;
            snap.RidgeCount = 0;

            for (int i = 0; i < all.Length; i++)
            {
                var d = all[i];
                if (d == null) continue;

                int e = d.Elevation;
                if (!snap.ElevationHistogram.TryGetValue(e, out int c)) c = 0;
                snap.ElevationHistogram[e] = c + 1;

                switch (d.Content)
                {
                    case District.Contents.Land: snap.LandCount++; break;
                    case District.Contents.Coastal: snap.CoastalCount++; break;
                    case District.Contents.Ocean: snap.OceanCount++; break;
                    case District.Contents.Lake: snap.LakeCount++; break;
                    case District.Contents.Ridge: snap.RidgeCount++; break;
                }
            }

            snap.ContextLakeSetsCount = ctx.Lakes?.Count ?? 0;
            snap.ContextLakeTilesCount = ctx.Lakes == null ? 0 : ctx.Lakes.Sum(s => s?.Count ?? 0);

            WorldgenTelemetry.WriteJsonOncePerRun(ctx, "MapGen_Elevations", snap, ref state.ElevationsSnapshotWritten);
        }
    }
}