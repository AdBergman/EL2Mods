using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Amplitude.Mercury.WorldGenerator.Generator.Tasks.Generator;
using Amplitude.Mercury.WorldGenerator.Generator.World;
using Amplitude.Mercury.WorldGenerator.Generator.World.Info;
using EL2MapGenMod.Tuning;
using EL2MapGenMod.Util;

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

            var snap = state.Elevations;
            snap.Stage = "CreateElevations.UpdateContext.finalVerifyPass";
            snap.Rows = ctx.Grid?.Rows ?? 0;
            snap.Columns = ctx.Grid?.Columns ?? 0;

            snap.SeaLevelsBefore = null;
            snap.SeaLevelsAfter = (ctx.RecessSeaLevels != null && ctx.RecessSeaLevels.Count > 0)
                ? new List<int>(ctx.RecessSeaLevels)
                : null;

            snap.ElevationHistogram.Clear();

            District[] all = ctx.AllDistrict;
            if (all == null || all.Length == 0)
                return;

            snap.LandCount = 0;
            snap.CoastalCount = 0;
            snap.OceanCount = 0;
            snap.LakeCount = 0;
            snap.RidgeCount = 0;

            snap.BottomBandTileCount = 0;
            snap.BottomBandWaterContentCount = 0;
            snap.BottomBandLandLeakCount = 0;

            for (int i = 0; i < all.Length; i++)
            {
                District d = all[i];
                if (d == null)
                    continue;

                int e = d.Elevation;
                if (!snap.ElevationHistogram.TryGetValue(e, out int c))
                    c = 0;
                snap.ElevationHistogram[e] = c + 1;

                switch (d.Content)
                {
                    case District.Contents.Land: snap.LandCount++; break;
                    case District.Contents.Coastal: snap.CoastalCount++; break;
                    case District.Contents.Ocean: snap.OceanCount++; break;
                    case District.Contents.Lake: snap.LakeCount++; break;
                    case District.Contents.Ridge: snap.RidgeCount++; break;
                }

                if (d.Elevation <= WorldGenTuningProfile.BottomWaterMaxElevation)
                {
                    snap.BottomBandTileCount++;

                    bool isWater =
                        d.Content == District.Contents.Lake ||
                        d.Content == District.Contents.Coastal ||
                        d.Content == District.Contents.Ocean;

                    if (isWater)
                        snap.BottomBandWaterContentCount++;
                    else
                        snap.BottomBandLandLeakCount++;
                }
            }

            snap.ContextLakeSetsCount = ctx.Lakes?.Count ?? 0;
            snap.ContextLakeTilesCount = ctx.Lakes == null ? 0 : ctx.Lakes.Sum(s => s?.Count ?? 0);

            if (snap.BottomBandTileCount > 0)
            {
                snap.PermanentWaterPercentage =
                    (100.0 * snap.BottomBandWaterContentCount) / snap.BottomBandTileCount;
            }
            else
            {
                snap.PermanentWaterPercentage = 0.0;
            }
            
            int totalMapTiles = snap.LandCount + snap.CoastalCount + snap.OceanCount + snap.LakeCount + snap.RidgeCount;
            if (totalMapTiles > 0)
            {
                snap.TotalWaterPercentage = (100.0 * (snap.CoastalCount + snap.OceanCount + snap.LakeCount)) / totalMapTiles;
            }
            else
            {
                snap.TotalWaterPercentage = 0.0;
            }

            WorldgenTelemetry.WriteJsonOncePerRun(
                ctx,
                "MapGen_Elevations",
                snap,
                ref state.ElevationsSnapshotWritten);
        }
    }
}