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
            public bool ElevationsSnapshotWritten = false;

            public readonly ElevationSnapshot Elevations = new ElevationSnapshot();
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

        internal sealed class ElevationSnapshot
        {
            [JsonProperty] public string Stage = string.Empty;
            [JsonProperty] public int Rows = 0;
            [JsonProperty] public int Columns = 0;

            [JsonProperty] public SortedDictionary<int, int> ElevationHistogram = new SortedDictionary<int, int>();

            [JsonProperty] public int LandCount = 0;
            [JsonProperty] public int CoastalCount = 0;
            [JsonProperty] public int OceanCount = 0;
            [JsonProperty] public int LakeCount = 0;
            [JsonProperty] public int RidgeCount = 0;

            [JsonProperty] public double PermanentWaterPercentage = 0.0;
            [JsonProperty] public double TotalWaterPercentage = 0.0;

            [JsonProperty] public int ContextLakeSetsCount = 0;
            [JsonProperty] public int ContextLakeTilesCount = 0;

            [JsonProperty] public int BottomBandTileCount = 0;
            [JsonProperty] public int BottomBandWaterContentCount = 0;
            [JsonProperty] public int BottomBandLandLeakCount = 0;

            [JsonProperty] public List<int> SeaLevelsBefore = null;
            [JsonProperty] public List<int> SeaLevelsAfter = null;
        }
    }
}