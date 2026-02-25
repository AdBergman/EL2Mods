﻿using System;
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
            // Hard invariant: land must start at least at elevation 6 (so initial sea becomes 5 by vanilla rule).
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
            // Avoid Math.Max here due to ambiguous overload / numeric conversion issues on older compilers.
            if (o.RidgePresencePercent < (byte)WorldGenTuningProfile.RidgePresenceFloor)
                o.RidgePresencePercent = (byte)WorldGenTuningProfile.RidgePresenceFloor;

            o.RidgeMinElevation = ClampUtil.ClampByte(o.RidgeMinElevation + WorldGenTuningProfile.RidgeMinElevationDelta);

            // Slightly longer ridge chains (CreateElevations clamps huge clusters already)
            o.MaxRidgeSize = ClampUtil.ClampByte(o.MaxRidgeSize + WorldGenTuningProfile.RidgeMaxSizeBonus);
        }

        private static void ApplyRivers(WorldGeneratorOptions o)
        {
            // Avoid Math.Max here due to ambiguous overload / numeric conversion issues on older compilers.
            if (o.RiverPresencePercent < (byte)WorldGenTuningProfile.RiverPresenceFloor)
                o.RiverPresencePercent = (byte)WorldGenTuningProfile.RiverPresenceFloor;

            // More candidate sources and allow slightly tighter spacing
            // (Important later if we experiment with branching: more sources + shorter min-distance -> more intersections/opportunities)
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
    }
}