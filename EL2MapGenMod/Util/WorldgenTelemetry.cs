// action: UPDATE
// namespace: EL2MapGenMod.Util
// class: WorldgenTelemetry

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using Newtonsoft.Json;

namespace EL2MapGenMod.Util
{
    internal static class WorldgenTelemetry
    {
        internal sealed class RunState
        {
            public bool ElevationsSnapshotWritten;
            public bool PoiSummaryWritten;

            public readonly ElevationSnapshot Elevations = new ElevationSnapshot();
            public readonly PoiTelemetry Poi = new PoiTelemetry();
        }

        private static readonly ConditionalWeakTable<object, RunState> StateByContext =
            new ConditionalWeakTable<object, RunState>();

        internal static RunState GetOrCreate(object ctx)
        {
            if (ctx == null) return null;
            return StateByContext.GetOrCreateValue(ctx);
        }

        public static bool Enabled => Tuning.WorldGenTuningProfile.EnableWorldgenTelemetry;

        public static string GetOutputDir()
        {
            string root = Paths.BepInExRootPath;
            string dir = Path.Combine(root, "WorldgenTelemetry", Tuning.WorldGenTuningProfile.TelemetryOutputFolder);
            Directory.CreateDirectory(dir);
            return dir;
        }

        internal static void WriteJsonOncePerRun(object ctx, string filePrefix, object payload, ref bool alreadyWrittenFlag)
        {
            if (!Enabled) return;
            if (ctx == null || payload == null) return;
            if (alreadyWrittenFlag) return;

            alreadyWrittenFlag = true;

            string dir = GetOutputDir();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string file = Path.Combine(dir, $"{filePrefix}_{stamp}.json");

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            File.WriteAllText(file, JsonConvert.SerializeObject(payload, settings));
        }

        // -------------------------
        // Data Models
        // -------------------------

        internal sealed class ElevationSnapshot
        {
            public string Stage;
            public int Rows;
            public int Columns;

            public Dictionary<int, int> ElevationHistogram = new Dictionary<int, int>();

            public int LandCount;
            public int CoastalCount;
            public int OceanCount;
            public int LakeCount;
            public int RidgeCount;

            public int ContextLakeSetsCount;
            public int ContextLakeTilesCount;

            // NEW: sea levels before/after rebuild
            public List<int> SeaLevelsBefore;
            public List<int> SeaLevelsAfter;
        }

        internal sealed class PoiTelemetry
        {
            public int TotalChecks;
            public int AllowedChecks;

            public int Reject_NullOrInvalid;
            public int Reject_TypeNotTracked;
            public int Reject_Content;
            public int Reject_BandElevation;
            public int Reject_AlreadyHasPoi;
            public int Reject_WonderNear;
            public int Reject_RiverConstraint;

            public int StrategicLuxuryChecks;
            public int StrategicLuxuryShifted;
        }
    }
}