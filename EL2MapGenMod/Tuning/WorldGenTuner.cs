using System;
using Amplitude.Mercury.WorldGenerator;

namespace EL2MapGenMod.Tuning
{
    internal static class WorldGenTuner
    {
        public static void Apply(WorldGeneratorOptions o)
        {
            if (o == null)
                return;

            ApplyMapSize(o);
            ApplyElevationsAndRidges(o);
            ApplyRivers(o);
            ApplyLakes(o);
        }

        private static void ApplyMapSize(WorldGeneratorOptions o)
        {
            // Scale is used in CompleteContextSetup:
            // rows = WorldHeight * HeightScale / 100, cols = WorldWidth * WidthScale / 100
            double scaleFactor = WorldGenTuningProfile.MapScalePercent / 100.0;

            int newWidthScale = (int)Math.Round(o.WidthScale * scaleFactor);
            int newHeightScale = (int)Math.Round(o.HeightScale * scaleFactor);

            o.WidthScale = ClampUtil.ClampByte(newWidthScale);
            o.HeightScale = ClampUtil.ClampByte(newHeightScale);
        }

        private static void ApplyElevationsAndRidges(WorldGeneratorOptions o)
        {
            // Hard invariant: land must start at least at elevation target.
            // We clamp upward only (never force presets down).
            if (o.StartLandElevation < WorldGenTuningProfile.StartLandElevationTarget)
                o.StartLandElevation = WorldGenTuningProfile.StartLandElevationTarget;

            // Give headroom for taller peaks
            o.MaxLandElevation = ClampUtil.ClampSByte(o.MaxLandElevation + WorldGenTuningProfile.MaxLandElevationRaise);

            // Encourage steeper growth
            o.ElevationGrowPercent = ClampUtil.ClampByte(o.ElevationGrowPercent + WorldGenTuningProfile.ElevationGrowPercentBonus);

            // Keep movement reasonable while allowing stronger relief
            o.MaxCliffDeltaElevation = ClampUtil.ClampSByte(o.MaxCliffDeltaElevation + 1);
            o.MaxPassableDeltaElevation = ClampUtil.ClampSByte(o.MaxPassableDeltaElevation + 1);

            // More cliffs & ridges, but don’t erase passables
            o.CliffPrevalence = ClampUtil.ClampByte(o.CliffPrevalence + 15);
            o.PassablePrevalence = ClampUtil.ClampByte(o.PassablePrevalence + 5);

            // Ridges: more frequent and slightly easier to qualify for ridge candidacy
            if (o.RidgePresencePercent < (byte)WorldGenTuningProfile.RidgePresenceFloor)
                o.RidgePresencePercent = (byte)WorldGenTuningProfile.RidgePresenceFloor;

            o.RidgeMinElevation = ClampUtil.ClampByte(o.RidgeMinElevation + WorldGenTuningProfile.RidgeMinElevationDelta);

            // Slightly longer ridge chains (CreateElevations clamps huge clusters already)
            o.MaxRidgeSize = ClampUtil.ClampByte(o.MaxRidgeSize + WorldGenTuningProfile.RidgeMaxSizeBonus);
        }

        private static void ApplyRivers(WorldGeneratorOptions o)
        {
            if (o.RiverPresencePercent < (byte)WorldGenTuningProfile.RiverPresenceFloor)
                o.RiverPresencePercent = (byte)WorldGenTuningProfile.RiverPresenceFloor;

            // More candidate sources and allow slightly tighter spacing
            o.RiverSeedCount = ClampUtil.ClampByte(o.RiverSeedCount + WorldGenTuningProfile.RiverSeedCountBonus);
            o.RiverSourcesMinDistance = ClampUtil.ClampByte(o.RiverSourcesMinDistance + WorldGenTuningProfile.RiverSourcesMinDistanceDelta);

            // More rivers overall
            o.RiverMinCount = ClampUtil.ClampByte(o.RiverMinCount + WorldGenTuningProfile.RiverMinCountBonus);
            o.RiverMaxCount = ClampUtil.ClampByte(o.RiverMaxCount + WorldGenTuningProfile.RiverMaxCountBonus);

            // Slightly longer rivers
            o.RiverMaxLength = ClampUtil.ClampByte(o.RiverMaxLength + WorldGenTuningProfile.RiverMaxLengthBonus);

            // Keep the algorithm stable: ensure min <= max
            if (o.RiverMinCount > o.RiverMaxCount)
                o.RiverMinCount = o.RiverMaxCount;

            if (o.RiverMinLength > o.RiverMaxLength)
                o.RiverMinLength = o.RiverMaxLength;
        }

        private static void ApplyLakes(WorldGeneratorOptions o)
        {
            // -------------------------------------------------------------
            // 1) Increase probability that a valid lake cluster becomes a lake
            // -------------------------------------------------------------
            int boosted = o.LakePresencePercent + WorldGenTuningProfile.LakePresencePercentBonus;
            boosted = ClampUtil.ClampInt(boosted, 0, 60);
            o.LakePresencePercent = (byte)boosted;

            // -------------------------------------------------------------
            // 2) Relax lake area constraints so more clusters survive filtering
            // -------------------------------------------------------------
            int minLakeArea = o.MinLakeArea + WorldGenTuningProfile.MinLakeAreaDelta;
            int maxLakeArea = o.MaxLakeArea + WorldGenTuningProfile.MaxLakeAreaDelta;

            // Floors
            if (minLakeArea < WorldGenTuningProfile.MinLakeAreaFloor)
                minLakeArea = WorldGenTuningProfile.MinLakeAreaFloor;

            int maxFloor = WorldGenTuningProfile.MaxLakeAreaFloor;
            if (maxFloor < minLakeArea)
                maxFloor = minLakeArea;

            if (maxLakeArea < maxFloor)
                maxLakeArea = maxFloor;

            o.MinLakeArea = ClampUtil.ClampSByte(minLakeArea);
            o.MaxLakeArea = ClampUtil.ClampSByte(maxLakeArea);

            // -------------------------------------------------------------
            // 3) Optional: loosen cliff constraint slightly to allow more basin seeds
            // -------------------------------------------------------------
            if (WorldGenTuningProfile.MaxCliffDeltaElevationDelta != 0)
            {
                int newMaxCliffDelta = o.MaxCliffDeltaElevation + WorldGenTuningProfile.MaxCliffDeltaElevationDelta;
                // keep sane (avoid huge cliffs). 1..10 is a safe generic range.
                newMaxCliffDelta = ClampUtil.ClampInt(newMaxCliffDelta, 1, 10);
                o.MaxCliffDeltaElevation = ClampUtil.ClampSByte(newMaxCliffDelta);
            }
        }
    }
}