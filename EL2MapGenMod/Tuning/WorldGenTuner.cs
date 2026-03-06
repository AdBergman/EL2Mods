using System;
using Amplitude.Mercury.WorldGenerator;

namespace EL2MapGenMod.Tuning
{
    internal static class WorldGenTuner
    {
        public static void Apply(WorldGeneratorOptions o)
        {
            if (o == null) return;

            ApplyMapSize(o);
            ApplyElevationsAndRidges(o);
            ApplyRivers(o);
            ApplyLakes(o);
        }

        private static void ApplyMapSize(WorldGeneratorOptions o)
        {
            double scaleFactor = WorldGenTuningProfile.MapScalePercent / 100.0;
            o.WidthScale = ClampUtil.ClampByte((int)Math.Round(o.WidthScale * scaleFactor));
            o.HeightScale = ClampUtil.ClampByte((int)Math.Round(o.HeightScale * scaleFactor));
        }

        private static void ApplyElevationsAndRidges(WorldGeneratorOptions o)
        {
            // By setting this to 10, the engine's internal logic 
            // will treat 9 and below as the Sea Level.
            o.StartLandElevation = WorldGenTuningProfile.StartLandElevationTarget;

            // FIX: Remove the 'WorldSeaLevel' line that caused the error.
            
            // INCREASE WATER RATIO:
            // We lower the continent area to 50%. This forces the engine 
            // to leave 50% of the map as background 'Ocean' districts.
            o.ContinentUsedAreaPercent = 50; 
            o.ContinentAvoidedAreaPercent = 30;

            // SQUISH LOGIC:
            o.MaxPassableDeltaElevation = 1; 
            o.MaxCliffDeltaElevation = 3;
            o.ElevationGrowPercent = 90; 
            o.CliffPrevalence = 85;

            o.MaxLandElevation = (sbyte)(o.MaxLandElevation + WorldGenTuningProfile.MaxLandElevationRaise);

            // Ridges
            o.RidgePresencePercent = (byte)WorldGenTuningProfile.RidgePresenceFloor;
            o.RidgeMinElevation = (byte)(o.RidgeMinElevation + WorldGenTuningProfile.RidgeMinElevationDelta);
            o.MaxRidgeSize = (byte)(o.MaxRidgeSize + WorldGenTuningProfile.RidgeMaxSizeBonus);
        }

        private static void ApplyRivers(WorldGeneratorOptions o)
        {
            o.RiverPresencePercent = (byte)WorldGenTuningProfile.RiverPresenceFloor;
            o.RiverSeedCount = ClampUtil.ClampByte(o.RiverSeedCount + WorldGenTuningProfile.RiverSeedCountBonus);
            o.RiverMinCount = ClampUtil.ClampByte(o.RiverMinCount + WorldGenTuningProfile.RiverMinCountBonus);
            o.RiverMaxCount = ClampUtil.ClampByte(o.RiverMaxCount + WorldGenTuningProfile.RiverMaxCountBonus);
            o.RiverMaxLength = ClampUtil.ClampByte(o.RiverMaxLength + WorldGenTuningProfile.RiverMaxLengthBonus);
        }

        private static void ApplyLakes(WorldGeneratorOptions o)
        {
            o.LakePresencePercent = (byte)ClampUtil.ClampInt(o.LakePresencePercent + WorldGenTuningProfile.LakePresencePercentBonus, 0, 60);
            int minLakeArea = Math.Max(o.MinLakeArea + WorldGenTuningProfile.MinLakeAreaDelta, WorldGenTuningProfile.MinLakeAreaFloor);
            o.MinLakeArea = ClampUtil.ClampSByte(minLakeArea);
            o.MaxLakeArea = ClampUtil.ClampSByte(Math.Max(o.MaxLakeArea + WorldGenTuningProfile.MaxLakeAreaDelta, minLakeArea));
        }
    }
}